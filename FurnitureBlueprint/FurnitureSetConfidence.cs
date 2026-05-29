using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public enum FurnitureSetConfidenceTier
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public readonly struct FurnitureSetConfidenceReport
    {
        public FurnitureSetConfidenceTier Tier { get; }
        public bool PreferSeedCluster { get; }
        public bool StrictSlotGate { get; }
        public int StyleAlignmentPercent { get; }

        public FurnitureSetConfidenceReport(
            FurnitureSetConfidenceTier tier,
            bool preferSeedCluster,
            bool strictSlotGate,
            int styleAlignmentPercent)
        {
            Tier = tier;
            PreferSeedCluster = preferSeedCluster;
            StrictSlotGate = strictSlotGate;
            StyleAlignmentPercent = styleAlignmentPercent;
        }
    }

    /// <summary>
    /// 套组置信度：标准 wiki 套组走严格后缀/槽位门；非标 mod 走 seed-cluster + 宽松启发式。
    /// </summary>
    internal static class FurnitureSetConfidence
    {
        internal const int LowMaterialCandidateCap = 36;

        public static FurnitureSetConfidenceReport EvaluatePreview(
            int seedType,
            FurnitureStyleSignature signature,
            int materialBlock,
            FurnitureStyleSignature blockSig)
        {
            if (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
            {
                return new FurnitureSetConfidenceReport(
                    FurnitureSetConfidenceTier.Low,
                    preferSeedCluster: true,
                    strictSlotGate: false,
                    styleAlignmentPercent: 0);
            }

            ModItem seedMod = ItemLoader.GetItem(seedType);
            bool isVanilla = seedMod == null || seedMod.Mod.Name == "Terraria";

            if (materialBlock > ItemID.None
                && RecipeAnalyzer.IsHighFanoutMaterial(materialBlock)
                && !isVanilla)
            {
                return new FurnitureSetConfidenceReport(
                    FurnitureSetConfidenceTier.Low,
                    preferSeedCluster: true,
                    strictSlotGate: false,
                    styleAlignmentPercent: 0);
            }

            string styleKey = ResolveStyleKey(blockSig, seedType, materialBlock);
            if (string.IsNullOrWhiteSpace(styleKey))
            {
                return new FurnitureSetConfidenceReport(
                    FurnitureSetConfidenceTier.Low,
                    preferSeedCluster: true,
                    strictSlotGate: false,
                    styleAlignmentPercent: 0);
            }

            if (isVanilla && materialBlock > ItemID.None)
            {
                return new FurnitureSetConfidenceReport(
                    FurnitureSetConfidenceTier.High,
                    preferSeedCluster: false,
                    strictSlotGate: true,
                    styleAlignmentPercent: 100);
            }

            if (!isVanilla
                && !FurnitureSetMaterialRules.UsesModLineageAnchor(seedType)
                && FurnitureSetCatalog.TrySuggestTier(seedType, out FurnitureSetConfidenceTier catalogTier))
            {
                return new FurnitureSetConfidenceReport(
                    catalogTier,
                    preferSeedCluster: catalogTier == FurnitureSetConfidenceTier.Low,
                    strictSlotGate: catalogTier == FurnitureSetConfidenceTier.High,
                    styleAlignmentPercent: catalogTier == FurnitureSetConfidenceTier.High ? 90 : 60);
            }

            if (isVanilla)
            {
                return new FurnitureSetConfidenceReport(
                    FurnitureSetConfidenceTier.Medium,
                    preferSeedCluster: true,
                    strictSlotGate: false,
                    styleAlignmentPercent: 50);
            }

            if (materialBlock > ItemID.None && StyleKeysAlign(styleKey, materialBlock))
            {
                return new FurnitureSetConfidenceReport(
                    FurnitureSetConfidenceTier.Medium,
                    preferSeedCluster: false,
                    strictSlotGate: false,
                    styleAlignmentPercent: 60);
            }

            return new FurnitureSetConfidenceReport(
                FurnitureSetConfidenceTier.Low,
                preferSeedCluster: true,
                strictSlotGate: false,
                styleAlignmentPercent: 0);
        }

        public static FurnitureSetConfidenceReport Evaluate(FurnitureRecognitionJob job)
        {
            var preview = EvaluatePreview(
                job.SeedType,
                job.BlockSig,
                job.MaterialBlock,
                job.BlockSig);

            int styleMatch;
            int styleTotal;
            ComputeStyleAlignment(job, out styleMatch, out styleTotal);
            int alignPct = styleTotal > 0 ? styleMatch * 100 / styleTotal : preview.StyleAlignmentPercent;

            FurnitureSetConfidenceTier tier = preview.Tier;
            if (preview.Tier != FurnitureSetConfidenceTier.Low)
            {
                if (alignPct >= 72)
                    tier = FurnitureSetConfidenceTier.High;
                else if (alignPct >= 42)
                    tier = FurnitureSetConfidenceTier.Medium;
                else
                    tier = FurnitureSetConfidenceTier.Low;
            }

            if (!FurnitureSetMaterialRules.UsesModLineageAnchor(job.SeedType)
                && FurnitureSetCatalog.TryGetForItem(job.SeedType, out FurnitureSetCatalog.Snapshot catalog)
                && catalog.LooksLikeStandardWikiSet)
            {
                if (catalog.FurnitureSlotCount >= 18 && catalog.SuffixMatchPercent >= 82)
                    tier = FurnitureSetConfidenceTier.High;
                else if (tier == FurnitureSetConfidenceTier.Low)
                    tier = FurnitureSetConfidenceTier.Medium;
            }

            bool strict = tier == FurnitureSetConfidenceTier.High;
            return new FurnitureSetConfidenceReport(tier, preview.PreferSeedCluster, strict, alignPct);
        }

        private static void ComputeStyleAlignment(
            FurnitureRecognitionJob job,
            out int styleMatch,
            out int styleTotal)
        {
            styleMatch = 0;
            styleTotal = 0;

            string blockKey = ResolveStyleKey(job.BlockSig, job.SeedType, job.MaterialBlock);
            if (string.IsNullOrWhiteSpace(blockKey))
                return;

            IEnumerable<int> types = job.FinalizeCandidates ?? (IEnumerable<int>)job.CandidateList;
            if (types == null)
                return;

            foreach (int type in types)
            {
                if (type <= ItemID.None)
                    continue;

                styleTotal++;
                string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(type);
                if (StyleKeysAlign(blockKey, productKey))
                    styleMatch++;
            }
        }

        private static string ResolveStyleKey(
            FurnitureStyleSignature blockSig,
            int seedType,
            int materialBlock)
        {
            if (!string.IsNullOrWhiteSpace(blockSig.StyleKey))
                return blockSig.StyleKey.Trim();

            if (materialBlock > ItemID.None)
                return FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);

            return FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
        }

        private static bool StyleKeysAlign(string blockKey, int materialOrProductType)
        {
            if (materialOrProductType <= ItemID.None || string.IsNullOrWhiteSpace(blockKey))
                return false;

            string other = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialOrProductType);
            return StyleKeysAlign(blockKey, other);
        }

        private static bool StyleKeysAlign(string blockKey, string productKey)
        {
            if (string.IsNullOrWhiteSpace(blockKey) || string.IsNullOrWhiteSpace(productKey))
                return false;

            if (string.Equals(blockKey, productKey, System.StringComparison.OrdinalIgnoreCase))
                return true;

            return FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, productKey)
                || FurnitureMaterialKeyNormalizer.StyleKeysMatch(blockKey, productKey)
                || FurnitureStyleSignature.StyleKeySameMaterialFamily(blockKey, productKey);
        }
    }
}
