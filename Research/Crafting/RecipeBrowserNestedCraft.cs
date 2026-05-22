using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research.Players;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
    /// <summary>
    /// Recipe Browser RecipePath.GetCraftPaths + CraftPath 的旅途绿脸移植（逻辑照搬，仅额外微光屏蔽）。
    /// 对应 RB：extendedCraft / ObtainableFilter → craftPaths.Count &gt; 0。
    /// </summary>
    internal static class RecipeBrowserNestedCraft
    {
        internal static Dictionary<int, List<Recipe>> RecipeDictionary;

        private static HashSet<int> _shimmerBlockedTypes;
        private static bool _shimmerBlockCacheValid;

        internal static void InvalidateCaches()
        {
            RecipeDictionary = null;
            InvalidateShimmerBlockCache();
        }

        internal static void InvalidateShimmerBlockCache() => _shimmerBlockCacheValid = false;

        /// <summary>对齐 RB：旅途模式下已研究材料视为 JourneyDuplicate（与 GameModeInfo.IsJourneyMode 等效判定）。</summary>
        internal static bool UseJourneyDuplicateForMaterials =>
            RecipeAnalyzer.IsJourneyWorld && Main.netMode != NetmodeID.Server;

        // —— 绿脸对外 API（仅保留：种子配方筛选 + 产物未研究 + RB 路径 + 微光）——

        public static bool RecipeHasNestedCraftPath(Recipe recipe)
        {
            if (recipe?.createItem == null || recipe.createItem.IsAir)
                return false;
            if (IsShimmerBlockedProduct(recipe))
                return false;
            return GetCraftPaths(recipe, CancellationToken.None, single: true).Count > 0;
        }

        public static List<Recipe> GetQualifyingRecipesForGreenFace(int productType, int seedType)
        {
            var list = new List<Recipe>();
            if (seedType <= ItemID.None || IsItemShimmerBlocked(productType))
                return list;
            if (!IsGreenFaceProduct(productType))
                return list;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                if (!RecipeAnalyzer.RecipeUsesIngredient(recipe, seedType))
                    continue;
                if (RecipeHasNestedCraftPath(recipe))
                    list.Add(recipe);
            }
            return list;
        }

        /// <summary>绿脸产物：在旅途目录内且尚未研究完成。</summary>
        public static bool IsGreenFaceProduct(int productType) =>
            RecipeAnalyzer.TryGetJourneyUnlockQuota(productType, out int quota)
            && quota > 0
            && !RecipeAnalyzer.IsFullyResearched(productType);

        public static bool IsRecipeVisibleInPathPanel(Recipe recipe) =>
            recipe != null && !IsShimmerDecraftRecipe(recipe) && !IsItemShimmerBlocked(recipe.createItem.type);

        // —— RecipePath（照搬）——

        internal static bool ItemFullyResearched(int itemId) => RecipeAnalyzer.IsFullyResearched(itemId);

        private static List<Recipe> GetProducerRecipes(int itemType)
        {
            EnsureRecipeDictionary();
            if (RecipeDictionary.TryGetValue(itemType, out List<Recipe> cached))
                return cached;

            var list = new List<Recipe>();
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;
                if (recipe.createItem.type == itemType)
                    list.Add(recipe);
            }
            return list;
        }

        private static void EnsureRecipeDictionary()
        {
            if (RecipeDictionary != null)
                return;

            RecipeDictionary = new Dictionary<int, List<Recipe>>();
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe r = Main.recipe[i];
                if (r?.createItem == null || r.createItem.IsAir)
                    continue;
                int type = r.createItem.type;
                if (!RecipeDictionary.TryGetValue(type, out List<Recipe> list))
                {
                    list = new List<Recipe>();
                    RecipeDictionary[type] = list;
                }
                list.Add(r);
            }

            var remove = new List<int>();
            foreach (KeyValuePair<int, List<Recipe>> kv in RecipeDictionary)
            {
                if (kv.Value.Count > 15)
                    remove.Add(kv.Key);
            }
            foreach (int key in remove)
                RecipeDictionary.Remove(key);
        }

        internal static List<RBCraftPath> GetCraftPaths(Recipe recipe, CancellationToken token, bool single)
        {
            EnsureRecipeDictionary();
            Dictionary<int, int> haveItems = CalculateHaveItems();
            var list = new List<RBCraftPath>();
            var craftPath = new RBCraftPath(recipe, haveItems);
            FindCraftPaths(list, craftPath, token, single);

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (PathUsesUnseenStation(list[i]) || PathUsesShimmerBlockedItem(list[i]))
                    list.RemoveAt(i);
            }
            return list;
        }

        private static bool PathUsesUnseenStation(RBCraftPath path) =>
            path.Root.GetAllChildrenPreOrder().OfType<RBCraftPath.RecipeNode>().Any(rn =>
                rn.Recipe.requiredTile.Any(tile => tile > -1 && !ResearchCraftingPlayer.IsCraftingStationSeen(tile)));

        private static bool PathUsesShimmerBlockedItem(RBCraftPath path)
        {
            foreach (RBCraftPath.CraftPathNode node in path.Root.GetAllChildrenPreOrder())
            {
                if (node is RBCraftPath.JourneyDuplicateItemNode j && IsItemShimmerBlocked(j.ItemId))
                    return true;
                if (node is RBCraftPath.HaveItemNode h && IsItemShimmerBlocked(h.ItemId))
                    return true;
                if (node is RBCraftPath.UnfulfilledNode u)
                {
                    foreach (int t in u.ItemTypes)
                    {
                        if (IsItemShimmerBlocked(t))
                            return true;
                    }
                }
            }
            return IsItemShimmerBlocked(path.Root is RBCraftPath.RecipeNode rn ? rn.Recipe.createItem.type : 0);
        }

        private static Dictionary<int, int> CalculateHaveItems()
        {
            var dictionary = new Dictionary<int, int>();
            Player player = Main.LocalPlayer;
            if (player == null)
                return dictionary;

            for (int i = 0; i < 59; i++)
            {
                Item item = player.inventory[i];
                if (i == 58)
                    item = Main.mouseItem;
                if (item == null || item.IsAir)
                    continue;
                dictionary.TryGetValue(item.type, out int value);
                dictionary[item.type] = value + item.stack;
            }
            return dictionary;
        }

        internal static void AdjustItemCount(Dictionary<int, int> d, int key, int adjustment)
        {
            d.TryGetValue(key, out int value);
            d[key] = value + adjustment;
            if (d[key] <= 0)
                d.Remove(key);
        }

        private static void FindCraftPaths(List<RBCraftPath> paths, RBCraftPath inProgress, CancellationToken token, bool single)
        {
            if (single && paths.Count > 0)
                return;
            if (token.IsCancellationRequested)
                return;
            if (inProgress.Root.GetAllChildrenPreOrder().Count() > 20)
                return;

            RBCraftPath.UnfulfilledNode current = inProgress.GetCurrent();
            if (current == null)
            {
                paths.Add(inProgress.Clone());
                return;
            }

            HashSet<int> viableIngredients = new HashSet<int>(current.ItemTypes);
            current.CheckParentsForRecipeLoopViaIngredients(viableIngredients);
            if (viableIngredients.Count == 0)
                return;

            int stack = current.Stack;
            var candidates = new List<Recipe>();
            foreach (int ingredientType in viableIngredients)
            {
                foreach (Recipe producer in GetProducerRecipes(ingredientType))
                    candidates.Add(producer);
            }

            foreach (Recipe producer in candidates)
            {
                if (IsShimmerDecraftRecipe(producer) || IsItemShimmerBlocked(producer.createItem.type))
                    continue;

                if (inProgress.Root is RBCraftPath.RecipeNode rootRecipeNode)
                {
                    Recipe rootRecipe = rootRecipeNode.Recipe;
                    if (producer.requiredItem.Any(x => x.type == rootRecipe.createItem.type && x.stack >= rootRecipe.createItem.stack))
                        continue;
                }

                if (current.CheckParentsForRecipeLoop(producer))
                    continue;

                int needed = (stack - 1) / producer.createItem.stack + 1;
                RBCraftPath.RecipeNode recipeNode = inProgress.Push(current, producer, needed);
                FindCraftPaths(paths, inProgress, token, single);
                inProgress.Pop(current, recipeNode);
            }
        }

        // —— 微光（唯一额外规则）——

        private static bool IsShimmerBlockedProduct(Recipe recipe)
        {
            if (recipe?.createItem == null)
                return true;
            if (IsShimmerDecraftRecipe(recipe))
                return true;
            return IsItemShimmerBlocked(recipe.createItem.type);
        }

        internal static bool IsItemShimmerBlocked(int itemType)
        {
            if (itemType <= ItemID.None || ResearchCraftingPlayer.HasEncounteredShimmer)
                return false;
            EnsureShimmerBlockedTypes();
            return _shimmerBlockedTypes.Contains(itemType);
        }

        internal static bool IsShimmerDecraftRecipe(Recipe recipe)
        {
            if (recipe?.createItem == null || recipe.createItem.IsAir)
                return false;
            int product = recipe.createItem.type;
            int idx = ShimmerTransforms.GetDecraftingRecipeIndex(product);
            return idx >= 0 && idx < Recipe.numRecipes && Main.recipe[idx] == recipe;
        }

        private static void EnsureShimmerBlockedTypes()
        {
            if (_shimmerBlockCacheValid)
                return;
            _shimmerBlockedTypes = new HashSet<int>();
            _shimmerBlockCacheValid = true;
            if (ResearchCraftingPlayer.HasEncounteredShimmer)
                return;

            for (int i = 1; i < ItemID.Sets.ShimmerTransformToItem.Length; i++)
            {
                int to = ItemID.Sets.ShimmerTransformToItem[i];
                if (to > ItemID.None)
                    _shimmerBlockedTypes.Add(to);
            }

            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe r = Main.recipe[i];
                if (IsShimmerDecraftRecipe(r))
                    _shimmerBlockedTypes.Add(r.createItem.type);
            }

            bool changed = true;
            int guard = 0;
            while (changed && guard++ < ItemLoader.ItemCount)
            {
                changed = false;
                for (int i = 0; i < Recipe.numRecipes; i++)
                {
                    Recipe r = Main.recipe[i];
                    if (r?.createItem == null || r.createItem.IsAir)
                        continue;
                    int prod = r.createItem.type;
                    if (_shimmerBlockedTypes.Contains(prod) || !RecipeUsesOnlyBlockedMaterials(r))
                        continue;
                    if (_shimmerBlockedTypes.Add(prod))
                        changed = true;
                }
            }
        }

        private static bool RecipeUsesOnlyBlockedMaterials(Recipe recipe)
        {
            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;

                bool matchedGroup = false;
                foreach (int gid in recipe.acceptedGroups)
                {
                    if (gid < 0 || gid >= RecipeGroup.recipeGroups.Count)
                        continue;
                    RecipeGroup g = RecipeGroup.recipeGroups[gid];
                    if (g == null || req.type != g.IconicItemId)
                        continue;
                    matchedGroup = true;
                    if (g.ValidItems.Any(v => !_shimmerBlockedTypes.Contains(v)))
                        return false;
                    break;
                }

                if (!matchedGroup && !_shimmerBlockedTypes.Contains(req.type))
                    return false;
            }
            return true;
        }
    }

    /// <summary>Recipe Browser CraftPath（嵌套合成树）。</summary>
    internal sealed class RBCraftPath
    {
        internal Dictionary<int, int> HaveItems;
        internal CraftPathNode Root;
        private UnfulfilledNode _current;

        public RBCraftPath(Recipe root, Dictionary<int, int> haveItems)
        {
            HaveItems = haveItems;
            Root = new RecipeNode(root, 1, -1, null, this);
            ConsumeResources(Root);
        }

        internal RecipeNode Push(UnfulfilledNode current, Recipe recipe, int needed)
        {
            _current = null;
            var recipeNode = new RecipeNode(recipe, needed, current.ChildNumber, current.Parent, this);
            current.Parent = null;
            current.ChildNumber = -1;
            ConsumeResources(recipeNode);
            return recipeNode;
        }

        internal void Pop(UnfulfilledNode current, CraftPathNode pushed)
        {
            _current = current;
            current.Parent = pushed.Parent;
            current.ChildNumber = pushed.ChildNumber;
            if (current.Parent?.Children != null && current.ChildNumber >= 0)
                current.Parent.Children[current.ChildNumber] = current;
            pushed.Parent = null;
            pushed.ChildNumber = -1;
            UnConsumeResources(pushed);
        }

        internal UnfulfilledNode GetCurrent()
        {
            if (_current == null || _current.ChildNumber == -1)
                _current = Root.FindUnfulfilled();
            return _current;
        }

        internal RBCraftPath Clone()
        {
            var copy = (RBCraftPath)MemberwiseClone();
            copy.Root = Root.Clone();
            copy._current = null;
            return copy;
        }

        private void ConsumeResources(CraftPathNode node) => node?.ConsumeResources(this);
        private void UnConsumeResources(CraftPathNode node) => node?.UnConsumeResources(this);

        internal abstract class CraftPathNode
        {
            internal CraftPathNode Parent;
            internal int ChildNumber = -1;
            internal CraftPathNode[] Children;
            internal RBCraftPath CraftPath;

            protected CraftPathNode(int childNumber, CraftPathNode parent, RBCraftPath craftPath)
            {
                ChildNumber = childNumber;
                Parent = parent;
                CraftPath = craftPath;
                if (parent?.Children != null && childNumber >= 0)
                    parent.Children[childNumber] = this;
            }

            internal UnfulfilledNode FindUnfulfilled()
            {
                if (this is UnfulfilledNode u)
                    return u;
                if (Children == null)
                    return null;
                foreach (CraftPathNode child in Children)
                {
                    UnfulfilledNode found = child?.FindUnfulfilled();
                    if (found != null)
                        return found;
                }
                return null;
            }

            internal virtual CraftPathNode Clone()
            {
                var copy = (CraftPathNode)MemberwiseClone();
                copy.Parent = null;
                copy.CraftPath = null;
                copy.Children = Children?.Select(x => x?.Clone()).ToArray();
                return copy;
            }

            internal virtual void ConsumeResources(RBCraftPath path)
            {
                if (Children == null)
                    return;
                foreach (CraftPathNode child in Children)
                    child?.ConsumeResources(path);
            }

            internal virtual void UnConsumeResources(RBCraftPath path)
            {
                if (Children == null)
                    return;
                foreach (CraftPathNode child in Children)
                    child?.UnConsumeResources(path);
            }

            public IEnumerable<CraftPathNode> GetAllChildrenPreOrder()
            {
                yield return this;
                if (Children == null)
                    yield break;
                foreach (CraftPathNode child in Children)
                {
                    if (child == null)
                        continue;
                    foreach (CraftPathNode sub in child.GetAllChildrenPreOrder())
                        yield return sub;
                }
            }
        }

        internal sealed class HaveItemNode : CraftPathNode
        {
            internal int ItemId;
            internal int Stack;

            public HaveItemNode(int itemId, int stack, int childNumber, CraftPathNode parent, RBCraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                ItemId = itemId;
                Stack = stack;
            }

            internal override void ConsumeResources(RBCraftPath path)
            {
                RecipeBrowserNestedCraft.AdjustItemCount(path.HaveItems, ItemId, -Stack);
                base.ConsumeResources(path);
            }

            internal override void UnConsumeResources(RBCraftPath path)
            {
                RecipeBrowserNestedCraft.AdjustItemCount(path.HaveItems, ItemId, Stack);
                base.UnConsumeResources(path);
            }
        }

        internal sealed class HaveItemsNode : CraftPathNode
        {
            internal List<Tuple<int, int>> ListOfItems;

            public HaveItemsNode(List<Tuple<int, int>> listOfItems, int childNumber, CraftPathNode parent, RBCraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                ListOfItems = listOfItems;
            }

            internal override void ConsumeResources(RBCraftPath path)
            {
                foreach (Tuple<int, int> pair in ListOfItems)
                    RecipeBrowserNestedCraft.AdjustItemCount(path.HaveItems, pair.Item1, -pair.Item2);
                base.ConsumeResources(path);
            }

            internal override void UnConsumeResources(RBCraftPath path)
            {
                foreach (Tuple<int, int> pair in ListOfItems)
                    RecipeBrowserNestedCraft.AdjustItemCount(path.HaveItems, pair.Item1, pair.Item2);
                base.UnConsumeResources(path);
            }
        }

        internal sealed class JourneyDuplicateItemNode : CraftPathNode
        {
            internal int ItemId;
            internal int Stack;

            public JourneyDuplicateItemNode(int itemId, int stack, int childNumber, CraftPathNode parent, RBCraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                ItemId = itemId;
                Stack = stack;
            }
        }

        internal sealed class UnfulfilledNode : CraftPathNode
        {
            internal HashSet<int> ItemTypes;
            internal int Stack;

            public UnfulfilledNode(int item, int stack, int childNumber, CraftPathNode parent, RBCraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                Stack = stack;
                ItemTypes = new HashSet<int> { item };
            }

            public UnfulfilledNode(RecipeGroup recipeGroup, int stack, int childNumber, CraftPathNode parent, RBCraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                Stack = stack;
                ItemTypes = new HashSet<int>(recipeGroup.ValidItems);
            }

            internal void CheckParentsForRecipeLoopViaIngredients(HashSet<int> viableIngredients)
            {
                CraftPathNode node = Parent;
                while (node != null)
                {
                    if (node is RecipeNode recipeNode)
                    {
                        viableIngredients.Remove(recipeNode.Recipe.createItem.type);
                        node = node.Parent;
                    }
                    else if (node is HaveItemNode || node is HaveItemsNode)
                    {
                        node = node.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            internal bool CheckParentsForRecipeLoop(Recipe recipe)
            {
                CraftPathNode node = Parent;
                while (node != null)
                {
                    if (node is RecipeNode recipeNode)
                    {
                        if (recipeNode.Recipe.createItem.type == recipe.createItem.type)
                            return true;
                        node = node.Parent;
                    }
                    else if (node is HaveItemNode || node is HaveItemsNode)
                    {
                        node = node.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
                return false;
            }
        }

        internal sealed class RecipeNode : CraftPathNode
        {
            internal Recipe Recipe;
            internal int Multiplier;

            public RecipeNode(Recipe recipe, int multiplier, int childNumber, CraftPathNode parent, RBCraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                Recipe = recipe;
                Multiplier = multiplier;
                Children = new CraftPathNode[recipe.requiredItem.Count(x => x != null && !x.IsAir)];

                for (int num = 0; num < Children.Length; num++)
                {
                    bool matchedGroup = false;
                    foreach (int acceptedGroup in recipe.acceptedGroups)
                    {
                        if (acceptedGroup < 0 || acceptedGroup >= RecipeGroup.recipeGroups.Count)
                            continue;
                        RecipeGroup group = RecipeGroup.recipeGroups[acceptedGroup];
                        if (recipe.requiredItem[num].type != group.IconicItemId)
                            continue;

                        matchedGroup = true;
                        bool satisfied = false;
                        bool partialInInventory = false;
                        int needStack = recipe.requiredItem[num].stack * multiplier;

                        foreach (int validItem in group.ValidItems)
                        {
                            if (RecipeBrowserNestedCraft.IsItemShimmerBlocked(validItem))
                                continue;

                            if (RecipeBrowserNestedCraft.UseJourneyDuplicateForMaterials && RecipeBrowserNestedCraft.ItemFullyResearched(validItem))
                            {
                                Children[num] = new JourneyDuplicateItemNode(validItem, needStack, num, this, craftPath);
                                satisfied = true;
                                break;
                            }

                            if (craftPath.HaveItems.TryGetValue(validItem, out int have) && have >= needStack)
                            {
                                Children[num] = new HaveItemNode(validItem, needStack, num, this, craftPath);
                                satisfied = true;
                                break;
                            }

                            if (craftPath.HaveItems.ContainsKey(validItem))
                                partialInInventory = true;
                        }

                        if (!satisfied && partialInInventory)
                        {
                            var list = new List<Tuple<int, int>>();
                            int remaining = needStack;
                            foreach (int validItem2 in group.ValidItems)
                            {
                                if (remaining <= 0 || !craftPath.HaveItems.TryGetValue(validItem2, out int have2))
                                    continue;
                                int take = Math.Min(remaining, have2);
                                list.Add(new Tuple<int, int>(validItem2, take));
                                remaining -= take;
                            }
                            Children[num] = new HaveItemsNode(list, num, this, craftPath);
                            if (remaining > 0)
                            {
                                Children[num].Children = new CraftPathNode[1];
                                Children[num].Children[0] = new UnfulfilledNode(group, remaining, 0, Children[num], craftPath);
                            }
                        }
                        else if (!satisfied)
                        {
                            Children[num] = new UnfulfilledNode(group, needStack, num, this, craftPath);
                        }
                        break;
                    }

                    if (!matchedGroup)
                    {
                        int type = recipe.requiredItem[num].type;
                        int needStack = recipe.requiredItem[num].stack * multiplier;

                        if (RecipeBrowserNestedCraft.IsItemShimmerBlocked(type))
                        {
                            Children[num] = new UnfulfilledNode(type, needStack, num, this, craftPath);
                        }
                        else if (RecipeBrowserNestedCraft.UseJourneyDuplicateForMaterials && RecipeBrowserNestedCraft.ItemFullyResearched(type))
                        {
                            Children[num] = new JourneyDuplicateItemNode(type, needStack, num, this, craftPath);
                        }
                        else if (craftPath.HaveItems.TryGetValue(type, out int have) && have >= needStack)
                        {
                            Children[num] = new HaveItemNode(type, needStack, num, this, craftPath);
                        }
                        else if (craftPath.HaveItems.ContainsKey(type))
                        {
                            int partial = craftPath.HaveItems[type];
                            Children[num] = new HaveItemNode(type, partial, num, this, craftPath);
                            int remaining = needStack - partial;
                            Children[num].Children = new CraftPathNode[1];
                            Children[num].Children[0] = new UnfulfilledNode(type, remaining, 0, Children[num], craftPath);
                        }
                        else
                        {
                            Children[num] = new UnfulfilledNode(type, needStack, num, this, craftPath);
                        }
                    }
                }
            }
        }
    }
}
