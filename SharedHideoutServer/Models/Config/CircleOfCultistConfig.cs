using System.Text.Json.Serialization;

namespace SharedHideoutServer.Models.Config;

public class CircleOfCultistConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("rewards")]
    public bool Rewards { get; set; } = true;

    [JsonPropertyName("sameRewards")]
    public bool SameRewards { get; set; } = true;
}
