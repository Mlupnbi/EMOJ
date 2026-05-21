using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.Research
{
    public static class RecipeAnalyzer
    {
        public static bool IsJourneyWorld =>
            Main.ActiveWorldFileData != null && Main.ActiveWorldFileData.GameMode == GameModeID.Creative;

        public static bool IsFullyResearched(int itemType)
        {
            int? remaining = CreativeUI.GetSacrificesRemaining(itemType);
            return remaining.HasValue && remaining.Value <= 0;
        }

        /// <summary>兼容旧调用名：与 <see cref="IsFullyResearched"/> 相同�?</summary>
        public static bool IsResearched(int itemType) => IsFullyResearched(itemType);

        public static int? GetSacrificesRemaining(int itemType) => CreativeUI.GetSacrificesRemaining(itemType);

        /// <summary>旅途研究目录中解锁无限复制所需的总献祭数；不在目录则返回 false�?</summary>
        public static bool TryGetJourneyUnlockQuota(int itemType, out int amountNeeded)
        {
            amountNeeded = 0;
            return CreativeItemSacrificesCatalog.Instance != null
                && CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(itemType, out amountNeeded)
                && amountNeeded > 0;
        }

        /// <summary>当前献祭进度是否达到或超过「需求量的一半」（向上取整）�?</summary>
        public static bool IsJourneyResearchProgressAtLeastHalf(int itemType)
        {
            if (!TryGetJourneyUnlockQuota(itemType, out int needed))
                return false;
            int? rem = GetSacrificesRemaining(itemType);
            if (!rem.HasValue)
                return false;
            int sacrificed = needed - rem.Value;
            if (sacrificed < 0)
                sacrificed = 0;
            return sacrificed * 2 >= needed;
        }

        /// <summary>所有「以 seed 为材料之一」的配方产物�? type 去重集合（全局查询基准）�?</summary>
        public static List<int> GetAllProductTypesUsingMaterial(int seedType)
        {
            var set = new HashSet<int>();
            if (seedType <= 0) return new List<int>();
            foreach (Recipe recipe in Main.recipe)
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;
                if (!RecipeUsesIngredient(recipe, seedType))
                    continue;
                set.Add(recipe.createItem.type);
            }
            return set.OrderBy(t => t).ToList();
        }

        public static List<int> GetDeepCraftableProducts(Item seedItem)
        {
            var result = new List<int>();
            if (seedItem.IsAir) return result;
            int seedType = seedItem.type;

            foreach (Recipe recipe in Main.recipe)
            {
                if (recipe.createItem == null || recipe.createItem.IsAir)
                    continue;
                if (IsFullyResearched(recipe.createItem.type))
                    continue;
                if (!RecipeUsesIngredient(recipe, seedType))
                    continue;
                if (!AllMaterialsResearched(recipe))
                    continue;
                if (!result.Contains(recipe.createItem.type))
                    result.Add(recipe.createItem.type);
            }
            return result;
        }

        public static List<int> FilterJourneyYellow(int seedType, IEnumerable<int> fromProducts)
        {
            return fromProducts.Where(t => IsFullyResearched(t)).Distinct().OrderBy(t => t).ToList();
        }

        public static List<int> FilterJourneyGreen(Item seedItem) => GetDeepCraftableProducts(seedItem);

        public static List<int> FilterJourneyBlue(int seedType)
        {
            var set = new HashSet<int>();
            foreach (Recipe recipe in Main.recipe)
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;
                if (!RecipeUsesIngredient(recipe, seedType))
                    continue;
                if (AllMaterialsResearched(recipe))
                    continue;
                if (!IsFullyResearched(recipe.createItem.type))
                    set.Add(recipe.createItem.type);
            }
            if (!IsFullyResearched(seedType))
                set.Add(seedType);
            return set.OrderBy(t => t).ToList();
        }

        public static List<int> FilterAdventurePurple(int seedType, out Dictionary<int, bool> canCraftByProduct)
        {
            canCraftByProduct = new Dictionary<int, bool>();
            var list = GetAllProductTypesUsingMaterial(seedType);
            Player p = Main.LocalPlayer;
            if (p == null)
            {
                foreach (int product in list)
                    canCraftByProduct[product] = false;
                return list.OrderBy(t => t).ToList();
            }
            foreach (int product in list)
            {
                bool any = false;
                foreach (Recipe recipe in Main.recipe)
                {
                    if (recipe?.createItem == null || recipe.createItem.IsAir || recipe.createItem.type != product)
                        continue;
                    if (!RecipeUsesIngredient(recipe, seedType))
                        continue;
                    if (PlayerCanCraftRecipe(recipe, p))
                    {
                        any = true;
                        break;
                    }
                }
                canCraftByProduct[product] = any;
            }
            Dictionary<int, bool> d = canCraftByProduct;
            return list
                .OrderByDescending(t => d.TryGetValue(t, out bool c) && c)
                .ThenBy(t => t)
                .ToList();
        }

        public static ResearchFaceMode GetDefaultFaceForJourneySeed(Item seed)
        {
            if (seed.IsAir) return ResearchFaceMode.Green;
            return IsFullyResearched(seed.type) ? ResearchFaceMode.Green : ResearchFaceMode.Blue;
        }

        private static bool RecipeUsesIngredient(Recipe recipe, int itemType)
        {
            int n = recipe.requiredItem.Count;
            for (int i = 0; i < n; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir) continue;
                if (req.type == itemType) return true;
                int gid = i < recipe.acceptedGroups.Count ? recipe.acceptedGroups[i] : -1;
                if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
                {
                    RecipeGroup group = RecipeGroup.recipeGroups[gid];
                    if (group != null && group.ContainsItem(itemType)) return true;
                }
            }
            return false;
        }

        public static bool AllMaterialsResearched(Recipe recipe)
        {
            int n = recipe.requiredItem.Count;
            for (int i = 0; i < n; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir) continue;
                if (!IsFullyResearched(req.type))
                {
                    bool foundAlternative = false;
                    int gid = i < recipe.acceptedGroups.Count ? recipe.acceptedGroups[i] : -1;
                    if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
                    {
                        RecipeGroup group = RecipeGroup.recipeGroups[gid];
                        if (group != null && group.ValidItems.Any(IsFullyResearched))
                        {
                            foundAlternative = true;
                        }
                    }
                    if (!foundAlternative) return false;
                }
            }
            return true;
        }

        public static List<Recipe> GetRecipesForItem(int itemType)
        {
            var recipes = new List<Recipe>();
            foreach (Recipe recipe in Main.recipe)
            {
                if (recipe.createItem != null && !recipe.createItem.IsAir && recipe.createItem.type == itemType)
                    recipes.Add(recipe);
            }
            return recipes;
        }

        public static bool CanBeResearched(int itemType)
        {
            foreach (Recipe recipe in Main.recipe)
            {
                if (recipe.createItem == null || recipe.createItem.IsAir)
                    continue;
                if (recipe.createItem.type != itemType)
                    continue;
                if (AllMaterialsResearched(recipe))
                    return true;
            }
            return false;
        }

        public static bool PlayerCanCraftRecipe(Recipe recipe, Player player)
        {
            if (player == null || recipe == null) return false;
            int n = recipe.requiredItem.Count;
            for (int i = 0; i < n; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir) continue;
                int need = req.stack;
                if (need <= 0) continue;
                int gid = i < recipe.acceptedGroups.Count ? recipe.acceptedGroups[i] : -1;
                if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count && RecipeGroup.recipeGroups[gid] != null)
                {
                    int have = CountGroupInInventory(RecipeGroup.recipeGroups[gid], player);
                    if (have < need) return false;
                }
                else
                {
                    int have = CountItemInCraftingSources(player, req.type);
                    if (have < need) return false;
                }
            }
            return true;
        }

        public static bool PlayerCanCraftAnyRecipeForProduct(int productType, int mustUseMaterialType, Player player)
        {
            foreach (Recipe recipe in Main.recipe)
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir || recipe.createItem.type != productType)
                    continue;
                if (!RecipeUsesIngredient(recipe, mustUseMaterialType))
                    continue;
                if (PlayerCanCraftRecipe(recipe, player))
                    return true;
            }
            return false;
        }

        private static int CountItemInCraftingSources(Player player, int itemType)
        {
            int c = 0;
            for (int i = 0; i < 58; i++)
            {
                Item it = player.inventory[i];
                if (!it.IsAir && it.type == itemType) c += it.stack;
            }
            void addBank(Item[] bank)
            {
                if (bank == null) return;
                foreach (Item it in bank)
                {
                    if (it != null && !it.IsAir && it.type == itemType) c += it.stack;
                }
            }
            addBank(player.bank.item);
            addBank(player.bank2.item);
            addBank(player.bank3.item);
            addBank(player.bank4.item);
            return c;
        }

        private static int CountGroupInInventory(RecipeGroup group, Player player)
        {
            if (group?.ValidItems == null) return 0;
            int c = 0;
            foreach (int valid in group.ValidItems)
                c += CountItemInCraftingSources(player, valid);
            return c;
        }
    }
}
