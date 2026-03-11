//public ItemEventRouterResponse ToggleArea(PmcData pmcData, HideoutToggleAreaRequestData request, MongoId sessionID)

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
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

public class ToggleAreaPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.ToggleArea));
    }

    [PatchPrefix]
    public static bool Prefix(HideoutController __instance, ref ItemEventRouterResponse __result, PmcData pmcData, HideoutToggleAreaRequestData request, MongoId sessionID)
    {
        if (ServiceLocator.ServiceProvider.GetService<SharedHideoutServerWebSocket>() is SharedHideoutServerWebSocket websocket
            && ServiceLocator.ServiceProvider.GetService<ISptLogger<HideoutController>>() is ISptLogger<HideoutController> logger
            && ServiceLocator.ServiceProvider.GetService<JsonUtil>() is JsonUtil jsonUtil
            && ServiceLocator.ServiceProvider.GetService<DatabaseService>() is DatabaseService databaseService
            && ServiceLocator.ServiceProvider.GetService<HideoutHelper>() is HideoutHelper hideoutHelper
            && ServiceLocator.ServiceProvider.GetService<ProfileHelper>() is ProfileHelper profileHelper
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<EventOutputHolder>() is EventOutputHolder eventOutputHolder
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

                // Force a production update (occur before area is toggled as it could be generator and doing it after generator enabled would cause incorrect calculaton of production progress)
                hideoutHelper.UpdatePlayerHideout(profileId);

                var hideoutArea = profilePmcData.Hideout.Areas.FirstOrDefault(area => area.Type == request.AreaType);
                if (hideoutArea is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area", request.AreaType));
                    
                    __result = httpResponseUtil.AppendErrorToOutput(requesterOutput);
                    return false;
                }

                hideoutArea.Active = request.Enabled;

            }

            if (sync)
            {
                websocket.SendToAllExcept(sessionID, jsonUtil.Serialize(request));
            }

            __result = requesterOutput;
            return false;
        }

        return true;
    }
}