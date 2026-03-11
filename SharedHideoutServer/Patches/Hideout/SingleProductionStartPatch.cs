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
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;

public class SingleProductionStartPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HideoutController).GetMethod(nameof(HideoutController.SingleProductionStart));
    }

    [PatchPrefix]
    public static bool Prefix(ref ItemEventRouterResponse __result, PmcData pmcData, HideoutSingleProductionStartRequestData request, MongoId sessionID)
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
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService)
        {
            var config = configService.Config;
            var sync = config.Sync.AreaState.Enabled && config.Sync.AreaUpgrade;

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionID;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;

                // Start production
                hideoutHelper.RegisterProduction(profilePmcData, request, profileId);

                LogHelper.Logger.Info($"Registered production for: {saveServer.GetProfiles()[profileId].ProfileInfo.Username}");
            }

            // Find the recipe of the production
            var recipe = databaseService.GetHideout().Production.Recipes.FirstOrDefault(production => production.Id == request.RecipeId);

            // Find the actual amount of items we need to remove because body can send weird data
            var recipeRequirementsClone = cloner.Clone(recipe.Requirements.Where(r => r.Type == "Item" || r.Type == "Tool"));

            List<IdWithCount> itemsToDelete = [];
            var output = eventOutputHolder.GetOutput(sessionID);
            itemsToDelete.AddRange(request.Tools);
            itemsToDelete.AddRange(request.Items);

            foreach (var itemToDelete in itemsToDelete)
            {
                var itemToCheck = pmcData.Inventory.Items.FirstOrDefault(i => i.Id == itemToDelete.Id);
                var requirement = recipeRequirementsClone.FirstOrDefault(requirement => requirement.TemplateId == itemToCheck.Template);

                // Handle tools not having a `count`, but always only requiring 1
                var requiredCount = requirement.Count ?? 1;
                if (requiredCount <= 0)
                {
                    continue;
                }

                inventoryHelper.RemoveItemByCount(pmcData, itemToDelete.Id, requiredCount, sessionID, output);

                // Tools don't have a count
                if (requirement.Type != "Tool")
                {
                    requirement.Count -= (int)itemToDelete.Count;
                }
            }

            if (sync)
            {
                websocket.SendToAllExcept(sessionID, jsonUtil.Serialize(request));
            }

            __result = output;
            return false;
        }

        return true;
    }
}