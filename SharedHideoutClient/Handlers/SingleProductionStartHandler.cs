using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class SingleProductionStartHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutSingleProductionStart;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutSingleProductionStartRequestData>(json);
            _controller.ProductionStart(request.RecipeId);
            return Task.CompletedTask;
        }
    }
}
