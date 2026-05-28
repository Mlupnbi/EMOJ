using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research.Crafting;

namespace EvenMoreOverpoweredJourney.Research
{
    public static class RecipeAnalyzer
    {
        private static int _recipeIndexBuiltForCount = -1;
        private static List<Recipe>[] _producersByItemType;
        private static List<Recipe>[] _consumersByItemType;
        private static List<Recipe>[] _recipesByGroupId;
        private static List<int>[] _groupsForItemType;

        public const int HighMaterialFanoutThreshold = 240;

        /// <summary>??/??????????????????</summary>
        public static long WarmIndices()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            EnsureRecipeIndices();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        public static int EstimateMaterialFanout(int itemType)
        {
            EnsureRecipeIndices();
            if (itemType <= ItemID.None || _consumersByItemType == null || itemType >= _consumersByItemType.Length)
                return 0;

            int count = _consumersByItemType[itemType]?.Count ?? 0;
            if (_groupsForItemType != null && _recipesByGroupId != null && itemType < _groupsForItemType.Length)
            {
                List<int> groupIds = _groupsForItemType[itemType];
                if (groupIds != null)
                {
                    foreach (int gid in groupIds)
                    {
                        if (gid >= 0 && gid < _recipesByGroupId.Length)
                            count += _recipesByGroupId[gid]?.Count ?? 0;
                    }
                }
            }

            return count;
        }

        public static bool IsHighFanoutMaterial(int itemType) =>
            EstimateMaterialFanout(itemType) > HighMaterialFanoutThreshold;

        public static bool IsJourneyWorld =>
            Main.ActiveWorldFileData != null && Main.ActiveWorldFileData.GameMode == GameModeID.Creative;

        public static bool IsFullyResearched(int itemType)
        {
            int? remaining = CreativeUI.GetSacrificesRemaining(itemType);
            return remaining.HasValue && remaining.Value <= 0;
        }

        /// <summary>å…¼å®¹æ—§è°ƒç”¨åï¼šä¸ <see cref="IsFullyResearched"/> ç›¸åŒã€?</summary>
        public static bool IsResearched(int itemType) => IsFullyResearched(itemType);

        public static int? GetSacrificesRemaining(int itemType) => CreativeUI.GetSacrificesRemaining(itemType);

        /// <summary>æ—…é€”ç ”ç©¶ç›®å½•ä¸­è§£é”æ— é™å¤åˆ¶æ‰€éœ€çš„æ€»çŒ®ç¥­æ•°ï¼›ä¸åœ¨ç›®å½•åˆ™è¿”å› falseã€?</summary>
        public static bool TryGetJourneyUnlockQuota(int itemType, out int amountNeeded)
        {
            amountNeeded = 0;
            return CreativeItemSacrificesCatalog.Instance != null
                && CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(itemType, out amountNeeded)
                && amountNeeded > 0;
        }

        /// <summary>å½“å‰çŒ®ç¥­è¿›åº¦æ˜¯å¦è¾¾åˆ°æˆ–è¶…è¿‡ã€Œéœ€æ±‚é‡çš„ä¸€åŠã€ï¼ˆå‘ä¸Šå–æ•´ï¼‰ã€?</summary>
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

        /// <summary>æ‰€æœ‰ã€Œä»¥ seed ä¸ºææ–™ä¹‹ä¸€ã€çš„é…æ–¹äº§ç‰©çš? type å»é‡é›†åˆï¼ˆå…¨å±€æŸ¥è¯¢åŸºå‡†ï¼‰ã€?</summary>
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

