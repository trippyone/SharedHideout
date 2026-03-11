using EFT;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutTakeItemOutRequestData
    {
        [JsonProperty("areaType")]
        public EAreaType AreaType { get; set; }

        [JsonProperty("slots")]
        public List<int> Slots { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }
}
