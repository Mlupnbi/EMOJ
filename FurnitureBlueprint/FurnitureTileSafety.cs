using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
    }
}
