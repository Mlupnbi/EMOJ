using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Registry
{
    /// <summary>
    /// 启动时建立 Tile/Wall → 放置用 Item 的反查表（参考 ImproveGame MaterialCore）。
    /// 供放置预览、缺件检测与 Phase 2 材料校验使用。
    /// </summary>
    public static class FurnitureTileItemRegistry
    {
        private static readonly Dictionary<int, int> TileDefaultItem = new();
        private static readonly Dictionary<long, int> TileStyleItem = new();
        private static readonly Dictionary<int, HashSet<int>> KnownStylesByTile = new();
        private static readonly Dictionary<int, int> WallDefaultItem = new();
        private static bool _built;

        public static bool IsBuilt => _built;

        /// <summary>至少有一个物品使用该 (tile, placeStyle) 放置时才允许 GetTileData（避免 native 闪退）。</summary>
        public static bool IsKnownPlacementStyle(int tile, int style)
        {
            EnsureBuilt();
            return KnownStylesByTile.TryGetValue(tile, out HashSet<int> styles) && styles.Contains(style);
        }

        public static bool TryGetKnownStyles(int tile, out int[] styles)
        {
            EnsureBuilt();
            if (!KnownStylesByTile.TryGetValue(tile, out HashSet<int> set) || set.Count == 0)
            {
                styles = null;
                return false;
            }

            styles = new int[set.Count];
            set.CopyTo(styles);
            return true;
        }

        public static void Build()
        {
            TileDefaultItem.Clear();
            TileStyleItem.Clear();
            KnownStylesByTile.Clear();
            WallDefaultItem.Clear();
            _built = true;

            int maxItem = ItemLoader.ItemCount;
            for (int type = ItemID.None + 1; type < maxItem; type++)
            {
                Item item = new Item();
                try
                {
                    item.SetDefaults(type);
                }
                catch
                {
                    continue;
                }

                if (item.createTile >= TileID.Dirt)
                    RegisterTileItem(item.createTile, item.placeStyle, type);
                else if (item.createWall > WallID.None)
                    RegisterWallItem(item.createWall, type);
            }
        }

        public static bool TryGetItemForTile(int tileType, int placeStyle, out int itemType)
        {
            EnsureBuilt();
            long key = PackTileStyle(tileType, placeStyle);
            if (TileStyleItem.TryGetValue(key, out itemType))
                return true;

            if (placeStyle != 0 && TileStyleItem.TryGetValue(PackTileStyle(tileType, 0), out itemType))
                return true;

            return TileDefaultItem.TryGetValue(tileType, out itemType);
        }

        public static bool TryGetItemForWall(int wallType, out int itemType)
        {
            EnsureBuilt();
            return WallDefaultItem.TryGetValue(wallType, out itemType);
        }

        private static void EnsureBuilt()
        {
            if (!_built)
                Build();
        }

        private static void RegisterTileItem(int tile, int style, int itemType)
        {
            if (tile < TileID.Dirt || itemType <= ItemID.None)
                return;

            long key = PackTileStyle(tile, style);
            if (!TileStyleItem.ContainsKey(key))
                TileStyleItem[key] = itemType;

            if (!TileDefaultItem.ContainsKey(tile))
                TileDefaultItem[tile] = itemType;

            if (!KnownStylesByTile.TryGetValue(tile, out HashSet<int> styles))
            {
                styles = new HashSet<int>();
                KnownStylesByTile[tile] = styles;
            }

            styles.Add(style);
        }

        private static void RegisterWallItem(int wall, int itemType)
        {
            if (wall <= WallID.None || itemType <= ItemID.None)
                return;

            if (!WallDefaultItem.ContainsKey(wall))
                WallDefaultItem[wall] = itemType;
        }

        private static long PackTileStyle(int tile, int style) => ((long)tile << 32) | (uint)style;
    }
}
