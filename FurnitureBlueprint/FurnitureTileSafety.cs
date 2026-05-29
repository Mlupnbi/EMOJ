using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Registry;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>图格数组越界保护（全物品扫描 / mod 图格 ID 时常触发）。</summary>
    internal static class FurnitureTileSafety
    {
        public static bool IsValidTileId(int tile) =>
            tile >= 0 && tile < TileLoader.TileCount;

        public static bool InBoolSet(bool[] set, int tile) =>
            set != null && tile >= 0 && tile < set.Length && set[tile];

        public static bool InIntSet(int[] set, int tile) =>
            set != null && tile >= 0 && tile < set.Length && set[tile] != 0;

        public static bool RoomNeedsCountsAsChair(int tile, int style) =>
            InIntSet(TileID.Sets.RoomNeeds.CountsAsChair, tile) && style is not 1 and not 20;

        public static bool RoomNeedsCountsAsTable(int tile) =>
            InIntSet(TileID.Sets.RoomNeeds.CountsAsTable, tile);

        public static bool RoomNeedsCountsAsDoor(int tile) =>
            InIntSet(TileID.Sets.RoomNeeds.CountsAsDoor, tile);

        public static bool RoomNeedsCountsAsTorch(int tile) =>
            InIntSet(TileID.Sets.RoomNeeds.CountsAsTorch, tile);

        public static bool IsPlatformTile(int tile) =>
            InBoolSet(TileID.Sets.Platforms, tile);

        /// <summary>mod 图格 style 异常时 GetTileData 可能 native 闪退，统一经此入口。</summary>
        public static TileObjectData TryGetTileData(int tile, int style)
        {
            if (!IsValidTileId(tile) || style < 0 || style > byte.MaxValue)
                return null;

            // 仅查询有物品实际使用的 placeStyle，避免对无效 style 触发 native 闪退。
            if (!FurnitureTileItemRegistry.IsKnownPlacementStyle(tile, style))
                return null;

            try
            {
                return TileObjectData.GetTileData(tile, style);
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"GetTileData failed tile={tile} style={style}: {ex.Message}");
                return null;
            }
        }

        public static bool HasTileData(int tile, int style) => TryGetTileData(tile, style) != null;

        public static bool IsPhysicallySolidTile(int tile)
        {
            if (!IsValidTileId(tile) || tile < TileID.Dirt)
                return false;

            if (InBoolSet(TileID.Sets.Platforms, tile))
                return false;

            if (tile >= Main.tileSolid.Length || tile >= Main.tileSolidTop.Length)
                return false;

            return Main.tileSolid[tile] && !Main.tileSolidTop[tile];
        }

        public static bool IsTileSolidTop(int tile) =>
            IsValidTileId(tile) && tile < Main.tileSolidTop.Length && Main.tileSolidTop[tile];

        public static bool IsTileLighted(int tile) =>
            IsValidTileId(tile) && Main.tileLighted != null && tile < Main.tileLighted.Length && Main.tileLighted[tile];

        /// <summary>无 TileObjectData 的实心块（墙/不可放置物 createTile=-1 已在入口拦截）。</summary>
        public static bool IsPlainSolidBlock(int tile) =>
            IsPhysicallySolidTile(tile) && TryGetTileData(tile, 0) == null;

        public static bool HasPlaceableTile(Item item) =>
            item != null && !item.IsAir && IsValidTileId(item.createTile) && item.createTile >= TileID.Dirt;
    }
}
