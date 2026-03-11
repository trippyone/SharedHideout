using Newtonsoft.Json;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutCancelProductionRequestData
    {
        [JsonProperty("recipeId")]
        public string RecipeId { get; set; }
    }
}
