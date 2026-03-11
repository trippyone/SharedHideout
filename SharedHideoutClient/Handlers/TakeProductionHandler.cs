using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class TakeProductionHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutTakeProduction;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutTakeProductionRequestData>(json);
            _controller.TakeProduction(request.RecipeId, request.SyncRewards);
            return Task.CompletedTask;
        }
    }
}
