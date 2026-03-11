using Newtonsoft.Json;

namespace SharedHideoutClient.Models.Requests
{
    public class HideoutScavCaseProductionStartRequestData
    {
        [JsonProperty("recipeId")]
        public string RecipeId { get; set; }
    }
}
