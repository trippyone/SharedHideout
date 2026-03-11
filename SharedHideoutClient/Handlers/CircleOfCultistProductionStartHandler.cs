using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class CircleOfCultistProductionStartHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutCircleOfCultistProductionStart;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutCircleOfCultistProductionStartRequestData>(json);
            _controller.CircleOfCultistProductionStart(request.CraftTime);
            return Task.CompletedTask;
        }
    }
}
