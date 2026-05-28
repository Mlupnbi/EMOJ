using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>倒推：仅收集「能合成该种子」的配方原料（如宇宙床 → 宇宙砖×15 + 丝绸）。</summary>
    public static class FurnitureReverseRecipeIngredients
    {
        private const int MaxPickerItems = 12;
        private const int MaxGroupExpand = 8;

        public static void CollectForSeed(
            int seedType,
            FurnitureStyleSignature seedSig,
            List<(int type, int score)> scoreboard,
            ref int bestAnchor,
            ref int bestAnchorScore)
        {
            if (seedType <= ItemID.None)
                return;

            string targetStyle = seedSig.StyleKey?.Trim() ?? "";
            if (string.IsNullOrEmpty(targetStyle))
                targetStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);

            foreach (Recipe recipe in FurnitureRecipeLookup.GetRecipesCreating(seedType))
            {
                if (recipe?.requiredItem == null)
                    continue;

                for (int i = 0; i < recipe.requiredItem.Count; i++)
                {
                    Item req = recipe.requiredItem[i];
                    if (req == null || req.IsAir)
                        continue;

                    int gid = RecipeAnalyzer.GetAcceptedGroupId(recipe, i);
                    if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
                    {
                        RecipeGroup group = RecipeGroup.recipeGroups[gid];
                        if (group?.ValidItems != null)
                        {
                            foreach (int groupType in FurnitureRecipeGroupSampling.Sample(
                                         group.ValidItems, targetStyle, MaxGroupExpand))
                            {
                                AddIngredient(seedType, groupType, targetStyle, seedSig, scoreboard, ref bestAnchor, ref bestAnchorScore);
                            }
                        }
                    }

                    AddIngredient(seedType, req.type, targetStyle, seedSig, scoreboard, ref bestAnchor, ref bestAnchorScore);
                }
            }

            PickBestAnchorAmongBlocks(scoreboard, ref bestAnchor, ref bestAnchorScore);
        }

        public static List<int> BuildPickerList(
            int seedType,
            List<(int type, int score)> scoreboard,
            int preferredAnchor)
        {
            var ordered = new List<int>();
            var seen = new HashSet<int>();

            if (preferredAnchor > ItemID.None && seen.Add(preferredAnchor))
                ordered.Add(preferredAnchor);

            scoreboard.Sort((a, b) => FurnitureReverseAnchorResolver.CombineMaterialRankScore(seedType, b.type)
                .CompareTo(FurnitureReverseAnchorResolver.CombineMaterialRankScore(seedType, a.type)));

            foreach ((int type, int score) entry in scoreboard)
            {
                if (!seen.Add(entry.type))
                    continue;

                Item probe = new Item();
                probe.SetDefaults(entry.type);
                bool block = FurnitureMaterialAnchor.IsValidAnchorBlock(probe);
                int station = FurnitureReverseAnchorResolver.ScoreStationMaterialLink(seedType, entry.type);
                bool craftMat = entry.type < ItemID.Sets.IsAMaterial.Length && ItemID.Sets.IsAMaterial[entry.type];

                if (!block && station <= 0 && !craftMat && entry.score <= 0)
                    continue;

                ordered.Add(entry.type);
                if (ordered.Count >= MaxPickerItems)
                    break;
            }

            return ordered;
        }

        public static int PickDefaultPlaceableBlock(int seedType, IReadOnlyList<int> pickerItems)
        {
            if (pickerItems == null || pickerItems.Count == 0)
                return ItemID.None;

            ModItem seedMod = ItemLoader.GetItem(seedType);
            bool seedFromMod = seedMod != null;
            string seedStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            Item seedProbe = new Item();
            FurnitureItemDefaults.TrySetDefaults(seedProbe, seedType);
            string seedDisplay = (seedProbe.Name ?? "").ToLowerInvariant();
            bool modLineageSet = FurnitureSetMaterialRules.UsesModLineageAnchor(seedType);
            bool nothingnessSet = modLineageSet && seedDisplay.Contains("无");

            int best = ItemID.None;
            int bestRank = int.MinValue;
            foreach (int type in pickerItems)
            {
                Item probe = new Item();
                if (!FurnitureItemDefaults.TrySetDefaults(probe, type))
                    continue;
                if (!FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
                    continue;

                if (seedFromMod && RecipeAnalyzer.IsHighFanoutMaterial(type))
                {
                    ModItem blockMod = ItemLoader.GetItem(type);
                    if (blockMod == null || blockMod.Mod.Name == "Terraria")
                        continue;
                }

                int rank = FurnitureReverseAnchorResolver.CombineMaterialRankScore(seedType, type);
                string blockStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(type);
                if (!string.IsNullOrWhiteSpace(seedStyle)
                    && (FurnitureStyleSignature.StyleKeyFuzzyMatch(seedStyle, blockStyle)
                        || FurnitureMaterialKeyNormalizer.SameMaterialFamily(seedStyle, blockStyle)))
                    rank += 6_000;

                if (modLineageSet)
                {
                    if (FurnitureSetMaterialRules.IsForbiddenGenericMaterial(type, seedType))
                        continue;

                    string blockDisplay = (probe.Name ?? "").ToLowerInvariant();
                    string seedMoniker = FurnitureSetRecognizer.ExtractDisplayLineageMoniker(seedType).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(seedMoniker)
                        && (blockDisplay.Contains(seedMoniker)
                            || FurnitureStyleSignature.StyleKeyFuzzyMatch(seedStyle, blockStyle)))
                        rank += 10_000;
                    else if (nothingnessSet
                        && (blockDisplay.Contains("无")
                            || (blockStyle?.IndexOf("nothing", System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0))
                        rank += 10_000;
                }

                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = type;
                }
            }

            return best;
        }

        private static void AddIngredient(
            int seedType,
            int ingredientType,
            string targetStyle,
            FurnitureStyleSignature seedSig,
            List<(int type, int score)> scoreboard,
            ref int bestAnchor,
            ref int bestAnchorScore)
        {
            if (ingredientType <= ItemID.None)
                return;

            Item probe = new Item();
            probe.SetDefaults(ingredientType);
            int score = FurnitureReverseAnchorResolver.ScoreIngredientNameFit(ingredientType, targetStyle, seedSig, probe);
            score += FurnitureReverseAnchorResolver.ScoreStationMaterialLink(seedType, ingredientType) * 2;

            if (score <= 0 && FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
            {
                ModItem seedMod = ItemLoader.GetItem(seedType);
                ModItem ingMod = ItemLoader.GetItem(ingredientType);
                if (seedMod != null && ingMod != null && seedMod.Mod.Name == ingMod.Mod.Name)
                    score = 2_000;
            }

            if (score <= 0 && ingredientType < ItemID.Sets.IsAMaterial.Length && ItemID.Sets.IsAMaterial[ingredientType])
                score = 400;

            if (score <= 0)
                return;

            int existing = scoreboard.FindIndex(e => e.type == ingredientType);
            if (existing >= 0)
            {
                if (score > scoreboard[existing].score)
                    scoreboard[existing] = (ingredientType, score);
            }
            else
            {
                scoreboard.Add((ingredientType, score));
            }
        }

        private static void PickBestAnchorAmongBlocks(
            List<(int type, int score)> scoreboard,
            ref int bestAnchor,
            ref int bestAnchorScore)
        {
            bestAnchor = ItemID.None;
            bestAnchorScore = int.MinValue;

            foreach ((int type, int score) entry in scoreboard)
            {
                Item probe = new Item();
                probe.SetDefaults(entry.type);
                if (!FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
                    continue;

                if (entry.score > bestAnchorScore)
                {
                    bestAnchorScore = entry.score;
                    bestAnchor = entry.type;
                }
            }
        }
    }
}
