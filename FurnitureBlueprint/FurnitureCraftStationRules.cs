using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 制作台兼容：死/无等上位工作台 ? 普通工作台；锯木机/生命木织机互不替代。
    /// </summary>
    internal static class FurnitureCraftStationRules
    {
        /// <summary>方块（锚点块）/墙/平台：不参与制作台分流与「仅特殊台」剔除。</summary>
        public static bool IsStationCollectionExempt(int productType, int materialBlock = ItemID.None)
        {
            if (productType <= ItemID.None)
                return false;

            if (materialBlock > ItemID.None && productType == materialBlock)
                return true;

            if (!FurnitureRecognitionCaches.TryGetProbe(productType, out Item probe))
                return false;

            if (probe.createWall > WallID.None && probe.createTile < TileID.Dirt)
                return true;

            if (probe.createTile >= TileID.Dirt && TileID.Sets.Platforms[probe.createTile])
                return true;

            if (FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
                return true;

            if (FurnitureSlotClassifier.TryGetSlot(probe, out FurnitureSlotKind kind, out _))
            {
                kind = FurnitureWikiSlots.NormalizeClassified(kind);
                if (kind is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    return true;
            }

            return false;
        }

        public static bool IsVanillaWorkbenchTile(int tileId) => tileId == TileID.WorkBenches;

        public static bool IsVanillaSawmillTile(int tileId) => tileId == TileID.Sawmill;

        public static bool IsVanillaLivingLoomTile(int tileId) => tileId == TileID.LivingLoom;

        /// <summary>种子套组是否使用「可接管普通工作台」的模组上位台（死/无等）。</summary>
        public static bool UsesEnhancedWorkbenchSubstitution(int seedType, FurnitureCraftStationProfile profile)
        {
            if (!profile.IsConstrained)
                return false;

            if (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
                return true;

            if (profile.ImpliesLivingWoodStation || profile.ImpliesSawmillStation)
                return false;

            return profile.HasModWorkbenchStation() && !profile.HasVanillaSpecialStation();
        }

        /// <summary>
        /// 配方是否「仅能」在普通工作台以外的台制作（相对 WorkBenches 而言）。
        /// 用于：生命木织机/锯木机套组筛掉死/无专属家具。
        /// </summary>
        public static bool RecipeRequiresExclusiveStation(Recipe recipe, FurnitureCraftStationProfile seedProfile)
        {
            if (recipe?.requiredTile == null || recipe.requiredTile.Count == 0)
                return false;

            if (!seedProfile.IsConstrained || !seedProfile.RecipeCompatible(recipe))
                return false;

            var workbenchOnly = new FurnitureCraftStationProfile();
            workbenchOnly.AddStationTileForExpansion(TileID.WorkBenches);

            if (workbenchOnly.RecipeCompatible(recipe))
                return false;

            if (RecipeRequiresOnlyVanillaSpecialStation(recipe))
                return false;

            return true;
        }

        /// <summary>产物是否存在「仅上位台/专属台」合成路径（相对种子 profile 而言）。</summary>
        public static bool ProductHasExclusiveStationPath(
            int productType,
            FurnitureCraftStationProfile seedProfile,
            int seedType,
            int materialBlock = ItemID.None)
        {
            if (productType <= ItemID.None || !seedProfile.IsConstrained)
                return false;

            if (IsStationCollectionExempt(productType, materialBlock))
                return false;

            bool anyExclusive = false;
            bool anyShared = false;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                if (!seedProfile.RecipeCompatible(recipe))
                    continue;

                if (RecipeRequiresExclusiveStation(recipe, seedProfile))
                    anyExclusive = true;
                else
                    anyShared = true;
            }

            if (anyExclusive && !anyShared)
                return true;

            if (!UsesEnhancedWorkbenchSubstitution(seedType, seedProfile))
                return false;

            return anyExclusive && !anyShared;
        }

        /// <summary>非上位套组收集产物时：排除只能在上位台造的家具。</summary>
        public static bool ShouldExcludeFromProductCollect(
            int productType,
            FurnitureCraftStationProfile seedProfile,
            int seedType,
            int materialBlock = ItemID.None)
        {
            if (!seedProfile.IsConstrained)
                return false;

            if (IsStationCollectionExempt(productType, materialBlock))
                return false;

            if (UsesEnhancedWorkbenchSubstitution(seedType, seedProfile))
                return false;

            return ProductHasExclusiveStationPath(productType, seedProfile, seedType, materialBlock);
        }

        private static bool RecipeRequiresOnlyVanillaSpecialStation(Recipe recipe)
        {
            if (recipe?.requiredTile == null)
                return false;

            bool any = false;
            foreach (int tid in recipe.requiredTile)
            {
                if (tid < 0)
                    continue;
                any = true;
                if (!IsVanillaLivingLoomTile(tid) && !IsVanillaSawmillTile(tid))
                    return false;
            }

            return any;
        }
    }
}
