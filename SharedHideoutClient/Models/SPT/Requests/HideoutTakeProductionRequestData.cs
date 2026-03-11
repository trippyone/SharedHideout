using Newtonsoft.Json;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutTakeProductionRequestData
    {
        [JsonProperty("recipeId")]
        public string RecipeId { get; set; }

        [JsonProperty("syncRewards")]
        public bool SyncRewards { get; set; }
    }
}
