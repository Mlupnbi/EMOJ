using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using EvenMoreOverpoweredJourney.Core.Logging;
using EmojLog = EvenMoreOverpoweredJourney.Core.Logging.EmojLog;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public static class FurnitureSlotPicker
    {
        public static int PickForSlot(
            List<int> candidates,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            int minimumScore = FurnitureSlotScoring.MinBucketPickScore,
            HashSet<int> occupied = null,
            FurnitureRecognizeContext ctx = null) =>
            PickBest(candidates, slot, seedType, materialBlock, blockSig, stations, minimumScore, "bucket", occupied, ctx);

        public static int PickByScoreFromProducts(
            IEnumerable<int> products,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            int minimumScore,
            HashSet<int> occupied = null,
            FurnitureRecognizeContext ctx = null)
        {
            var list = new List<int>();
            if (products != null)
            {
                foreach (int type in products)
                {
                    if (type <= ItemID.None)
                        continue;
                    if (occupied != null && occupied.Contains(type))
                        continue;
                    list.Add(type);
                }
            }

            return PickBest(list, slot, seedType, materialBlock, blockSig, stations, minimumScore, "score-pool", occupied, ctx);
        }

        private static int PickBest(
            List<int> candidates,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            int minimumScore,
            string logTag,
            HashSet<int> occupied,
            FurnitureRecognizeContext ctx)
        {
            if (candidates == null || candidates.Count == 0)
                return ItemID.None;

            if (seedType > ItemID.None
                && FurnitureSlotScoring.IsSeedHomeSlot(seedType, slot, seedType))
            {
                foreach (int home in candidates)
                {
                    if (home != seedType)
                        continue;
                    if (occupied != null && occupied.Contains(home))
                        break;
                    EmojLogDiagnostics.LogSlotPickScores(seedType, slot, materialBlock, home,
                        new[] { (home, FurnitureSlotScoring.SeedExactBonus) });
                    return home;
                }
            }

            bool logScores = EmojLog.IsFullMode;
            List<(int type, int score)> scoreRows = logScores ? new List<(int type, int score)>() : null;
            int best = ItemID.None;
            int bestScore = 0;
            int scored = 0;

            foreach (int type in candidates)
            {
                if (occupied != null && occupied.Contains(type)
                    && !FurnitureSlotScoring.IsSeedHomeSlot(type, slot, seedType))
                    continue;

                if (!FurnitureSlotScoring.PassesSlotPickGate(type, slot, seedType, materialBlock, ctx))
                    continue;

                int score = FurnitureSlotScoring.ScoreCandidate(
                    type, slot, seedType, materialBlock, blockSig, stations, ctx);
                if (score <= 0)
                    continue;

                scored++;
                scoreRows?.Add((type, score));
                if (score > bestScore || (score == bestScore && score > 0 && PreferTieBreak(type, best, slot, seedType, materialBlock)))
                {
                    bestScore = score;
                    best = type;
                }
            }

            scoreRows?.Sort((a, b) => b.score.CompareTo(a.score));

            if (best <= ItemID.None || bestScore < minimumScore)
            {
                EmojLogDiagnostics.LogSlotPickScores(seedType, slot, materialBlock, ItemID.None, scoreRows);
                FurnitureBlueprintLog.InfoFull(
                    $"slot pick below threshold {logTag} {slot} material={materialBlock} pool={candidates.Count} " +
                    $"scored={scored} occupied={occupied?.Count ?? 0} best={best} score={bestScore} min={minimumScore}");
                return ItemID.None;
            }

            EmojLogDiagnostics.LogSlotPickScores(seedType, slot, materialBlock, best, scoreRows);
            return best;
        }

        private static bool PreferTieBreak(
            int challenger,
            int incumbent,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock)
        {
            if (incumbent <= ItemID.None)
                return true;

            int a = FurnitureSetLineageScoring.ScoreSeedLineage(challenger, seedType, materialBlock)
                + FurnitureRecipeSlotSignals.ScoreNameBonus(challenger, slot);
            int b = FurnitureSetLineageScoring.ScoreSeedLineage(incumbent, seedType, materialBlock)
                + FurnitureRecipeSlotSignals.ScoreNameBonus(incumbent, slot);
            if (a != b)
                return a > b;

            return challenger < incumbent;
        }
    }
}
