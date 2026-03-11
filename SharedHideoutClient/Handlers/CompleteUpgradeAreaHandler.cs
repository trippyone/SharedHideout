using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class CompleteUpgradeAreaHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutUpgradeComplete;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutUpgradeCompleteRequestData>(json);
            _controller.CompleteUpgradeArea(request.AreaType);
            return Task.CompletedTask;
        }
    }
}
