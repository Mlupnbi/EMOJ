using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 同木异套（生命木/死/无）：占位同类词与材料对齐；材料保持 Wood/红木，不靠假方块 redirect。
    /// </summary>
    internal static class FurnitureGenericWoodLineageRules
    {
        public const int PlaceholderCommonWordBoost = 5_600;
        public const int PlaceholderPartialLineageBoost = 2_100;

        public static bool IsGenericWoodMaterial(int materialBlock) =>
            materialBlock is ItemID.Wood or ItemID.RichMahogany
            || FurnitureVanillaLivingWoodBridge.IsRegularWoodMaterial(materialBlock)
            || FurnitureVanillaLivingWoodBridge.IsRichMahoganyMaterial(materialBlock);

        public static bool ShouldBoostPlaceholderCommonWords(int seedType, int materialBlock)
        {
            if (!IsWeakLineageSeed(seedType) || !IsMaterialAlignedWithSeedLineage(seedType, materialBlock))
                return false;

            // 模组血统（死/无/古代）：禁止 common-word 拉原版木/生命木，改由 style cluster 扩候选
            if (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
            {
                ModItem mi = ItemLoader.GetItem(seedType);
                if (mi != null && mi.Mod.Name != "Terraria")
                    return false;
            }

            return true;
        }

        public static bool IsWeakLineageSeed(int seedType)
        {
            if (seedType <= ItemID.None)
                return false;

            if (FurnitureVanillaLivingWoodBridge.TryGetRecipeWoodMaterial(seedType, out _))
                return true;

            if (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
                return true;

            if (FurnitureVanillaLivingWoodBridge.TryGetRecipeWoodMaterial(seedType, out _))
                return true;

            if (FurnitureSetMaterialRules.UsesModSpecificMaterialBlock(seedType))
                return false;

            string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            return !string.IsNullOrWhiteSpace(key) && FurnitureTileSlotRegistry.IsWeakStyleKey(key);
        }

        public static bool IsMaterialAlignedWithSeedLineage(int seedType, int materialBlock)
        {
            if (seedType <= ItemID.None || materialBlock <= ItemID.None)
                return false;

            if (FurnitureVanillaLivingWoodBridge.TryGetRecipeWoodMaterial(seedType, out int wood)
                && materialBlock == wood)
                return true;

            if (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
            {
                if (FurnitureSetMaterialRules.IsAllowedModLineageWood(materialBlock, seedType))
                    return true;

                if (FurnitureSetMaterialRules.IsForbiddenGenericMaterial(materialBlock, seedType))
                    return false;
            }

            string seedKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            string matKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
            if (FurnitureMaterialKeyNormalizer.StyleKeysMatch(seedKey, matKey))
                return true;

            string seedMoniker = FurnitureSetLineageScoring.ExtractSeedLineageMoniker(seedType);
            if (seedMoniker.Length >= 2)
            {
                string matName = FurnitureItemDefaults.SafeItemName(materialBlock);
                string matToken = FurnitureNameSignals.NormalizeMaterialDisplayName(matName);
                if (matToken.Length >= 2
                    && (seedMoniker.StartsWith(matToken, StringComparison.OrdinalIgnoreCase)
                        || matName.Contains(seedMoniker, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            ModItem seedMod = ItemLoader.GetItem(seedType);
            ModItem matMod = ItemLoader.GetItem(materialBlock);
            return seedMod != null && matMod != null && seedMod.Mod.Name == matMod.Mod.Name;
        }

        public static bool IsLivingRichMahoganySeed(int seedType)
        {
            if (seedType <= ItemID.None)
                return false;

            string name = FurnitureItemDefaults.SafeItemName(seedType).ToLowerInvariant();
            string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);

            if (name.Contains("生命红木") || name.Contains("living rich mahogany"))
                return true;

            return key.Contains("LivingRichMahogany", StringComparison.OrdinalIgnoreCase)
                || (key.Contains("Living", StringComparison.OrdinalIgnoreCase)
                    && key.Contains("Mahogany", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>不再把木/红木 redirect 到门、徽章等；制作站负责分流。</summary>
        public static int RedirectGenericWoodMaterial(int seedType, int materialBlock) => materialBlock;

        public static bool IsSchemeCacheable(FurnitureScheme scheme, int primarySeed, int materialBlock)
        {
            if (scheme == null || primarySeed <= ItemID.None || materialBlock <= ItemID.None)
                return false;

            Item probe = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(probe, materialBlock))
                return false;

            if (!FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
                return false;

            if (FurnitureSetMaterialRules.IsForbiddenGenericMaterial(materialBlock, primarySeed))
                return false;

            if (IsWeakLineageSeed(primarySeed) && !IsMaterialAlignedWithSeedLineage(primarySeed, materialBlock))
                return false;

            int wikiFilled = CountWikiFilled(scheme);
            if (wikiFilled < 12)
                return false;

            if (RecipeAnalyzer.IsHighFanoutMaterial(materialBlock) && IsWeakLineageSeed(primarySeed) && wikiFilled < 18)
                return false;

            return true;
        }

        private static int CountWikiFilled(FurnitureScheme scheme)
        {
            int n = 0;
            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                if (scheme.GetSlot(kind) > ItemID.None)
                    n++;
            }
            return n;
        }
    }
}
