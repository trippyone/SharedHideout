using SharedHideoutServer.Models.Config;
using System.Text.Json.Serialization;

namespace SharedHideout.Models.Config;

public class SharedHideoutServerConfig
{
    [JsonPropertyName("sync")]
    public SyncConfig Sync { get; set; } = new();
}
