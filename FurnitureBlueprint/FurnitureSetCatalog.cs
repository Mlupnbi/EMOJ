using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 加载期 styleKey 套组索引：统计各 mod 下同名后缀家具覆盖槽位数，供置信度分层与 batch 审计。
    /// </summary>
    internal static class FurnitureSetCatalog
    {
        public readonly struct Snapshot
        {
            public string ModKey { get; }
            public string StyleKey { get; }
            public int FurnitureSlotCount { get; }
            public int SuffixMatchCount { get; }
            public int MaterialBlockType { get; }
            public int RepresentativeSeed { get; }

            internal Snapshot(
                string modKey,
                string styleKey,
                int furnitureSlotCount,
                int suffixMatchCount,
                int materialBlockType,
                int representativeSeed)
            {
                ModKey = modKey;
                StyleKey = styleKey;
                FurnitureSlotCount = furnitureSlotCount;
                SuffixMatchCount = suffixMatchCount;
                MaterialBlockType = materialBlockType;
                RepresentativeSeed = representativeSeed;
            }

            public int SuffixMatchPercent =>
                FurnitureSlotCount > 0 ? SuffixMatchCount * 100 / FurnitureSlotCount : 0;

            public bool LooksLikeStandardWikiSet =>
                FurnitureSlotCount >= 16 && SuffixMatchPercent >= 75;
        }

        private sealed class MutableEntry
        {
            public string ModKey;
            public string StyleKey;
            public int MaterialBlockType;
            public int RepresentativeSeed;
            public int RepresentativePriority;
            public readonly Dictionary<FurnitureSlotKind, int> SlotItems = new();
        }

        private static readonly Dictionary<string, MutableEntry> Entries =
            new Dictionary<string, MutableEntry>(StringComparer.OrdinalIgnoreCase);

        private static readonly FurnitureSlotKind[] RepresentativePriority =
        {
            FurnitureSlotKind.Table,
            FurnitureSlotKind.Workbench,
            FurnitureSlotKind.Bed,
            FurnitureSlotKind.Chair,
            FurnitureSlotKind.Door,
            FurnitureSlotKind.Dresser,
            FurnitureSlotKind.Bookcase,
            FurnitureSlotKind.Sofa,
            FurnitureSlotKind.Chest,
            FurnitureSlotKind.Lamp,
            FurnitureSlotKind.Sink,
            FurnitureSlotKind.Bathtub,
            FurnitureSlotKind.Piano
        };

        public const int BatchMinFurnitureSlots = 8;
        public const int BatchMaxRepresentativeSeeds = 512;

        /// <summary>Sets 批量测试：仅导出像完整套组的 catalog 代表 seed（去重、按槽位质量排序、硬上限）。</summary>
        public static void CollectBatchRepresentativeSeeds(
            List<int> dest,
            int minFurnitureSlots = BatchMinFurnitureSlots,
            int maxSeeds = BatchMaxRepresentativeSeeds)
        {
            if (dest == null || maxSeeds <= 0)
                return;

            var ranked = new List<(int seed, int score)>(Entries.Count);
            foreach (MutableEntry entry in Entries.Values)
            {
                if (entry.RepresentativeSeed <= ItemID.None)
                    continue;

                if (ShouldSkipBatchRepresentative(entry.RepresentativeSeed))
                    continue;

                Snapshot snap = ToSnapshot(entry);
                if (snap.FurnitureSlotCount < minFurnitureSlots)
                    continue;

                int score = snap.FurnitureSlotCount * 100 + snap.SuffixMatchPercent;
                ranked.Add((entry.RepresentativeSeed, score));
            }

            ranked.Sort((a, b) => b.score.CompareTo(a.score));

            var seen = new HashSet<int>();
            int rawDistinct = 0;
            foreach ((int seed, int _) in ranked)
            {
                if (!seen.Add(seed))
                    continue;

                rawDistinct++;
                if (dest.Count >= maxSeeds)
                    continue;

                dest.Add(seed);
            }

            FurnitureBlueprintLog.Info(
                $"set-catalog batch seeds catalog={Entries.Count} candidates={ranked.Count} distinct={rawDistinct} kept={dest.Count} minSlots={minFurnitureSlots} cap={maxSeeds}");
        }

        private static bool ShouldSkipBatchRepresentative(int type)
        {
            if (FurnitureNameSignals.IsDecorativeMark(type))
                return true;

            string name = FurnitureSchemeSlotFormatter.GetInternalEnglishName(type);
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string lower = name.ToLowerInvariant();
            return lower.Contains("banner") || lower.Contains("flag");
        }

        public static bool IsBuilt => Entries.Count > 0;

        public static void Clear() => Entries.Clear();

        private static int _incrementalCursor = ItemID.None;

        public static void BeginIncrementalBuild()
        {
            Clear();
            _incrementalCursor = ItemID.None + 1;
        }

        /// <summary>分帧构建索引，避免 TEST_BLUEPRINT 启动时单帧扫全物品。</summary>
        public static bool RegisterNextBatch(int batchSize, out bool complete)
        {
            complete = false;
            if (_incrementalCursor <= ItemID.None)
                return false;

            int max = ItemLoader.ItemCount;
            int end = Math.Min(_incrementalCursor + batchSize, max);
            for (int type = _incrementalCursor; type < end; type++)
                RegisterItem(type);

            _incrementalCursor = end;
            if (_incrementalCursor < max)
                return true;

            complete = true;
            _incrementalCursor = ItemID.None;
            FurnitureBlueprintLog.InfoFull($"set-catalog built entries={Entries.Count}");
            return true;
        }

        public static void Build()
        {
            Clear();
            int max = ItemLoader.ItemCount;
            for (int type = ItemID.None + 1; type < max; type++)
                RegisterItem(type);

            FurnitureBlueprintLog.InfoFull($"set-catalog built entries={Entries.Count}");
        }

        public static bool TryGet(string modKey, string styleKey, out Snapshot snapshot)
        {
            snapshot = default;
            if (string.IsNullOrWhiteSpace(modKey) || string.IsNullOrWhiteSpace(styleKey))
                return false;

            if (!Entries.TryGetValue(MakeKey(modKey, styleKey), out MutableEntry entry))
                return false;

            snapshot = ToSnapshot(entry);
            return true;
        }

        public static bool TryGetForItem(int itemType, out Snapshot snapshot)
        {
            snapshot = default;
            if (itemType <= ItemID.None)
                return false;

            ModItem mi = ItemLoader.GetItem(itemType);
            string modKey = mi?.Mod.Name ?? "Terraria";
            string styleKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
            if (string.IsNullOrWhiteSpace(styleKey))
                styleKey = FurnitureSetRecognizer.ExtractDisplayLineageMoniker(itemType);

            return TryGet(modKey, styleKey, out snapshot);
        }

        public static bool TrySuggestTier(int itemType, out FurnitureSetConfidenceTier tier)
        {
            tier = FurnitureSetConfidenceTier.Medium;
            if (!TryGetForItem(itemType, out Snapshot snap))
                return false;

            if (snap.FurnitureSlotCount >= 18 && snap.SuffixMatchPercent >= 82)
            {
                tier = FurnitureSetConfidenceTier.High;
                return true;
            }

            if (snap.LooksLikeStandardWikiSet)
            {
                tier = FurnitureSetConfidenceTier.Medium;
                return true;
            }

            return false;
        }

        private static void RegisterItem(int type)
        {
            if (type <= ItemID.None)
                return;

            if (FurnitureNameSignals.IsDecorativeMark(type))
                return;

            ModItem mi = ItemLoader.GetItem(type);
            string modKey = mi?.Mod.Name ?? "Terraria";

            string styleKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(type);
            if (string.IsNullOrWhiteSpace(styleKey))
                styleKey = FurnitureSetRecognizer.ExtractDisplayLineageMoniker(type);
            if (string.IsNullOrWhiteSpace(styleKey))
                return;

            if (FurnitureMaterialBlockResolver.SeedIsMaterialBlock(type)
                || (FurnitureRecognitionCaches.TryGetProbe(type, out Item probe)
                    && FurnitureMaterialAnchor.IsValidAnchorBlock(probe)))
            {
                RegisterMaterialBlock(modKey, styleKey, type);
                return;
            }

            if (!FurnitureRecognitionCaches.IsPlaceableFurniture(type))
                return;

            if (!FurnitureSlotClassifier.TryGetSlotFromType(type, out FurnitureSlotKind kind))
                return;

            kind = FurnitureWikiSlots.NormalizeClassified(kind);
            if (kind is FurnitureSlotKind.None
                or FurnitureSlotKind.Block
                or FurnitureSlotKind.Wall
                or FurnitureSlotKind.Platform)
                return;

            MutableEntry entry = GetOrCreate(modKey, styleKey);
            if (!entry.SlotItems.ContainsKey(kind))
                entry.SlotItems[kind] = type;

            int priority = RepresentativePriorityIndex(kind);
            if (priority > entry.RepresentativePriority)
            {
                entry.RepresentativePriority = priority;
                entry.RepresentativeSeed = type;
            }
        }

        private static void RegisterMaterialBlock(string modKey, string styleKey, int blockType)
        {
            MutableEntry entry = GetOrCreate(modKey, styleKey);
            if (entry.MaterialBlockType <= ItemID.None)
                entry.MaterialBlockType = blockType;
        }

        private static MutableEntry GetOrCreate(string modKey, string styleKey)
        {
            string key = MakeKey(modKey, styleKey);
            if (!Entries.TryGetValue(key, out MutableEntry entry))
            {
                entry = new MutableEntry { ModKey = modKey, StyleKey = styleKey.Trim() };
                Entries[key] = entry;
            }

            return entry;
        }

        private static Snapshot ToSnapshot(MutableEntry entry)
        {
            int furnitureSlots = 0;
            int suffixMatches = 0;
            foreach (KeyValuePair<FurnitureSlotKind, int> kv in entry.SlotItems)
            {
                if (kv.Key is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    continue;

                furnitureSlots++;
                if (FurnitureSlotScoring.HasInternalSlotSuffix(kv.Value, kv.Key))
                    suffixMatches++;
            }

            return new Snapshot(
                entry.ModKey,
                entry.StyleKey,
                furnitureSlots,
                suffixMatches,
                entry.MaterialBlockType,
                entry.RepresentativeSeed);
        }

        private static string MakeKey(string modKey, string styleKey) =>
            modKey + "|" + styleKey.Trim();

        private static int RepresentativePriorityIndex(FurnitureSlotKind kind)
        {
            for (int i = 0; i < RepresentativePriority.Length; i++)
            {
                if (RepresentativePriority[i] == kind)
                    return RepresentativePriority.Length - i;
            }

            return 1;
        }
    }
}
