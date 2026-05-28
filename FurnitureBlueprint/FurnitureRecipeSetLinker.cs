using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public static class FurnitureRecipeSetLinker
    {
        /// <summary>
        /// 正推（方块种子）：查「消耗该方块/材料」时造出的物品（墙、家具、平台等）。
        /// 倒推锚点见 <see cref="FurnitureReverseAnchorResolver"/>，此处不用「合成 seed 的原料」扩套。
        /// </summary>
        public static void AddSeedProducts(int seedType, HashSet<int> dest, FurnitureCraftStationProfile stationProfile)
        {
            if (dest == null || seedType <= ItemID.None)
                return;

            FurnitureStyleSignature sig = FurnitureStyleSignature.FromItemTypeForRecipes(seedType);
            string modKey = GetModKey(seedType);

            foreach (int product in FurnitureRecipeProductEnumerator.EnumerateProducts(seedType, stationProfile: stationProfile))
            {
                if (GetModKey(product) == modKey)
                    dest.Add(product);
            }

            FurnitureRecipeProductEnumerator.AddStyledGroupProducts(
                seedType, sig, modKey, dest, allowPlacementLine: sig.SeedIsMaterialBlock, stationProfile: stationProfile);
        }

        /// <summary>锚点方块/材料的产物（种子是椅子等时，先解析出对应木块再扩产物）。</summary>
        public static void AddProductsConsumingMaterial(
            int materialType,
            string modKey,
            HashSet<int> dest,
            FurnitureCraftStationProfile stationProfile)
        {
            if (dest == null || materialType <= ItemID.None)
                return;

            FurnitureStyleSignature sig = FurnitureStyleSignature.FromItemTypeForRecipes(materialType);

            foreach (int product in FurnitureRecipeProductEnumerator.EnumerateProducts(materialType, stationProfile: stationProfile))
            {
                if (GetModKey(product) == modKey)
                    dest.Add(product);
            }

            FurnitureRecipeProductEnumerator.AddStyledGroupProducts(
                materialType, sig, modKey, dest, allowPlacementLine: true, stationProfile: stationProfile);
        }

        public static void AddRecipeLinkedProducts(
            int seedType,
            HashSet<int> dest,
            FurnitureCraftStationProfile stationProfile = null)
        {
            if (dest == null || seedType <= ItemID.None)
                return;

            stationProfile ??= FurnitureCraftStationProfile.FromSeed(seedType);

            Item seed = new Item();
            seed.SetDefaults(seedType);

            if (FurnitureMaterialAnchor.IsValidAnchorBlock(seed))
            {
                AddSeedProducts(seedType, dest, stationProfile);
                return;
            }

            dest.Add(seedType);

            FurnitureStyleSignature seedSig = FurnitureStyleSignature.FromItemType(seedType);
            string modKey = GetModKey(seedType);
            int anchor = FurnitureReverseAnchorResolver.ResolveAnchorFromSeed(seedType, seedSig);

            if (anchor > ItemID.None && anchor != seedType)
                AddProductsConsumingMaterial(anchor, modKey, dest, stationProfile);
        }

        /// <summary>候选物品的配方是否消耗 material（产物方向，含配方组）。</summary>
        public static bool ProductUsesMaterial(int productType, int materialType)
        {
            if (productType <= ItemID.None || materialType <= ItemID.None)
                return false;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                if (RecipeAnalyzer.RecipeUsesIngredient(recipe, materialType))
                    return true;
            }
            return false;
        }

        /// <summary>产物配方是否精确消耗 material（不含仅通过 RecipeGroup 匹配）。</summary>
        public static bool ProductUsesExactMaterial(int productType, int materialType)
        {
            if (productType <= ItemID.None || materialType <= ItemID.None)
                return false;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                if (RecipeAnalyzer.RecipeUsesExactIngredient(recipe, materialType))
                    return true;
            }
            return false;
        }

        /// <summary>通过配方组关联的产物：名称 StyleKey 须与材料种子一致。</summary>
        public static bool ProductBelongsToMaterialStyle(int productType, int materialType, FurnitureStyleSignature materialSig)
        {
            if (productType <= ItemID.None || materialType <= ItemID.None)
                return false;

            if (ProductUsesExactMaterial(productType, materialType))
                return true;

            if (!ProductUsesMaterial(productType, materialType))
                return false;

            string materialKey = materialSig.StyleKey;
            if (string.IsNullOrWhiteSpace(materialKey))
                materialKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialType);

            if (string.IsNullOrWhiteSpace(materialKey))
                return false;

            string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(productType);
            return FurnitureStyleSignature.StyleKeySameMaterialFamily(materialKey.Trim(), productKey)
                || FurnitureStyleSignature.StyleKeyFuzzyMatch(materialKey.Trim(), productKey);
        }

        /// <summary>木组等：产物消耗该方块且与材料同 placeStyle 线、同模组。</summary>
        public static bool ProductMatchesPlacementLine(int productType, int materialType, FurnitureStyleSignature materialSig)
        {
            if (!materialSig.UsesPlacementStyleLine || materialSig.PlacementTile < TileID.Dirt)
                return false;

            if (!ProductUsesMaterial(productType, materialType))
                return false;

            FurnitureStyleSignature productSig = FurnitureStyleSignature.FromItemType(productType);
            return productSig.PlacementTile == materialSig.PlacementTile
                && productSig.PlacementStyle == materialSig.PlacementStyle
                && productSig.ModKey == materialSig.ModKey;
        }

        private static string GetModKey(int itemType)
        {
            ModItem mi = ItemLoader.GetItem(itemType);
            return mi == null ? "Terraria" : mi.Mod.Name;
        }
    }
}
