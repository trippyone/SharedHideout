using System.Text.Json.Serialization;

namespace SharedHideoutServer.Models.Config;

public class SyncConfig
{
    [JsonPropertyName("areaUpgrade")]
    public bool AreaUpgrade { get; set; } = true;

    [JsonPropertyName("areaState")]
    public AreasStateConfig AreaState { get; set; } = new();

    [JsonPropertyName("circleOfCultist")]
    public CircleOfCultistConfig CircleOfCultist { get; set; } = new();
}