// public ItemEventRouterResponse TakeProduction(PmcData pmcData, HideoutTakeProductionRequestData request, MongoId sessionID)

using HarmonyLib;
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
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;

public class TakeProductionPatch : AbstractPatch
{
    static readonly MethodInfo _handleRecipe =
        AccessTools.Method(typeof(HideoutController), "HandleRecipe");

    static readonly MethodInfo _handleScavCase =
        AccessTools.Method(typeof(HideoutController), "HandleScavCase");

    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.TakeProduction));
    }

    [PatchPrefix]
    public static bool Prefix(HideoutController __instance, ref ItemEventRouterResponse __result, PmcData pmcData, HideoutTakeProductionRequestData request, MongoId sessionID)
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
            && ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>() is HttpResponseUtil httpResponseUtil
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService
            && ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>() is ServerLocalisationService serverLocalisationService)
        {
            var config = configService.Config;

            var sync = config.Sync.AreaState.Enabled && config.Sync.AreaUpgrade;
            var syncRewards = config.Sync.AreaState.Rewards;

            var requesterOutput = eventOutputHolder.GetOutput(sessionID);
            var hideoutDb = databaseService.GetHideout();

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionID;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;
                var profileOutput = eventOutputHolder.GetOutput(profileId);

                if (request.RecipeId == HideoutHelper.BitcoinProductionId)
                {
                    // Ensure server and client are in-sync when player presses 'get items' on farm
                    hideoutHelper.UpdatePlayerHideout(profileId);
                    hideoutHelper.GetBTC(profilePmcData, request, profileId, profileOutput);
                    continue;
                }

                var recipe = hideoutDb.Production.Recipes.FirstOrDefault(r => r.Id == request.RecipeId);
                if (recipe is not null)
                {
                    if (isRequester)
                    {
                        _handleRecipe.Invoke(__instance, [profileId, recipe, profilePmcData, request, requesterOutput]);
                    }
                    else
                    {
                        if (syncRewards)
                        {
                            _handleRecipe.Invoke(__instance, [profileId, recipe, profilePmcData, request, profileOutput]);
                        }
                        else
                        {
                            // TODO: continuous recipes?
                            if (!recipe.Continuous ?? false)
                                profilePmcData.Hideout.Production.Remove(recipe.Id);
                        }

                    }
                    continue;
                }

                var scavCase = hideoutDb.Production.ScavRecipes.FirstOrDefault(r => r.Id == request.RecipeId);
                if (scavCase is not null)
                {
                    if (isRequester)
                    {
                        _handleScavCase.Invoke(__instance, [profileId, profilePmcData, request, requesterOutput]);
                    }
                    else
                    {
                        if (syncRewards)
                        {
                            _handleScavCase.Invoke(__instance, [profileId, profilePmcData, request, profileOutput]);
                        }
                        else
                        {
                            profilePmcData.Hideout.Production.Remove(recipe.Id);
                        }

                    }
                    continue;
                }
            }

            if (sync)
            {
                websocket.SendToAllExcept(sessionID, jsonUtil.Serialize(new
                {
                    Action = "HideoutTakeProduction",
                    recipeId = request.RecipeId.ToString(),
                    syncRewards
                }));
            }

            __result = requesterOutput;
            return false;
        }

        return true;
    }
}
