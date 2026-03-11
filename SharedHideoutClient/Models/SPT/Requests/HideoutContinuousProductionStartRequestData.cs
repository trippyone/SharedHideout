using Newtonsoft.Json;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutContinuousProductionStartRequestData
    {
        [JsonProperty("recipeId")]
        public string RecipeId { get; set; }
    }
}
