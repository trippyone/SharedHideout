using SharedHideoutServer.Services;
using SharedHideoutServer.Websocket;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

public class StartUpgradePatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.StartUpgrade));
    }

    [PatchPrefix]
    public static bool Prefix(PmcData pmcData, HideoutUpgradeRequestData request, MongoId sessionID, ItemEventRouterResponse output)
    {
        if (ServiceLocator.ServiceProvider.GetService<SharedHideoutServerWebSocket>() is SharedHideoutServerWebSocket websocket
            && ServiceLocator.ServiceProvider.GetService<ISptLogger<HideoutController>>() is ISptLogger<HideoutController> logger
            && ServiceLocator.ServiceProvider.GetService<JsonUtil>() is JsonUtil jsonUtil
            && ServiceLocator.ServiceProvider.GetService<PaymentHelper>() is PaymentHelper paymentHelper
            && ServiceLocator.ServiceProvider.GetService<InventoryHelper>() is InventoryHelper inventoryHelper
            && ServiceLocator.ServiceProvider.GetService<DatabaseService>() is DatabaseService databaseService
            && ServiceLocator.ServiceProvider.GetService<ProfileHelper>() is ProfileHelper profileHelper
            && ServiceLocator.ServiceProvider.GetService<TimeUtil>() is TimeUtil timeUtil
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService
            && ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>() is HttpResponseUtil httpResponseUtil
            && ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>() is ServerLocalisationService serverLocalisationService)
        {
            var config = configService.Config;
            var sync = config.Sync.AreaUpgrade;

            var items = request
                .Items.Select(reqItem =>
                {
                    var item = pmcData.Inventory.Items.FirstOrDefault(invItem => invItem.Id == reqItem.Id);
                    return new { inventoryItem = item, requestedItem = reqItem };
                })
                .ToList();

            // If it's not money, its construction / barter items
            foreach (var item in items)
            {
                if (item.inventoryItem is null)
                {
                    //logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_item_in_inventory", item.requestedItem.Id));
                    //httpResponseUtil.AppendErrorToOutput(output);

                    return false;
                }

                if (
                    paymentHelper.IsMoneyTpl(item.inventoryItem.Template)
                    && item.inventoryItem.Upd is not null
                    && item.inventoryItem.Upd.StackObjectsCount is not null
                    && item.inventoryItem.Upd.StackObjectsCount > item.requestedItem.Count
                )
                {
                    item.inventoryItem.Upd.StackObjectsCount -= item.requestedItem.Count;
                }
                else
                {
                    inventoryHelper.RemoveItem(pmcData, item.inventoryItem.Id, sessionID, output);
                }
            }

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionID;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;

                // Construction time management
                var profileHideoutArea = profilePmcData.Hideout.Areas.FirstOrDefault(area => area.Type == request.AreaType);
                if (profileHideoutArea is null)
                {
                    //logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area", request.AreaType));
                    //httpResponseUtil.AppendErrorToOutput(output);

                    return false;
                }

                var hideoutDataDb = databaseService.GetTables().Hideout.Areas.FirstOrDefault(area => area.Type == request.AreaType);
                if (hideoutDataDb is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area_in_database", request.AreaType));
                    
                    httpResponseUtil.AppendErrorToOutput(output);
                    return false;
                }

                var ctime = hideoutDataDb.Stages[(profileHideoutArea.Level + 1).ToString()].ConstructionTime;
                if (ctime > 0)
                {
                    if (profileHelper.IsDeveloperAccount(profileId))
                    {
                        ctime = 40;
                    }

                    var timestamp = timeUtil.GetTimeStamp();

                    profileHideoutArea.CompleteTime = (int)Math.Round(timestamp + ctime.Value);
                    profileHideoutArea.Constructing = true;
                }
            }

            if (sync)
            {
                websocket.SendToAllExcept(sessionID, jsonUtil.Serialize(request));
            }

            return false;
        }

        return true;
    }
}