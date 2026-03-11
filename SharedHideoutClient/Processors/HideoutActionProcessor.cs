using Newtonsoft.Json;
using SharedHideoutClient.Enums;
using SharedHideoutClient.Models.Interfaces;
using SharedHideoutClient.Models.SPT.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharedHideoutClient
{
    public class HideoutActionProcessor
    {
        private readonly Dictionary<EHideoutActionType, IHideoutActionHandler> _handlers = [];

        public void RegisterHandler(IHideoutActionHandler handler)
        {
            if (!_handlers.ContainsKey(handler.ActionType))
            {
                _handlers.Add(handler.ActionType, handler);
            }
        }

        public async Task ProcessRequest(string json)
        {
            var request = JsonConvert.DeserializeObject<ActionObject>(json);

            LogHelper.Logger.LogInfo($"Received Action: {request.Action}");
            LogHelper.Logger.LogInfo($"{json}");

            if (Enum.TryParse<EHideoutActionType>(request.Action, out var action))
            {
                if (_handlers.TryGetValue(action, out var handler))
                {
                    await handler.HandleRequest(json);
                }
                else
                {
                    LogHelper.Logger.LogWarning($"[Processor] No handler for: {request.Action}");
                }
            }
            else
            {
                LogHelper.Logger.LogWarning($"Unknown action: {request.Action}");
            }
        }
    }
}
