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
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

public class ContinuousProductionStartPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.ContinuousProductionStart));
    }

    [PatchPrefix]
    public static bool Prefix(ref ItemEventRouterResponse __result, PmcData pmcData, HideoutContinuousProductionStartRequestData request, MongoId sessionID)
    {
        if (ServiceLocator.ServiceProvider.GetService<EventOutputHolder>() is EventOutputHolder eventOutputHolder
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<SharedHideoutServerWebSocket>() is SharedHideoutServerWebSocket websocket
            && ServiceLocator.ServiceProvider.GetService<JsonUtil>() is JsonUtil jsonUtil
            && ServiceLocator.ServiceProvider.GetService<HideoutHelper>() is HideoutHelper hideoutHelper
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService)
        {
            var config = configService.Config;
            var sync = config.Sync.AreaState.Enabled && config.Sync.AreaUpgrade;

            var requesterOutput = eventOutputHolder.GetOutput(sessionID);

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionID;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;

                hideoutHelper.RegisterProduction(profilePmcData, request, profileId);
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
