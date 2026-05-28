using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 生命木/生命红木：配方原料为木材(9)/红木(620)，套组由生命木织机等制作台区分（非专用「生命木块」）。
    /// </summary>
    public static class FurnitureVanillaLivingWoodBridge
    {
        public const string LivingWoodStyleKey = "LivingWood";
        public const string LivingRichMahoganyStyleKey = "LivingRichMahogany";

        /// <summary>种子套组在配方上实际消耗的原木类型（若有）。</summary>
        public static bool TryGetRecipeWoodMaterial(int seedType, out int woodMaterial)
        {
            woodMaterial = ItemID.None;
            if (seedType <= ItemID.None)
                return false;

            if (FurnitureGenericWoodLineageRules.IsLivingRichMahoganySeed(seedType))
            {
                woodMaterial = ItemID.RichMahogany;
                return true;
            }

            if (IsLivingWoodFamily(seedType)
                || ShouldTreatRegularWoodAsLivingWood(seedType)
                || IsLivingWoodNamedSeed(seedType))
            {
                woodMaterial = ItemID.Wood;
                return true;
            }

            return false;
        }

        public static void RegisterRecipeGroups()
        {
            if (ItemID.Wood > ItemID.None)
                FurnitureRecipeGroupMaterialBridge.RegisterRepresentative(RecipeGroupID.Wood, ItemID.Wood);
        }

        /// <summary>正推候选集用的材料：生命木系 → 木材，生命红木 → 红木（不再映射到门/徽章）。</summary>
        public static bool TryGetProductExpansionMaterial(int seedType, int anchorType, out int materialType)
        {
            if (TryGetRecipeWoodMaterial(seedType, out int wood))
            {
                materialType = wood;
                return true;
            }

            materialType = anchorType > ItemID.None ? anchorType : seedType;
            return false;
        }

        /// <summary>过滤/选优用的套组签名（StyleKey 为 LivingWood / LivingRichMahogany，材料仍为木/红木）。</summary>
        public static bool TryGetSetSignature(int seedType, out FurnitureStyleSignature signature)
        {
            signature = default;
            if (seedType <= ItemID.None)
                return false;

            if (FurnitureGenericWoodLineageRules.IsLivingRichMahoganySeed(seedType))
            {
                signature = new FurnitureStyleSignature
                {
                    ModKey = "Terraria",
                    StyleKey = LivingRichMahoganyStyleKey,
                    SeedIsMaterialBlock = false
                };
                return true;
            }

            if (!ShouldTreatRegularWoodAsLivingWood(seedType) && !IsLivingWoodFamily(seedType))
                return false;

            signature = new FurnitureStyleSignature
            {
                ModKey = "Terraria",
                StyleKey = LivingWoodStyleKey,
                SeedIsMaterialBlock = false
            };
            return true;
        }

        /// <summary>倒推：生命木种子若反推到 Wood，保持 Wood（不改成门/块）。</summary>
        public static int RedirectReverseAnchor(int seedType, int resolvedIngredient)
        {
            if (resolvedIngredient <= ItemID.None)
                return resolvedIngredient;

            if (TryGetRecipeWoodMaterial(seedType, out int wood) && IsRegularWoodMaterial(resolvedIngredient))
            {
                if (wood != resolvedIngredient)
                {
                    FurnitureBlueprintLog.InfoFull(
                        $"livingwood normalize reverse seed={seedType} ing={resolvedIngredient} -> wood={wood}");
                }
                return wood;
            }

            return resolvedIngredient;
        }

        /// <summary>倒推打分：生命木种子遇到 Wood 原料时仍对 Wood 计分。</summary>
        public static int RedirectIngredientForScoring(int seedType, int ingredientType) =>
            ingredientType;

        public static bool ShouldTreatRegularWoodAsLivingWood(int seedType)
        {
            if (!IsRegularVanillaWoodFamily(seedType))
                return false;

            FurnitureCraftStationProfile profile = FurnitureCraftStationProfile.FromSeed(seedType);
            return profile.IsConstrained
                && profile.ImpliesLivingWoodStation
                && !profile.ImpliesSawmillStation;
        }

        public static bool IsRegularWoodMaterial(int itemType)
        {
            if (itemType <= ItemID.None)
                return false;

            if (itemType == ItemID.Wood)
                return true;

            string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
            return key.Equals("Wood", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRichMahoganyMaterial(int itemType)
        {
            if (itemType == ItemID.RichMahogany)
                return true;

            string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
            return key.Equals("RichMahogany", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Mahogany", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLivingWoodFamily(int itemType)
        {
            if (itemType <= ItemID.None)
                return false;

            string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
            if (key.Contains("LivingWood", StringComparison.OrdinalIgnoreCase)
                || key.Contains("Living", StringComparison.OrdinalIgnoreCase) && key.Contains("Wood", StringComparison.OrdinalIgnoreCase))
                return true;

            string name = FurnitureItemDefaults.SafeItemName(itemType).ToLowerInvariant();
            return name.Contains("living wood") || name.Contains("生命木");
        }

        public static bool IsRegularVanillaWoodFamily(int itemType)
        {
            if (itemType <= ItemID.None || IsLivingWoodFamily(itemType))
                return false;

            if (itemType == ItemID.Wood)
                return true;

            string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
            if (key.StartsWith("Wooden", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Wood", StringComparison.OrdinalIgnoreCase))
                return true;

            Item probe = new Item();
            probe.SetDefaults(itemType);
            if (probe.type != itemType)
                return false;

            ModItem mi = ItemLoader.GetItem(itemType);
            if (mi != null && mi.Mod.Name != "Terraria")
                return false;

            if (key.EndsWith("Wood", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("Living", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static bool IsLivingWoodNamedSeed(int seedType)
        {
            string name = FurnitureItemDefaults.SafeItemName(seedType).ToLowerInvariant();
            return name.Contains("生命木") && !name.Contains("生命红木");
        }
    }
}
