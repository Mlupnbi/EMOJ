using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Research.Players;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
    /// <summary>
    /// 旅途绿脸：查询物必须已完全研究；列举以它为原料（含嵌套中间产物）、
    /// 其余旅途材料均已研究、配方环境可用、且产物尚未研究满的条目。
    /// </summary>
    internal static class RecipeBrowserNestedCraft
    {
        private const int MaterialChainMaxDepth = 32;
        /// <summary>高扇出材料仍展开 2 层（木材→板材→家具），避免只扫到墓碑等少数条目。</summary>
        private const int HighFanoutMaxDepth = 2;
        private const int NormalMaxCandidates = 1024;
        /// <summary>木材等 fanout&gt;240 时仍须覆盖全部直接产物（原版木材 fanout≈449，192 截断会漏掉未研究项）。</summary>
        private const int HighFanoutMaxCandidates = 1024;
        private const long DefaultStepBudgetMs = 12;

        private static HashSet<int> _shimmerTransformTargets;
        private static bool _shimmerTargetsBuilt;

        internal static void InvalidateCaches()
        {
            _shimmerTargetsBuilt = false;
            _shimmerTransformTargets = null;
        }

        public sealed class GreenFaceQuerySession
        {
            internal int SeedType;
            internal List<int> Candidates = new List<int>();
            internal int CandidateIndex;
            internal readonly List<int> Results = new List<int>();
            internal bool Complete;
            internal bool HighFanout;
            internal Dictionary<int, bool> ResearchMemo;
            internal HashSet<int> ResearchVisiting;
            internal Dictionary<int, bool> SeedMemo;
            internal HashSet<int> SeedVisiting;
            internal int RejectNotProduct;
            internal int RejectNoValidRecipe;
            internal int RejectIngredients;
            internal int RejectNoSeed;
        }

        public static GreenFaceQuerySession BeginGreenFaceQuery(int seedType)
        {
            var session = new GreenFaceQuerySession { SeedType = seedType };
            session.ResearchMemo = new Dictionary<int, bool>();
            session.ResearchVisiting = new HashSet<int>();
            session.SeedMemo = new Dictionary<int, bool>();
            session.SeedVisiting = new HashSet<int>();

            if (seedType <= ItemID.None || !RecipeAnalyzer.IsFullyResearched(seedType) || Main.gameMenu || Main.dedServ)
            {
                session.Complete = true;
                return session;
            }

            EnsureShimmerTransformTargets();
            session.HighFanout = RecipeAnalyzer.IsHighFanoutMaterial(seedType);
            int maxDepth = session.HighFanout ? HighFanoutMaxDepth : MaterialChainMaxDepth;
            int fanout = RecipeAnalyzer.EstimateMaterialFanout(seedType);
            int maxCandidates = session.HighFanout
                ? System.Math.Min(fanout + 64, 2048)
                : NormalMaxCandidates;
            session.Candidates = CollectCandidateProductsFromSeed(seedType, maxDepth, maxCandidates);
            int eligible = CountGreenFaceProducts(session.Candidates);
            session.CandidateIndex = 0;

            if (session.HighFanout)
            {
                EmojLog.Info(EmojLogChannel.Research,
                    $"GreenFace high-fanout seed={seedType} fanout={fanout} candidates={session.Candidates.Count} eligibleProducts={eligible}");
            }

            if (session.Candidates.Count == 0)
                session.Complete = true;

            return session;
        }

        public static bool StepGreenFaceQuery(GreenFaceQuerySession session, long budgetMs = DefaultStepBudgetMs)
        {
            if (session == null || session.Complete)
                return true;

            var sw = Stopwatch.StartNew();
            while (session.CandidateIndex < session.Candidates.Count)
            {
                int productType = session.Candidates[session.CandidateIndex++];
                if (!IsGreenFaceProduct(productType))
                {
                    session.RejectNotProduct++;
                    continue;
                }

                session.ResearchVisiting.Clear();
                session.SeedVisiting.Clear();

                bool qualifies = false;
                bool sawValidRecipe = false;
                bool sawIngredients = false;
                bool sawSeed = false;
                foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
                {
                    if (!IsGreenFaceRecipeValid(recipe))
                        continue;
                    sawValidRecipe = true;
                    if (!RecipeIngredientsResearchComplete(recipe, session.ResearchMemo, session.ResearchVisiting, 0))
                        continue;
                    sawIngredients = true;
                    if (!RecipeDependsOnSeed(recipe, session.SeedType, session.SeedMemo, session.SeedVisiting, 0))
                        continue;
                    sawSeed = true;
                    qualifies = true;
                    break;
                }

                if (!qualifies)
                {
                    if (!sawValidRecipe)
                        session.RejectNoValidRecipe++;
                    else if (!sawIngredients)
                        session.RejectIngredients++;
                    else if (!sawSeed)
                        session.RejectNoSeed++;
                }

                if (qualifies && !session.Results.Contains(productType))
                    session.Results.Add(productType);

                if (sw.ElapsedMilliseconds >= budgetMs)
                    return false;
            }

            session.Complete = true;
            session.Results.Sort();
            EmojLog.Info(EmojLogChannel.Research,
                $"GreenFace complete seed={session.SeedType} highFanout={session.HighFanout} candidates={session.Candidates.Count} results={session.Results.Count} " +
                $"reject[notProduct={session.RejectNotProduct},noRecipe={session.RejectNoValidRecipe},ingredients={session.RejectIngredients},noSeed={session.RejectNoSeed}] " +
                $"env[seenTiles={ResearchCraftingPlayer.CountSeenTiles()},researchTiles={ResearchCraftingPlayer.CountResearchedTiles()},seen={ResearchCraftingPlayer.SeenEnvironment},research={ResearchCraftingPlayer.ResearchedEnvironment}]");
            return true;
        }

        public static List<int> GetDeepCraftableProductsForGreenFace(int seedType)
        {
            GreenFaceQuerySession session = BeginGreenFaceQuery(seedType);
            StepGreenFaceQuery(session, long.MaxValue);
            return session.Results;
        }

        public static List<Recipe> GetQualifyingRecipesForGreenFace(int productType, int seedType)
        {
            var list = new List<Recipe>();
            if (seedType <= ItemID.None || !RecipeAnalyzer.IsFullyResearched(seedType))
                return list;

            try
            {
                EnsureShimmerTransformTargets();

                var researchMemo = new Dictionary<int, bool>();
                var researchVisiting = new HashSet<int>();
                var seedMemo = new Dictionary<int, bool>();
                var seedVisiting = new HashSet<int>();

                foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
                {
                    if (!IsGreenFaceRecipeValid(recipe))
                        continue;
                    if (!RecipeIngredientsResearchComplete(recipe, researchMemo, researchVisiting, 0))
                        continue;
                    if (!RecipeDependsOnSeed(recipe, seedType, seedMemo, seedVisiting, 0))
                        continue;
                    list.Add(recipe);
                }
            }
            catch (System.Exception ex)
            {
                EmojLog.Warn(EmojLogChannel.Research, $"GreenFace recipes failed product={productType} seed={seedType}: {ex.Message}");
            }

            return list;
        }

        /// <summary>绿脸产物：尚未旅途研究满即可（勿要求必须在献祭目录中，否则木墙/家具等会被整类排除）。</summary>
        public static bool IsGreenFaceProduct(int productType) =>
            !RecipeAnalyzer.IsFullyResearched(productType);

        private static List<int> CollectCandidateProductsFromSeed(int seedType, int maxDepth, int maxCandidates)
        {
            var candidates = new HashSet<int>();
            var frontier = new Queue<int>();
            var seenMaterials = new HashSet<int> { seedType };

            frontier.Enqueue(seedType);
            int depth = 0;

            while (frontier.Count > 0 && depth < maxDepth && candidates.Count < maxCandidates)
            {
                int levelSize = frontier.Count;
                depth++;

                for (int i = 0; i < levelSize && candidates.Count < maxCandidates; i++)
                {
                    int material = frontier.Dequeue();
                    foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(material))
                    {
                        if (recipe?.createItem == null || recipe.createItem.IsAir)
                            continue;

                        int productType = recipe.createItem.type;
                        if (productType <= ItemID.None)
                            continue;

                        candidates.Add(productType);
                        if (candidates.Count >= maxCandidates)
                            break;

                        if (seenMaterials.Add(productType))
                            frontier.Enqueue(productType);
                    }
                }
            }

            return PrioritizeGreenFaceCandidates(candidates, maxCandidates);
        }

        private static int CountGreenFaceProducts(IReadOnlyList<int> types)
        {
            int count = 0;
            for (int i = 0; i < types.Count; i++)
            {
                if (IsGreenFaceProduct(types[i]))
                    count++;
            }

            return count;
        }

        /// <summary>未研究且可旅途研究的产物优先，避免 OrderBy(type) 把低 ID 已研究物占满配额。</summary>
        private static List<int> PrioritizeGreenFaceCandidates(HashSet<int> candidates, int maxCandidates)
        {
            var list = candidates.ToList();
            list.Sort((a, b) =>
            {
                bool ga = IsGreenFaceProduct(a);
                bool gb = IsGreenFaceProduct(b);
                if (ga != gb)
                    return gb.CompareTo(ga);
                bool ja = RecipeAnalyzer.TryGetJourneyUnlockQuota(a, out int qa) && qa > 0;
                bool jb = RecipeAnalyzer.TryGetJourneyUnlockQuota(b, out int qb) && qb > 0;
                if (ja != jb)
                    return jb.CompareTo(ja);
                return a.CompareTo(b);
            });

            if (list.Count > maxCandidates)
                list.RemoveRange(maxCandidates, list.Count - maxCandidates);

            return list;
        }

        private static bool RecipeIngredientsResearchComplete(
            Recipe recipe,
            Dictionary<int, bool> memo,
            HashSet<int> visiting,
            int depth)
        {
            if (depth > MaterialChainMaxDepth)
                return false;
            if (!IsGreenFaceRecipeValid(recipe))
                return false;

            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;
                if (!IsMaterialResearchComplete(req.type, recipe, i, visiting, memo, depth + 1, fromGroupAlternative: false))
                    return false;
            }

            return true;
        }

        private static bool IsMaterialResearchComplete(
            int itemType,
            Recipe parentRecipe,
            int slotIndex,
            HashSet<int> visiting,
            Dictionary<int, bool> memo,
            int depth,
            bool fromGroupAlternative)
        {
            if (itemType <= ItemID.None)
                return true;
            if (depth > MaterialChainMaxDepth)
                return false;

            if (memo.TryGetValue(itemType, out bool cached))
                return cached;

            if (!RecipeAnalyzer.TryGetJourneyUnlockQuota(itemType, out _))
            {
                memo[itemType] = true;
                return true;
            }

            if (RecipeAnalyzer.IsFullyResearched(itemType))
            {
                memo[itemType] = true;
                return true;
            }

            if (!fromGroupAlternative)
            {
                int gid = slotIndex < parentRecipe.acceptedGroups.Count ? parentRecipe.acceptedGroups[slotIndex] : -1;
                if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
                {
                    RecipeGroup group = RecipeGroup.recipeGroups[gid];
                    if (group?.ValidItems != null && group.ContainsItem(itemType))
                    {
                        bool anyOk = false;
                        foreach (int valid in group.ValidItems)
                        {
                            if (IsMaterialResearchComplete(valid, parentRecipe, slotIndex, visiting, memo, depth, fromGroupAlternative: true))
                            {
                                anyOk = true;
                                break;
                            }
                        }

                        memo[itemType] = anyOk;
                        return anyOk;
                    }
                }
            }

            if (visiting.Contains(itemType))
            {
                memo[itemType] = false;
                return false;
            }

            visiting.Add(itemType);
            try
            {
                bool ok = false;
                foreach (Recipe producer in RecipeAnalyzer.GetRecipesForItem(itemType))
                {
                    if (!IsGreenFaceRecipeValid(producer))
                        continue;
                    if (RecipeIngredientsResearchComplete(producer, memo, visiting, depth + 1))
                    {
                        ok = true;
                        break;
                    }
                }

                memo[itemType] = ok;
                return ok;
            }
            finally
            {
                visiting.Remove(itemType);
            }
        }

        private static bool RecipeDependsOnSeed(
            Recipe recipe,
            int seedType,
            Dictionary<int, bool> memo,
            HashSet<int> visiting,
            int depth)
        {
            if (depth > MaterialChainMaxDepth)
                return false;

            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;
                if (IngredientDependsOnSeed(req.type, recipe, i, seedType, memo, visiting, depth + 1))
                    return true;
            }

            return false;
        }

        private static bool IngredientDependsOnSeed(
            int itemType,
            Recipe parentRecipe,
            int slotIndex,
            int seedType,
            Dictionary<int, bool> memo,
            HashSet<int> visiting,
            int depth)
        {
            if (itemType == seedType)
                return true;
            if (depth > MaterialChainMaxDepth)
                return false;

            int gid = slotIndex < parentRecipe.acceptedGroups.Count ? parentRecipe.acceptedGroups[slotIndex] : -1;
            if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
            {
                RecipeGroup group = RecipeGroup.recipeGroups[gid];
                if (group != null && group.ContainsItem(itemType) && group.ContainsItem(seedType))
                    return true;
            }

            if (RecipeAnalyzer.IsFullyResearched(itemType))
                return false;

            if (memo.TryGetValue(itemType, out bool cached))
                return cached;

            if (!RecipeAnalyzer.TryGetJourneyUnlockQuota(itemType, out _))
            {
                memo[itemType] = false;
                return false;
            }

            if (visiting.Contains(itemType))
                return false;

            visiting.Add(itemType);
            bool ok = false;
            foreach (Recipe producer in RecipeAnalyzer.GetRecipesForItem(itemType))
            {
                if (RecipeDependsOnSeed(producer, seedType, memo, visiting, depth + 1))
                {
                    ok = true;
                    break;
                }
            }

            visiting.Remove(itemType);
            memo[itemType] = ok;
            return ok;
        }

        private static bool IsGreenFaceRecipeValid(Recipe recipe)
        {
            if (recipe?.createItem == null || recipe.createItem.IsAir)
                return false;
            if (IsShimmerBlockedProduct(recipe))
                return false;
            if (Main.netMode == NetmodeID.Server)
                return false;
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return false;

            if (!MeetsGreenFaceRecipeConditions(recipe))
                return false;

            if (!ResearchCraftEnvironment.IsRecipeEnvironmentUnlocked(recipe))
                return false;
            return true;
        }

        /// <summary>绿脸预览不模拟 Boss/事件进度；液体/群系/附近环境由 <see cref="ResearchCraftEnvironment"/> 判定。</summary>
        private static bool MeetsGreenFaceRecipeConditions(Recipe recipe) =>
            recipe != null && !recipe.Disabled;

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

            EnsureShimmerTransformTargets();
            if (_shimmerTransformTargets != null && _shimmerTransformTargets.Contains(itemType))
                return true;

            int idx = ShimmerTransforms.GetDecraftingRecipeIndex(itemType);
            return idx >= 0 && idx < Recipe.numRecipes;
        }

        internal static bool IsShimmerDecraftRecipe(Recipe recipe)
        {
            if (recipe?.createItem == null || recipe.createItem.IsAir)
                return false;
            int product = recipe.createItem.type;
            int idx = ShimmerTransforms.GetDecraftingRecipeIndex(product);
            return idx >= 0 && idx < Recipe.numRecipes && Main.recipe[idx] == recipe;
        }

        private static void EnsureShimmerTransformTargets()
        {
            if (_shimmerTargetsBuilt)
                return;

            _shimmerTransformTargets = new HashSet<int>();
            _shimmerTargetsBuilt = true;
            if (ResearchCraftingPlayer.HasEncounteredShimmer)
                return;

            int[] targets = ItemID.Sets.ShimmerTransformToItem;
            if (targets == null)
                return;

            for (int i = 1; i < targets.Length; i++)
            {
                int to = targets[i];
                if (to > ItemID.None)
                    _shimmerTransformTargets.Add(to);
            }
        }
    }
}
