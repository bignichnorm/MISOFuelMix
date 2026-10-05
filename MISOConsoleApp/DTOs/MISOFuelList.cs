using System.Text.Json.Serialization;

namespace MISOQueryingApp.DTOs;

public class MISOFuelList
{
    [JsonPropertyName("Type")]
    public List<MISOFuelItem>? Type { get; set; }
}
