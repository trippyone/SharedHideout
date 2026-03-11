using EFT;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutPutItemInRequestData
    {
        [JsonProperty("areaType")]
        public EAreaType AreaType { get; set; }

        [JsonProperty("items")]
        public Dictionary<string, HideoutSlotItem> Items { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public class HideoutSlotItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("tpl")]
        public string Tpl { get; set; }
    }
}
