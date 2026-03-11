using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class TakeItemsFromAreaSlotsHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutTakeItemsFromAreaSlots;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutTakeItemOutRequestData>(json);
            _controller.TakeItemsFromAreaSlots(request.AreaType, request.Slots);
            return Task.CompletedTask;
        }
    }
}
