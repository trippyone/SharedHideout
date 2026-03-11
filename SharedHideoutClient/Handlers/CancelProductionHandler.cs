using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class CancelProductionHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutCancelProduction;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutCancelProductionRequestData>(json);
            _controller.CancelProduction(request.RecipeId);
            return Task.CompletedTask;
        }
    }
}
