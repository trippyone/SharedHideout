using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class ToggleAreaHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutToggleArea;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutToggleAreaRequestData>(json);
            _controller.ToggleArea(request.AreaType, request.Enabled);
            return Task.CompletedTask;
        }
    }
}
