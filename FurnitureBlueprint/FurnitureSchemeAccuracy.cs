using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>wiki 占格与槽位/血统准确度分离统计。</summary>
    public static class FurnitureSchemeAccuracy
    {
        public readonly struct Report
        {
            public int Filled { get; }
            public int Accurate { get; }
            public int SlotMismatch { get; }
            public int LineageMiss { get; }
            public int VanillaLeak { get; }
            public int MaterialMiss { get; }
            public int StyleSlotMatch { get; }
            public int StyleSlotFilled { get; }
            public int WikiMatch { get; }
            public int WikiChecked { get; }
            public string WikiSetLabel { get; }

            public Report(
                int filled,
                int accurate,
                int slotMismatch,
                int lineageMiss,
                int vanillaLeak,
                int materialMiss = 0,
                int styleSlotMatch = 0,
                int styleSlotFilled = 0,
                int wikiMatch = 0,
                int wikiChecked = 0,
                string wikiSetLabel = null)
            {
                Filled = filled;
                Accurate = accurate;
                SlotMismatch = slotMismatch;
                LineageMiss = lineageMiss;
                VanillaLeak = vanillaLeak;
                MaterialMiss = materialMiss;
                StyleSlotMatch = styleSlotMatch;
                StyleSlotFilled = styleSlotFilled;
                WikiMatch = wikiMatch;
                WikiChecked = wikiChecked;
                WikiSetLabel = wikiSetLabel ?? string.Empty;
            }
        }

        public static Report Evaluate(int seedType, int materialBlock, FurnitureScheme scheme)
        {
            if (scheme == null)
                return default;

            FurnitureBlueprintCrashDiagnostics.BeginSeed(seedType);
            FurnitureBlueprintCrashDiagnostics.Phase("accuracy", "begin");

            bool modSeed = IsModSeed(seedType);
            string seedStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            if (string.IsNullOrWhiteSpace(seedStyle))
                seedStyle = FurnitureSetRecognizer.ExtractDisplayLineageMoniker(seedType);

            int filled = 0;
            int accurate = 0;
            int slotMismatch = 0;
            int lineageMiss = 0;
            int vanillaLeak = 0;
            int materialMiss = modSeed && IsGenericWoodAnchor(materialBlock, seedType) ? 1 : 0;
            int styleSlotMatch = 0;
            int styleSlotFilled = 0;

            foreach (FurnitureSlotKind slot in FurnitureWikiSlots.RecognitionOrder)
            {
                if (slot is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    continue;

                int pick = scheme.GetSlot(slot);
                if (pick <= ItemID.None)
                    continue;

                FurnitureBlueprintCrashDiagnostics.Item(slot, pick, "accuracy-slot");

                filled++;

                FurnitureBlueprintCrashDiagnostics.Check(slot, pick, "slot-mismatch");
                bool mismatch = IsSlotMismatch(pick, slot);
                FurnitureBlueprintCrashDiagnostics.Check(slot, pick, $"slot-mismatch-result={mismatch}");

                FurnitureBlueprintCrashDiagnostics.Check(slot, pick, "lineage");
                bool lineage = modSeed && IsLineageMiss(seedType, seedStyle, materialBlock, pick);
                FurnitureBlueprintCrashDiagnostics.Check(slot, pick, $"lineage-result={lineage}");

                bool vanilla = modSeed && IsVanillaLeak(pick);

                if (mismatch)
                    slotMismatch++;
                if (lineage)
                    lineageMiss++;
                if (vanilla)
                    vanillaLeak++;

                if (!mismatch && !lineage && !vanilla)
                    accurate++;

                if (CountsTowardStyleSlotMatch(slot, pick))
                {
                    styleSlotFilled++;
                    FurnitureBlueprintCrashDiagnostics.Check(slot, pick, "style-slot-match");
                    if (PassesStyleSlotMatch(pick, slot, seedType))
                        styleSlotMatch++;
                }
            }

            FurnitureBlueprintCrashDiagnostics.Phase("accuracy", "wiki-match");
            var wikiReport = EvaluateWikiMatch(seedType, scheme);
            FurnitureBlueprintCrashDiagnostics.Phase("accuracy", "end");
            FurnitureBlueprintCrashDiagnostics.EndSeed();

            return new Report(
                filled,
                accurate,
                slotMismatch,
                lineageMiss,
                vanillaLeak,
                materialMiss,
                styleSlotMatch,
                styleSlotFilled,
                wikiMatch: wikiReport.Match,
                wikiChecked: wikiReport.Checked,
                wikiSetLabel: wikiReport.SetLabel);
        }

        private static FurnitureWikiExpectations.WikiMatchReport EvaluateWikiMatch(int seedType, FurnitureScheme scheme)
        {
            if (!FurnitureWikiExpectations.IsLoaded)
                return default;

            return FurnitureWikiExpectations.Evaluate(seedType, scheme);
        }

        /// <summary>internal 后缀 + 种子 style 一致（不依赖 wiki）。</summary>
        public static bool PassesStyleSlotMatch(int pick, FurnitureSlotKind slot, int seedType)
        {
            if (pick <= ItemID.None || seedType <= ItemID.None)
                return false;

            if (!FurnitureSlotScoring.HasInternalSlotSuffix(pick, slot))
                return false;

            return FurnitureStylePrefixCatalog.ProductMatchesSeedStyle(pick, seedType, ItemID.None);
        }

        private static bool CountsTowardStyleSlotMatch(FurnitureSlotKind slot, int pick) =>
            pick > ItemID.None
            && slot is not FurnitureSlotKind.Block
                and not FurnitureSlotKind.Wall
                and not FurnitureSlotKind.Platform;

        /// <summary>wiki 期望 internal 名对照（仅 batch/审计用；无登记时返回 false）。</summary>
        public static bool TryWikiStyleMatch(int seedType, int pick, out string reason)
        {
            reason = null;
            if (pick <= ItemID.None || seedType <= ItemID.None)
                return true;

            string seedStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            if (string.IsNullOrWhiteSpace(seedStyle))
                seedStyle = FurnitureSetRecognizer.ExtractDisplayLineageMoniker(seedType);
            if (string.IsNullOrWhiteSpace(seedStyle))
                return true;

            if (IsVanillaItem(pick))
            {
                reason = "vanilla_pick";
                return false;
            }

            string pickStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(pick);
            if (string.IsNullOrWhiteSpace(pickStyle))
                return true;

            if (FurnitureStyleSignature.StyleKeyFuzzyMatch(seedStyle, pickStyle)
                || FurnitureMaterialKeyNormalizer.SameMaterialFamily(seedStyle, pickStyle))
                return true;

            reason = $"style:{pickStyle}!={seedStyle}";
            return false;
        }

        private static bool IsModSeed(int seedType)
        {
            ModItem mi = ItemLoader.GetItem(seedType);
            return mi != null && mi.Mod.Name != "Terraria";
        }

        private static bool IsSlotMismatch(int pick, FurnitureSlotKind expected)
        {
            FurnitureBlueprintCrashDiagnostics.Check(expected, pick, "try-get-slot");
            if (FurnitureSlotClassifier.TryGetSlotFromType(pick, out FurnitureSlotKind classified))
            {
                classified = FurnitureWikiSlots.NormalizeClassified(classified);
                if (classified == expected)
                    return false;
            }

            FurnitureBlueprintCrashDiagnostics.Check(expected, pick, "infer-classify");
            if (FurnitureSlotScoring.TryInferClassifySlot(pick, out FurnitureSlotKind inferred, out _)
                && FurnitureWikiSlots.NormalizeClassified(inferred) == expected)
                return false;

            FurnitureBlueprintCrashDiagnostics.Check(expected, pick, "slot-evidence");
            return !PassesSlotEvidence(pick, expected);
        }

        private static bool PassesSlotEvidence(int pick, FurnitureSlotKind slot) => slot switch
        {
            FurnitureSlotKind.Chair => FurnitureNameSignals.MeetsChairPickEvidence(pick, ItemID.None, null),
            FurnitureSlotKind.Bed => FurnitureNameSignals.MeetsBedPickEvidence(pick, ItemID.None, ItemID.None),
            FurnitureSlotKind.Bathtub => FurnitureNameSignals.MeetsBathtubPickEvidence(pick, ItemID.None, ItemID.None),
            FurnitureSlotKind.Workbench => FurnitureNameSignals.MeetsWorkbenchPickEvidence(pick, ItemID.None, ItemID.None),
            FurnitureSlotKind.Sink => FurnitureNameSignals.MeetsSinkPickEvidence(pick, ItemID.None, ItemID.None),
            FurnitureSlotKind.Table => FurnitureNameSignals.MeetsTablePickEvidence(pick, ItemID.None, ItemID.None),
            _ => FurnitureSlotScoring.ScoreClassifyOnly(pick, slot) >= FurnitureSlotScoring.MinClassifyRecipeScore
        };

        private static bool IsLineageMiss(int seedType, string seedStyle, int materialBlock, int pick)
        {
            ModItem seedMod = ItemLoader.GetItem(seedType);
            if (seedMod == null)
                return false;

            if (IsVanillaItem(pick))
                return true;

            ModItem pickMod = ItemLoader.GetItem(pick);
            if (pickMod == null)
                return false;

            if (seedMod.Mod.Name == pickMod.Mod.Name)
            {
                string pickStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(pick);
                if (string.IsNullOrWhiteSpace(pickStyle))
                    return false;

                if (FurnitureStyleSignature.StyleKeyFuzzyMatch(seedStyle, pickStyle)
                    || FurnitureMaterialKeyNormalizer.SameMaterialFamily(seedStyle, pickStyle))
                    return false;

                if (materialBlock > ItemID.None && FurnitureRecipeSetLinker.ProductUsesMaterial(pick, materialBlock))
                    return false;

                if (FurnitureSetLineageScoring.ScoreSeedLineage(pick, seedType, materialBlock) > 0)
                    return false;

                return true;
            }

            return true;
        }

        private static bool IsVanillaItem(int pick)
        {
            if (pick <= ItemID.None)
                return false;

            ModItem mi = ItemLoader.GetItem(pick);
            return mi == null || mi.Mod.Name == "Terraria";
        }

        private static bool IsVanillaLeak(int pick) => IsVanillaItem(pick);

        private static bool IsGenericWoodAnchor(int materialBlock, int seedType) =>
            FurnitureSetMaterialRules.IsMisalignedGenericMaterialForModSeed(seedType, materialBlock);
    }
}
