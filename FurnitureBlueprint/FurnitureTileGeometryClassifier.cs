using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 按 TileObjectData 宽高、锚点、图格属性分类（Gemini 维度一/二），优先于模组英文名。
    /// </summary>
    public static class FurnitureTileGeometryClassifier
    {
        public static bool TryClassify(int tile, int style, string nameHint, out FurnitureSlotKind kind)
        {
            kind = FurnitureSlotKind.None;
            if (tile < TileID.Dirt)
                return false;

            TileObjectData data = TileObjectData.GetTileData(tile, style);
            if (data == null)
                return false;

            string name = nameHint?.ToLowerInvariant() ?? "";
            int w = data.Width;
            int h = data.Height;
            if (w <= 0 || h <= 0)
                return false;

            bool anchorBottom = HasBottomAnchor(data);
            bool anchorTop = HasTopAnchor(data);
            bool topSolid = Main.tileSolidTop[tile];
            bool lighted = Main.tileLighted != null && tile < Main.tileLighted.Length && Main.tileLighted[tile];

            if (w == 1 && h == 1 && (lighted || name.Contains("cup") || name.Contains("杯")
                || name.Contains("candle") || name.Contains("torch") || name.Contains("lantern")))
            {
                kind = FurnitureSlotKind.Candle;
                return true;
            }

            if (w == 2 && h == 1 && anchorBottom && topSolid)
            {
                kind = FurnitureSlotKind.Workbench;
                return true;
            }

            if (w == 1 && h == 3 && anchorBottom && anchorTop)
            {
                kind = FurnitureSlotKind.Door;
                return true;
            }

            if (tile == TileID.OpenDoor || tile == TileID.ClosedDoor)
            {
                kind = FurnitureSlotKind.Door;
                return true;
            }

            if (w == 3 && h == 4 && anchorBottom)
            {
                kind = FurnitureSlotKind.Bookcase;
                return true;
            }

            if (w == 2 && h == 5 && anchorBottom)
            {
                kind = FurnitureSlotKind.Clock;
                return true;
            }

            if (lighted || name.Contains("torch") || name.Contains("candle")
                || name.Contains(" lantern") || name.EndsWith("lantern")
                || name.Contains(" lamp") || name.EndsWith("lamp")
                || name.Contains("chandelier") || name.Contains("campfire"))
            {
                if (anchorTop && w >= 3)
                {
                    kind = FurnitureSlotKind.Chandelier;
                    return true;
                }

                if (anchorTop)
                {
                    kind = FurnitureSlotKind.Lantern;
                    return true;
                }

                if (h >= 3)
                {
                    kind = FurnitureSlotKind.Lamp;
                    return true;
                }

                if (w >= 2)
                {
                    kind = FurnitureSlotKind.Candelabra;
                    return true;
                }

                kind = FurnitureSlotKind.Candle;
                return true;
            }

            if (w == 1 && h == 2 && anchorBottom)
            {
                kind = FurnitureSlotKind.Chair;
                if (name.Contains("toilet") || style is 1 or 20)
                    kind = FurnitureSlotKind.Toilet;
                return true;
            }

            if (w == 4 && h == 2 && anchorBottom)
            {
                if (tile == TileID.Bathtubs)
                    kind = FurnitureSlotKind.Bathtub;
                else if (tile == TileID.Beds)
                    kind = FurnitureSlotKind.Bed;
                else if (name.Contains("bath") || name.Contains("浴"))
                    kind = FurnitureSlotKind.Bathtub;
                else
                    kind = FurnitureSlotKind.Bed;
                return true;
            }

            if (anchorBottom && (w >= 3 && w <= 6) && (h >= 2 && h <= 4)
                && (name.Contains("bed") || tile == TileID.Beds))
            {
                kind = FurnitureSlotKind.Bed;
                return true;
            }

            if (anchorBottom && w >= 2 && h >= 2
                && (name.Contains("toilet") || name.Contains("马桶")))
            {
                kind = FurnitureSlotKind.Toilet;
                return true;
            }

            if (w == 3 && h == 2 && anchorBottom)
            {
                if (tile == TileID.Pianos)
                    kind = FurnitureSlotKind.Piano;
                else if (tile == TileID.Benches)
                    kind = FurnitureSlotKind.Sofa;
                else if (tile == TileID.Sinks)
                    kind = FurnitureSlotKind.Sink;
                else if (TileID.Sets.BasicDresser[tile] || name.Contains("dresser") || name.Contains("梳妆"))
                    kind = FurnitureSlotKind.Dresser;
                else if (name.Contains("piano") || name.Contains("钢琴"))
                    kind = FurnitureSlotKind.Piano;
                else if (name.Contains("sofa") || name.Contains("couch") || name.Contains("沙发"))
                    kind = FurnitureSlotKind.Sofa;
                else if (name.Contains("sink") || name.Contains("水槽") || name.Contains("洗手"))
                    kind = FurnitureSlotKind.Sink;
                else
                    kind = FurnitureSlotKind.Table;
                return true;
            }

            if (w == 2 && h == 2 && anchorBottom)
            {
                if (TileID.Sets.BasicChest[tile] || name.Contains("chest"))
                {
                    kind = FurnitureSlotKind.Chest;
                    return true;
                }
            }

            if (w == 2 && h == 1 && anchorBottom)
            {
                if (tile == TileID.Bathtubs || name.Contains("bathtub") || name.Contains("bath") || name.Contains("浴"))
                {
                    kind = FurnitureSlotKind.Bathtub;
                    return true;
                }
            }

            return false;
        }

        public static string GetTileNameHint(int tile)
        {
            ModTile mt = TileLoader.GetTile(tile);
            if (mt == null)
                return "";
            return mt.Name + " " + mt.FullName;
        }

        private static bool HasBottomAnchor(TileObjectData data) =>
            data.AnchorBottom.type != AnchorType.None;

        private static bool HasTopAnchor(TileObjectData data) =>
            data.AnchorTop.type != AnchorType.None;
    }
}
