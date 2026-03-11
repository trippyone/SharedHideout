using BepInEx;
using Diz.Utils;
using SharedHideoutClient.Controllers;
using SharedHideoutClient.Handlers;
using SharedHideoutClient.Websocket;
using System;

// Reference: 

namespace SharedHideoutClient
{
    [BepInPlugin("com.trippy.SharedHideoutClient", "SharedHideoutClient", VERSION)]
    public class SharedHideoutClientPlugin : BaseUnityPlugin
    {
        public const string VERSION = "0.0.1";
        public static SharedHideoutClientPlugin Instance { get; set; }

        private void Awake()
        {
            try
            {
                Instance = this;
                LogHelper.Logger = this.Logger;

                HideoutActionController controller = new();
                HideoutActionProcessor processor = new();

                processor.RegisterHandler(new UpgradeAreaHandler(controller));
                processor.RegisterHandler(new CompleteUpgradeAreaHandler(controller));

                processor.RegisterHandler(new SingleProductionStartHandler(controller));
                processor.RegisterHandler(new ContinuousProductionStartHandler(controller));
                processor.RegisterHandler(new ScavCaseProductionStartHandler(controller));
                processor.RegisterHandler(new CircleOfCultistProductionStartHandler(controller));
                processor.RegisterHandler(new TakeProductionHandler(controller));
                processor.RegisterHandler(new CancelProductionHandler(controller));

                processor.RegisterHandler(new ToggleAreaHandler(controller));

                processor.RegisterHandler(new PutItemsInAreaSlotsHandler(controller));
                processor.RegisterHandler(new TakeItemsFromAreaSlotsHandler(controller));

                SharedHideoutClientWebsocket websocket = new();

                websocket.OnMessageReceived += (json) =>
                {
                    AsyncWorker.RunInMainTread(async () => await processor.ProcessRequest(json));
                };

                websocket.Connect();

                Logger.LogInfo("SharedHideoutClient loaded.");
            }
            catch (Exception exception)
            {
                LogHelper.Logger.LogInfo(exception);
            }
        }
    }
}