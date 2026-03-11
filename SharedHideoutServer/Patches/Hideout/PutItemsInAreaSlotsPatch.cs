using SharedHideoutServer;
using SharedHideoutServer.Services;
using SharedHideoutServer.Websocket;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;
using System.Text.Json.Nodes;

//public ItemEventRouterResponse PutItemsInAreaSlots(

public class PutItemsInAreaSlotsPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.PutItemsInAreaSlots));
    }

    [PatchPrefix]
    public static bool Prefix(ref ItemEventRouterResponse __result, PmcData pmcData, HideoutPutItemInRequestData addItemToHideoutRequest, MongoId sessionID)
    {
        if (ServiceLocator.ServiceProvider.GetService<EventOutputHolder>() is EventOutputHolder eventOutputHolder
            && ServiceLocator.ServiceProvider.GetService<PaymentHelper>() is PaymentHelper paymentHelper
            && ServiceLocator.ServiceProvider.GetService<InventoryHelper>() is InventoryHelper inventoryHelper
            && ServiceLocator.ServiceProvider.GetService<DatabaseService>() is DatabaseService databaseService
            && ServiceLocator.ServiceProvider.GetService<TimeUtil>() is TimeUtil timeUtil
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<SharedHideoutServerWebSocket>() is SharedHideoutServerWebSocket websocket
            && ServiceLocator.ServiceProvider.GetService<JsonUtil>() is JsonUtil jsonUtil
            && ServiceLocator.ServiceProvider.GetService<HideoutHelper>() is HideoutHelper hideoutHelper
            && ServiceLocator.ServiceProvider.GetService<ICloner>() is ICloner cloner
            && ServiceLocator.ServiceProvider.GetService<ISptLogger<HideoutController>>() is ISptLogger<HideoutController> logger
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService
            && ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>() is HttpResponseUtil httpResponseUtil
            && ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>() is ServerLocalisationService serverLocalisationService)
        {
            var config = configService.Config;
            var sync = config.Sync.AreaState.Enabled;

            var requesterOutput = eventOutputHolder.GetOutput(sessionID);

            // Find item in player inventory we want to move
            var itemsToAdd = addItemToHideoutRequest.Items.Select(kvp =>
            {
                var item = pmcData.Inventory.Items.FirstOrDefault(invItem => invItem.Id == kvp.Value.Id);
                return new
                {
                    inventoryItem = item,
                    requestedItem = kvp.Value,
                    slot = kvp.Key,
                };
            });

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionID;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = saveServer.GetProfiles()[profileId].CharacterData.PmcData;

                // Find area we want to put item into
                var hideoutArea = profilePmcData.Hideout.Areas.FirstOrDefault(area => area.Type == addItemToHideoutRequest.AreaType);
                if (hideoutArea is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area_in_database", addItemToHideoutRequest.AreaType));

                    __result = httpResponseUtil.AppendErrorToOutput(requesterOutput);
                    return false;
                }

                foreach (var item in itemsToAdd)
                {
                    if (item.inventoryItem is null)
                    {

                        logger.Error(
                            "unable to find item in inventory"
                        );
                        
                        __result = httpResponseUtil.AppendErrorToOutput(requesterOutput);
                        return false;
                        
                    }

                    // Add item to area.slots
                    var destinationLocationIndex = int.Parse(item.slot);
                    var hideoutSlotIndex = hideoutArea.Slots.FindIndex(slot => slot.LocationIndex == destinationLocationIndex);
                    if (hideoutSlotIndex == -1)
                    {
                        logger.Error(
                            $"Unable to put item: {item.requestedItem.Id} into slot as slot cannot be found for area: {addItemToHideoutRequest.AreaType}, skipping"
                        );
                        continue;
                    }

                    hideoutArea.Slots[hideoutSlotIndex].Items =
                    [
                        new HideoutItem
                        {
                            Id = item.inventoryItem.Id,
                            Template = item.inventoryItem.Template,
                            Upd = item.inventoryItem.Upd,
                        },
                    ];
                }

                // Trigger a forced update
                hideoutHelper.UpdatePlayerHideout(profileId);
            }

            var json = JsonNode.Parse(jsonUtil.Serialize(addItemToHideoutRequest));

            foreach (var item in addItemToHideoutRequest.Items)
            {
                var inventoryItem = pmcData.Inventory.Items
                    .FirstOrDefault(i => i.Id == item.Value.Id);

                if (inventoryItem != null)
                    json["items"][item.Key]["tpl"] = inventoryItem.Template.ToString();
            }

            foreach (var item in itemsToAdd)
            {
                inventoryHelper.RemoveItem(pmcData, item.inventoryItem.Id, sessionID, requesterOutput);
            }

            hideoutHelper.UpdatePlayerHideout(sessionID);

            if (sync)
            {
                websocket.SendToAllExcept(sessionID, json.ToJsonString());
            }

            __result = requesterOutput;
            return false;
        }

        return true;
    }
}
