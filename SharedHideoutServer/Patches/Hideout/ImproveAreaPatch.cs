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
using System.Reflection;

public class ImproveAreaPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.ImproveArea));
    }

    [PatchPrefix]
    public static bool Prefix(ref ItemEventRouterResponse __result, MongoId sessionId, PmcData pmcData, HideoutImproveAreaRequestData request)
    {
        if (ServiceLocator.ServiceProvider.GetService<EventOutputHolder>() is EventOutputHolder eventOutputHolder
            && ServiceLocator.ServiceProvider.GetService<PaymentHelper>() is PaymentHelper paymentHelper
            && ServiceLocator.ServiceProvider.GetService<InventoryHelper>() is InventoryHelper inventoryHelper
            && ServiceLocator.ServiceProvider.GetService<DatabaseService>() is DatabaseService databaseService
            && ServiceLocator.ServiceProvider.GetService<TimeUtil>() is TimeUtil timeUtil
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<SharedHideoutServerWebSocket>() is SharedHideoutServerWebSocket websocket
            && ServiceLocator.ServiceProvider.GetService<JsonUtil>() is JsonUtil jsonUtil
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService
            && ServiceLocator.ServiceProvider.GetService<ISptLogger<HideoutController>>() is ISptLogger<HideoutController> logger
            && ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>() is HttpResponseUtil httpResponseUtil
            && ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>() is ServerLocalisationService serverLocalisationService)
        {
            var config = configService.Config;
            var sync = config.Sync.AreaUpgrade;

            var requesterOutput = eventOutputHolder.GetOutput(sessionId);

            // Create mapping of required item with corresponding item from player inventory
            var items = request.Items.Select(reqItem =>
            {
                var item = pmcData.Inventory.Items.FirstOrDefault(invItem => invItem.Id == reqItem.Id);
                return new { inventoryItem = item, requestedItem = reqItem };
            });

            // If it's not money, its construction / barter items
            foreach (var item in items)
            {
                if (item.inventoryItem is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_item_in_inventory", item.requestedItem.Id));
                    __result = httpResponseUtil.AppendErrorToOutput(requesterOutput);
                    return false;
                }

                if (
                    paymentHelper.IsMoneyTpl(item.inventoryItem.Template)
                    && item.inventoryItem.Upd?.StackObjectsCount != null
                    && item.inventoryItem.Upd.StackObjectsCount > item.requestedItem.Count
                )
                {
                    item.inventoryItem.Upd.StackObjectsCount -= item.requestedItem.Count;
                }
                else
                {
                    inventoryHelper.RemoveItem(pmcData, item.inventoryItem.Id, sessionId, requesterOutput);
                }
            }

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionId;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;
                var profileOutput = eventOutputHolder.GetOutput(profileId);

                var profileHideoutArea = profilePmcData.Hideout.Areas.FirstOrDefault(x => x.Type == request.AreaType);
                if (profileHideoutArea is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area", request.AreaType));
                    __result = httpResponseUtil.AppendErrorToOutput(requesterOutput);
                    return false;
                }

                var hideoutDbData = databaseService.GetHideout().Areas.FirstOrDefault(area => area.Type == request.AreaType);
                if (hideoutDbData is null)
                {
                    //logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area_in_database", request.AreaType));
                    //return httpResponseUtil.AppendErrorToOutput(output);
                }

                // Add all improvements to output object
                var improvements = hideoutDbData.Stages[profileHideoutArea.Level.ToString()].Improvements;
                var timestamp = timeUtil.GetTimeStamp();

                // nullguard
                if (isRequester)
                    requesterOutput.ProfileChanges[profileId].Improvements ??= [];
                else
                    profileOutput.ProfileChanges[profileId].Improvements ??= [];

                foreach (var stageImprovement in improvements)
                {
                    var improvementDetails = new HideoutImprovement
                    {
                        Completed = false,
                        ImproveCompleteTimestamp = (long)(timestamp + stageImprovement.ImprovementTime),
                    };

                    if (isRequester)
                        requesterOutput.ProfileChanges[profileId].Improvements[stageImprovement.Id] = improvementDetails;
                    else
                        profileOutput.ProfileChanges[profileId].Improvements[stageImprovement.Id] = improvementDetails;

                    profilePmcData.Hideout.Improvements ??= [];
                    profilePmcData.Hideout.Improvements[stageImprovement.Id] = improvementDetails;
                }
            }

            if (sync)
            {
                websocket.SendToAllExcept(sessionId, jsonUtil.Serialize(request));
            }

            __result = requesterOutput;

            return false;
        }

        return true;
    }
}