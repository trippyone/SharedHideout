using Newtonsoft.Json;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutCircleOfCultistProductionStartRequestData
    {
        [JsonProperty("craftTime")]
        public int CraftTime { get; set; }
    }
}
