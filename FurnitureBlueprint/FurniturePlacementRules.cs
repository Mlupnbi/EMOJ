using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>放置前图格校验与清理。</summary>
    public static class FurniturePlacementRules
    {
        public static bool IsPlatform(Item item) =>
            item != null && !item.IsAir && item.createTile >= TileID.Dirt && TileID.Sets.Platforms[item.createTile];

        public static bool IsSolidBlockTile(Item item) =>
            item != null && !item.IsAir && item.createTile >= TileID.Dirt
            && Main.tileSolid[item.createTile] && !Main.tileSolidTop[item.createTile]
            && !TileID.Sets.Platforms[item.createTile];

        public static bool CanPlaceKind(Item item, FurnitureSlotKind kind)
        {
            if (item == null || item.IsAir)
                return false;

            if (kind == FurnitureSlotKind.Platform)
                return IsPlatform(item);

            if (kind == FurnitureSlotKind.Block)
                return IsSolidBlockTile(item);

            if (item.createTile < TileID.Dirt && item.createWall <= WallID.None)
                return false;

            if (!FurnitureSlotClassifier.TryGetSlot(item, out FurnitureSlotKind classified))
                return false;

            return FurnitureWikiSlots.NormalizeClassified(classified) == kind;
        }

        public static void PrepareCell(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 1))
                return;

            WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: true);
        }

        /// <summary>按 TileObjectData  footprint 清理锚点及占用格（参考 IG BongBongPlace 先炸后放）。</summary>
        public static void PrepareFootprint(Item item, int anchorX, int anchorY)
        {
            if (item == null || item.IsAir || item.createTile < TileID.Dirt)
            {
                PrepareCell(anchorX, anchorY);
                return;
            }

            TileObjectData data = TileObjectData.GetTileData(item.createTile, item.placeStyle);
            if (data == null)
            {
                PrepareCell(anchorX, anchorY);
                return;
            }

            Point16 origin = data.Origin;
            for (int dx = 0; dx < data.Width; dx++)
            {
                for (int dy = 0; dy < data.Height; dy++)
                {
                    int x = anchorX + dx - origin.X;
                    int y = anchorY + dy - origin.Y;
                    if (!WorldGen.InWorld(x, y, 1))
                        continue;

                    WorldGen.KillWall(x, y);
                    PrepareCell(x, y);
                }
            }
        }
    }
}
