using HarmonyLib;
using HarmonyLib.Tools;
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
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;

public class StartSacrificePatch : AbstractPatch
{
    static readonly MethodInfo _getSacrificedItems =
        AccessTools.Method(typeof(CircleOfCultistService), "GetSacrificedItems");

    static readonly MethodInfo _getRewardAmountMultiplier =
        AccessTools.Method(typeof(CircleOfCultistService), "GetRewardAmountMultiplier");

    static readonly MethodInfo _generateSacrificedItemsCache =
        AccessTools.Method(typeof(CircleOfCultistService), "GenerateSacrificedItemsCache");

    static readonly MethodInfo _checkForDirectReward =
        AccessTools.Method(typeof(CircleOfCultistService), "CheckForDirectReward");

    static readonly MethodInfo _getCircleCraftingInfo =
        AccessTools.Method(typeof(CircleOfCultistService), "GetCircleCraftingInfo");

    static readonly MethodInfo _registerCircleOfCultistProduction =
        AccessTools.Method(typeof(CircleOfCultistService), "RegisterCircleOfCultistProduction");

    static readonly MethodInfo _getCultistCircleRewardPool =
        AccessTools.Method(typeof(CircleOfCultistService), "GetCultistCircleRewardPool");

    static readonly MethodInfo _getRewardsWithinBudget =
        AccessTools.Method(typeof(CircleOfCultistService), "GetRewardsWithinBudget");

    static readonly MethodInfo _getDirectRewards =
        AccessTools.Method(typeof(CircleOfCultistService), "GetDirectRewards");

    static readonly MethodInfo _addRewardsToCircleContainer =
        AccessTools.Method(typeof(CircleOfCultistService), "AddRewardsToCircleContainer");