        /// <summary>????????????????????????????????0.1 ??????</summary>
        public static List<int> GetDeepCraftableProducts(Item seedItem)
        {
            var result = new List<int>();
            if (seedItem == null || seedItem.IsAir)
                return result;

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

        private static void EnsureRecipeIndices()
        {
            int recipeCount = Recipe.numRecipes;
            if (_producersByItemType != null && _consumersByItemType != null && _recipesByGroupId != null
                && _groupsForItemType != null && _recipeIndexBuiltForCount == recipeCount)
                return;

            int itemCount = ItemLoader.ItemCount;
            int groupCount = RecipeGroup.recipeGroups.Count;
            _producersByItemType = new List<Recipe>[itemCount];
            _consumersByItemType = new List<Recipe>[itemCount];
            _recipesByGroupId = new List<Recipe>[groupCount];
            _groupsForItemType = new List<int>[itemCount];

            for (int g = 0; g < groupCount; g++)
            {
                RecipeGroup group = RecipeGroup.recipeGroups[g];
                if (group?.ValidItems == null)
                    continue;
                foreach (int valid in group.ValidItems)
                {
                    if (valid <= ItemID.None || valid >= itemCount)
                        continue;
                    _groupsForItemType[valid] ??= new List<int>();
                    _groupsForItemType[valid].Add(g);
                }
            }

            for (int i = 0; i < recipeCount; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                int product = recipe.createItem.type;
                if (product > ItemID.None && product < itemCount)
                {
                    _producersByItemType[product] ??= new List<Recipe>();
                    _producersByItemType[product].Add(recipe);
                }

                IndexRecipeConsumers(recipe, itemCount);
            }

            _recipeIndexBuiltForCount = recipeCount;
        }

        private static void IndexRecipeConsumers(Recipe recipe, int itemCount)
        {
            if (recipe.requiredItem == null)
                return;

            int slotCount = recipe.requiredItem.Count;
            for (int slot = 0; slot < slotCount; slot++)
            {
                Item req = recipe.requiredItem[slot];
                if (req == null || req.IsAir)
                    continue;

                AddConsumer(req.type, recipe, itemCount);

                int gid = GetAcceptedGroupId(recipe, slot);
                if (gid < 0 || gid >= _recipesByGroupId.Length)
                    continue;

                _recipesByGroupId[gid] ??= new List<Recipe>();
                List<Recipe> groupList = _recipesByGroupId[gid];
                if (groupList.Count == 0 || groupList[groupList.Count - 1] != recipe)
                    groupList.Add(recipe);
            }
        }

        private static void AddConsumer(int itemType, Recipe recipe, int itemCount)
        {
            if (itemType <= ItemID.None || itemType >= itemCount)
                return;
            _consumersByItemType[itemType] ??= new List<Recipe>();
            List<Recipe> list = _consumersByItemType[itemType];
            if (list.Count == 0 || list[list.Count - 1] != recipe)
                list.Add(recipe);
        }

        private static IEnumerable<Recipe> GetProducersFor(int productType)
        {
            EnsureRecipeIndices();
            if (productType <= ItemID.None || _producersByItemType == null || productType >= _producersByItemType.Length)
                return Enumerable.Empty<Recipe>();
            return _producersByItemType[productType] ?? Enumerable.Empty<Recipe>();
        }

        public static IEnumerable<Recipe> GetRecipesConsumingMaterial(int itemType)
        {
            EnsureRecipeIndices();
            if (itemType <= ItemID.None || _consumersByItemType == null || itemType >= _consumersByItemType.Length)
                yield break;

            var seen = new HashSet<Recipe>();
            List<Recipe> direct = _consumersByItemType[itemType];
            if (direct != null)
            {
                foreach (Recipe recipe in direct)
                {
                    if (recipe != null && seen.Add(recipe))
                        yield return recipe;
                }
            }

            if (_groupsForItemType == null || _recipesByGroupId == null || itemType >= _groupsForItemType.Length)
                yield break;

            List<int> groupIds = _groupsForItemType[itemType];
            if (groupIds == null)
                yield break;

            foreach (int gid in groupIds)
            {
                if (gid < 0 || gid >= _recipesByGroupId.Length)
                    continue;
                List<Recipe> groupRecipes = _recipesByGroupId[gid];
                if (groupRecipes == null)
                    continue;
                foreach (Recipe recipe in groupRecipes)
                {
                    if (recipe != null && seen.Add(recipe))
                        yield return recipe;
                }
            }
        }

        public static List<int> FilterJourneyYellow(int seedType, IEnumerable<int> fromProducts)
        {
            return fromProducts.Where(t => IsFullyResearched(t)).Distinct().OrderBy(t => t).ToList();
        }

        public static List<int> FilterJourneyGreen(Item seedItem)
        {
            if (seedItem == null || seedItem.IsAir)
                return new List<int>();
            return RecipeBrowserNestedCraft.GetDeepCraftableProductsForGreenFace(seedItem.type);
        }

        public static List<int> FilterJourneyBlue(int seedType)
        {
            HashSet<int> greenExclude = null;
            if (IsFullyResearched(seedType))
            {
                greenExclude = new HashSet<int>(
                    RecipeBrowserNestedCraft.GetDeepCraftableProductsForGreenFace(seedType));
            }

            var set = new HashSet<int>();
            foreach (Recipe recipe in Main.recipe)
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;
                if (!RecipeUsesIngredient(recipe, seedType))
                    continue;
                int product = recipe.createItem.type;
                if (IsFullyResearched(product))
                    continue;
                if (greenExclude != null && greenExclude.Contains(product))
                    continue;
                set.Add(product);
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

        public static int GetAcceptedGroupId(Recipe recipe, int slotIndex)
        {
            if (recipe?.acceptedGroups == null || slotIndex < 0 || slotIndex >= recipe.acceptedGroups.Count)
                return -1;
            return recipe.acceptedGroups[slotIndex];
        }

        public static bool RecipeUsesIngredient(Recipe recipe, int itemType)
        {
            if (recipe?.requiredItem == null)
                return false;

            int n = recipe.requiredItem.Count;
            for (int i = 0; i < n; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir) continue;
                if (req.type == itemType) return true;
                int gid = GetAcceptedGroupId(recipe, i);
                if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
                {
                    RecipeGroup group = RecipeGroup.recipeGroups[gid];
                    if (group != null && group.ContainsItem(itemType)) return true;
                }
            }
            return false;
        }

        /// <summary>??????????? type ????????????? RecipeGroup ?????????</summary>
        public static bool RecipeUsesExactIngredient(Recipe recipe, int itemType)
        {
            if (recipe?.requiredItem == null || itemType <= ItemID.None)
                return false;

            int n = recipe.requiredItem.Count;
            for (int i = 0; i < n; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req != null && !req.IsAir && req.type == itemType)
                    return true;
            }
            return false;
        }

