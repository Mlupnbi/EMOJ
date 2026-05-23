using System.Collections.Generic;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
    /// <summary>无 createTile 但应提供制作环境的物品 → Tile 列表（对齐 ImproveGame AddPortableCraftingStation）。</summary>
    internal static class PortableCraftEnvironmentRegistry
    {
        private static readonly Dictionary<int, int[]> ByItemType = new Dictionary<int, int[]>();

        internal static void Register(int itemType, params int[] tileTypes)
        {
            if (itemType <= 0 || tileTypes == null || tileTypes.Length == 0)
                return;
            ByItemType[itemType] = tileTypes;
        }

        internal static void RegisterImproveGamePortableStations(IReadOnlyDictionary<int, List<int>> source)
        {
            if (source == null)
                return;

            foreach (var pair in source)
            {
                if (pair.Value == null || pair.Value.Count == 0)
                    continue;
                ByItemType[pair.Key] = pair.Value.ToArray();
            }
        }

        internal static bool TryGetTiles(int itemType, out int[] tileTypes)
        {
            if (ByItemType.TryGetValue(itemType, out tileTypes))
                return true;
            tileTypes = null;
            return false;
        }
    }
}
