using System.Text.Json.Serialization;

namespace JsonToDocker.models;

// Represents one entry in the Azure App Service app settings export format.
public record AppSetting(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("slotSetting")]
    bool SlotSetting
);