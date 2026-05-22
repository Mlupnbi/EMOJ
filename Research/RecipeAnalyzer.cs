using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.Research
{
    public static class RecipeAnalyzer
    {
        private const int MaterialChainMaxDepth = 48;

        private static int _recipeIndexBuiltForCount = -1;
        private static List<Recipe>[] _producersByItemType;

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
            if (seedItem.IsAir)
                return result;

            int seedType = seedItem.type;
            EnsureProducerIndex();
            var memo = new Dictionary<int, bool>();
            var visiting = new HashSet<int>();

            foreach (int productType in GetAllProductTypesUsingMaterial(seedType))
            {
                if (IsFullyResearched(productType))
                    continue;
                if (IsShimmerItemTransformLink(seedType, productType))
                    continue;

                bool qualifies = false;
                foreach (Recipe recipe in GetProducersFor(productType))
                {
                    if (!RecipeUsesIngredient(recipe, seedType))
                        continue;
                    if (!IsGreenFaceCraftPathValid(recipe, seedType))
                        continue;
                    if (RecipeIngredientsResearchComplete(recipe, memo, visiting, 0))
                    {
                        qualifies = true;
                        break;
                    }
                }

                if (qualifies)
                    result.Add(productType);
            }

            return result;
        }

        private static void EnsureProducerIndex()
        {
            int recipeCount = Recipe.numRecipes;
            if (_producersByItemType != null && _recipeIndexBuiltForCount == recipeCount)
                return;

            int itemCount = ItemLoader.ItemCount;
            _producersByItemType = new List<Recipe>[itemCount];
            for (int i = 0; i < recipeCount; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;
                int product = recipe.createItem.type;
                if (product <= ItemID.None || product >= itemCount)
                    continue;
                _producersByItemType[product] ??= new List<Recipe>();
                _producersByItemType[product].Add(recipe);
            }
            _recipeIndexBuiltForCount = recipeCount;
        }

        private static IEnumerable<Recipe> GetProducersFor(int productType)
        {
            if (productType <= ItemID.None || _producersByItemType == null || productType >= _producersByItemType.Length)
                return Enumerable.Empty<Recipe>();
            return _producersByItemType[productType] ?? Enumerable.Empty<Recipe>();
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

        public static bool RecipeUsesIngredient(Recipe recipe, int itemType)
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

        /// <summary>??????????????????????????????</summary>
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
            EnsureProducerIndex();
            var recipes = new List<Recipe>();
            foreach (Recipe recipe in GetProducersFor(itemType))
                recipes.Add(recipe);
            return recipes;
        }

        public static bool IsInJourneyResearchCatalog(int itemType) =>
            TryGetJourneyUnlockQuota(itemType, out _);

        public static bool RecipeIngredientsResearchComplete(
            Recipe recipe,
            Dictionary<int, bool> memo = null,
            HashSet<int> visiting = null,
            int depth = 0)
        {
            memo ??= new Dictionary<int, bool>();
            visiting ??= new HashSet<int>();
            if (depth > MaterialChainMaxDepth)
                return false;
            if (!IsGreenFaceCraftPathValid(recipe))
                return false;

            EnsureProducerIndex();
            int n = recipe.requiredItem.Count;
            for (int i = 0; i < n; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;
                if (!IsMaterialResearchComplete(req.type, visiting, memo, depth + 1))
                    return false;
            }
            return true;
        }

        private static bool IsMaterialResearchComplete(
            int itemType,
            HashSet<int> visiting,
            Dictionary<int, bool> memo,
            int depth)
        {
            if (itemType <= ItemID.None)
                return true;
            if (depth > MaterialChainMaxDepth)
                return false;

            if (memo.TryGetValue(itemType, out bool cached))
                return cached;

            if (!IsInJourneyResearchCatalog(itemType))
            {
                memo[itemType] = true;
                return true;
            }

            if (IsFullyResearched(itemType))
            {
                memo[itemType] = true;
                return true;
            }

            if (visiting.Contains(itemType))
                return false;

            visiting.Add(itemType);
            bool ok = false;
            foreach (Recipe producer in GetProducersFor(itemType))
            {
                if (!IsGreenFaceCraftPathValid(producer))
                    continue;
                if (RecipeIngredientsResearchComplete(producer, memo, visiting, depth + 1))
                {
                    ok = true;
                    break;
                }
            }
            visiting.Remove(itemType);
            memo[itemType] = ok;
            return ok;
        }

        public static bool IsGreenFaceCraftPathValid(Recipe recipe, int seedType = -1)
        {
            if (recipe?.createItem == null || recipe.createItem.IsAir)
                return false;
            if (IsShimmerExcludedRecipe(recipe, seedType))
                return false;
            if (Main.netMode == NetmodeID.Server)
                return false;
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return false;
            return RecipeLoader.RecipeAvailable(recipe);
        }

        private static bool IsShimmerExcludedRecipe(Recipe recipe, int seedType = -1)
        {
            if (recipe?.createItem == null || recipe.createItem.IsAir)
                return false;

            int product = recipe.createItem.type;
            int decraftIdx = ShimmerTransforms.GetDecraftingRecipeIndex(product);
            if (decraftIdx >= 0 && decraftIdx < Recipe.numRecipes && Main.recipe[decraftIdx] == recipe)
                return true;

            if (seedType > ItemID.None && IsShimmerItemTransformLink(seedType, product))
                return true;

            return false;
        }

        private static bool IsShimmerItemTransformLink(int inputType, int outputType)
        {
            if (inputType > ItemID.None && inputType < ItemID.Sets.ShimmerTransformToItem.Length)
            {
                if (ItemID.Sets.ShimmerTransformToItem[inputType] == outputType)
                    return true;
            }
            if (outputType > ItemID.None && outputType < ItemID.Sets.ShimmerTransformToItem.Length)
            {
                if (ItemID.Sets.ShimmerTransformToItem[outputType] == inputType)
                    return true;
            }
            return false;
        }

        public static List<Recipe> GetGreenFaceQualifyingRecipes(int productType, int seedType)
        {
            var recipes = new List<Recipe>();
            if (seedType <= ItemID.None)
                return recipes;

            EnsureProducerIndex();
            var memo = new Dictionary<int, bool>();
            var visiting = new HashSet<int>();
            foreach (Recipe recipe in GetProducersFor(productType))
            {
                if (!RecipeUsesIngredient(recipe, seedType))
                    continue;
                if (!IsGreenFaceCraftPathValid(recipe, seedType))
                    continue;
                if (!RecipeIngredientsResearchComplete(recipe, memo, visiting, 0))
                    continue;
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
