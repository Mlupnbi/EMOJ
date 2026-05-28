using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
    /// <summary>
    /// 旅途绿格台子：仅当「旅途献祭目录中、能代表该合成站」的物品已研究满时视为解锁。
    /// 不使用 SeenTiles、AdjTiles 扩散、ProvidesStation 升级链。
    /// </summary>
    internal static class JourneyStationSacrifice
    {
        private static Dictionary<int, List<int>> _itemsByPlaceTile;
        private static Dictionary<int, List<int>> _portableGrantItemsByTile;
        private static bool _cacheBuilt;

        internal static void InvalidateCache()
        {
            _cacheBuilt = false;
            _itemsByPlaceTile = null;
            _portableGrantItemsByTile = null;
        }

        internal static bool IsTileUnlockedBySacrifice(int tileType)
        {
            if (tileType <= 0)
                return true;

            EnsureCache();

            if (_itemsByPlaceTile != null
                && _itemsByPlaceTile.TryGetValue(tileType, out List<int> placeItems)
                && AnyItemFullyResearched(placeItems))
            {
                return true;
            }

            if (_portableGrantItemsByTile != null
                && _portableGrantItemsByTile.TryGetValue(tileType, out List<int> portableItems)
                && AnyItemFullyResearched(portableItems))
            {
                return true;
            }

            return false;
        }

        internal static string DescribeTileSacrificeState(int tileType)
        {
            if (tileType <= 0)
                return "n/a";

            EnsureCache();
            var researched = new List<string>();
            var pending = new List<string>();

            void scan(Dictionary<int, List<int>> map)
            {
                if (map == null || !map.TryGetValue(tileType, out List<int> items))
                    return;

                foreach (int itemType in items)
                {
                    string label = $"{Lang.GetItemName(itemType).Value}(id={itemType})";
                    if (RecipeAnalyzer.IsFullyResearched(itemType))
                        researched.Add(label);
                    else
                        pending.Add(label);
                }
            }

            scan(_itemsByPlaceTile);
            scan(_portableGrantItemsByTile);

            if (researched.Count == 0 && pending.Count == 0)
                return $"tile{tileType}:noCatalogItem";

            return $"tile{tileType}:ok=[{string.Join(",", researched)}] need=[{string.Join(",", pending)}]";
        }

        private static bool AnyItemFullyResearched(List<int> itemTypes)
        {
            for (int i = 0; i < itemTypes.Count; i++)
            {
                if (RecipeAnalyzer.IsFullyResearched(itemTypes[i]))
                    return true;
            }

            return false;
        }

        private static void EnsureCache()
        {
            if (_cacheBuilt)
                return;

            _itemsByPlaceTile = new Dictionary<int, List<int>>();
            _portableGrantItemsByTile = new Dictionary<int, List<int>>();

            for (int itemType = 1; itemType < ItemLoader.ItemCount; itemType++)
            {
                if (!RecipeAnalyzer.TryGetJourneyUnlockQuota(itemType, out _))
                    continue;

                Item item = new Item();
                item.SetDefaults(itemType);
                if (item.IsAir)
                    continue;

                if (item.createTile >= 0)
                    AddItem(_itemsByPlaceTile, item.createTile, itemType);

                if (PortableCraftEnvironmentRegistry.TryGetTiles(itemType, out int[] portableTiles))
                {
                    foreach (int tile in portableTiles)
                    {
                        if (tile >= 0)
                            AddItem(_portableGrantItemsByTile, tile, itemType);
                    }
                }
            }

            _cacheBuilt = true;
        }

        private static void AddItem(Dictionary<int, List<int>> map, int key, int itemType)
        {
            if (!map.TryGetValue(key, out List<int> list))
            {
                list = new List<int>();
                map[key] = list;
            }

            if (!list.Contains(itemType))
                list.Add(itemType);
        }
    }
}
