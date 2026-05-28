using System.Collections.Generic;

using Terraria;

using Terraria.ID;

using Terraria.ModLoader;

using EvenMoreOverpoweredJourney.Research;



namespace EvenMoreOverpoweredJourney.FurnitureBlueprint

{

    /// <summary>

    /// 以材料方块为锚：仅收集「消耗该方块」的配方产物 + 对应墙，不做全模组族扫描/闭包/二次 linker。

    /// </summary>

    public static class FurnitureMaterialProductCollector

    {

        public const int MaxProducts = 192;



        public static HashSet<int> CollectFromMaterialBlock(
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stationProfile = null,
            int seedType = ItemID.None)

        {

            var dest = new HashSet<int>();

            if (materialBlock <= ItemID.None)

                return dest;



            Item blockProbe = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(blockProbe, materialBlock))
                return dest;
            if (!FurnitureMaterialAnchor.IsValidAnchorBlock(blockProbe))
                return dest;



            dest.Add(materialBlock);

            string modKey = GetModKey(materialBlock);

            string blockKey = blockSig.StyleKey?.Trim() ?? FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);

            int added = 0;

            int recipeCap = RecipeAnalyzer.IsHighFanoutMaterial(materialBlock) ? 96 : MaxProducts;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(materialBlock))

            {

                if (added >= recipeCap)

                    break;



                if (recipe?.createItem == null || recipe.createItem.IsAir)

                    continue;



                int product = recipe.createItem.type;
                if (stationProfile != null && stationProfile.IsConstrained
                    && !stationProfile.RecipeCompatible(recipe)
                    && !FurnitureCraftStationRules.IsStationCollectionExempt(product, materialBlock))
                    continue;



                if (!RecipeAnalyzer.RecipeUsesIngredient(recipe, materialBlock))

                    continue;



                if (dest.Contains(product))

                    continue;



                if (!ProductMatchesMaterialBlock(product, materialBlock, modKey, blockKey, seedType))

                    continue;

                if (stationProfile != null && stationProfile.IsConstrained && seedType > ItemID.None
                    && FurnitureCraftStationRules.ShouldExcludeFromProductCollect(
                        product, stationProfile, seedType, materialBlock))
                    continue;



                Item probe = new Item();
                if (!FurnitureItemDefaults.TrySetDefaults(probe, product))
                    continue;

                bool isWall = probe.createWall > WallID.None && probe.createTile < TileID.Dirt;

                if (!isWall && !FurnitureCandidateFilter.IsPlaceableFurnitureItem(probe))

                    continue;



                dest.Add(product);

                added++;

            }



            if (FurnitureWallResolver.TryResolveWallFromBlock(materialBlock, blockSig, out int resolvedWall)

                && resolvedWall > ItemID.None)

                dest.Add(resolvedWall);



            FurnitureBlueprintLog.InfoFull(

                $"material collect strict block={materialBlock} style={blockKey} recipe_products={added} total={dest.Count}");

            return dest;

        }



        internal static bool ProductMatchesMaterialBlock(

            int productType,

            int materialBlock,

            string modKey,

            string blockKey,

            int seedType = ItemID.None)

        {

            string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(productType);

            if (FurnitureRecipeSetLinker.ProductUsesExactMaterial(productType, materialBlock))

            {

                if (seedType > ItemID.None

                    && FurnitureSetMaterialRules.UsesModLineageAnchor(seedType)

                    && RecipeAnalyzer.IsHighFanoutMaterial(materialBlock))

                {

                    if (!FurnitureSlotScoring.PassesModLineageWoodProductLink(

                            productType, seedType, materialBlock, blockKey, productKey))

                        return false;

                }

                return true;

            }



            if (string.IsNullOrWhiteSpace(blockKey))

                return false;



            if (string.Equals(productKey, blockKey, System.StringComparison.OrdinalIgnoreCase))

                return true;



            if (FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, productKey))

                return true;



            if (FurnitureMaterialKeyNormalizer.Normalize(productKey)

                .Equals(FurnitureMaterialKeyNormalizer.Normalize(blockKey), System.StringComparison.OrdinalIgnoreCase))

                return true;



            if (RecipeAnalyzer.IsHighFanoutMaterial(materialBlock))
            {
                if (FurnitureRecipeSetLinker.ProductUsesExactMaterial(productType, materialBlock))
                    return true;
                return FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, productKey)
                    || FurnitureMaterialKeyNormalizer.StyleKeysMatch(blockKey, productKey);
            }

            if (FurnitureMaterialKeyNormalizer.SameMaterialFamily(blockKey, productKey))

                return true;



            return false;

        }



        private static string GetModKey(int itemType)

        {

            ModItem mi = ItemLoader.GetItem(itemType);

            return mi == null ? "Terraria" : mi.Mod.Name;

        }

    }

}


