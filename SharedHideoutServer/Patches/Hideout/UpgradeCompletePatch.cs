using HarmonyLib;
using SharedHideout.Models.Config;
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
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

public class UpgradeCompletePatch : AbstractPatch
{
    // protected function
    static readonly MethodInfo _addContainerImprovementToProfile =
        AccessTools.Method(typeof(HideoutController), "AddContainerImprovementToProfile");

    // protected function
    static readonly MethodInfo _setWallVisibleIfPrereqsMet =
        AccessTools.Method(typeof(HideoutController), "SetWallVisibleIfPrereqsMet");

    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.UpgradeComplete));
    }

    [PatchPrefix]
    public static bool Prefix(HideoutController __instance, PmcData pmcData, HideoutUpgradeCompleteRequestData request, MongoId sessionID, ref ItemEventRouterResponse output)
    {
        if (ServiceLocator.ServiceProvider.GetService<SharedHideoutServerWebSocket>() is SharedHideoutServerWebSocket websocket
            && ServiceLocator.ServiceProvider.GetService<ISptLogger<HideoutController>>() is ISptLogger<HideoutController> logger
            && ServiceLocator.ServiceProvider.GetService<JsonUtil>() is JsonUtil jsonUtil
            && ServiceLocator.ServiceProvider.GetService<DatabaseService>() is DatabaseService databaseService
            && ServiceLocator.ServiceProvider.GetService<HideoutHelper>() is HideoutHelper hideoutHelper
            && ServiceLocator.ServiceProvider.GetService<ProfileHelper>() is ProfileHelper profileHelper
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService
            && ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>() is HttpResponseUtil httpResponseUtil
            && ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>() is ServerLocalisationService serverLocalisationService)
        {
            SharedHideoutServerConfig config = configService.Config;
            var sync = config.Sync.AreaUpgrade;

            var hideout = databaseService.GetHideout();
            var globals = databaseService.GetGlobals();

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionID;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;

                var profileHideoutArea = profilePmcData.Hideout.Areas.FirstOrDefault(area => area.Type == request.AreaType);
                if (profileHideoutArea is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area", request.AreaType));
                    
                    httpResponseUtil.AppendErrorToOutput(output);
                    return false;
                }

                var nextLevel = profileHideoutArea.Level + 1;

                var hideoutData = hideout.Areas.FirstOrDefault(area => area.Type == profileHideoutArea.Type);
                if (hideoutData is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area_in_database", request.AreaType));
                    
                    httpResponseUtil.AppendErrorToOutput(output);
                    return false;
                }

                // Apply bonuses
                if (!hideoutData.Stages.TryGetValue(nextLevel.ToString(), out var hideoutStage))
                {
                    logger.Error($"Stage level: {nextLevel} not found for area: {request.AreaType}");

                    return false;
                }

                // Upgrade profile values
                profileHideoutArea.Level = nextLevel;
                profileHideoutArea.CompleteTime = 0;
                profileHideoutArea.Constructing = false;

                var bonuses = hideoutStage.Bonuses;
                if (bonuses?.Count > 0)
                {
                    foreach (var bonus in bonuses)
                    {
                        hideoutHelper.ApplyPlayerUpgradesBonus(profilePmcData, bonus);
                    }
                }

                // Upgrade includes a container improvement/addition
                if (hideoutStage.Container.HasValue && !hideoutStage.Container.Value.IsEmpty)
                {
                    _addContainerImprovementToProfile.Invoke(__instance, [output, profileId, profilePmcData, profileHideoutArea, hideoutData, hideoutStage]);
                }

                // Upgrading water collector / med station
                if (profileHideoutArea.Type is HideoutAreas.WaterCollector or HideoutAreas.MedStation)
                {
                    _setWallVisibleIfPrereqsMet.Invoke(__instance, [profilePmcData]);
                }

                // Cleanup temporary buffs/debuffs from wall if complete
                if (profileHideoutArea.Type == HideoutAreas.EmergencyWall && profileHideoutArea.Level == 6)
                {
                    hideoutHelper.RemoveHideoutWallBuffsAndDebuffs(hideoutData, profilePmcData);
                }

                // Add Skill Points Per Area Upgrade
                profileHelper.AddSkillPointsToPlayer(
                    profilePmcData,
                    SkillTypes.HideoutManagement,
                    globals.Configuration.SkillsSettings.HideoutManagement.SkillPointsPerAreaUpgrade,
                    true
                );
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