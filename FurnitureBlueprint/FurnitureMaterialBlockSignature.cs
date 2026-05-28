using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 正推产物与风格过滤一律锚定「已选材料方块」，不用种子家具名（如 Cosmic 床 vs Cosmilite 砖）。
    /// </summary>
    public static class FurnitureMaterialBlockSignature
    {
        public static FurnitureStyleSignature ForProducts(
            int materialBlock,
            FurnitureStyleSignature seedSig,
            int seedType)
        {
            if (materialBlock <= ItemID.None)
                return seedSig;

            if (FurnitureVanillaLivingWoodBridge.TryGetSetSignature(seedType, out FurnitureStyleSignature setSig))
                return MergeSetStyleWithPlacement(setSig, seedSig, seedType);

            if (FurnitureSetMaterialRules.TryGetModLineageSetSignature(seedType, out FurnitureStyleSignature lineageSig))
                return MergeSetStyleWithPlacement(lineageSig, seedSig, seedType);

            FurnitureStyleSignature materialSig = FurnitureStyleSignature.FromItemTypeForRecipes(materialBlock);

            int lineTile = materialSig.PlacementTile;
            int lineStyle = materialSig.PlacementStyle;
            bool useLine = materialSig.UsesPlacementStyleLine;

            if (seedType > ItemID.None)
            {
                FurnitureStyleSignature fromSeed = FurnitureStyleSignature.FromItemType(seedType);
                if (fromSeed.UsesPlacementStyleLine && fromSeed.PlacementTile >= TileID.Dirt)
                {
                    lineTile = fromSeed.PlacementTile;
                    lineStyle = fromSeed.PlacementStyle;
                    useLine = true;
                }
            }

            Item blockItem = new Item();
            blockItem.SetDefaults(materialBlock);
            if (!useLine && blockItem.createTile >= TileID.Dirt)
            {
                lineTile = blockItem.createTile;
                lineStyle = blockItem.placeStyle;
                useLine = true;
            }

            return new FurnitureStyleSignature
            {
                ModKey = materialSig.ModKey,
                StyleKey = materialSig.StyleKey,
                PlacementTile = lineTile,
                PlacementStyle = lineStyle,
                UsesPlacementStyleLine = useLine,
                SeedIsMaterialBlock = true
            };
        }

        private static FurnitureStyleSignature MergeSetStyleWithPlacement(
            FurnitureStyleSignature setSig,
            FurnitureStyleSignature seedSig,
            int seedType)
        {
            int lineTile = seedSig.PlacementTile;
            int lineStyle = seedSig.PlacementStyle;
            bool useLine = seedSig.UsesPlacementStyleLine;

            if (seedType > ItemID.None)
            {
                FurnitureStyleSignature fromSeed = FurnitureStyleSignature.FromItemType(seedType);
                if (fromSeed.UsesPlacementStyleLine && fromSeed.PlacementTile >= TileID.Dirt)
                {
                    lineTile = fromSeed.PlacementTile;
                    lineStyle = fromSeed.PlacementStyle;
                    useLine = true;
                }
            }

            return new FurnitureStyleSignature
            {
                ModKey = setSig.ModKey,
                StyleKey = setSig.StyleKey,
                PlacementTile = lineTile,
                PlacementStyle = lineStyle,
                UsesPlacementStyleLine = useLine,
                SeedIsMaterialBlock = true
            };
        }
    }
}
