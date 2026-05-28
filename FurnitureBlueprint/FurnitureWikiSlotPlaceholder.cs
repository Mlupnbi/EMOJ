using System.Collections.Generic;
using Terraria.ID;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 单次 Recognize 内：每个仍空的 wiki 槽仅做一次占位计分（不重复 product-backfill + wiki-completion）。
    /// </summary>
    public static class FurnitureWikiSlotPlaceholder
    {
        public static void FillEmptySlotsOnce(
            FurnitureScheme scheme,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            HashSet<int> candidates,
            Dictionary<FurnitureSlotKind, List<int>> perSlot,
            FurnitureCraftStationProfile stations,
            FurnitureRecognizeContext ctx)
        {
            if (scheme == null || materialBlock <= ItemID.None || candidates == null || candidates.Count == 0)
                return;

            var occupied = FurnitureSchemeOccupancy.CollectOccupied(scheme, materialBlock, seedType);
            int filledBefore = CountWikiFilled(scheme);

            HashSet<int> expanded = null;
            bool expansionBuilt = false;

            foreach (FurnitureSlotKind slot in FurnitureWikiSlots.RecognitionOrder)
            {
                if (slot is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    continue;

                if (scheme.GetSlot(slot) > ItemID.None)
                    continue;

                if (ctx != null && !ctx.TryBeginPlaceholderAttempt(slot))
                    continue;

                if (!expansionBuilt)
                {
                    expanded = BuildExpandedCandidates(seedType, materialBlock, blockSig, candidates, stations);
                    expansionBuilt = true;
                }

                List<int> pool = FurniturePlaceholderPool.Build(
                    slot, perSlot, expanded, occupied, seedType, materialBlock, ctx);
                int pick = FurnitureSlotPicker.PickForSlot(
                    pool, slot, seedType, materialBlock, blockSig, stations,
                    FurnitureSlotScoring.GetMinPlaceholderScore(slot, seedType, materialBlock), occupied, ctx);

                EmojLogDiagnostics.LogSlotResolved(seedType, slot, pick, pool.Count, "placeholder-once");

                if (pick <= ItemID.None)
                    continue;

                scheme.SetSlot(slot, pick);
                FurnitureSchemeOccupancy.MarkUsed(occupied, pick);

                if (perSlot != null)
                {
                    if (!perSlot.TryGetValue(slot, out List<int> list))
                    {
                        list = new List<int>();
                        perSlot[slot] = list;
                    }

                    if (!list.Contains(pick))
                        list.Add(pick);
                }
            }

            int filledAfter = CountWikiFilled(scheme);
            if (filledAfter > filledBefore)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"placeholder-once seed={seedType} material={materialBlock} wiki {filledBefore}->{filledAfter} occupied={occupied.Count}");
            }
        }

        private static HashSet<int> BuildExpandedCandidates(
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            HashSet<int> candidates,
            FurnitureCraftStationProfile stations)
        {
            var expanded = new HashSet<int>(candidates);
            if (expanded.Count >= FurnitureSlotScoring.MaxPlaceholderCandidates)
                return expanded;

            if (RecipeAnalyzer.IsHighFanoutMaterial(materialBlock))
                return expanded;

            int cap = FurnitureSlotScoring.MaxPlaceholderCandidates * 4;
            if (expanded.Count < cap)
                FurnitureCandidateExpander.Expand(seedType, blockSig, materialBlock, expanded);

            if (expanded.Count >= cap)
                return expanded;

            int before = expanded.Count;
            foreach (int product in FurnitureMaterialProductCollector.CollectFromMaterialBlock(materialBlock, blockSig))
            {
                if (expanded.Count >= cap)
                    break;
                expanded.Add(product);
            }

            if (expanded.Count > before)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"placeholder expand strict seed={seedType} material={materialBlock} added={expanded.Count - before} total={expanded.Count}");
            }

            FurnitureCandidateExpander.EnsureMaterialRoleProducts(materialBlock, blockSig, expanded);

            return expanded;
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
    }
}
