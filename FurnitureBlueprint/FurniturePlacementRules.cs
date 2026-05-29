using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ObjectData;
namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>放置前图格校验与清理。</summary>
    public static class FurniturePlacementRules
    {
        public static bool IsPlatform(Item item) =>
            item != null && !item.IsAir && FurnitureTileSafety.IsPlatformTile(item.createTile);

        public static bool IsSolidBlockTile(Item item) =>
            item != null && !item.IsAir && FurnitureTileSafety.IsPhysicallySolidTile(item.createTile);

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

        /// <summary>
        /// 按 TileObjectData footprint 只清前景图格（KillTile），不拆背景墙。
        /// 墙与前景在同一格可共存；拆墙会在放置失败时造成「墙被爆掉」，且仅对少数需背靠墙的家具（如椅子）不利。
        /// 床/桌/箱等多格家具放置不依赖背景墙，只需脚下支撑与空间（见 TileObjectData.AnchorBottom 等）。
        /// </summary>
        public static void PrepareFootprint(Item item, int anchorX, int anchorY)
        {
            if (item == null || item.IsAir || item.createTile < TileID.Dirt)
            {
                PrepareCell(anchorX, anchorY);
                return;
            }

            TileObjectData data = FurnitureTileSafety.TryGetTileData(item.createTile, item.placeStyle);
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

                    PrepareCell(x, y);
                }
            }
        }

        /// <summary>返回 item footprint 覆盖的世界坐标（用于失败时补回模板墙）。</summary>
        public static void CollectFootprintWorldTiles(Item item, int anchorX, int anchorY, List<Point> dest)
        {
            dest.Clear();
            if (item == null || item.IsAir || item.createTile < TileID.Dirt)
            {
                dest.Add(new Point(anchorX, anchorY));
                return;
            }

            TileObjectData data = FurnitureTileSafety.TryGetTileData(item.createTile, item.placeStyle);
            if (data == null)
            {
                dest.Add(new Point(anchorX, anchorY));
                return;
            }

            Point16 origin = data.Origin;
            for (int dx = 0; dx < data.Width; dx++)
            {
                for (int dy = 0; dy < data.Height; dy++)
                {
                    int x = anchorX + dx - origin.X;
                    int y = anchorY + dy - origin.Y;
                    if (WorldGen.InWorld(x, y, 1))
                        dest.Add(new Point(x, y));
                }
            }
        }
    }
}
