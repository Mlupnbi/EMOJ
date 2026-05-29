using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>候选扩展入口：仅走产物管道（已弃用全物品风格簇扫描）。</summary>
    public static class FurnitureStyleClusterCatalog
    {
        public static void ClearCache() { }

        public static void ExpandFromSeed(
            int seedType,
            FurnitureStyleSignature signature,
            HashSet<int> dest,
            int materialBlockOverride = ItemID.None)
        {
            if (dest == null || seedType <= ItemID.None)
                return;

            int materialBlock;
            Item seed = new Item();
            seed.SetDefaults(seedType);

            if (materialBlockOverride > ItemID.None)
            {
                materialBlock = materialBlockOverride;
            }
            else
            {
                int anchor = seedType;
                if (!FurnitureMaterialAnchor.IsValidAnchorBlock(seed))
                {
                    anchor = FurnitureSetRecognizer.ResolveAnchorMaterialPublic(seedType, signature);
                    anchor = FurnitureVanillaLivingWoodBridge.RedirectReverseAnchor(seedType, anchor);
                }

                if (FurnitureVanillaLivingWoodBridge.TryGetProductExpansionMaterial(seedType, anchor, out int expandMat))
                    anchor = expandMat;

                materialBlock = anchor;
                Item anchorItem = new Item();
                anchorItem.SetDefaults(anchor);
                if (!FurnitureMaterialAnchor.IsValidAnchorBlock(anchorItem))
                {
                    int resolved = FurnitureMaterialAnchor.ResolvePlaceableBlock(anchor, signature);
                    if (resolved > ItemID.None)
                        materialBlock = resolved;
                }
            }

            FurnitureStyleSignature blockSig = FurnitureStyleSignature.FromItemTypeForRecipes(materialBlock);
            if (FurnitureVanillaLivingWoodBridge.TryGetSetSignature(seedType, out FurnitureStyleSignature lwSig))
                blockSig = lwSig;
            else
            {
                FurnitureStyleSignature merged = BuildFilterSignature(signature, materialBlock, seed);
                blockSig = new FurnitureStyleSignature
                {
                    ModKey = merged.ModKey,
                    StyleKey = merged.StyleKey,
                    PlacementTile = merged.PlacementTile,
                    PlacementStyle = merged.PlacementStyle,
                    UsesPlacementStyleLine = merged.UsesPlacementStyleLine,
                    SeedIsMaterialBlock = true
                };
            }

            FurnitureCraftStationProfile stations = FurnitureCraftStationProfile.FromSeed(seedType);
            foreach (int t in FurnitureProductPipeline.CollectMaterialFirstProducts(seedType, materialBlock, blockSig, stations))
                dest.Add(t);
        }

        /// <summary>过滤签名：StyleKey 跟种子；placeStyle 线优先从锚点方块推断（Gemini 第一层）。</summary>
        private static FurnitureStyleSignature BuildFilterSignature(
            FurnitureStyleSignature seedSig, int anchor, Item seedItem)
        {
            if (seedItem != null && !seedItem.IsAir && seedItem.createTile >= TileID.Dirt
                && !FurnitureMaterialAnchor.IsValidAnchorBlock(seedItem)
                && FurnitureStyleSignature.FromItemType(seedItem.type).UsesPlacementStyleLine)
            {
                FurnitureStyleSignature fromSeed = FurnitureStyleSignature.FromItemType(seedItem.type);
                return new FurnitureStyleSignature
                {
                    ModKey = seedSig.ModKey,
                    StyleKey = seedSig.StyleKey,
                    PlacementTile = fromSeed.PlacementTile,
                    PlacementStyle = fromSeed.PlacementStyle,
                    UsesPlacementStyleLine = true,
                    SeedIsMaterialBlock = false
                };
            }

            if (FurnitureMaterialAnchor.IsValidAnchorBlock(seedItem) || anchor <= ItemID.None)
                return seedSig;

            FurnitureStyleSignature anchorSig = FurnitureStyleSignature.FromItemType(anchor);
            if (!FurnitureStyleSignature.StyleKeySameMaterialFamily(seedSig.StyleKey, anchorSig.StyleKey)
                && !FurnitureMaterialKeyNormalizer.SameMaterialFamily(seedSig.StyleKey, anchorSig.StyleKey))
                return seedSig;

            return new FurnitureStyleSignature
            {
                ModKey = seedSig.ModKey,
                StyleKey = seedSig.StyleKey,
                PlacementTile = anchorSig.PlacementTile >= TileID.Dirt ? anchorSig.PlacementTile : seedSig.PlacementTile,
                PlacementStyle = anchorSig.UsesPlacementStyleLine ? anchorSig.PlacementStyle : seedSig.PlacementStyle,
                UsesPlacementStyleLine = anchorSig.UsesPlacementStyleLine || seedSig.UsesPlacementStyleLine,
                SeedIsMaterialBlock = false
            };
        }
    }
}
