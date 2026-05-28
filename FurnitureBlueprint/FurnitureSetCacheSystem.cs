using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 套组级缓存：一次完整识别后，查询结果内任意已登记物品均复用同一方案（避免按槽位种子重复识别导致膨胀）。
    /// 关闭世界时清空（OnWorldLoad / Unload）。
    /// </summary>
    public sealed class FurnitureSetCacheSystem : ModSystem
    {
        private static readonly Dictionary<long, FurnitureScheme> SchemesByKey = new();
        private static readonly Dictionary<int, long> ItemToSetKey = new();

        public override void OnModLoad()
        {
            InvalidateAll();
            FurnitureStyleClusterCatalog.ClearCache();
            FurnitureSetCatalog.Clear();
        }

        public override void OnWorldLoad() => InvalidateAll();

        public override void Unload() => InvalidateAll();

        public static long MakeSetKey(int primarySeed, int materialBlock) =>
            ((long)primarySeed << 32) | (uint)(materialBlock > ItemID.None ? materialBlock : 0);

        public static bool TryGetCachedSchemeForItem(
            int queryItemType,
            int requiredMaterialBlock,
            out FurnitureScheme scheme,
            out int materialBlock)
        {
            scheme = null;
            materialBlock = ItemID.None;
            if (queryItemType <= ItemID.None)
                return false;

            if (!ItemToSetKey.TryGetValue(queryItemType, out long key))
                return false;

            if (!SchemesByKey.TryGetValue(key, out FurnitureScheme stored) || stored == null)
                return false;

            materialBlock = (int)(key & 0xFFFFFFFF);
            if (requiredMaterialBlock > ItemID.None && materialBlock != requiredMaterialBlock)
                return false;

            if (requiredMaterialBlock > ItemID.None
                && stored.AnchorMaterialType > ItemID.None
                && stored.AnchorMaterialType != requiredMaterialBlock)
                return false;

            if (!FurnitureGenericWoodLineageRules.IsSchemeCacheable(stored, (int)(key >> 32), materialBlock))
                return false;

            scheme = stored.Clone();
            return true;
        }

        private static int CountWikiFilled(FurnitureScheme scheme)
        {
            int n = 0;
            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                if (scheme.GetSlot(kind) > ItemID.None)
                    n++;
            }
            return n;
        }

        public static bool TryGetCached(int seedType, int anchorBlockOverride, out FurnitureScheme scheme)
        {
            if (TryGetCachedSchemeForItem(seedType, anchorBlockOverride, out scheme, out int mat))
                return true;

            long key = MakeSetKey(seedType, anchorBlockOverride);
            if (SchemesByKey.TryGetValue(key, out FurnitureScheme stored) && stored != null)
            {
                scheme = stored.Clone();
                return true;
            }

            scheme = null;
            return false;
        }

        public static void RegisterScheme(FurnitureScheme scheme, int primarySeed, int materialBlock)
        {
            if (scheme == null || primarySeed <= ItemID.None || materialBlock <= ItemID.None)
                return;

            if (!FurnitureGenericWoodLineageRules.IsSchemeCacheable(scheme, primarySeed, materialBlock))
            {
                FurnitureBlueprintLog.InfoFull(
                    $"recognize cache skip seed={primarySeed} material={materialBlock} filled={CountWikiFilled(scheme)}/{FurnitureWikiSlots.TotalCount}");
                return;
            }

            long key = MakeSetKey(primarySeed, materialBlock);
            SchemesByKey[key] = scheme.Clone();

            RegisterItemKey(primarySeed, key);
            RegisterItemKey(materialBlock, key);
            if (scheme.SeedType > ItemID.None)
                RegisterItemKey(scheme.SeedType, key);

            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                int type = scheme.SlotItemTypes[i];
                if (type > ItemID.None)
                    RegisterItemKey(type, key);
            }
        }

        public static void CacheAutoScheme(int seedType, int anchorBlockOverride, FurnitureScheme scheme) =>
            RegisterScheme(scheme, seedType, anchorBlockOverride);

        private static void RegisterItemKey(int itemType, long key)
        {
            if (itemType <= ItemID.None)
                return;
            ItemToSetKey[itemType] = key;
        }

        public static void Invalidate(int seedType)
        {
            if (seedType <= ItemID.None)
                return;

            var removeKeys = new List<long>();
            foreach (KeyValuePair<long, FurnitureScheme> kv in SchemesByKey)
            {
                if ((int)(kv.Key >> 32) == seedType)
                    removeKeys.Add(kv.Key);
            }

            for (int i = 0; i < removeKeys.Count; i++)
                RemoveSetKey(removeKeys[i]);
        }

        public static void InvalidateAll()
        {
            ClearSchemesOnly();
            FurnitureRecognitionCaches.Clear();
            FurnitureReverseSeedProbeCache.Clear();
        }

        public static void ClearSchemesOnly()
        {
            SchemesByKey.Clear();
            ItemToSetKey.Clear();
        }

        private static void RemoveSetKey(long key)
        {
            SchemesByKey.Remove(key);
            var removeItems = new List<int>();
            foreach (KeyValuePair<int, long> kv in ItemToSetKey)
            {
                if (kv.Value == key)
                    removeItems.Add(kv.Key);
            }

            for (int i = 0; i < removeItems.Count; i++)
                ItemToSetKey.Remove(removeItems[i]);
        }
    }
}
