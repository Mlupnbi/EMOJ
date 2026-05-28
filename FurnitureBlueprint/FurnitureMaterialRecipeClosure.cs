using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.ItemHub.Rules;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 以材料块为根的正向配方闭包：中间物（锭等）由材料块造出，其下游家具仍算「材料块相关产物」。
    /// </summary>
    public static class FurnitureMaterialRecipeClosure
    {
        public const int MaxDepth = 4;
        public const int MaxIngredientNodes = 96;
        public const int MaxProducts = 320;

        public static void AddTransitiveProducts(
            int materialBlock,
            string modKey,
            FurnitureStyleSignature blockSig,
            HashSet<int> dest)
        {
            if (dest == null || materialBlock <= ItemID.None)
                return;

            var ingredientFrontier = new Queue<(int type, int depth)>();
            var seenIngredients = new HashSet<int> { materialBlock };
            ingredientFrontier.Enqueue((materialBlock, 0));
            int ingredientNodes = 0;
            int added = 0;

            while (ingredientFrontier.Count > 0 && ingredientNodes < MaxIngredientNodes && dest.Count < MaxProducts)
            {
                (int ing, int depth) = ingredientFrontier.Dequeue();
                ingredientNodes++;

                if (depth >= MaxDepth)
                    continue;

                foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(ing))
                {
                    if (recipe?.createItem == null || recipe.createItem.IsAir)
                        continue;

                    int product = recipe.createItem.type;
                    if (GetModKey(product) != modKey)
                        continue;

                    if (!dest.Add(product))
                        continue;

                    added++;
                    if (depth + 1 < MaxDepth && ShouldContinueAsIngredient(product, materialBlock, blockSig))
                    {
                        if (seenIngredients.Add(product))
                            ingredientFrontier.Enqueue((product, depth + 1));
                    }
                }
            }

            if (added > 0)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"material closure block={materialBlock} added={added} total={dest.Count}");
            }
        }

        private static bool ProductTiedToMaterialStyle(
            int product,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            Recipe recipe,
            int immediateIngredient)
        {
            if (RecipeAnalyzer.RecipeUsesExactIngredient(recipe, materialBlock))
                return true;

            if (immediateIngredient == materialBlock)
                return true;

            if (FurnitureRecipeSetLinker.ProductBelongsToMaterialStyle(product, materialBlock, blockSig))
                return true;

            Item probe = new Item();
            probe.SetDefaults(product);
            if (FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
                return FurnitureStyleSignature.StyleKeyFuzzyMatch(
                    blockSig.StyleKey, FurnitureSetRecognizer.ExtractStyleKeyPublic(product));

            return FurnitureStyleSignature.StyleKeyFuzzyMatch(
                       blockSig.StyleKey, FurnitureSetRecognizer.ExtractStyleKeyPublic(product))
                   || FurnitureMaterialKeyNormalizer.SameMaterialFamily(
                       blockSig.StyleKey, FurnitureSetRecognizer.ExtractStyleKeyPublic(product));
        }

        private static bool ShouldContinueAsIngredient(int product, int materialBlock, FurnitureStyleSignature blockSig)
        {
            Item probe = new Item();
            probe.SetDefaults(product);
            if (probe.IsAir)
                return false;

            if (FurnitureCandidateFilter.IsPlaceableFurnitureItem(probe))
                return false;

            if (product < ItemID.Sets.IsAMaterial.Length && ItemID.Sets.IsAMaterial[product])
                return true;

            if (HubCollectibleRules.IsMaterial(probe))
                return true;

            if (FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
                return true;

            string blockKey = blockSig.StyleKey?.Trim() ?? FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
            string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(product);
            return FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, productKey)
                || FurnitureMaterialKeyNormalizer.SameMaterialFamily(blockKey, productKey);
        }

        private static string GetModKey(int itemType)
        {
            ModItem mi = ItemLoader.GetItem(itemType);
            return mi == null ? "Terraria" : mi.Mod.Name;
        }
    }
}
