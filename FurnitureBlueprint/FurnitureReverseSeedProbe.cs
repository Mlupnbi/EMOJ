using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 种子倒推：单次扫描配方原料表，供材料块解析与材料选择器共用（避免重复 CollectIngredientScoreboard）。
    /// </summary>
    public sealed class FurnitureReverseSeedProbe
    {
        public int SeedType { get; init; }
        public FurnitureStyleSignature SeedSignature { get; init; }
        public string TargetStyle { get; init; } = "";
        public int BestAnchorIngredient { get; init; }
        public int BestAnchorScore { get; init; }
        public List<int> PickerCandidates { get; init; } = new List<int>();
    }

    public static class FurnitureReverseSeedProbeCache
    {
        private static readonly Dictionary<int, FurnitureReverseSeedProbe> Cache = new Dictionary<int, FurnitureReverseSeedProbe>();

        public static FurnitureReverseSeedProbe Ensure(int seedType)
        {
            if (seedType <= ItemID.None)
                return Empty(seedType);

            if (Cache.TryGetValue(seedType, out FurnitureReverseSeedProbe cached))
                return cached;

            FurnitureReverseSeedProbe probe = Build(seedType);
            Cache[seedType] = probe;
            return probe;
        }

        public static void Clear() => Cache.Clear();

        private static FurnitureReverseSeedProbe Empty(int seedType) => new FurnitureReverseSeedProbe { SeedType = seedType };

        private static FurnitureReverseSeedProbe Build(int seedType)
        {
            Item seedItem = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(seedItem, seedType))
                return Empty(seedType);

            if (FurnitureMaterialAnchor.IsValidAnchorBlock(seedItem))
            {
                return new FurnitureReverseSeedProbe
                {
                    SeedType = seedType,
                    SeedSignature = FurnitureStyleSignature.FromItemType(seedType),
                    TargetStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType),
                    BestAnchorIngredient = seedType,
                    BestAnchorScore = 10_000,
                    PickerCandidates = new List<int> { seedType }
                };
            }

            FurnitureStyleSignature sig = FurnitureStyleSignature.FromItemType(seedType);
            string targetStyle = sig.StyleKey?.Trim() ?? "";
            if (string.IsNullOrEmpty(targetStyle))
                targetStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);

            int best = ItemID.None;
            int bestScore = int.MinValue;
            var scoreboard = new List<(int type, int score)>();

            if (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType) && seedItem.createTile >= TileID.Dirt)
            {
                int lineFirst = FurniturePlacementLineMaterialResolver.TryResolveBlockFromFurnitureSeed(seedType);
                if (lineFirst > ItemID.None
                    && !FurnitureSetMaterialRules.IsForbiddenGenericMaterial(lineFirst, seedType))
                {
                    best = lineFirst;
                    bestScore = 12_000;
                    scoreboard.Add((lineFirst, bestScore));
                }
            }

            FurnitureReverseRecipeIngredients.CollectForSeed(seedType, sig, scoreboard, ref best, ref bestScore);

            if (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
            {
                for (int i = scoreboard.Count - 1; i >= 0; i--)
                {
                    if (FurnitureSetMaterialRules.IsForbiddenGenericMaterial(scoreboard[i].type, seedType))
                        scoreboard.RemoveAt(i);
                }

                if (FurnitureSetMaterialRules.IsForbiddenGenericMaterial(best, seedType))
                    best = ItemID.None;
            }

            if (scoreboard.Count > 0)
            {
                scoreboard.Sort((a, b) => b.score.CompareTo(a.score));
                int logCount = System.Math.Min(8, scoreboard.Count);
                for (int i = 0; i < logCount; i++)
                {
                    (int type, int score) entry = scoreboard[i];
                    int station = FurnitureReverseAnchorResolver.ScoreStationMaterialLink(seedType, entry.type);
                    int rank = FurnitureReverseAnchorResolver.CombineMaterialRankScore(seedType, entry.type);
                    FurnitureBlueprintLog.InfoFull(
                        $"reverse recipe-ingredient seed={seedType} #{i + 1} type={entry.type} score={entry.score} station={station} rank={rank}");
                }
            }

            if (best <= ItemID.None && seedItem.createTile >= TileID.Dirt)
            {
                int lineBlock = FurniturePlacementLineMaterialResolver.TryResolveBlockFromFurnitureSeed(seedType);
                if (lineBlock > ItemID.None)
                {
                    Item lineProbe = new Item();
                    lineProbe.SetDefaults(lineBlock);
                    if (FurnitureMaterialAnchor.IsValidAnchorBlock(lineProbe))
                    {
                        best = lineBlock;
                        bestScore = 3_000;
                        scoreboard.Add((lineBlock, bestScore));
                    }
                }
            }

            int anchorIngredient = FurnitureVanillaLivingWoodBridge.RedirectReverseAnchor(seedType, best);

            if (anchorIngredient > ItemID.None)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"reverse anchor seed={seedType} ingredient={anchorIngredient} score={bestScore} (recipe-only)");
            }
            else
            {
                FurnitureBlueprintLog.InfoFull($"reverse anchor seed={seedType} failed (no recipe ingredients)");
            }

            var picker = FurnitureReverseRecipeIngredients.BuildPickerList(seedType, scoreboard, anchorIngredient);
            if (picker.Count > 0)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"material picker seed={seedType} count={picker.Count} default_block={FurnitureReverseRecipeIngredients.PickDefaultPlaceableBlock(seedType, picker)}");
            }

            return new FurnitureReverseSeedProbe
            {
                SeedType = seedType,
                SeedSignature = sig,
                TargetStyle = targetStyle,
                BestAnchorIngredient = anchorIngredient,
                BestAnchorScore = bestScore,
                PickerCandidates = picker
            };
        }

    }
}
