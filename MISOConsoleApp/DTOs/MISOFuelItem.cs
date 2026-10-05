using System.Text.Json.Serialization;

namespace MISOQueryingApp.DTOs;

public class MISOFuelItem
{
    [JsonPropertyName("CATEGORY")]
    public string? Category { get; set; }

    [JsonPropertyName("ACT")]
    public string? MegaWatts { get; set; }
}
