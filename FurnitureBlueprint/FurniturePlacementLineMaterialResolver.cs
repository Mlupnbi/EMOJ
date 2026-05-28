using System.Collections.Generic;

using Terraria;

using Terraria.ID;

using Terraria.ModLoader;

using EvenMoreOverpoweredJourney.Research;



namespace EvenMoreOverpoweredJourney.FurnitureBlueprint

{

    /// <summary>

    /// 模组家具常无标准英文名 StyleKey：从 (tile, placeStyle) 图格线上的兄弟物品配方倒推材料方块。

    /// </summary>

    public static class FurniturePlacementLineMaterialResolver

    {

        public const int MaxSiblings = 96;



        public static int TryResolveBlockFromFurnitureSeed(int seedType)

        {

            if (seedType <= ItemID.None)

                return ItemID.None;



            var scoreboard = new List<(int type, int score)>();

            FurnitureStyleSignature sig = FurnitureStyleSignature.FromItemType(seedType);

            CollectBlockCandidatesFromPlacementLine(seedType, sig, scoreboard);

            if (scoreboard.Count == 0)

                return ItemID.None;



            scoreboard.Sort((a, b) => FurnitureReverseAnchorResolver.CombineMaterialRankScore(seedType, b.type)
                .CompareTo(FurnitureReverseAnchorResolver.CombineMaterialRankScore(seedType, a.type)));

            int best = scoreboard[0].type;

            FurnitureBlueprintLog.InfoFull(

                $"placement line block seed={seedType} block={best} score={scoreboard[0].score} candidates={scoreboard.Count}");

            return best;

        }



        public static void CollectBlockCandidatesFromPlacementLine(

            int seedType,

            FurnitureStyleSignature sig,

            List<(int type, int score)> scoreboard)

        {

            if (seedType <= ItemID.None || scoreboard == null)

                return;



            Item seed = new Item();

            seed.SetDefaults(seedType);

            if (seed.createTile < TileID.Dirt)

                return;



            var siblings = new HashSet<int> { seedType };

            FurnitureTileSlotRegistry.AddPlacementLineSiblings(

                seed.createTile, seed.placeStyle, sig.ModKey, sig.StyleKey, siblings, MaxSiblings);

            if (seed.createTile >= TileID.Count)

            {

                FurnitureTileSlotRegistry.AddAllItemsOnModTile(

                    seed.createTile, sig.ModKey, sig.StyleKey, siblings, MaxSiblings);

            }



            var seenBlocks = new HashSet<int>();



            foreach (int sibling in siblings)

            {

                foreach (Recipe recipe in FurnitureRecipeLookup.GetRecipesCreating(sibling))

                {

                    if (recipe?.requiredItem == null)

                        continue;



                    for (int i = 0; i < recipe.requiredItem.Count; i++)

                    {

                        Item req = recipe.requiredItem[i];

                        if (req == null || req.IsAir)

                            continue;



                        MergeBlockScore(seedType, sig, req.type, scoreboard, seenBlocks);



                        int gid = RecipeAnalyzer.GetAcceptedGroupId(recipe, i);

                        if (gid < 0 || gid >= RecipeGroup.recipeGroups.Count)

                            continue;



                        RecipeGroup group = RecipeGroup.recipeGroups[gid];

                        if (group?.ValidItems == null)

                            continue;



                        foreach (int groupType in group.ValidItems)

                            MergeBlockScore(seedType, sig, groupType, scoreboard, seenBlocks);

                    }

                }

            }

        }



        private static void MergeBlockScore(

            int seedType,

            FurnitureStyleSignature sig,

            int ingredientType,

            List<(int type, int score)> scoreboard,

            HashSet<int> seenBlocks)

        {

            if (ingredientType <= ItemID.None || !seenBlocks.Add(ingredientType))

                return;



            Item probe = new Item();

            probe.SetDefaults(ingredientType);

            if (!FurnitureMaterialAnchor.IsValidAnchorBlock(probe))

                return;



            int score = ScoreBlockForSeed(seedType, sig, ingredientType, probe);

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



        private static int ScoreBlockForSeed(

            int seedType,

            FurnitureStyleSignature sig,

            int blockType,

            Item blockItem)

        {

            string seedKey = sig.StyleKey?.Trim() ?? FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);

            int score = FurnitureReverseAnchorResolver.ScoreIngredientNameFit(

                blockType, seedKey, sig, blockItem);



            if (score <= 0)

            {

                ModItem seedMod = ItemLoader.GetItem(seedType);

                ModItem blockMod = ItemLoader.GetItem(blockType);

                if (seedMod != null && blockMod != null && seedMod.Mod.Name == blockMod.Mod.Name)

                    score = 2_500;

            }



            if (FurnitureMaterialAnchor.IsValidAnchorBlock(blockItem))

                score += 600;

            score += FurnitureReverseAnchorResolver.ScoreStationMaterialLink(seedType, blockType) * 150;

            return score;

        }

    }

}