    static readonly string CircleOfCultistSlotId = "CircleOfCultistsGrid1";

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CircleOfCultistService), "StartSacrifice");
    }

    [PatchPrefix]
    public static bool Prefix(
        CircleOfCultistService __instance,
        ref ItemEventRouterResponse __result,
        MongoId sessionId,
        PmcData pmcData,
        HideoutCircleOfCultistProductionStartRequestData request)
    {
        if (ServiceLocator.ServiceProvider.GetService<EventOutputHolder>() is EventOutputHolder eventOutputHolder
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<SharedHideoutServerWebSocket>() is SharedHideoutServerWebSocket websocket
            && ServiceLocator.ServiceProvider.GetService<JsonUtil>() is JsonUtil jsonUtil
            && ServiceLocator.ServiceProvider.GetService<DatabaseService>() is DatabaseService databaseService
            && ServiceLocator.ServiceProvider.GetService<HideoutHelper>() is HideoutHelper hideoutHelper
            && ServiceLocator.ServiceProvider.GetService<InventoryHelper>() is InventoryHelper inventoryHelper
            && ServiceLocator.ServiceProvider.GetService<ItemHelper>() is ItemHelper itemHelper
            && ServiceLocator.ServiceProvider.GetService<ConfigServer>() is ConfigServer configServer
            && ServiceLocator.ServiceProvider.GetService<ICloner>() is ICloner cloner
            && ServiceLocator.ServiceProvider.GetService<ConfigService>() is ConfigService configService
            && ServiceLocator.ServiceProvider.GetService<ISptLogger<HideoutController>>() is ISptLogger<HideoutController> logger
            && ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>() is HttpResponseUtil httpResponseUtil
            && ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>() is ServerLocalisationService localisationService)
        {
            var config = configService.Config;
            var sync = config.Sync.CircleOfCultist.Enabled;
            var syncRewards = config.Sync.CircleOfCultist.Rewards;
            var syncSameRewards = config.Sync.CircleOfCultist.SameRewards;

            var hideoutConfig = configServer.GetConfig<HideoutConfig>();
            var requesterOutput = eventOutputHolder.GetOutput(sessionId);

            var cultistCircleStashId = pmcData.Inventory?.HideoutAreaStashes?.GetValueOrDefault(
                ((int)HideoutAreas.CircleOfCultists).ToString()
            );

            if (cultistCircleStashId is null)
            {
                logger.Error(localisationService.GetText("cultistcircle-unable_to_find_stash_id"));

                __result = requesterOutput;
                return false;
            }

            var cultistCraftData = databaseService.GetHideout().Production.CultistRecipes.FirstOrDefault();
            var sacrificedItems = (List<Item>)_getSacrificedItems.Invoke(__instance, [pmcData])!;
            var sacrificedItemCostRoubles = sacrificedItems.Aggregate(0D, (sum, item) => sum + (itemHelper.GetItemPrice(item.Template) ?? 0));

            var rewardAmountMultiplier = (double)_getRewardAmountMultiplier.Invoke(__instance, [pmcData, hideoutConfig.CultistCircle])!;

            var rewardAmountRoubles = Math.Round(sacrificedItemCostRoubles * rewardAmountMultiplier);

            var directRewardsCache = _generateSacrificedItemsCache.Invoke(__instance, [hideoutConfig.CultistCircle.DirectRewards]);
            var directRewardSettings = (DirectRewardSettings?)_checkForDirectReward.Invoke(__instance, [sessionId, sacrificedItems, directRewardsCache]);
            var hasDirectReward = directRewardSettings?.Reward.Count > 0;

            var craftingInfo = (CircleCraftDetails)_getCircleCraftingInfo.Invoke(__instance, [rewardAmountRoubles, hideoutConfig.CultistCircle, directRewardSettings])!;

            List<List<Item>> requesterRewards;
            if (hasDirectReward)
            {
                requesterRewards = (List<List<Item>>)_getDirectRewards.Invoke(__instance, [sessionId, directRewardSettings, cultistCircleStashId.Value])!;
            }
            else
            {
                var rewardPool = (List<MongoId>)_getCultistCircleRewardPool.Invoke(__instance, [sessionId, pmcData, craftingInfo, hideoutConfig.CultistCircle])!;
                requesterRewards = (List<List<Item>>)_getRewardsWithinBudget.Invoke(__instance, [rewardPool, rewardAmountRoubles, cultistCircleStashId.Value, hideoutConfig.CultistCircle])!;
            }

            var cultistStashDbItem = itemHelper.GetItem(ItemTpl.HIDEOUTAREACONTAINER_CIRCLEOFCULTISTS_STASH_1);

            foreach (var (profileId, profile) in saveServer.GetProfiles())
            {
                var isRequester = profileId == sessionId;

                if (!isRequester && !sync)
                    continue;

                var profilePmcData = profile.CharacterData.PmcData;
                var profileOutput = eventOutputHolder.GetOutput(profileId);

                var profileCultistCircleStashId = profilePmcData.Inventory?.HideoutAreaStashes?.GetValueOrDefault(
                    ((int)HideoutAreas.CircleOfCultists).ToString());

                _registerCircleOfCultistProduction.Invoke(__instance, [profileId, profilePmcData, cultistCraftData.Id, sacrificedItems, (double)craftingInfo.Time]);

                List<List<Item>>? rewards = null;
                if (isRequester)
                {
                    foreach (var item in sacrificedItems.Where(i => i.SlotId == CircleOfCultistSlotId))
                        inventoryHelper.RemoveItem(profilePmcData, item.Id, sessionId, requesterOutput);

                    rewards = requesterRewards;
                }
                else if (syncRewards)
                {
                    if (syncSameRewards)
                    {
                        rewards = requesterRewards.Select(items =>
                        {
                            var clonedReward = cloner.Clone(items);
                            foreach (var item in clonedReward.Where(i => i.ParentId == cultistCircleStashId.Value))
                                item.ParentId = profileCultistCircleStashId.Value;
                            return clonedReward;
                        }).ToList();
                    }
                    else
                    {
                        if (hasDirectReward)
                        {
                            rewards = (List<List<Item>>)_getDirectRewards.Invoke(__instance, [profileId, directRewardSettings, profileCultistCircleStashId.Value])!;
                        }
                        else
                        {
                            var rewardPool = (List<MongoId>)_getCultistCircleRewardPool.Invoke(__instance, [profileId, profilePmcData, craftingInfo, hideoutConfig.CultistCircle])!;
                            rewards = (List<List<Item>>)_getRewardsWithinBudget.Invoke(__instance, [rewardPool, rewardAmountRoubles, profileCultistCircleStashId.Value, hideoutConfig.CultistCircle])!;
                        }
                    }
                }

                if (rewards != null)
                {
                    var containerGrid = inventoryHelper.GetContainerSlotMap(cultistStashDbItem.Value.Id);
                    _addRewardsToCircleContainer.Invoke(__instance, [profileId, profilePmcData, rewards, containerGrid, profileCultistCircleStashId, isRequester ? requesterOutput : profileOutput]);
                }
            }

            if (sync)
            {
                // TODO: Use strong typing
                websocket.SendToAllExcept(sessionId, jsonUtil.Serialize(new
                {
                    Action = "HideoutCircleOfCultistProductionStart",
                    craftTime = (int)craftingInfo.Time
                }));
            }

            __result = requesterOutput;
            return false;
        }

        return true;
    }
}
