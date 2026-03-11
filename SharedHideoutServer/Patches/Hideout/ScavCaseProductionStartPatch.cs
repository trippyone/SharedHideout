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
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

public class ScavCaseProductionStartPatch : AbstractPatch
{
    static readonly MethodInfo _getScavCaseTime =
        AccessTools.Method(typeof(HideoutController), "GetScavCaseTime");

    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.ScavCaseProductionStart));
    }

    [PatchPrefix]
    public static bool Prefix(HideoutController __instance, ref ItemEventRouterResponse __result, PmcData pmcData, HideoutScavCaseStartRequestData request, MongoId sessionID)
    {
        if (ServiceLocator.ServiceProvider.GetService<EventOutputHolder>() is EventOutputHolder eventOutputHolder
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<SharedHideoutServerWebSocket>() is SharedHideoutServerWebSocket websocket
            && ServiceLocator.ServiceProvider.GetService<JsonUtil>() is JsonUtil jsonUtil
            && ServiceLocator.ServiceProvider.GetService<HideoutHelper>() is HideoutHelper hideoutHelper
            && ServiceLocator.ServiceProvider.GetService<InventoryHelper>() is InventoryHelper inventoryHelper
            && ServiceLocator.ServiceProvider.GetService<DatabaseService>() is DatabaseService databaseService
            && ServiceLocator.ServiceProvider.GetService<FenceService>() is FenceService fenceService
            && ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>() is HttpResponseUtil httpResponseUtil
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService
            && ServiceLocator.ServiceProvider.GetService<ProfileHelper>() is ProfileHelper profileHelper
            && ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>() is ServerLocalisationService serverLocalisationService
            && ServiceLocator.ServiceProvider.GetService<ISptLogger<HideoutController>>() is ISptLogger<HideoutController> logger)
        {
            var config = configService.Config;
            var sync = config.Sync.AreaState.Enabled && config.Sync.AreaUpgrade;
            var requesterOutput = eventOutputHolder.GetOutput(sessionID);

            foreach (var requestedItem in request.Items)
            {
                var inventoryItem = pmcData.Inventory.Items.FirstOrDefault(item => item.Id == requestedItem.Id);
                if (inventoryItem is null)
                {
                    
                    logger.Error(
                        serverLocalisationService.GetText(
                            "hideout-unable_to_find_scavcase_requested_item_in_profile_inventory",
                            requestedItem.Id
                        )
                    );
                    
                    __result = httpResponseUtil.AppendErrorToOutput(requesterOutput);
                    return false;
                }

                if (inventoryItem.Upd?.StackObjectsCount is not null && inventoryItem.Upd.StackObjectsCount > requestedItem.Count)
                {
                    inventoryItem.Upd.StackObjectsCount -= requestedItem.Count;
                }
                else
                {
                    inventoryHelper.RemoveItem(pmcData, requestedItem.Id, sessionID, requesterOutput);
                }
            }

            var recipe = databaseService.GetHideout().Production?.ScavRecipes?.FirstOrDefault(r => r.Id == request.RecipeId);
            if (recipe is null)
            {
                logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_scav_case_recipie_in_database", request.RecipeId));

                __result = httpResponseUtil.AppendErrorToOutput(requesterOutput);
                return false;
            }

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionID;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;

                // @Important: Here we need to be very exact:
                // - normal recipe: Production time value is stored in attribute "productionTime" with small "p"
                // - scav case recipe: Production time value is stored in attribute "ProductionTime" with capital "P"
                var adjustedCraftTime =
                    recipe.ProductionTime
                    - hideoutHelper.GetSkillProductionTimeReduction(
                        profilePmcData,
                        recipe.ProductionTime ?? 0,
                        SkillTypes.Crafting,
                        databaseService.GetGlobals().Configuration.SkillsSettings.Crafting.CraftTimeReductionPerLevel
                    );

                var modifiedScavCaseTime = (double?)_getScavCaseTime.Invoke(__instance, [profilePmcData, adjustedCraftTime]);

                profilePmcData.Hideout.Production[request.RecipeId] = hideoutHelper.InitProduction(
                    request.RecipeId,
                    (int)(profileHelper.IsDeveloperAccount(profileId) ? 40 : modifiedScavCaseTime),
                    false
                );
                profilePmcData.Hideout.Production[request.RecipeId].SptIsScavCase = true;

                profileHelper.AddSkillPointsToPlayer(profilePmcData, SkillTypes.Charisma, 1, true);
                profileHelper.AddSkillPointsToPlayer(profilePmcData, SkillTypes.HideoutManagement, 1, true);
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