        /// <summary>ÏûºÄ¸Ã²ÄÁÏ£¨¾«È·ÅäÁÏ£©µÄÅä·½²úÎï type ÁĞ±í£»×ßË÷ÒıÇÒ´øÉÏÏŞ£¬±ÜÃâÑÎ¿éµÈ¸ßÉÈ³ö²ÄÁÏÉ¨È«±í¡£</summary>
        public static List<int> GetProductTypesUsingExactMaterial(int materialType, int maxProducts = 256)
        {
            var set = new HashSet<int>();
            if (materialType <= ItemID.None)
                return new List<int>();

            foreach (Recipe recipe in GetRecipesConsumingMaterial(materialType))
            {
                if (maxProducts > 0 && set.Count >= maxProducts)
                    break;

                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;
                if (!RecipeUsesExactIngredient(recipe, materialType))
                    continue;
                set.Add(recipe.createItem.type);
            }

            return set.OrderBy(t => t).ToList();
        }

        /// <summary>???????????????????? type??????? ? ?????</summary>
        public static List<int> GetProductTypesConsumingMaterial(int materialType)
        {
            var set = new HashSet<int>();
            if (materialType <= ItemID.None)
                return new List<int>();

            foreach (Recipe recipe in GetRecipesConsumingMaterial(materialType))
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;
                set.Add(recipe.createItem.type);
            }

            return set.OrderBy(t => t).ToList();
        }

        /// <summary>??????????????????????????????</summary>
        public static bool AllMaterialsResearched(Recipe recipe)
        {
            if (recipe?.requiredItem == null)
                return true;

            int n = recipe.requiredItem.Count;
            for (int i = 0; i < n; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir) continue;
                if (!IsFullyResearched(req.type))
                {
                    bool foundAlternative = false;
                    int gid = GetAcceptedGroupId(recipe, i);
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
            EnsureRecipeIndices();
            var recipes = new List<Recipe>();
            foreach (Recipe recipe in GetProducersFor(itemType))
                recipes.Add(recipe);
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
            if (player == null || recipe?.requiredItem == null) return false;
            int n = recipe.requiredItem.Count;
            for (int i = 0; i < n; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir) continue;
                int need = req.stack;
                if (need <= 0) continue;
                int gid = GetAcceptedGroupId(recipe, i);
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
