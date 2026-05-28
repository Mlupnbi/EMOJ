using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 材料块 StyleKey 族内的配方家具（如陨石砖锚点 → 消耗陨石锭的床/浴缸），仍由材料块族名锚定，非全模组扫描。
    /// </summary>
    public static class FurnitureMaterialFamilyRecipeExpander
    {
        public const int MaxRecipeScan = 6_000;
        public const int MaxAdd = 160;

        public static void AddFamilyFurnitureProducts(
            int materialBlock,
            FurnitureStyleSignature blockSig,
            HashSet<int> dest)
        {
            if (dest == null || materialBlock <= ItemID.None)
                return;

            if (RecipeAnalyzer.IsHighFanoutMaterial(materialBlock))
                return;

            string modKey = GetModKey(materialBlock);
            string family = FurnitureMaterialKeyNormalizer.Normalize(
                blockSig.StyleKey?.Trim() ?? FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock));
            if (family.Length < 4 || FurnitureTileSlotRegistry.IsWeakStyleKey(family))
                return;

            int added = 0;
            int scanned = 0;

            foreach (Recipe recipe in Main.recipe)
            {
                if (++scanned > MaxRecipeScan || added >= MaxAdd)
                    break;

                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                int product = recipe.createItem.type;
                if (GetModKey(product) != modKey || dest.Contains(product))
                    continue;

                if (!RecipeUsesMaterialFamilyIngredient(recipe, family))
                    continue;

                Item probe = new Item();
                probe.SetDefaults(product);
                bool wall = probe.createWall > WallID.None && probe.createTile < TileID.Dirt;
                if (!wall && !FurnitureCandidateFilter.IsPlaceableFurnitureItem(probe))
                    continue;

                string productFamily = FurnitureMaterialKeyNormalizer.Normalize(
                    FurnitureSetRecognizer.ExtractStyleKeyPublic(product));
                if (!FurnitureMaterialKeyNormalizer.SameMaterialFamily(family, productFamily))
                    continue;

                if (dest.Add(product))
                    added++;
            }

            if (added > 0)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"material family expand block={materialBlock} family={family} added={added} total={dest.Count}");
            }
        }

        private static bool RecipeUsesMaterialFamilyIngredient(Recipe recipe, string family)
        {
            if (recipe?.requiredItem == null)
                return false;

            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;

                string reqFamily = FurnitureMaterialKeyNormalizer.Normalize(
                    FurnitureSetRecognizer.ExtractStyleKeyPublic(req.type));
                if (FurnitureMaterialKeyNormalizer.SameMaterialFamily(family, reqFamily))
                    return true;

                int gid = RecipeAnalyzer.GetAcceptedGroupId(recipe, i);
                if (gid < 0 || gid >= RecipeGroup.recipeGroups.Count)
                    continue;

                RecipeGroup group = RecipeGroup.recipeGroups[gid];
                if (group?.ValidItems == null)
                    continue;

                foreach (int groupType in FurnitureRecipeGroupSampling.Sample(group.ValidItems, family))
                {
                    string gFamily = FurnitureMaterialKeyNormalizer.Normalize(
                        FurnitureSetRecognizer.ExtractStyleKeyPublic(groupType));
                    if (FurnitureMaterialKeyNormalizer.SameMaterialFamily(family, gFamily))
                        return true;
                }
            }

            return false;
        }

        private static string GetModKey(int itemType)
        {
            ModItem mi = ItemLoader.GetItem(itemType);
            return mi == null ? "Terraria" : mi.Mod.Name;
        }
    }
}
