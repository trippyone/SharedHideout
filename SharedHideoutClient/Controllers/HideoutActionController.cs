using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using HarmonyLib;
using SharedHideoutClient.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharedHideoutClient.Controllers
{
    public class HideoutActionController
    {
        public bool GeneratorStatus { get; private set; } = false;
        public static HideoutActionController Instance { get; private set; }

        public HideoutActionController()
        {
            Instance = this;
        }

        public void UpgradeArea(EAreaType areaType)
        {
            if (!HideoutHelper.TryGetHideoutAndArea(areaType, out var hideout, out var area))
                return;

            area.CurrentStage.StartTime = EFTDateTimeClass.UtcNow;
            area.CurrentStage.ActionGoing = true;
            area.CurrentStage.Waiting = true;
            area.Status = (area.Status != EAreaStatus.ReadyToUpgrade && area.Status != EAreaStatus.LockedToUpgrade)
                ? EAreaStatus.Constructing : (area.NextStage.AutoUpgrade ? EAreaStatus.AutoUpgrading : EAreaStatus.Upgrading);
        }

        public void CompleteUpgradeArea(EAreaType areaType)
        {
            if (!HideoutHelper.TryGetHideoutAndArea(areaType, out var hideout, out var area))
                return;

            GClass2204 areaInfo = hideout.GClass2204_0.FirstOrDefault(x => x.Type == (int)area.Template.Type);

            if (areaInfo == null)
                return;

            areaInfo.Level++;

            foreach (SkillBonusAbstractClass bonus in area.StageAt(areaInfo.Level).Bonuses.Data)
            {
                if (bonus.Passive && !bonus.Production && bonus is GClass1834 energyBonus)
                {
                    float max = hideout.ISession.Profile.Health.Energy.Maximum;
                    hideout.ISession.Profile.Health.Energy.Maximum = (float)energyBonus.CalculateValue((double)max);
                }
            }

            var onAreaUpdated = AccessTools.Field(typeof(HideoutClass), "action_2")?.GetValue(hideout) as Action;
            onAreaUpdated?.Invoke();

            area.method_3();
        }

        public void ProductionStart(string recipeId)
        {
            if (!HideoutHelper.TryGetProducerAndScheme(recipeId, out var producer, out var scheme))
                return;

            producer.StartProducing(scheme);
            HideoutHelper.FireProduceStatusChanged(producer);
        }

        public void ContinuousProductionStart(string recipeId)
        {
            ProductionStart(recipeId);
            HideoutHelper.UpdatePermanentProductionView();
        }

        public void CircleOfCultistProductionStart(int craftTime)
        {
            if (!HideoutHelper.TryGetHideoutAndArea(EAreaType.CircleOfCultists, out var hideout, out var area))
                return;

            if (!HideoutHelper.TryGetAreaProducer(area, out var producer))
                return;

            var scheme = producer.Schemes.Values.FirstOrDefault();

            if (scheme == null)
                return;

            var producingItem = new GClass2438(scheme._id, craftTime, 1f);
            producer.AddProducingItem(producingItem);

            var onProductionStarted = AccessTools.Field(typeof(GClass2431), "action_3")?.GetValue(producer) as Action<GClass2431, GClass2438>;
            onProductionStarted?.Invoke(producer, producingItem);

            HideoutHelper.FireProduceStatusChanged(producer);
        }

        public void TakeProduction(string recipeId, bool syncRewards)
        {
            var hideout = Singleton<HideoutClass>.Instance;
            var inventoryController = hideout.InventoryController_0;
            var stash = inventoryController.Inventory.Stash;

            if (!HideoutHelper.TryGetProducerAndScheme(recipeId, out var producer, out var scheme))
                return;

            if (syncRewards)
            {
                var completedItems = producer.CompleteItemsStorage.GetItems(recipeId);
                if (completedItems != null)
                {
                    foreach (var item in completedItems)
                    {
                        var location = stash.Grids[0].FindLocationForItem(item);

                        if (location == null)
                            continue; // Item is lost due to lack of space.

                        stash.Grids[0].Add(item, location.LocationInGrid, false);
                    }
                }

                var onProducedItemsReceived = AccessTools.Field(typeof(HideoutClass), "action_3")?.GetValue(hideout) as Action<GClass2431, string>;
                onProducedItemsReceived?.Invoke(producer, recipeId);
            }

            producer.GetItems(recipeId);
            HideoutHelper.FireProduceStatusChanged(producer);

            var onDataChanged = AccessTools.Field(typeof(GClass2431), "action_3")?.GetValue(producer) as Action;
            onDataChanged?.Invoke();
        }

        public void CancelProduction(string recipeId)
        {
            if (!HideoutHelper.TryGetProducerAndScheme(recipeId, out var producer, out _))
                return;

            producer.TryUpdateProduction(recipeId, null);
            HideoutHelper.UpdatePermanentProductionView();
        }

        public void ToggleArea(EAreaType areaType, bool toggle)
        {
            if (!HideoutHelper.TryGetHideoutAndArea(areaType, out var hideout, out var area))
                return;

            switch (areaType)
            {
                case EAreaType.Generator:
                    ToggleGenerator(hideout, area, toggle);
                    break;
                default:
                    LogHelper.Logger.LogInfo($"AreaToggle: unknown areaType: {areaType}");
                    break;

            }
        }

        public void PutItemsInAreaSlots(EAreaType areaType, Dictionary<string, HideoutSlotItem> items)
        {
            if (!HideoutHelper.TryGetHideoutAndArea(areaType, out _, out var area))
                return;

            switch (areaType)
            {
                case EAreaType.BitcoinFarm:
                    PutGraphicsCardInBitcoinFarm(area, items);
                    break;
                case EAreaType.WaterCollector:
                    PutItemsInWaterCollector(area, items);
                    break;
                default:
                    PutConsumableItemsInArea(area, items);
                    break;
            }
        }

        public void TakeItemsFromAreaSlots(EAreaType areaType, List<int> slots)
        {
            if (!HideoutHelper.TryGetHideoutAndArea(areaType, out _, out var area))
                return;

            switch (areaType)
            {
                case EAreaType.WaterCollector:
                    TakeItemsFromWaterCollector(area, slots);
                    break;
                default:
                    TakeConsumableItemsFromArea(area, slots);
                    break;
            }
        }

        private void TakeConsumableItemsFromArea(AreaData area, List<int> slots)
        {
            if (area.Template.AreaBehaviour is not IHideoutConsumer consumer)
                return;

            var consumerItems = (BarterItemItemClass[])consumer.UsingItems.Clone();

            foreach (var slot in slots)
            {
                consumerItems[slot] = null;
            }

            consumer.InstallConsumableItems(consumerItems);

            foreach (var slot in slots)
            {
                HideoutHelper.FireConsumableItemChanged(consumer, null, slot);
            }
        }

        private void TakeItemsFromWaterCollector(AreaData area, List<int> slotItems)
        {
            if (!HideoutHelper.TryGetAreaProducer(area, out var producer))
                return;

            var items = (BarterItemItemClass[])producer.ResourceConsumer.UsingItems.Clone();

            foreach (var slot in slotItems)
            {
                items[slot] = null;
            }

            producer.ResourceConsumer.SetItems(items);

            var onConsumableItemChanged = typeof(GClass2434).GetField("action_8",
                BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(producer) as Action<Item, int>;

            foreach (var slot in slotItems)
            {
                onConsumableItemChanged?.Invoke(null, slot);
            }

            producer.method_8();
            HideoutHelper.FireProduceStatusChanged(producer);
            HideoutHelper.UpdatePermanentProductionView();
        }

        private void PutConsumableItemsInArea(AreaData area, Dictionary<string, HideoutSlotItem> slotItems)
        {
            if (area.Template.AreaBehaviour is not IHideoutConsumer consumer)
                return;

            var consumerItems = (BarterItemItemClass[])consumer.UsingItems.Clone();

            foreach (var slotItem in slotItems)
            {
                int slot = int.Parse(slotItem.Key);
                consumerItems[slot] = Singleton<ItemFactoryClass>.Instance.CreateItem(slotItem.Value.Id, slotItem.Value.Tpl, null) as BarterItemItemClass;
            }

            consumer.InstallConsumableItems(consumerItems);

            foreach (var slotItem in slotItems)
            {
                int slot = int.Parse(slotItem.Key);
                HideoutHelper.FireConsumableItemChanged(consumer, consumer.UsingItems[slot], slot);
            }
        }

        private void PutItemsInWaterCollector(AreaData area, Dictionary<string, HideoutSlotItem> slotItems)
        {
            if (!HideoutHelper.TryGetAreaProducer(area, out var producer))
                return;

            var consumerItems = (BarterItemItemClass[])producer.ResourceConsumer.UsingItems.Clone();

            foreach (var slotItem in slotItems)
            {
                int slot = int.Parse(slotItem.Key);
                consumerItems[slot] = Singleton<ItemFactoryClass>.Instance.CreateItem(slotItem.Value.Id, slotItem.Value.Tpl, null) as BarterItemItemClass;
            }

            producer.ResourceConsumer.SetItems(consumerItems);

            var onConsumableItemChanged = typeof(GClass2434).GetField("action_8",
                BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(producer) as Action<Item, int>;

            foreach (var slotItem in slotItems)
            {
                int slot = int.Parse(slotItem.Key);
                onConsumableItemChanged?.Invoke(producer.UsingItems[slot], slot);
            }

            producer.method_8();
            HideoutHelper.FireProduceStatusChanged(producer);
            HideoutHelper.UpdatePermanentProductionView();
        }

        private void PutGraphicsCardInBitcoinFarm(AreaData area, Dictionary<string, HideoutSlotItem> items)
        {
            if (!HideoutHelper.TryGetAreaProducer(area, out var producer)) 
                return;

            int gpuCount = items.Count;

            producer.InstalledSuppliesCount += gpuCount;
            area.AttachVideoCard(gpuCount);

            if (!producer.ProducingItems.Any())
            {
                producer.StartProducing(producer.Gclass2440_0);
            }

            UnityEngine.Object.FindObjectOfType<FarmingView>()?.UpdateView();
        }

        private void ToggleGenerator(HideoutClass hideout, AreaData area, bool toggle)
        {
            area.IsActive = toggle;
            hideout.EnergyController.SetSwitchedStatus(toggle);
            hideout.ProductionController?.EnergySupplyChanged(toggle);
            hideout.method_20(toggle); // lights status
            hideout.method_36(); //icon status

            var overlay = UnityEngine.Object.FindObjectOfType<HideoutScreenOverlay>();

            if (overlay == null)
                return;

            // _generatorButton toggle
            AccessTools.Method(typeof(HideoutScreenOverlay), "method_14").Invoke(overlay, [false]);
        }
    }
}
