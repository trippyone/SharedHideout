using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace SharedHideoutClient
{
    public static class HideoutHelper
    {
        public static bool TryGetHideoutAndArea(EAreaType areaType, out HideoutClass hideout, out AreaData area)
        {
            hideout = Singleton<HideoutClass>.Instance;
            area = null;

            if (hideout == null)
            {
                return false;
            }

            if (hideout.AreaDatas == null || hideout.AreaDatas.Count == 0)
            {
                return false;
            }

            area = hideout.AreaDatas.FirstOrDefault(x => x.Template.Type == areaType);

            if (area == null)
            {
                return false;
            }

            return true;
        }

        public static bool TryGetProducerAndScheme(string recipeId, out GClass2431 producer, out ProductionBuildAbstractClass scheme)
        {
            var hideout = Singleton<HideoutClass>.Instance;
            var productionController = hideout.ProductionController;

            producer = null;
            scheme = null;

            foreach (var area in hideout.AreaDatas)
            {
                var producers = productionController.GetAreaProducers(area);

                if (producers == null)
                    continue;

                foreach (var p in producers)
                {
                    if (p?.Schemes != null && p.Schemes.TryGetValue(recipeId, out scheme))
                    {
                        producer = p;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetAreaProducer(AreaData area, out GClass2434 producer)
        {
            producer = Singleton<HideoutClass>.Instance.ProductionController
                .GetAreaProducers(area)
                .OfType<GClass2434>()
                .FirstOrDefault();

            return producer != null;
        }

        public static void FireProduceStatusChanged(GClass2431 producer)
        {
            var action = AccessTools.Field(typeof(GClass2431), "action_2")?.GetValue(producer) as Action;
            action?.Invoke();
        }

        public static void FireConsumableItemChanged(IHideoutConsumer consumer, Item item, int slot)
        {
            var action = consumer.GetType()
                .GetField("action_0", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(consumer) as Action<Item, int>;
            
            action?.Invoke(item, slot);
        }

        public static void UpdatePermanentProductionView()
        {
            UnityEngine.Object.FindObjectOfType<PermanentProductionView>()?.UpdateView();
        }
    }
}
