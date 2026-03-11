using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class UpgradeAreaHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutUpgrade;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutUpgradeRequestData>(json);
            _controller.UpgradeArea(request.AreaType);
            return Task.CompletedTask;
        }
    }
}
