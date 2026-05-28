using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>识别热路径缓存：物品探测、槽位分类、材料产物集合。进世界时清空。</summary>
    internal static class FurnitureRecognitionCaches
    {
        private sealed class ProbeEntry
        {
            public int CreateTile;
            public int CreateWall;
            public int PlaceStyle;
            public string NameLower = "";
            public bool? PlaceableFurniture;
            public bool ClassificationCached;
            public FurnitureSlotKind ClassifiedSlot;
            public bool ClassificationSucceeded;
            public bool FailedDefaults;
        }

        private static readonly Dictionary<int, ProbeEntry> Probes = new();
        private static readonly Dictionary<long, HashSet<int>> MaterialProducts = new();

        public static void Clear()
        {
            Probes.Clear();
            MaterialProducts.Clear();
        }

        public static bool TryGetProbe(int type, out Item item)
        {
            item = null;
            if (type <= ItemID.None)
                return false;

            if (Probes.TryGetValue(type, out ProbeEntry entry) && entry.FailedDefaults)
                return false;

            if (TryGetSampleItem(type, out item))
            {
                EnsureProbeEntry(type, item);
                return true;
            }

            item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, type))
            {
                Probes[type] = new ProbeEntry { FailedDefaults = true };
                return false;
            }

            EnsureProbeEntry(type, item);
            return true;
        }

        private static bool TryGetSampleItem(int type, out Item item)
        {
            item = null;
            if (ContentSamples.ItemsByType.TryGetValue(type, out Item sample) && sample != null && !sample.IsAir)
            {
                item = sample;
                return true;
            }

            return false;
        }

        private static void EnsureProbeEntry(int type, Item item)
        {
            if (item == null || type <= ItemID.None)
                return;

            if (Probes.TryGetValue(type, out ProbeEntry existing) && !existing.FailedDefaults)
                return;

            Probes[type] = new ProbeEntry
            {
                CreateTile = item.createTile,
                CreateWall = item.createWall,
                PlaceStyle = item.placeStyle,
                NameLower = (item.Name ?? "").ToLowerInvariant()
            };
        }

        public static void ApplyProbeToItem(int type, Item item)
        {
            if (item == null || type <= ItemID.None)
                return;

            if (!Probes.ContainsKey(type))
                TryGetProbe(type, out _);
        }

        public static bool IsPlaceableFurniture(int type)
        {
            if (!TryGetProbe(type, out Item item))
                return false;

            ProbeEntry entry = Probes[type];
            if (entry.PlaceableFurniture.HasValue)
                return entry.PlaceableFurniture.Value;

            bool ok = FurnitureCandidateFilter.IsPlaceableFurnitureItem(item);
            entry.PlaceableFurniture = ok;
            return ok;
        }

        public static bool TryGetCachedClassification(int type, out FurnitureSlotKind kind)
        {
            kind = FurnitureSlotKind.None;
            if (type <= ItemID.None || !Probes.TryGetValue(type, out ProbeEntry entry) || !entry.ClassificationCached)
                return false;

            kind = entry.ClassifiedSlot;
            return entry.ClassificationSucceeded;
        }

        public static void CacheClassification(int type, FurnitureSlotKind kind, bool succeeded)
        {
            if (type <= ItemID.None)
                return;

            if (!Probes.TryGetValue(type, out ProbeEntry entry))
            {
                TryGetProbe(type, out _);
                entry = Probes[type];
            }

            entry.ClassificationCached = true;
            entry.ClassificationSucceeded = succeeded;
            entry.ClassifiedSlot = kind;
        }

        public static HashSet<int> GetOrCollectMaterialProducts(
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig)
        {
            FurnitureCraftStationProfile stations = FurnitureCraftStationProfile.FromSeed(seedType);
            int stationHash = 0;
            foreach (int tid in stations.StationTiles)
                stationHash ^= tid;

            string styleKey = blockSig.StyleKey?.Trim() ?? "";
            long key = ((long)materialBlock << 32)
                | (uint)(styleKey.GetHashCode() ^ seedType ^ stationHash);

            if (MaterialProducts.TryGetValue(key, out HashSet<int> cached))
            {
                var copy = new HashSet<int>(cached);
                return copy;
            }

            HashSet<int> fresh = FurnitureProductPipeline.CollectMaterialFirstProducts(
                seedType, materialBlock, blockSig, stations);
            fresh = FurnitureRecognizeCandidateCap.TrimIfNeeded(fresh, seedType, materialBlock, blockSig);
            MaterialProducts[key] = new HashSet<int>(fresh);
            return new HashSet<int>(fresh);
        }
    }
}
