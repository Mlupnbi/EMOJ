using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>´²/Ô¡¸×³£È±²Û£ºÍ¼¸ñÏßÐÖµÜ + ÑªÍ³ºòÑ¡µÍÃÅ¼÷»ØÌî¡£</summary>
    internal static class FurnitureBedBathtubBackfill
    {
        public static void TryFillEmptySlots(
            FurnitureScheme scheme,
            FurnitureRecognitionJob job,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            FurnitureRecognizeContext ctx)
        {
            if (scheme == null || job == null || seedType <= ItemID.None)
                return;

            TryFillSlot(
                scheme, job, seedType, materialBlock, blockSig, stations, ctx, FurnitureSlotKind.Bathtub);
            TryFillSlot(
                scheme, job, seedType, materialBlock, blockSig, stations, ctx, FurnitureSlotKind.Bed);
        }

        private static void TryFillSlot(
            FurnitureScheme scheme,
            FurnitureRecognitionJob job,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            FurnitureRecognizeContext ctx,
            FurnitureSlotKind slot)
        {
            if (scheme.GetSlot(slot) > ItemID.None)
                return;

            int pick = PickFromLineagePool(job, slot, seedType, materialBlock, blockSig, stations, ctx);
            if (pick <= ItemID.None)
                pick = PickFromPlacementLine(seedType, materialBlock, blockSig, slot);

            if (pick <= ItemID.None)
                return;

            scheme.SetSlot(slot, pick);
            FurnitureBlueprintLog.InfoFull(
                $"bed-bath backfill seed={seedType} slot={slot} pick={pick} material={materialBlock}");
        }

        private static int PickFromLineagePool(
            FurnitureRecognitionJob job,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            FurnitureRecognizeContext ctx)
        {
            var pool = new HashSet<int>();
            if (job.CandidateList != null)
            {
                for (int i = 0; i < job.CandidateList.Count; i++)
                    pool.Add(job.CandidateList[i]);
            }

            if (job.FinalizeCandidates != null)
            {
                foreach (int type in job.FinalizeCandidates)
                    pool.Add(type);
            }

            if (pool.Count == 0)
                return ItemID.None;

            int minScore = FurnitureSlotScoring.GetMinPickScore(slot, seedType, materialBlock);
            int pick = FurnitureSlotPicker.PickByScoreFromProducts(
                new List<int>(pool),
                slot,
                seedType,
                materialBlock,
                blockSig,
                stations,
                minScore,
                ctx: ctx);

            if (pick > ItemID.None)
                return pick;

            return slot switch
            {
                FurnitureSlotKind.Bathtub => TryPickBathtubRelaxed(new List<int>(pool), seedType, materialBlock),
                FurnitureSlotKind.Bed => TryPickBedRelaxed(new List<int>(pool), seedType, materialBlock),
                _ => ItemID.None
            };
        }

        private static int PickFromPlacementLine(
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureSlotKind slot)
        {
            FurnitureStyleSignature seedSig = FurnitureStyleSignature.FromItemType(seedType);
            int tile = seedSig.PlacementTile;
            int style = seedSig.PlacementStyle;
            if (tile < TileID.Dirt)
                return ItemID.None;

            string modKey = blockSig.ModKey;
            if (string.IsNullOrWhiteSpace(modKey))
            {
                ModItem seedMod = ItemLoader.GetItem(seedType);
                modKey = seedMod?.Mod.Name ?? "Terraria";
            }

            string styleKey = blockSig.StyleKey;
            if (string.IsNullOrWhiteSpace(styleKey))
                styleKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);

            var siblings = new HashSet<int>();
            FurnitureTileSlotRegistry.AddAllItemsOnPlacementTile(
                tile, modKey, styleKey, siblings, maxItems: 96, requireStyleMatch: true);

            int best = ItemID.None;
            int bestRank = int.MinValue;
            foreach (int type in siblings)
            {
                if (!MatchesSlot(type, slot))
                    continue;

                int rank = FurnitureSetLineageScoring.ScoreSeedLineage(type, seedType, materialBlock)
                    + FurnitureRecipeSlotSignals.ScoreNameBonus(type, slot);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = type;
                }
            }

            if (best <= ItemID.None)
                return ItemID.None;

            if (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
                return bestRank >= 0 ? best : ItemID.None;

            return bestRank >= FurnitureSetLineageScoring.LineageStrong / 4 ? best : ItemID.None;
        }

        private static bool MatchesSlot(int itemType, FurnitureSlotKind slot)
        {
            if (!FurnitureRecognitionCaches.TryGetProbe(itemType, out Item item))
                return false;

            if (!FurnitureSlotClassifier.TryGetSlot(item, out FurnitureSlotKind kind, out _))
                return false;

            return FurnitureWikiSlots.NormalizeClassified(kind) == slot;
        }

        private static int TryPickBathtubRelaxed(List<int> candidates, int seedType, int materialBlock)
        {
            int best = ItemID.None;
            int bestRank = int.MinValue;
            foreach (int type in candidates)
            {
                if (!FurnitureBathtubRules.SharesSetWithMaterial(type, materialBlock, seedType))
                    continue;

                int rank = FurnitureSetLineageScoring.ScoreSeedLineage(type, seedType, materialBlock)
                    + FurnitureRecipeSlotSignals.ScoreNameBonus(type, FurnitureSlotKind.Bathtub);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = type;
                }
            }

            return best;
        }

        private static int TryPickBedRelaxed(List<int> candidates, int seedType, int materialBlock)
        {
            int best = ItemID.None;
            int bestRank = int.MinValue;
            foreach (int type in candidates)
            {
                if (!FurnitureNameSignals.MeetsBedPickEvidence(type, materialBlock, seedType))
                    continue;

                int rank = FurnitureSetLineageScoring.ScoreSeedLineage(type, seedType, materialBlock)
                    + FurnitureRecipeSlotSignals.ScoreNameBonus(type, FurnitureSlotKind.Bed);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = type;
                }
            }

            return best;
        }
    }
}
