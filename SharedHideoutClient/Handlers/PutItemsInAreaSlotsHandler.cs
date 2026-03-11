using Newtonsoft.Json;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.Requests;
using System.Threading.Tasks;

namespace SharedHideoutClient.Handlers
{
    public class PutItemsInAreaSlotsHandler(HideoutActionController controller) : IHideoutActionHandler
    {
        private readonly HideoutActionController _controller = controller;
        public EHideoutActionType ActionType => EHideoutActionType.HideoutPutItemsInAreaSlots;

        public Task HandleRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<HideoutPutItemInRequestData>(json);
            _controller.PutItemsInAreaSlots(request.AreaType, request.Items);
            return Task.CompletedTask;
        }
    }
}
