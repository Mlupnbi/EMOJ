using System.Linq;
using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 「纯材料实心砖」与 wiki 家具槽分离：仅用于拦截硫磺砖等误进蜡烛槽，不得误伤门/平台/椅等。
    /// </summary>
    internal static class FurnitureBuildingBlockRules
    {
        public static bool IsPhysicallySolidTile(Item item)
        {
            if (item == null || item.IsAir || item.createTile < TileID.Dirt)
                return false;

            return FurnitureTileSafety.IsPhysicallySolidTile(item.createTile);
        }

        public static bool IsPlainMaterialBrick(Item item)
        {
            if (!IsPhysicallySolidTile(item))
                return false;

            if (HasFurnitureTileOrNameIdentity(item))
                return false;

            return true;
        }

        public static bool MustNotOccupyWikiFurnitureSlot(Item item, FurnitureSlotKind classifiedSlot)
        {
            if (classifiedSlot is FurnitureSlotKind.Block
                or FurnitureSlotKind.Wall
                or FurnitureSlotKind.Platform
                or FurnitureSlotKind.None)
                return false;

            return IsPlainMaterialBrick(item);
        }

        public static bool MustNotOccupyWikiFurnitureSlot(int itemType, FurnitureSlotKind classifiedSlot)
        {
            if (itemType <= ItemID.None)
                return false;

            if (!FurnitureRecognitionCaches.TryGetProbe(itemType, out Item item))
                return false;

            return MustNotOccupyWikiFurnitureSlot(item, classifiedSlot);
        }

        private static bool HasFurnitureTileOrNameIdentity(Item item)
        {
            int tile = item.createTile;
            int style = item.placeStyle;
            string name = (item.Name ?? "").ToLowerInvariant();

            if (name.Contains("门") || name.Contains("door"))
                return true;
            if (name.Contains("平台") || name.Contains("platform"))
                return true;
            if (name.Contains("椅") || name.Contains("chair"))
                return true;
            if ((name.Contains("桌") && !name.Contains("书桌")) || name.Contains("table"))
                return true;
            if (name.Contains("床") || name.Contains("bed"))
                return true;
            if (name.Contains("浴缸") || name.Contains("bathtub"))
                return true;
            if (name.Contains("烛") || name.Contains("candle") || name.Contains("torch"))
                return true;
            if (name.Contains("台灯") || name.Contains("吊灯") || name.Contains("灯笼"))
                return true;
            if (name.Contains("宝箱") || name.Contains("chest") || (name.Contains("箱") && !name.Contains("信箱")))
                return true;

            if (!FurnitureTileSafety.IsValidTileId(tile))
                return false;

            if (FurnitureTileSafety.RoomNeedsCountsAsDoor(tile))
                return true;
            if (FurnitureTileSafety.RoomNeedsCountsAsChair(tile, style))
                return true;
            if (FurnitureTileSafety.RoomNeedsCountsAsTable(tile))
                return true;
            if (FurnitureTileSafety.RoomNeedsCountsAsTorch(tile)
                || FurnitureTileSafety.InBoolSet(TileID.Sets.Torch, tile))
                return true;

            if (tile is TileID.Containers or TileID.Containers2 or TileID.Beds or TileID.Bathtubs
                or TileID.Bookcases or TileID.Dressers or TileID.Toilets or TileID.Sinks
                or TileID.WorkBenches or TileID.Lamps or TileID.Candelabras or TileID.Candles
                or TileID.Chandeliers or TileID.Pianos)
                return true;

            return false;
        }
    }
}
