using Newtonsoft.Json;

namespace SharedHideoutClient.Models.SPT.Requests
{
    public class ActionObject
    {
        [JsonProperty("Action")]
        public string Action { get; set; }
    }
}