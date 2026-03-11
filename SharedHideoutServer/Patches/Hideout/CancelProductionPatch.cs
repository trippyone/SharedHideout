using SharedHideoutServer;
using SharedHideoutServer.Services;
using SharedHideoutServer.Websocket;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

public class CancelProductionPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.CancelProduction));
    }

    [PatchPrefix]
    public static bool Prefix(ref ItemEventRouterResponse __result, MongoId sessionId, PmcData pmcData, HideoutCancelProductionRequestData request)
    {
        if (ServiceLocator.ServiceProvider.GetService<EventOutputHolder>() is EventOutputHolder eventOutputHolder
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<SharedHideoutServerWebSocket>() is SharedHideoutServerWebSocket websocket
            && ServiceLocator.ServiceProvider.GetService<JsonUtil>() is JsonUtil jsonUtil
            && ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>() is HttpResponseUtil httpResponseUtil
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService)
        {
            var config = configService.Config;
            var sync = config.Sync.AreaState.Enabled && config.Sync.AreaUpgrade;

            var requesterOutput = eventOutputHolder.GetOutput(sessionId);

            var craftToCancel = pmcData.Hideout.Production.GetValueOrDefault(request.RecipeId);
            if (craftToCancel is null)
            {
                var errorMessage = $"Unable to find craft {request.RecipeId} to cancel";
                LogHelper.Logger.Error(errorMessage);
                __result = httpResponseUtil.AppendErrorToOutput(requesterOutput, errorMessage);
                return false;
            }

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionId;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;

                if (profilePmcData.Hideout.Production.ContainsKey(request.RecipeId))
                {
                    profilePmcData.Hideout.Production[request.RecipeId] = null;
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
