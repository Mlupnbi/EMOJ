using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.ItemHub.Data
{
    /// <summary>
    /// ���䷽ requiredTile��ģ�� AdjTiles �� Creative �ֹ��ϳ�վ�б��ռ����ϳ�վ�������͡�
    /// </summary>
    public static class HubStationTileIndex
    {
        public static readonly HashSet<int> StationTileIdsFromRecipes = new HashSet<int>();
        public static bool StationTileIndexBuilt;

        public static void Reset()
        {
            StationTileIdsFromRecipes.Clear();
            StationTileIndexBuilt = false;
        }

        public static void BuildStationTileIndexFromRecipes()
        {
            if (StationTileIndexBuilt || Main.recipe == null)
                return;

            try
            {
                if (ContentSamples.CreativeHelper._manualCraftingStations != null)
                {
                    foreach (int tid in ContentSamples.CreativeHelper._manualCraftingStations)
                    {
                        if (tid >= 0)
                            AddStationTile(tid);
                    }
                }
            }
            catch
            {
                /* */
            }

            int n = Recipe.numRecipes;
            for (int i = 0; i < n; i++)
            {
                Recipe r = Main.recipe[i];
                if (r?.requiredTile == null)
                    continue;
                foreach (int tid in r.requiredTile)
                    AddStationTile(tid);
            }

            StationTileIndexBuilt = true;
        }

        private static void AddStationTile(int tid)
        {
            if (tid < 0)
                return;
            StationTileIdsFromRecipes.Add(tid);
            ModTile mt = TileLoader.GetTile(tid);
            if (mt?.AdjTiles == null)
                return;
            foreach (int adj in mt.AdjTiles)
            {
                if (adj >= 0)
                    StationTileIdsFromRecipes.Add(adj);
            }
        }
    }
}
