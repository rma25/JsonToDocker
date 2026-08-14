using System.Text.Json;
using JsonToDocker.models;

Console.WriteLine("Paste a JSON array or enter a file path (.json/.txt), then press Enter on a blank line:");

// Collect lines until the user submits a blank line (or EOF).
var lines = new List<string>();
while (Console.ReadLine() is { Length: > 0 } line)
{
    lines.Add(line);
}

var input = string.Join(Environment.NewLine, lines).Trim();

if (string.IsNullOrWhiteSpace(input))
{
    Console.Error.WriteLine("No input received. Please paste a JSON array or provide a file path.");
    return;
}

var isJson = input.StartsWith('[');
var isSingleLine = lines.Count == 1;

// A multi-line input that doesn't start with '[' most likely means the user
// accidentally combined a file path with a JSON string in the same paste.
if (!isJson && !isSingleLine)
{
    Console.Error.WriteLine(
        "It looks like you provided both a file path and a JSON string at the same time.\n" +
        "Please provide only one:\n" +
        "  • A JSON array (paste text starting with '[')\n" +
        "  • A single file path to a .json or .txt file");
    return;
}

// Resolve the raw JSON string — either from the pasted text or from a file.
string json;

if (isJson)
{
    json = input;
}
else
{
    // Single line that doesn't start with '[' — treat it as a file path.
    if (!File.Exists(input))
    {
        Console.Error.WriteLine(
            $"File not found: '{input}'\n" +
            "Please double-check the path and try again, or paste your JSON directly.");
        return;
    }

    var extension = Path.GetExtension(input);
    if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase) &&
        !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"Unsupported file type '{extension}'.\n" +
            "Only .json and .txt files are accepted.");
        return;
    }

    json = await File.ReadAllTextAsync(input);

    if (!json.TrimStart().StartsWith('['))
    {
        Console.Error.WriteLine(
            "The file does not appear to contain a valid JSON array.\n" +
            "Expected the content to start with '['.");
        return;
    }
}

// Deserialize and validate the JSON.
List<AppSetting> settings;

try
{
    settings = JsonSerializer.Deserialize<List<AppSetting>>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new JsonException("JSON deserialize to null.");
}
catch (JsonException ex)
{
    Console.Error.WriteLine(
        $"Invalid JSON: {ex.Message}\n\n" +
        "Expected a JSON array whose objects each have 'name', 'value', and 'slotSetting' fields.\n" +
        "Example:\n" +
        """  [{ "name": "MY_KEY", "value": "my_value", "slotSetting": false }]""");
    return;
}

// Entries with slotSetting: true are slot-specific and should not go into a Docker image.
var eligible = settings.Where(s => !s.SlotSetting).ToList();

if (eligible.Count == 0)
{
    Console.Error.WriteLine(
        "Nothing to output.\n" +
        "Every entry either has 'slotSetting: true' or the list is empty.");
    return;
}

// Emit the Docker Compose environment block.
Console.WriteLine("    environment:");
foreach (var s in eligible)
{
    Console.WriteLine($"      - {s.Name}={s.Value}");   
}


Console.WriteLine("Press any key to close...");
Console.ReadKey();