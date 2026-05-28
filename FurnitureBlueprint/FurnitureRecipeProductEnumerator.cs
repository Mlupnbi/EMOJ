using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>安全遍历「消耗材料」的产物：先精确配料，再限量配方组（防石类/木类爆栈卡死）。</summary>
    public static class FurnitureRecipeProductEnumerator
    {
        public const int DefaultMaxGroupProducts = 96;

        public static IEnumerable<int> EnumerateProducts(
            int materialType,
            int maxGroupProducts = DefaultMaxGroupProducts,
            FurnitureCraftStationProfile stationProfile = null)
        {
            if (materialType <= ItemID.None)
                yield break;

            var seen = new HashSet<int>();
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(materialType))
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                int product = recipe.createItem.type;
                if (!RecipePassesStation(recipe, stationProfile, product, materialType))
                    continue;

                if (!RecipeAnalyzer.RecipeUsesExactIngredient(recipe, materialType))
                    continue;

                if (seen.Add(product))
                    yield return product;
            }

            int groupAdded = 0;
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(materialType))
            {
                if (groupAdded >= maxGroupProducts)
                    yield break;

                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                int product = recipe.createItem.type;
                if (!RecipePassesStation(recipe, stationProfile, product, materialType))
                    continue;

                if (RecipeAnalyzer.RecipeUsesExactIngredient(recipe, materialType))
                    continue;

                if (!seen.Add(product))
                    continue;

                groupAdded++;
                yield return product;
            }
        }

        private static bool RecipePassesStation(
            Recipe recipe,
            FurnitureCraftStationProfile stationProfile,
            int productType = ItemID.None,
            int materialBlock = ItemID.None)
        {
            if (stationProfile == null || !stationProfile.IsConstrained)
                return true;

            if (productType > ItemID.None
                && FurnitureCraftStationRules.IsStationCollectionExempt(productType, materialBlock))
                return true;

            return stationProfile.RecipeCompatible(recipe);
        }

        public static void AddExactProducts(int materialType, HashSet<int> dest, int maxProducts = 192)
        {
            if (dest == null || materialType <= ItemID.None)
                return;

            foreach (int product in RecipeAnalyzer.GetProductTypesUsingExactMaterial(materialType, maxProducts))
                dest.Add(product);
        }

        public static void AddStyledGroupProducts(
            int materialType,
            FurnitureStyleSignature materialSig,
            string modKey,
            HashSet<int> dest,
            int maxGroupProducts = DefaultMaxGroupProducts,
            bool allowPlacementLine = false,
            FurnitureCraftStationProfile stationProfile = null)
        {
            if (dest == null || materialType <= ItemID.None || maxGroupProducts <= 0)
                return;

            string materialKey = materialSig.StyleKey;
            if (string.IsNullOrWhiteSpace(materialKey))
                materialKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialType);

            int added = 0;
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(materialType))
            {
                if (added >= maxGroupProducts)
                    break;

                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                int product = recipe.createItem.type;
                if (!RecipePassesStation(recipe, stationProfile, product, materialType))
                    continue;

                if (RecipeAnalyzer.RecipeUsesExactIngredient(recipe, materialType))
                    continue;

                if (dest.Contains(product))
                    continue;

                if (GetModKey(product) != modKey)
                    continue;

                if (FurnitureRecipeSetLinker.ProductBelongsToMaterialStyle(product, materialType, materialSig))
                {
                    dest.Add(product);
                    added++;
                    continue;
                }

                if (allowPlacementLine && materialSig.SeedIsMaterialBlock
                    && FurnitureRecipeSetLinker.ProductMatchesPlacementLine(product, materialType, materialSig))
                {
                    dest.Add(product);
                    added++;
                }
            }
        }

        private static string GetModKey(int itemType)
        {
            ModItem mi = ItemLoader.GetItem(itemType);
            return mi == null ? "Terraria" : mi.Mod.Name;
        }
    }
}
