using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>从方块/材料解析配套墙（精确配料 + 限量配方组 + 名称，禁止全物品扫描）。</summary>
    public static class FurnitureWallResolver
    {
        private const int MaxWallCandidates = 48;

        public static bool TryResolveWallFromBlock(int blockItemType, FurnitureStyleSignature signature, out int wallItemType)
        {
            wallItemType = ItemID.None;
            if (blockItemType <= ItemID.None)
                return false;

            Item block = new Item();
            block.SetDefaults(blockItemType);
            if (block.createTile < TileID.Dirt)
                return false;

            string blockKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(blockItemType).Trim();
            int bestScore = int.MinValue;
            int best = ItemID.None;
            int checkedCount = 0;

            foreach (int product in FurnitureRecipeProductEnumerator.EnumerateProducts(blockItemType, MaxWallCandidates * 2))
            {
                if (checkedCount++ > MaxWallCandidates * 3)
                    break;

                if (!TryScoreWall(product, blockItemType, blockKey, signature, out int score))
                    continue;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = product;
                }
            }

            wallItemType = best;
            if (best > ItemID.None)
                FurnitureBlueprintLog.InfoFull($"wall resolved block={blockItemType} wall={best} score={bestScore}");
            return best > ItemID.None;
        }

        public static bool IsWallPaperForBlock(int blockItemType, int wallItemType)
        {
            if (blockItemType <= ItemID.None || wallItemType <= ItemID.None)
                return false;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(wallItemType))
            {
                if (RecipeAnalyzer.RecipeUsesIngredient(recipe, blockItemType))
                    return true;
            }

            return RecipeUsesExactOnly(wallItemType, blockItemType);
        }

        private static bool TryScoreWall(
            int wallItem,
            int blockItem,
            string blockKey,
            FurnitureStyleSignature signature,
            out int score)
        {
            score = int.MinValue;
            Item probe = new Item();
            probe.SetDefaults(wallItem);
            if (probe.createWall <= WallID.None || probe.createTile >= TileID.Dirt)
                return false;

            string wallKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(wallItem).Trim();
            string wallLower = (probe.Name ?? wallKey).ToLowerInvariant();

            if (IsUnrelatedWallType(wallLower, wallKey, blockKey))
                return false;

            bool recipeLinked = RecipeUsesExactOnly(wallItem, blockItem);
            Recipe primary = GetPrimaryRecipe(wallItem);
            if (primary != null && RecipeAnalyzer.RecipeUsesIngredient(primary, blockItem))
                recipeLinked = true;

            bool styleMatch = !string.IsNullOrEmpty(blockKey)
                && (FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, wallKey)
                    || FurnitureMaterialKeyNormalizer.SameMaterialFamily(blockKey, wallKey)
                    || FurnitureStyleSignature.StyleKeySameMaterialFamily(blockKey, wallKey));

            if (!recipeLinked && !styleMatch)
                return false;

            score = 100;
            if (recipeLinked)
                score += 900;
            if (styleMatch)
                score += 600;

            if (wallLower.Contains("墙") || wallLower.Contains("wall"))
                score += 500;

            Item blockProbe = new Item();
            blockProbe.SetDefaults(blockItem);
            string matToken = FurnitureNameSignals.NormalizeMaterialDisplayName(blockProbe.Name);
            if (!string.IsNullOrEmpty(matToken) && wallLower.Contains(matToken))
                score += FurnitureSlotScoring.MaterialPartNameStrong;

            if (signature.MatchesItem(wallItem)
                || FurnitureStyleSignature.StyleKeySameMaterialFamily(signature.StyleKey, wallKey))
                score += 200;

            ModItem bm = ItemLoader.GetItem(blockItem);
            ModItem wm = ItemLoader.GetItem(wallItem);
            if (bm != null && wm != null && bm.Mod.Name == wm.Mod.Name)
                score += 80;

            return true;
        }

        private static bool IsUnrelatedWallType(string wallLower, string wallKey, string blockKey)
        {
            if (wallLower.Contains("fence") || wallKey.Contains("Fence", System.StringComparison.OrdinalIgnoreCase))
                return true;
            if ((wallLower.Contains("iron") || wallLower.Contains("chain") || wallLower.Contains("bar"))
                && !string.IsNullOrEmpty(blockKey)
                && !wallLower.Contains(blockKey.ToLowerInvariant()))
                return true;

            return false;
        }

        private static Recipe GetPrimaryRecipe(int productType)
        {
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
                return recipe;
            return null;
        }

        private static bool RecipeUsesExactOnly(int wallItem, int blockItem)
        {
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(wallItem))
            {
                if (RecipeAnalyzer.RecipeUsesExactIngredient(recipe, blockItem))
                    return true;
            }
            return false;
        }
    }
}
