using HarmonyLib;
using SharedHideoutServer;
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
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Collections.Frozen;
using System.Reflection;

public class TakeItemsFromAreaSlotsPatch : AbstractPatch
{
    static readonly MethodInfo _removeResourceFromArea =
        AccessTools.Method(typeof(HideoutController), "RemoveResourceFromArea");

    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.TakeItemsFromAreaSlots));
    }

    [PatchPrefix]
    public static bool Prefix(HideoutController __instance, ref ItemEventRouterResponse __result, PmcData pmcData, HideoutTakeItemOutRequestData request, MongoId sessionID, FrozenSet<HideoutAreas> ___AreasWithResources)
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

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionID;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;

                var hideoutArea = profilePmcData.Hideout?.Areas.FirstOrDefault(area => area.Type == request.AreaType);
                if (hideoutArea is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area", request.AreaType));
                    
                    __result = httpResponseUtil.AppendErrorToOutput(requesterOutput);
                    return false;
                }

                if (hideoutArea.Slots is null || hideoutArea.Slots.Count == 0)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_item_to_remove_from_area", hideoutArea.Type));
                    
                    __result = httpResponseUtil.AppendErrorToOutput(requesterOutput);
                    return false;
                }

                if (___AreasWithResources.Contains(hideoutArea.Type))
                {
                    if (isRequester)
                    {
                        _removeResourceFromArea.Invoke(__instance, [sessionID, pmcData, request, requesterOutput, hideoutArea]);
                    }
                    else
                    {
                        var slotIndexToRemove = request.Slots?.FirstOrDefault();
                        var hideoutSlotIndex = hideoutArea.Slots.FindIndex(slot => slot.LocationIndex == slotIndexToRemove);
                        hideoutArea.Slots[hideoutSlotIndex].Items = null;
                    }

                    hideoutHelper.UpdatePlayerHideout(profileId);
                }
            }

            if (sync)
            {
                LogHelper.Logger.Info(jsonUtil.Serialize(request));
                websocket.SendToAllExcept(sessionID, jsonUtil.Serialize(request));
            }

            __result = requesterOutput;
            return false;
        }

        return true;
    }
}
