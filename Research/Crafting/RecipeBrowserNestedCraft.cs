using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research.Players;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
    /// <summary>
    /// 旅途绿脸：查询物必须已完全研究；列举以它为原料（含嵌套中间产物）、
    /// 其余旅途材料均已研究、配方环境可用、且产物尚未研究满的条目。
    /// </summary>
    internal static class RecipeBrowserNestedCraft
    {
        private const int MaterialChainMaxDepth = 48;

        private static HashSet<int> _shimmerBlockedTypes;
        private static bool _shimmerBlockCacheValid;

        internal static void InvalidateCaches() => InvalidateShimmerBlockCache();

        internal static void InvalidateShimmerBlockCache() => _shimmerBlockCacheValid = false;

        public static List<int> GetDeepCraftableProductsForGreenFace(int seedType)
        {
            if (seedType <= ItemID.None)
                return new List<int>();

            // 绿脸前提：槽位查询物已研究满（未研究由 UI 自动切蓝脸）
            if (!RecipeAnalyzer.IsFullyResearched(seedType))
                return new List<int>();

            var products = new HashSet<int>();
            var researchMemo = new Dictionary<int, bool>();
            var researchVisiting = new HashSet<int>();
            var seedMemo = new Dictionary<int, bool>();
            var seedVisiting = new HashSet<int>();

            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                int productType = recipe.createItem.type;
                if (products.Contains(productType))
                    continue;
                if (!IsGreenFaceProduct(productType))
                    continue;
                if (!IsGreenFaceRecipeValid(recipe, seedType))
                    continue;
                if (!RecipeIngredientsResearchComplete(recipe, researchMemo, researchVisiting, 0))
                    continue;
                if (!RecipeDependsOnSeed(recipe, seedType, seedMemo, seedVisiting, 0))
                    continue;

                products.Add(productType);
            }

            return products.OrderBy(t => t).ToList();
        }

        public static List<Recipe> GetQualifyingRecipesForGreenFace(int productType, int seedType)
        {
            var list = new List<Recipe>();
            if (seedType <= ItemID.None || !RecipeAnalyzer.IsFullyResearched(seedType))
                return list;

            var researchMemo = new Dictionary<int, bool>();
            var researchVisiting = new HashSet<int>();
            var seedMemo = new Dictionary<int, bool>();
            var seedVisiting = new HashSet<int>();

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                if (!IsGreenFaceRecipeValid(recipe, seedType))
                    continue;
                if (!RecipeIngredientsResearchComplete(recipe, researchMemo, researchVisiting, 0))
                    continue;
                if (!RecipeDependsOnSeed(recipe, seedType, seedMemo, seedVisiting, 0))
                    continue;
                list.Add(recipe);
            }

            return list;
        }

        public static bool IsGreenFaceProduct(int productType) =>
            RecipeAnalyzer.TryGetJourneyUnlockQuota(productType, out int quota)
            && quota > 0
            && !RecipeAnalyzer.IsFullyResearched(productType);

        /// <summary>参与合成的旅途材料均已研究（或不在旅途目录）；中间产物可未研究但须能由已研究材料合成。</summary>
        private static bool RecipeIngredientsResearchComplete(
            Recipe recipe,
            Dictionary<int, bool> memo,
            HashSet<int> visiting,
            int depth)
        {
            if (depth > MaterialChainMaxDepth)
                return false;
            if (!IsGreenFaceRecipeValid(recipe))
                return false;

            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;
                if (!IsMaterialResearchComplete(req.type, recipe, i, visiting, memo, depth + 1))
                    return false;
            }

            return true;
        }

        private static bool IsMaterialResearchComplete(
            int itemType,
            Recipe parentRecipe,
            int slotIndex,
            HashSet<int> visiting,
            Dictionary<int, bool> memo,
            int depth)
        {
            if (itemType <= ItemID.None)
                return true;
            if (depth > MaterialChainMaxDepth)
                return false;

            if (memo.TryGetValue(itemType, out bool cached))
                return cached;

            int gid = slotIndex < parentRecipe.acceptedGroups.Count ? parentRecipe.acceptedGroups[slotIndex] : -1;
            if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
            {
                RecipeGroup group = RecipeGroup.recipeGroups[gid];
                if (group != null && group.ContainsItem(itemType))
                {
                    bool anyOk = false;
                    foreach (int valid in group.ValidItems)
                    {
                        if (IsMaterialResearchComplete(valid, parentRecipe, slotIndex, visiting, memo, depth))
                        {
                            anyOk = true;
                            break;
                        }
                    }
                    memo[itemType] = anyOk;
                    return anyOk;
                }
            }

            if (!RecipeAnalyzer.TryGetJourneyUnlockQuota(itemType, out _))
            {
                memo[itemType] = true;
                return true;
            }

            if (RecipeAnalyzer.IsFullyResearched(itemType))
            {
                memo[itemType] = true;
                return true;
            }

            if (visiting.Contains(itemType))
                return false;

            visiting.Add(itemType);
            bool ok = false;
            foreach (Recipe producer in RecipeAnalyzer.GetRecipesForItem(itemType))
            {
                if (!IsGreenFaceRecipeValid(producer))
                    continue;
                if (RecipeIngredientsResearchComplete(producer, memo, visiting, depth + 1))
                {
                    ok = true;
                    break;
                }
            }

            visiting.Remove(itemType);
            memo[itemType] = ok;
            return ok;
        }

        /// <summary>配方链上至少一处依赖查询物（已研究物本身或以其为原料的未研究中间产物）。</summary>
        private static bool RecipeDependsOnSeed(
            Recipe recipe,
            int seedType,
            Dictionary<int, bool> memo,
            HashSet<int> visiting,
            int depth)
        {
            if (depth > MaterialChainMaxDepth)
                return false;

            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;
                if (IngredientDependsOnSeed(req.type, recipe, i, seedType, memo, visiting, depth + 1))
                    return true;
            }

            return false;
        }

        private static bool IngredientDependsOnSeed(
            int itemType,
            Recipe parentRecipe,
            int slotIndex,
            int seedType,
            Dictionary<int, bool> memo,
            HashSet<int> visiting,
            int depth)
        {
            if (itemType == seedType)
                return true;
            if (depth > MaterialChainMaxDepth)
                return false;

            int gid = slotIndex < parentRecipe.acceptedGroups.Count ? parentRecipe.acceptedGroups[slotIndex] : -1;
            if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
            {
                RecipeGroup group = RecipeGroup.recipeGroups[gid];
                if (group != null && group.ContainsItem(itemType) && group.ContainsItem(seedType))
                    return true;
            }

            // 已研究的其它材料不算「以查询物为原料」
            if (RecipeAnalyzer.IsFullyResearched(itemType))
                return false;

            if (memo.TryGetValue(itemType, out bool cached))
                return cached;

            if (!RecipeAnalyzer.TryGetJourneyUnlockQuota(itemType, out _))
            {
                memo[itemType] = false;
                return false;
            }

            if (visiting.Contains(itemType))
                return false;

            visiting.Add(itemType);
            bool ok = false;
            foreach (Recipe producer in RecipeAnalyzer.GetRecipesForItem(itemType))
            {
                if (RecipeDependsOnSeed(producer, seedType, memo, visiting, depth + 1))
                {
                    ok = true;
                    break;
                }
            }

            visiting.Remove(itemType);
            memo[itemType] = ok;
            return ok;
        }

        private static bool IsGreenFaceRecipeValid(Recipe recipe, int seedType = -1)
        {
            if (recipe?.createItem == null || recipe.createItem.IsAir)
                return false;
            if (IsShimmerBlockedProduct(recipe))
                return false;
            if (Main.netMode == NetmodeID.Server)
                return false;
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return false;
            if (!RecipeLoader.RecipeAvailable(recipe))
                return false;
            if (!RequiredCraftingTilesSeen(recipe))
                return false;
            return true;
        }

        private static bool RequiredCraftingTilesSeen(Recipe recipe)
        {
            if (recipe.requiredTile == null || recipe.requiredTile.Count == 0)
                return true;

            bool[] seen = ResearchCraftingPlayer.SeenTiles;
            if (seen == null)
                return true;

            foreach (int tileId in recipe.requiredTile)
            {
                if (tileId < 0)
                    continue;
                if (tileId < seen.Length && seen[tileId])
                    return true;
            }

            return false;
        }

        private static bool IsShimmerBlockedProduct(Recipe recipe)
        {
            if (recipe?.createItem == null)
                return true;
            if (IsShimmerDecraftRecipe(recipe))
                return true;
            return IsItemShimmerBlocked(recipe.createItem.type);
        }

        internal static bool IsItemShimmerBlocked(int itemType)
        {
            if (itemType <= ItemID.None || ResearchCraftingPlayer.HasEncounteredShimmer)
                return false;
            EnsureShimmerBlockedTypes();
            return _shimmerBlockedTypes.Contains(itemType);
        }

        internal static bool IsShimmerDecraftRecipe(Recipe recipe)
        {
            if (recipe?.createItem == null || recipe.createItem.IsAir)
                return false;
            int product = recipe.createItem.type;
            int idx = ShimmerTransforms.GetDecraftingRecipeIndex(product);
            return idx >= 0 && idx < Recipe.numRecipes && Main.recipe[idx] == recipe;
        }

        private static void EnsureShimmerBlockedTypes()
        {
            if (_shimmerBlockCacheValid)
                return;

            _shimmerBlockedTypes = new HashSet<int>();
            _shimmerBlockCacheValid = true;
            if (ResearchCraftingPlayer.HasEncounteredShimmer)
                return;

            for (int i = 1; i < ItemID.Sets.ShimmerTransformToItem.Length; i++)
            {
                int to = ItemID.Sets.ShimmerTransformToItem[i];
                if (to > ItemID.None)
                    _shimmerBlockedTypes.Add(to);
            }

            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe r = Main.recipe[i];
                if (IsShimmerDecraftRecipe(r))
                    _shimmerBlockedTypes.Add(r.createItem.type);
            }

            bool changed = true;
            int guard = 0;
            while (changed && guard++ < ItemLoader.ItemCount)
            {
                changed = false;
                for (int i = 0; i < Recipe.numRecipes; i++)
                {
                    Recipe r = Main.recipe[i];
                    if (r?.createItem == null || r.createItem.IsAir)
                        continue;
                    int prod = r.createItem.type;
                    if (_shimmerBlockedTypes.Contains(prod) || !RecipeUsesOnlyBlockedMaterials(r))
                        continue;
                    if (_shimmerBlockedTypes.Add(prod))
                        changed = true;
                }
            }
        }

        private static bool RecipeUsesOnlyBlockedMaterials(Recipe recipe)
        {
            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;

                bool matchedGroup = false;
                foreach (int gid in recipe.acceptedGroups)
                {
                    if (gid < 0 || gid >= RecipeGroup.recipeGroups.Count)
                        continue;
                    RecipeGroup g = RecipeGroup.recipeGroups[gid];
                    if (g == null || req.type != g.IconicItemId)
                        continue;
                    matchedGroup = true;
                    if (g.ValidItems.Any(v => !_shimmerBlockedTypes.Contains(v)))
                        return false;
                    break;
                }

                if (!matchedGroup && !_shimmerBlockedTypes.Contains(req.type))
                    return false;
            }

            return true;
        }
    }
}
