using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class ScavCaseProductionStartHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutScavCaseProductionStart;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutScavCaseProductionStartRequestData>(json);
            _controller.ProductionStart(request.RecipeId);
            return Task.CompletedTask;
        }
    }
}
