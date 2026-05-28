using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 模组家具材料锚定：placement line 专用块 / 泛用木血统 / 高扇出材料与 style 对齐。
    /// 不含特定 mod 或中文套组名硬编码。
    /// </summary>
    internal static class FurnitureSetMaterialRules
    {
        public static bool UsesModLineageAnchor(int seedType)
        {
            if (seedType <= ItemID.None || UsesModSpecificMaterialBlock(seedType))
                return false;

            ModItem mi = ItemLoader.GetItem(seedType);
            if (mi == null || mi.Mod.Name == "Terraria")
                return false;

            if (string.IsNullOrWhiteSpace(FurnitureStylePrefixCatalog.ResolveStylePrefix(seedType)))
                return false;

            int anchor = FurnitureReverseSeedProbeCache.Ensure(seedType).BestAnchorIngredient;
            if (anchor <= ItemID.None || !IsGenericCraftMaterial(anchor))
                return false;

            return !MaterialAlignsWithSeedStyle(seedType, anchor);
        }

        /// <summary>种子图格线/配方可解析出与 style 对齐的 mod 专用材料块（非泛用木/高扇出）。</summary>
        public static bool UsesModSpecificMaterialBlock(int seedType)
        {
            if (seedType <= ItemID.None)
                return false;

            ModItem mi = ItemLoader.GetItem(seedType);
            if (mi == null || mi.Mod.Name == "Terraria")
                return false;

            int block = FurniturePlacementLineMaterialResolver.TryResolveBlockFromFurnitureSeed(seedType);
            if (block <= ItemID.None)
                return false;

            ModItem blockMod = ItemLoader.GetItem(block);
            if (blockMod == null || blockMod.Mod.Name == "Terraria")
                return false;

            if (IsGenericCraftMaterial(block))
                return false;

            return MaterialAlignsWithSeedStyle(seedType, block);
        }

        public static bool IsAllowedModLineageWood(int materialType, int seedType)
        {
            if (!UsesModLineageAnchor(seedType))
                return false;

            return materialType is ItemID.Wood or ItemID.RichMahogany;
        }

        public static bool TryGetModLineageSetSignature(int seedType, out FurnitureStyleSignature signature)
        {
            signature = default;
            if (!UsesModLineageAnchor(seedType))
                return false;

            ModItem mi = ItemLoader.GetItem(seedType);
            string modKey = mi?.Mod.Name ?? "Terraria";
            string styleKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            if (string.IsNullOrWhiteSpace(styleKey))
                styleKey = FurnitureSetRecognizer.ExtractDisplayLineageMoniker(seedType);

            FurnitureStyleSignature fromSeed = FurnitureStyleSignature.FromItemType(seedType);
            signature = new FurnitureStyleSignature
            {
                ModKey = modKey,
                StyleKey = styleKey,
                PlacementTile = fromSeed.PlacementTile,
                PlacementStyle = fromSeed.PlacementStyle,
                UsesPlacementStyleLine = fromSeed.UsesPlacementStyleLine,
                SeedIsMaterialBlock = false
            };
            return !string.IsNullOrWhiteSpace(styleKey);
        }

        public static bool IsForbiddenGenericMaterial(int materialType, int seedType)
        {
            if (materialType <= ItemID.None)
                return true;

            if (!UsesModLineageAnchor(seedType))
                return IsMisalignedGenericMaterialForModSeed(seedType, materialType);

            if (IsAllowedModLineageWood(materialType, seedType))
                return false;

            if (RecipeAnalyzer.IsHighFanoutMaterial(materialType))
            {
                ModItem blockMod = ItemLoader.GetItem(materialType);
                if (blockMod == null || blockMod.Mod.Name == "Terraria")
                    return true;
            }

            if (materialType is ItemID.Wood or ItemID.BorealWood or ItemID.PalmWood or ItemID.RichMahogany
                or ItemID.Ebonwood or ItemID.Shadewood or ItemID.Pearlwood)
                return true;

            return false;
        }

        /// <summary>所有 mod 种子：优先 placement line 专用块，其次血统/反推锚点，避免落回错位的泛用材料。</summary>
        public static int ResolveModMaterialBlock(int seedType, int currentMaterial)
        {
            ModItem seedMod = ItemLoader.GetItem(seedType);
            if (seedMod == null || seedMod.Mod.Name == "Terraria")
                return currentMaterial;

            int placementBlock = FurniturePlacementLineMaterialResolver.TryResolveBlockFromFurnitureSeed(seedType);
            if (placementBlock > ItemID.None
                && !IsForbiddenGenericMaterial(placementBlock, seedType)
                && MaterialAlignsWithSeedStyle(seedType, placementBlock))
                return placementBlock;

            if (UsesModLineageAnchor(seedType))
                return ResolveModLineageMaterialBlock(seedType, currentMaterial);

            if (currentMaterial > ItemID.None && IsMisalignedGenericMaterialForModSeed(seedType, currentMaterial))
            {
                foreach (int candidate in FurnitureReverseSeedProbeCache.Ensure(seedType).PickerCandidates)
                {
                    if (candidate <= ItemID.None || candidate == currentMaterial)
                        continue;

                    if (MaterialAlignsWithSeedStyle(seedType, candidate)
                        && !IsGenericCraftMaterial(candidate))
                        return candidate;
                }
            }

            return currentMaterial;
        }

        public static int ResolveModLineageMaterialBlock(int seedType, int currentMaterial)
        {
            if (!UsesModLineageAnchor(seedType))
                return currentMaterial;

            if (currentMaterial > ItemID.None && !IsForbiddenGenericMaterial(currentMaterial, seedType))
                return currentMaterial;

            if (IsForbiddenGenericMaterial(currentMaterial, seedType))
                return ItemID.None;

            return currentMaterial;
        }

        public static void ApplyLivingWoodRecipeMaterial(int seedType, ref int materialBlock)
        {
            if (seedType <= ItemID.None || UsesModLineageAnchor(seedType) || UsesModSpecificMaterialBlock(seedType))
                return;

            if (FurnitureVanillaLivingWoodBridge.TryGetRecipeWoodMaterial(seedType, out int recipeWood))
                materialBlock = recipeWood;
        }

        internal static bool MaterialAlignsWithSeedStyle(int seedType, int materialBlock)
        {
            if (seedType <= ItemID.None || materialBlock <= ItemID.None)
                return false;

            if (FurnitureStylePrefixCatalog.ProductMatchesSeedStyle(materialBlock, seedType, materialBlock))
                return true;

            string seedStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            string matStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
            if (string.IsNullOrWhiteSpace(seedStyle) || string.IsNullOrWhiteSpace(matStyle))
                return false;

            return FurnitureStyleSignature.StyleKeyFuzzyMatch(seedStyle, matStyle)
                || FurnitureMaterialKeyNormalizer.SameMaterialFamily(seedStyle, matStyle);
        }

        internal static bool IsMisalignedGenericMaterialForModSeed(int seedType, int materialBlock)
        {
            if (seedType <= ItemID.None || materialBlock <= ItemID.None)
                return false;

            ModItem seedMod = ItemLoader.GetItem(seedType);
            if (seedMod == null || seedMod.Mod.Name == "Terraria")
                return false;

            if (!IsGenericCraftMaterial(materialBlock))
                return false;

            return !MaterialAlignsWithSeedStyle(seedType, materialBlock);
        }

        private static bool IsGenericCraftMaterial(int materialType)
        {
            if (materialType <= ItemID.None)
                return false;

            if (RecipeAnalyzer.IsHighFanoutMaterial(materialType))
                return true;

            return materialType is ItemID.Wood or ItemID.BorealWood or ItemID.PalmWood or ItemID.RichMahogany
                or ItemID.Ebonwood or ItemID.Shadewood or ItemID.Pearlwood;
        }
    }
}
