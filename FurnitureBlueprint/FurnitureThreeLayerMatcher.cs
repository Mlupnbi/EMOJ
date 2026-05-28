using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// Gemini 三层：① placeStyle 线 ② StyleKey ③ 锚点材料（精确配料优先）。
    /// 仅在「产物闭包」之后对已有候选调用，避免全物品扫描。
    /// </summary>
    public static class FurnitureThreeLayerMatcher
    {
        public static bool PassesAnyLayer(int seedType, int candidateType, FurnitureStyleSignature seedSig, int anchorMaterial)
        {
            if (candidateType <= ItemID.None)
                return false;

            if (candidateType == seedType)
                return true;

            Item probe = new Item();
            probe.SetDefaults(candidateType);
            if (probe.createWall > WallID.None && probe.createTile < TileID.Dirt
                && anchorMaterial > ItemID.None
                && FurnitureWallResolver.IsWallPaperForBlock(anchorMaterial, candidateType))
                return true;

            if (FurnitureRecipeSetLinker.ProductUsesExactMaterial(candidateType, seedType))
                return true;

            if (anchorMaterial > ItemID.None
                && FurnitureRecipeSetLinker.ProductUsesExactMaterial(candidateType, anchorMaterial))
                return true;

            if (PassesLayer1PlaceStyleLine(seedSig, candidateType) && PassesLayer2StyleKey(seedSig, candidateType))
                return true;

            if (PassesLayer2StyleKey(seedSig, candidateType))
                return true;

            if (PassesLayer3AnchorMaterialExact(candidateType, anchorMaterial))
                return true;

            if (anchorMaterial > ItemID.None
                && FurnitureRecipeSetLinker.ProductBelongsToMaterialStyle(candidateType, anchorMaterial, seedSig))
                return true;

            return false;
        }

        public static void FilterProductSet(
            int seedType,
            FurnitureStyleSignature seedSig,
            int anchorMaterial,
            HashSet<int> products,
            FurnitureCraftStationProfile stationProfile = null)
        {
            if (products == null || products.Count == 0)
                return;

            var remove = new List<int>();
            foreach (int type in products)
            {
                if (!PassesAnyLayer(seedType, type, seedSig, anchorMaterial))
                    remove.Add(type);
                else if (!ProductHasCompatibleCraftStation(type, seedType, stationProfile, seedSig, anchorMaterial))
                    remove.Add(type);
            }

            for (int i = 0; i < remove.Count; i++)
                products.Remove(remove[i]);
        }

        private static bool ProductHasCompatibleCraftStation(
            int productType,
            int seedType,
            FurnitureCraftStationProfile stationProfile,
            FurnitureStyleSignature seedSig,
            int anchorMaterial = ItemID.None)
        {
            if (stationProfile == null || !stationProfile.IsConstrained)
                return true;

            if (FurnitureCraftStationRules.IsStationCollectionExempt(productType, anchorMaterial))
                return true;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                if (stationProfile.RecipeCompatible(recipe))
                    return true;
            }

            if (seedType > ItemID.None
                && FurnitureCraftStationRules.ShouldExcludeFromProductCollect(
                    productType, stationProfile, seedType, anchorMaterial))
                return false;

            if (PassesLayer2StyleKey(seedSig, productType))
                return true;

            string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(productType);
            if (!string.IsNullOrWhiteSpace(seedSig.StyleKey)
                && FurnitureMaterialKeyNormalizer.SameMaterialFamily(seedSig.StyleKey, productKey))
                return true;

            return false;
        }

        private static bool PassesLayer1PlaceStyleLine(FurnitureStyleSignature seed, int candidateType)
        {
            if (!seed.UsesPlacementStyleLine || seed.PlacementTile < TileID.Dirt)
                return false;

            Item probe = new Item();
            probe.SetDefaults(candidateType);
            return probe.createTile == seed.PlacementTile && probe.placeStyle == seed.PlacementStyle;
        }

        private static bool PassesLayer2StyleKey(FurnitureStyleSignature seed, int candidateType)
        {
            ModItem mi = ItemLoader.GetItem(candidateType);
            string modKey = mi == null ? "Terraria" : mi.Mod.Name;
            if (modKey != seed.ModKey)
                return false;
            if (string.IsNullOrWhiteSpace(seed.StyleKey))
                return false;

            string otherKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(candidateType);
            return FurnitureStyleSignature.StyleKeyFuzzyMatch(seed.StyleKey, otherKey);
        }

        private static bool PassesLayer3AnchorMaterialExact(int candidateType, int anchorMaterial)
        {
            if (anchorMaterial <= ItemID.None)
                return false;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(candidateType))
            {
                if (RecipeAnalyzer.RecipeUsesExactIngredient(recipe, anchorMaterial))
                    return true;
            }

            return false;
        }
    }
}
