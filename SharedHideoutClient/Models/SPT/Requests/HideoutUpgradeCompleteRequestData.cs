using EFT;
using Newtonsoft.Json;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutUpgradeCompleteRequestData
    {
        [JsonProperty("areaType")]
        public EAreaType AreaType { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }
}
