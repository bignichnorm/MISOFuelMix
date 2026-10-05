using System.Text.Json.Serialization;

namespace MISOQueryingApp.DTOs;

public class MISOFuelMixResponse
{
    [JsonPropertyName("RefId")]
    public string? ReferenceId { get; set; }

    [JsonPropertyName("TotalMW")]
    public string? TotalMegaWatts { get; set; }

    [JsonPropertyName("Fuel")]
    public MISOFuelList? Fuel { get; set; }
}
