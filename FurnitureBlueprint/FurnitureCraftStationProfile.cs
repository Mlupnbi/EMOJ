using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 从种子相关配方收集 requiredTile，用于区分「同木不同制作台」的家具套组（生命木 / 普通木 / 模组死木等）。
    /// </summary>
    public sealed class FurnitureCraftStationProfile
    {
        public readonly HashSet<int> StationTiles = new HashSet<int>();

        public bool IsConstrained => StationTiles.Count > 0;

        public bool ImpliesLivingWoodStation =>
            ContainsStation(TileID.LivingLoom);

        public bool ImpliesSawmillStation =>
            ContainsStation(TileID.Sawmill);

        public static FurnitureCraftStationProfile FromSeed(int seedType)
        {
            var profile = new FurnitureCraftStationProfile();
            if (seedType <= ItemID.None)
                return profile;

            bool modLineage = FurnitureSetMaterialRules.UsesModLineageAnchor(seedType);

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(seedType))
            {
                if (modLineage)
                    profile.AddRecipeStationsDirectOnly(recipe);
                else
                    profile.AddRecipeStations(recipe);
            }

            if (!profile.IsConstrained)
            {
                foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(seedType))
                {
                    if (modLineage)
                        profile.AddRecipeStationsDirectOnly(recipe);
                    else
                        profile.AddRecipeStations(recipe);
                }
            }

            if (modLineage)
                profile.StripIncidentalVanillaSpecialStations(seedType);

            if (FurnitureCraftStationRules.UsesEnhancedWorkbenchSubstitution(seedType, profile))
                profile.ExpandEnhancedWorkbenchSubstitution(modLineage);

            if (modLineage)
                profile.StripIncidentalVanillaSpecialStations(seedType);

            return profile;
        }

        /// <summary>死/无等上位台：显式并入普通工作台（AdjTiles 未声明时仍可用）。</summary>
        public void ExpandEnhancedWorkbenchSubstitution(bool modLineageAnchor = false)
        {
            AddStationTileDirect(TileID.WorkBenches);
            if (modLineageAnchor)
                ExpandReverseAdjacencyStationsDirectOnly();
            else
                ExpandReverseAdjacencyStations();
        }

        internal void AddStationTileForExpansion(int tileId) => AddStationTile(tileId);

        internal bool HasModWorkbenchStation()
        {
            foreach (int tid in StationTiles)
            {
                if (tid < TileID.Count)
                    continue;
                if (FurnitureCraftStationRules.IsVanillaWorkbenchTile(tid)
                    || FurnitureCraftStationRules.IsVanillaSawmillTile(tid)
                    || FurnitureCraftStationRules.IsVanillaLivingLoomTile(tid))
                    continue;
                return true;
            }

            return false;
        }

        internal bool HasVanillaSpecialStation() =>
            ImpliesLivingWoodStation || ImpliesSawmillStation;

        public bool RecipeCompatible(Recipe recipe)
        {
            if (recipe?.requiredTile == null || recipe.requiredTile.Count == 0)
                return !IsConstrained;

            if (!IsConstrained)
                return true;

            foreach (int tid in recipe.requiredTile)
            {
                if (tid < 0)
                    continue;

                if (StationTiles.Contains(tid))
                    return true;

                if (StationTilesOverlapAdjacency(tid))
                    return true;
            }

            return false;
        }

        public int ScoreRecipeMatch(Recipe recipe)
        {
            if (!IsConstrained || recipe?.requiredTile == null || recipe.requiredTile.Count == 0)
                return 0;

            int score = 0;
            foreach (int tid in recipe.requiredTile)
            {
                if (tid < 0)
                    continue;

                if (StationTiles.Contains(tid))
                    score += 80;
                else if (StationTilesOverlapAdjacency(tid))
                    score += 40;
            }

            return score;
        }

        private void AddRecipeStations(Recipe recipe)
        {
            if (recipe?.requiredTile == null)
                return;

            foreach (int tid in recipe.requiredTile)
                AddStationTile(tid);
        }

        private void AddRecipeStationsDirectOnly(Recipe recipe)
        {
            if (recipe?.requiredTile == null)
                return;

            foreach (int tid in recipe.requiredTile)
                AddStationTileDirect(tid);
        }

        /// <summary>模组上位台 AdjTiles 常误带入锯木机/生命木织机，血统套组只保留配方直接声明的台。</summary>
        internal void StripIncidentalVanillaSpecialStations(int seedType)
        {
            var direct = new HashSet<int>();
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(seedType))
                CollectDirectRequiredTiles(recipe, direct);

            if (direct.Count == 0)
            {
                foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(seedType))
                    CollectDirectRequiredTiles(recipe, direct);
            }

            if (!direct.Contains(TileID.Sawmill))
                StationTiles.Remove(TileID.Sawmill);
            if (!direct.Contains(TileID.LivingLoom))
                StationTiles.Remove(TileID.LivingLoom);
        }

        private static void CollectDirectRequiredTiles(Recipe recipe, HashSet<int> direct)
        {
            if (recipe?.requiredTile == null)
                return;

            foreach (int tid in recipe.requiredTile)
            {
                if (tid >= 0)
                    direct.Add(tid);
            }
        }

        private void AddStationTileDirect(int tid)
        {
            if (tid >= 0)
                StationTiles.Add(tid);
        }

        private void AddStationTile(int tid)
        {
            if (tid < 0)
                return;

            StationTiles.Add(tid);
            ModTile mt = TileLoader.GetTile(tid);
            if (mt?.AdjTiles == null)
                return;

            foreach (int adj in mt.AdjTiles)
            {
                if (adj >= 0)
                    StationTiles.Add(adj);
            }
        }

        /// <summary>反向 AdjTiles：其它台把本站列为替代目标时，也视为可用（不展开 AdjTiles 链）。</summary>
        private void ExpandReverseAdjacencyStationsDirectOnly()
        {
            var add = new List<int>();
            for (int tid = TileID.Count; tid < TileLoader.TileCount; tid++)
            {
                ModTile mt = TileLoader.GetTile(tid);
                if (mt?.AdjTiles == null)
                    continue;

                foreach (int adj in mt.AdjTiles)
                {
                    if (adj >= 0 && StationTiles.Contains(adj))
                    {
                        add.Add(tid);
                        break;
                    }
                }
            }

            for (int i = 0; i < add.Count; i++)
                AddStationTileDirect(add[i]);
        }

        /// <summary>反向 AdjTiles：其它台把本站列为替代目标时，也视为可用。</summary>
        private void ExpandReverseAdjacencyStations()
        {
            var add = new List<int>();
            for (int tid = TileID.Count; tid < TileLoader.TileCount; tid++)
            {
                ModTile mt = TileLoader.GetTile(tid);
                if (mt?.AdjTiles == null)
                    continue;

                foreach (int adj in mt.AdjTiles)
                {
                    if (adj >= 0 && StationTiles.Contains(adj))
                    {
                        add.Add(tid);
                        break;
                    }
                }
            }

            for (int i = 0; i < add.Count; i++)
                AddStationTile(add[i]);
        }

        private bool ContainsStation(int tileId)
        {
            if (tileId < 0)
                return false;

            if (StationTiles.Contains(tileId))
                return true;

            ModTile mt = TileLoader.GetTile(tileId);
            if (mt?.AdjTiles == null)
                return false;

            foreach (int adj in mt.AdjTiles)
            {
                if (adj >= 0 && StationTiles.Contains(adj))
                    return true;
            }

            return false;
        }

        private bool StationTilesOverlapAdjacency(int recipeTileId)
        {
            ModTile mt = TileLoader.GetTile(recipeTileId);
            if (mt?.AdjTiles == null)
                return false;

            foreach (int adj in mt.AdjTiles)
            {
                if (adj >= 0 && StationTiles.Contains(adj))
                    return true;
            }

            return false;
        }
    }
}
