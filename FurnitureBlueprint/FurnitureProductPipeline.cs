using System.Collections.Generic;

using Terraria;

using Terraria.ID;

using EvenMoreOverpoweredJourney.Research;



namespace EvenMoreOverpoweredJourney.FurnitureBlueprint

{

    /// <summary>材料块优先：仅该块配方产物 + 同图格线兄弟（仍用材料 StyleKey，不用种子 Cosmic 名）。</summary>

    public static class FurnitureProductPipeline

    {

        public static HashSet<int> CollectMaterialFirstProducts(

            int seedType,

            int materialBlock,

            FurnitureStyleSignature blockSig,

            FurnitureCraftStationProfile stationProfile = null)

        {

            if (materialBlock <= ItemID.None)

                return new HashSet<int>();



            stationProfile ??= FurnitureCraftStationProfile.FromSeed(seedType);

            FurnitureStyleSignature productSig = FurnitureMaterialBlockSignature.ForProducts(materialBlock, blockSig, seedType);



            HashSet<int> products = FurnitureMaterialProductCollector.CollectFromMaterialBlock(
                materialBlock, productSig, stationProfile, seedType);

            int afterCollect = products.Count;



            Item blockItem = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(blockItem, materialBlock))
                return products;
            if (!RecipeAnalyzer.IsHighFanoutMaterial(materialBlock)
                && FurnitureMaterialAnchor.IsValidAnchorBlock(blockItem)
                && blockItem.createTile >= TileID.Dirt)
            {
                FurnitureTileSlotRegistry.AddPlacementLineSiblings(
                    blockItem.createTile,
                    blockItem.placeStyle,
                    productSig.ModKey,
                    productSig.StyleKey,
                    products,
                    maxItems: 64);
            }

            if (seedType > ItemID.None && FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
            {
                FurnitureStyleSignature seedSig = FurnitureStyleSignature.FromItemType(seedType);
                if (seedSig.UsesPlacementStyleLine && seedSig.PlacementTile >= TileID.Dirt)
                {
                    FurnitureTileSlotRegistry.AddPlacementLineSiblings(
                        seedSig.PlacementTile,
                        seedSig.PlacementStyle,
                        productSig.ModKey,
                        productSig.StyleKey,
                        products,
                        maxItems: 64);
                }
            }



            if (seedType > ItemID.None && seedType != materialBlock)

                products.Add(seedType);



            FurnitureCandidateExpander.Expand(seedType, productSig, materialBlock, products);
            FurnitureMaterialPlacementExpander.ExpandFromMaterialAndSeed(products, seedType, materialBlock, productSig);
            FurnitureStylePrefixCatalog.ExpandForSeed(seedType, materialBlock, productSig, products);

            FurnitureBlueprintLog.InfoFull(

                $"products strict seed={seedType} block={materialBlock} material_style={productSig.StyleKey} count={products.Count} recipe_only={afterCollect}");

            return products;

        }

    }

}


