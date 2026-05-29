using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>在材料块/种子已确认的 placeStyle 图格线上补兄弟物品（仍锚定材料，非全模组扫描）。</summary>
    public static class FurnitureMaterialPlacementExpander
    {
        public static void ExpandFromMaterialAndSeed(
            HashSet<int> dest,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig)
        {
            if (dest == null || materialBlock <= ItemID.None)
                return;

            int lineTile = blockSig.PlacementTile;
            int lineStyle = blockSig.PlacementStyle;
            bool useLine = blockSig.UsesPlacementStyleLine;

            if (seedType > ItemID.None)
            {
                FurnitureStyleSignature seedSig = FurnitureStyleSignature.FromItemType(seedType);
                if (seedSig.UsesPlacementStyleLine && seedSig.PlacementTile >= TileID.Dirt)
                {
                    lineTile = seedSig.PlacementTile;
                    lineStyle = seedSig.PlacementStyle;
                    useLine = true;
                }
            }

            if (!useLine || lineTile < TileID.Dirt)
            {
                Item blockItem = new Item();
                if (FurnitureItemDefaults.TrySetDefaults(blockItem, materialBlock)
                    && blockItem.createTile >= TileID.Dirt)
                {
                    lineTile = blockItem.createTile;
                    lineStyle = blockItem.placeStyle;
                    useLine = true;
                }
            }

            if (!useLine || lineTile < TileID.Dirt)
                return;

            string modKey = blockSig.ModKey ?? "Terraria";
            string styleKey = blockSig.StyleKey?.Trim() ?? "";

            FurnitureTileSlotRegistry.AddPlacementLineSiblings(lineTile, lineStyle, modKey, styleKey, dest, maxItems: 128);

            // 模组常用单图格多 placeStyle 表示整套 22 件；必须收齐同 tile 全部 style，不能只收种子那一格。
            if (lineTile >= TileID.Count && !string.IsNullOrWhiteSpace(styleKey))
            {
                FurnitureTileSlotRegistry.AddAllItemsOnPlacementTile(
                    lineTile, modKey, styleKey, dest, maxItems: 128, requireStyleMatch: true);
            }
        }
    }
}
