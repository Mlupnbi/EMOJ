using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>防止木块等高扇出材料在 UI 线程一次分类过多产物导致卡死/闪退。</summary>
    internal static class FurnitureRecognizeCandidateCap
    {
        public const int MaxClassifyCandidates = 80;
        public const int MaxClassifyCandidatesModTight = 52;

        public static int GetMaxCandidates(int materialBlock, int seedType = ItemID.None)
        {
            int cap = ResolveCap(materialBlock, seedType);
            if (FurnitureBlueprintBatchTest.IsRunning)
                cap = Math.Min(cap, 40);
            return cap;
        }

        private static int ResolveCap(int materialBlock, int seedType)
        {
            if (seedType > ItemID.None && FurnitureSetMaterialRules.UsesModSpecificMaterialBlock(seedType))
                return 72;

            if (seedType > ItemID.None)
            {
                string seedName = FurnitureItemDefaults.SafeItemName(seedType);
                if (seedName.Contains("盐") || seedName.Contains("Salt", System.StringComparison.OrdinalIgnoreCase))
                    return 36;
                if (seedName.Contains("血肉") || seedName.Contains("Flesh", System.StringComparison.OrdinalIgnoreCase))
                    return 36;
            }

            if (materialBlock > ItemID.None)
            {
                string matName = FurnitureItemDefaults.SafeItemName(materialBlock);
                if (matName.Contains("血肉") || matName.Contains("Flesh", System.StringComparison.OrdinalIgnoreCase))
                    return 36;
            }

            if (materialBlock > ItemID.None)
            {
                string matName = FurnitureItemDefaults.SafeItemName(materialBlock);
                if (matName.Contains("盐") || matName.Contains("Salt", StringComparison.OrdinalIgnoreCase))
                    return 28;
            }

            if (materialBlock <= ItemID.None)
                return MaxClassifyCandidates;
            ModItem mi = ItemLoader.GetItem(materialBlock);
            return mi != null && mi.Mod.Name != "Terraria" ? MaxClassifyCandidatesModTight : MaxClassifyCandidates;
        }

        public static HashSet<int> TrimIfNeeded(
            HashSet<int> candidates,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig)
        {
            int cap = GetMaxCandidates(materialBlock, seedType);
            if (candidates == null || candidates.Count <= cap)
                return candidates;

            string modKey = GetModKey(materialBlock > ItemID.None ? materialBlock : seedType);
            string blockKey = blockSig.StyleKey?.Trim() ?? FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);

            var ranked = new List<(int type, int rank)>(candidates.Count);
            foreach (int type in candidates)
            {
                int rank = 0;
                if (type == seedType)
                    rank += 4_000;
                if (type == materialBlock)
                    rank += 3_500;

                if (FurnitureMaterialProductCollector.ProductMatchesMaterialBlock(type, materialBlock, modKey, blockKey))
                    rank += 2_000;

                if (FurnitureSetLineageScoring.ScoreSeedLineage(type, seedType, materialBlock) > 0)
                    rank += 1_500;

                ranked.Add((type, rank));
            }

            var trimmed = new HashSet<int>();
            foreach ((int type, int _) in ranked.OrderByDescending(r => r.rank).Take(cap))
                trimmed.Add(type);

            if (seedType > ItemID.None)
                trimmed.Add(seedType);
            if (materialBlock > ItemID.None)
                trimmed.Add(materialBlock);

            FurnitureBlueprintLog.Warn(
                $"recognize trim seed={seedType} material={materialBlock} before={candidates.Count} after={trimmed.Count}");

            return trimmed;
        }

        private static string GetModKey(int itemType)
        {
            ModItem mi = ItemLoader.GetItem(itemType);
            return mi == null ? "Terraria" : mi.Mod.Name;
        }
    }
}
