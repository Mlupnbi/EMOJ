using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 原版木/死木/珍珠木等：按材料块 StyleKey + 种子制作台筛配方（避免生命木桥接吞掉锯木台套组）。
    /// </summary>
    public static class FurnitureVanillaWoodSetExpander
    {
        public const int MaxScan = 2_500;
        public const int MaxAdd = 120;

        public static void Expand(int seedType, int materialBlock, FurnitureStyleSignature blockSig, HashSet<int> dest)
        {
            if (dest == null || materialBlock <= ItemID.None || seedType <= ItemID.None)
                return;

            if (!FurnitureVanillaLivingWoodBridge.IsRegularVanillaWoodFamily(materialBlock)
                && !FurnitureVanillaLivingWoodBridge.IsLivingWoodFamily(materialBlock))
                return;

            string materialKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
            if (string.IsNullOrWhiteSpace(materialKey))
                return;

            FurnitureCraftStationProfile stations = FurnitureCraftStationProfile.FromSeed(seedType);
            int before = dest.Count;
            int scanned = 0;
            int added = 0;

            foreach (Recipe recipe in Main.recipe)
            {
                if (++scanned > MaxScan || added >= MaxAdd)
                    break;

                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                if (stations.IsConstrained && !stations.RecipeCompatible(recipe))
                    continue;

                if (!RecipeUsesWoodFamilyIngredient(recipe, materialBlock, blockSig, materialKey))
                    continue;

                int product = recipe.createItem.type;
                if (dest.Contains(product))
                    continue;

                Item probe = new Item();
                probe.SetDefaults(product);
                if (!FurnitureCandidateFilter.IsPlaceableFurnitureItem(probe))
                    continue;

                string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(product);
                if (!StyleMatchesWoodSet(materialKey, productKey, materialBlock, product))
                    continue;

                if (dest.Add(product))
                    added++;
            }

            if (added > 0)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"wood set expand seed={seedType} block={materialBlock} key={materialKey} added={added} total={dest.Count} before={before}");
            }
        }

        private static bool RecipeUsesWoodFamilyIngredient(
            Recipe recipe,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            string materialKey)
        {
            if (recipe?.requiredItem == null)
                return false;

            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                if (RecipeAnalyzer.RecipeUsesIngredient(recipe, materialBlock))
                    return true;

                int gid = RecipeAnalyzer.GetAcceptedGroupId(recipe, i);
                if (gid == RecipeGroupID.Wood)
                {
                    if (FurnitureRecipeGroupMaterialBridge.TryResolveGroupIngredient(gid, blockSig, out int concrete)
                        && concrete == materialBlock)
                        return true;

                    if (materialKey.Contains("Living", System.StringComparison.OrdinalIgnoreCase)
                        && FurnitureVanillaLivingWoodBridge.IsLivingWoodFamily(materialBlock))
                        return true;

                    if (!materialKey.Contains("Living", System.StringComparison.OrdinalIgnoreCase)
                        && !FurnitureVanillaLivingWoodBridge.IsLivingWoodFamily(materialBlock))
                        return true;
                }
            }

            return false;
        }

        private static bool StyleMatchesWoodSet(string materialKey, string productKey, int materialBlock, int productType)
        {
            if (FurnitureStyleSignature.StyleKeyFuzzyMatch(materialKey, productKey))
                return true;

            if (FurnitureMaterialKeyNormalizer.SameMaterialFamily(materialKey, productKey))
                return true;

            if (productType == materialBlock)
                return true;

            return FurnitureRecipeSetLinker.ProductUsesExactMaterial(productType, materialBlock);
        }
    }
}
