using System.Text.Json.Serialization;

namespace SharedHideoutServer.Models.Config;

public class AreasStateConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("rewards")]
    public bool Rewards { get; set; } = true;
}
