using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class ContinuousProductionStartHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutContinuousProductionStart;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutContinuousProductionStartRequestData>(json);
            _controller.ContinuousProductionStart(request.RecipeId);
            return Task.CompletedTask;
        }
    }
}
