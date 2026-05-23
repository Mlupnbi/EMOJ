using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
    /// <summary>
    /// 合成台 Tile 的等价/升级链（对齐 Recipe Browser / ImproveGame / UpgradeResearch）。
    /// </summary>
    internal static class CraftingStationAdjacency
    {
        public static IEnumerable<int> Expand(int tileType)
        {
            if (tileType < 0)
                yield break;

            var seen = new HashSet<int>();
            foreach (int tile in ExpandInternal(tileType, seen))
                yield return tile;
        }

        public static void MarkExpanded(bool[] target, int tileType)
        {
            if (target == null || tileType < 0)
                return;

            foreach (int tile in Expand(tileType))
            {
                if (tile >= 0 && tile < target.Length)
                    target[tile] = true;
            }
        }

        private static IEnumerable<int> ExpandInternal(int tileType, HashSet<int> seen)
        {
            if (!seen.Add(tileType))
                yield break;

            yield return tileType;

            ModTile modTile = TileLoader.GetTile(tileType);
            if (modTile?.AdjTiles != null)
            {
                foreach (int adj in modTile.AdjTiles)
                {
                    foreach (int expanded in ExpandInternal(adj, seen))
                        yield return expanded;
                }
            }

            foreach (int mapped in MapVanillaUpgradeTiles(tileType))
            {
                foreach (int expanded in ExpandInternal(mapped, seen))
                    yield return expanded;
            }
        }

        private static IEnumerable<int> MapVanillaUpgradeTiles(int tileType)
        {
            switch (tileType)
            {
                case 302:
                case 77:
                    yield return TileID.Anvils;
                    break;
                case 133:
                    yield return TileID.Anvils;
                    yield return 77;
                    break;
                case 134:
                    yield return TileID.Anvils;
                    break;
                case 354:
                case 469:
                case 487:
                    yield return TileID.Furnaces;
                    break;
                case 355:
                    yield return TileID.Furnaces;
                    yield return TileID.Hellforge;
                    break;
            }
        }
    }
}
