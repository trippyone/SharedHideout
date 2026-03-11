using Newtonsoft.Json;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutSingleProductionStartRequestData
    {
        [JsonProperty("recipeId")]
        public string RecipeId { get; set; }
    }
}
