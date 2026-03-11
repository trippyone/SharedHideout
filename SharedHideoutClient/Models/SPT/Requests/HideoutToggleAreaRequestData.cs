using EFT;
using Newtonsoft.Json;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutToggleAreaRequestData
    {
        [JsonProperty("areaType")]
        public EAreaType AreaType { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }
}
