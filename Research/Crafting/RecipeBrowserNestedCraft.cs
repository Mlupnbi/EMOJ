using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Config;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Research.Players;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
    /// <summary>
    /// 旅途绿脸：① RB 快扫候选 → ② 嵌套入列（材料链 + 路径上台子已见过，对齐 RB RecipePath）→ ③ 定制绿/黄（1∨2 环境，不要求背包）。
    /// </summary>
    internal static class RecipeBrowserNestedCraft
    {
        internal enum GreenFaceQueryPhase
        {
            BuildList,
            Tint,
            Done
        }
        private const int MaterialChainMaxDepth = 32;
        /// <summary>嵌套中间物再扩一层产物（对齐 RB 嵌套规模，禁止多层 BFS）；可由配置关闭。</summary>
        private static int NestedIntermediateDepth =>
            OPJourneyConfig.UseGreenFaceExtendedNestedSearch() ? 1 : 0;
        private const int MaxGreenFaceCandidates = 2048;
        private const long DefaultStepBudgetMs = 12;
        private const long GreenFaceCompleteBudgetMs = 120_000;

        private static HashSet<int> _shimmerTransformTargets;
        private static bool _shimmerTargetsBuilt;

        private static int _greenFaceCacheSeed = ItemID.None;
        private static int _greenFaceCacheEnvSig = int.MinValue;
        private static bool _greenFaceCacheShimmer;
        private static int _greenFaceCacheResearchEpoch;
        private static List<int> _greenFaceCacheResults;
        private static HashSet<int> _greenFaceCacheImmediate;
        private static int _greenFaceResearchEpoch;

        internal static void InvalidateCaches()
        {
            _shimmerTargetsBuilt = false;
            _shimmerTransformTargets = null;
            JourneyStationSacrifice.InvalidateCache();
            InvalidateGreenFaceResultCache();
        }

        internal static void InvalidateGreenFaceResultCache()
        {
            _greenFaceResearchEpoch++;
            _greenFaceCacheSeed = ItemID.None;
            _greenFaceCacheEnvSig = int.MinValue;
            _greenFaceCacheResearchEpoch = -1;
            _greenFaceCacheResults = null;
            _greenFaceCacheImmediate = null;
        }

        internal sealed class GreenFaceQuerySession
        {
            internal int SeedType;
            internal List<int> Candidates = new List<int>();
            /// <summary>着色阶段索引（遍历 <see cref="Results"/>）。</summary>
            internal int CandidateIndex;
            internal readonly List<int> Results = new List<int>();
            /// <summary>旅途绿格：材料链已研究满 + 环境/台子 1∨2 满足（不要求背包有材料）。</summary>
            internal readonly HashSet<int> ImmediateCraftProducts = new HashSet<int>();
            /// <summary>RB 快扫直接层产物（着色时可跳过种子 scope 深判）。</summary>
            internal HashSet<int> DirectProducts;
            internal bool Complete;
            internal bool ListReady;
            internal GreenFaceQueryPhase Phase;
            internal long ListReadyMs;
            internal bool HighFanout;
            internal Dictionary<int, bool> ResearchMemo;
            internal Dictionary<int, bool> PathViableMemo;
            internal HashSet<int> ResearchVisiting;
            internal Dictionary<int, bool> RecipeEnvCache;
            internal Dictionary<int, bool> NestedListEnvCache;
            internal Dictionary<int, bool> JourneyGreenEnvCache;
            internal int RecipeEnvCacheSignature = int.MinValue;
            internal Dictionary<int, bool> ProductHasStationMemo;
            /// <summary>配方直接消耗种子得到的未研究中间产物（仅一层嵌套扩展用）。</summary>
            internal HashSet<int> DirectIntermediateTypes;
            internal HashSet<int> ListedProducts;
            internal Stopwatch TotalStopwatch;
            internal bool FromCache;
        }

        public static GreenFaceQuerySession BeginGreenFaceQuery(int seedType)
        {
            Main.LocalPlayer?.GetModPlayer<ResearchCraftingPlayer>()?.RefreshEnvironmentForResearchQuery();

            var session = new GreenFaceQuerySession { SeedType = seedType };
            session.ResearchMemo = new Dictionary<int, bool>();
            session.PathViableMemo = new Dictionary<int, bool>();
            session.ResearchVisiting = new HashSet<int>();

            if (seedType <= ItemID.None || !RecipeAnalyzer.IsFullyResearched(seedType) || Main.gameMenu || Main.dedServ)
            {
                session.Complete = true;
                return session;
            }

            if (TryRestoreGreenFaceCache(seedType, session))
                return session;

            session.TotalStopwatch = Stopwatch.StartNew();
            session.ProductHasStationMemo = new Dictionary<int, bool>();
            EnsureShimmerTransformTargets();
            session.HighFanout = RecipeAnalyzer.IsHighFanoutMaterial(seedType);
            int fanout = RecipeAnalyzer.EstimateMaterialFanout(seedType);
            session.Candidates = CollectGreenFaceCandidates(seedType, session);
            session.Phase = GreenFaceQueryPhase.BuildList;
            session.CandidateIndex = 0;
            session.ListReady = false;
            session.ListedProducts = new HashSet<int>();

            EmojLog.Info(EmojLogChannel.Research,
                $"GreenFace scan seed={seedType} fanout={fanout} candidates={session.Candidates.Count} highFanout={session.HighFanout}");

            if (session.Candidates.Count == 0)
            {
                session.ListReady = true;
                session.Complete = true;
                session.Phase = GreenFaceQueryPhase.Done;
            }

            return session;
        }

        /// <summary>
        /// 入列 = RB 嵌套：依赖种子 + 材料链已研究 + 路径每一步台子/环境已见过（无路径则不显示）。
        /// </summary>
        private static bool QualifiesForGreenFaceList(int productType, int seedType, GreenFaceQuerySession session)
        {
            if (!IsGreenFaceProduct(productType) || seedType <= ItemID.None)
                return false;

            session?.PathViableMemo?.Clear();
            session?.ResearchVisiting?.Clear();

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                if (!RecipeUsesSeedScope(recipe, seedType, session?.DirectIntermediateTypes))
                    continue;
                if (!RecipeCraftPathViable(recipe, session, session?.PathViableMemo, session?.ResearchVisiting, 0))
                    continue;

                return true;
            }

            return false;
        }

        /// <summary>对齐 RB RecipePath：配方链上每步材料可研究凑齐且台子已见过。</summary>
        private static bool RecipeCraftPathViable(
            Recipe recipe,
            GreenFaceQuerySession session,
            Dictionary<int, bool> memo,
            HashSet<int> visiting,
            int depth)
        {
            if (depth > MaterialChainMaxDepth)
                return false;
            if (!IsGreenFaceMaterialChainRecipe(recipe))
                return false;
            if (!IsRecipeNestedListEnvironmentOk(recipe, session))
                return false;

            if (recipe.requiredItem == null)
                return true;

            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;
                if (!IsMaterialPathViable(req.type, recipe, i, visiting, memo, session, depth + 1, fromGroupAlternative: false))
                    return false;
            }

            return true;
        }

        private static bool IsMaterialPathViable(
            int itemType,
            Recipe parentRecipe,
            int slotIndex,
            HashSet<int> visiting,
            Dictionary<int, bool> memo,
            GreenFaceQuerySession session,
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
                int gid = RecipeAnalyzer.GetAcceptedGroupId(parentRecipe, slotIndex);
                if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
                {
                    RecipeGroup group = RecipeGroup.recipeGroups[gid];
                    if (group?.ValidItems != null && group.ContainsItem(itemType))
                    {
                        bool anyOk = false;
                        foreach (int valid in group.ValidItems)
                        {
                            if (IsMaterialPathViable(valid, parentRecipe, slotIndex, visiting, memo, session, depth, fromGroupAlternative: true))
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
                    if (!RecipeCraftPathViable(producer, session, memo, visiting, depth + 1))
                        continue;
                    ok = true;
                    break;
                }

                memo[itemType] = ok;
                return ok;
            }
            finally
            {
                visiting.Remove(itemType);
            }
        }

        private static bool IsRecipeNestedListEnvironmentOk(Recipe recipe, GreenFaceQuerySession session)
        {
            if (recipe == null)
                return false;

            if (session?.NestedListEnvCache != null)
            {
                int key = recipe.RecipeIndex;
                if (session.NestedListEnvCache.TryGetValue(key, out bool cached))
                    return cached;

                bool ok = ResearchCraftEnvironment.IsRecipeNestedListEnvironmentOk(recipe);
                session.NestedListEnvCache[key] = ok;
                return ok;
            }

            return ResearchCraftEnvironment.IsRecipeNestedListEnvironmentOk(recipe);
        }

        public static bool StepGreenFaceQuery(GreenFaceQuerySession session, long budgetMs = DefaultStepBudgetMs)
        {
            if (session == null || session.Complete)
                return true;

            if (session.Candidates == null)
            {
                session.Complete = true;
                return true;
            }

            EnsureRecipeEnvCache(session);

            var sw = Stopwatch.StartNew();
            long stepBudget = budgetMs >= int.MaxValue ? GreenFaceCompleteBudgetMs : budgetMs;

            if (session.Phase == GreenFaceQueryPhase.BuildList)
            {
                while (session.CandidateIndex < session.Candidates.Count)
                {
                    int productType = session.Candidates[session.CandidateIndex++];
                    if (QualifiesForGreenFaceList(productType, session.SeedType, session)
                        && session.ListedProducts.Add(productType))
                    {
                        session.Results.Add(productType);
                    }

                    if (sw.ElapsedMilliseconds >= stepBudget)
                        return false;
                }

                session.Results.Sort();
                session.ListReady = true;
                session.ListReadyMs = session.TotalStopwatch?.ElapsedMilliseconds ?? sw.ElapsedMilliseconds;
                session.CandidateIndex = 0;
                session.Phase = GreenFaceQueryPhase.Tint;

                EmojLog.Info(EmojLogChannel.Research,
                    $"GreenFace listReady seed={session.SeedType} candidates={session.Candidates.Count} list={session.Results.Count} ms={session.ListReadyMs}");

                if (session.Results.Count == 0)
                {
                    session.Phase = GreenFaceQueryPhase.Done;
                    session.Complete = true;
                    if (!session.FromCache)
                        StoreGreenFaceCache(session);
                }

                return false;
            }

            if (session.Results.Count == 0)
            {
                session.Complete = true;
                return true;
            }

            while (session.CandidateIndex < session.Results.Count)
            {
                int productType = session.Results[session.CandidateIndex++];
                session.ResearchVisiting?.Clear();

                if (TryEvaluateJourneyReady(productType, session.SeedType, session))
                    session.ImmediateCraftProducts.Add(productType);

                if (sw.ElapsedMilliseconds >= stepBudget)
                    return false;
            }

            session.Phase = GreenFaceQueryPhase.Done;
            session.Complete = true;
            long totalMs = session.TotalStopwatch?.ElapsedMilliseconds ?? sw.ElapsedMilliseconds;
            long tintMs = totalMs - session.ListReadyMs;
            if (!session.FromCache)
                StoreGreenFaceCache(session);
            EmojLog.Info(EmojLogChannel.Research,
                $"GreenFace complete seed={session.SeedType} ms={totalMs} listMs={session.ListReadyMs} tintMs={tintMs} cached={session.FromCache} highFanout={session.HighFanout} list={session.Results.Count} journeyGreen={session.ImmediateCraftProducts.Count} journeyYellow={session.Results.Count - session.ImmediateCraftProducts.Count} " +
                $"env[seenTiles={ResearchCraftingPlayer.CountSeenTiles()},researchTiles={ResearchCraftingPlayer.CountResearchedTiles()},seen={ResearchCraftingPlayer.SeenEnvironment},research={ResearchCraftingPlayer.ResearchedEnvironment}]");
            GreenFaceDiagnostics.LogQueryComplete(session);
            return true;
        }

        private static bool TryRestoreGreenFaceCache(int seedType, GreenFaceQuerySession session)
        {
            if (_greenFaceCacheResults == null || _greenFaceCacheSeed != seedType)
                return false;

            int sig = ResearchCraftingPlayer.GetEnvironmentSignature();
            bool shimmer = ResearchCraftingPlayer.HasEncounteredShimmer;
            if (_greenFaceCacheEnvSig != sig || _greenFaceCacheShimmer != shimmer
                || _greenFaceCacheResearchEpoch != _greenFaceResearchEpoch)
                return false;

            session.FromCache = true;
            session.ListReady = true;
            session.Phase = GreenFaceQueryPhase.Done;
            session.Complete = true;
            session.Results.AddRange(_greenFaceCacheResults);
            foreach (int type in _greenFaceCacheImmediate)
                session.ImmediateCraftProducts.Add(type);
            EmojLog.Info(EmojLogChannel.Research,
                $"GreenFace cache hit seed={seedType} results={session.Results.Count} immediate={session.ImmediateCraftProducts.Count} envSig={sig}");
            return true;
        }

        private static void StoreGreenFaceCache(GreenFaceQuerySession session)
        {
            _greenFaceCacheSeed = session.SeedType;
            _greenFaceCacheEnvSig = ResearchCraftingPlayer.GetEnvironmentSignature();
            _greenFaceCacheShimmer = ResearchCraftingPlayer.HasEncounteredShimmer;
            _greenFaceCacheResearchEpoch = _greenFaceResearchEpoch;
            _greenFaceCacheResults = new List<int>(session.Results);
            _greenFaceCacheImmediate = new HashSet<int>(session.ImmediateCraftProducts);
        }

        public static List<int> GetDeepCraftableProductsForGreenFace(int seedType)
        {
            GreenFaceQuerySession session = BeginGreenFaceQuery(seedType);
            while (!session.Complete)
                StepGreenFaceQuery(session);
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
                var pathMemo = new Dictionary<int, bool>();
                var researchVisiting = new HashSet<int>();
                var scratch = CreateTintScratchSession(seedType);
                scratch.ResearchMemo = researchMemo;
                scratch.PathViableMemo = pathMemo;
                scratch.ResearchVisiting = researchVisiting;

                foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
                {
                    if (!RecipeUsesSeedScope(recipe, seedType, scratch.DirectIntermediateTypes))
                        continue;
                    if (!RecipeCraftPathViable(recipe, scratch, pathMemo, researchVisiting, 0))
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

        /// <summary>调试：原版当前可合成列表（与 RB 绿格一致，非 EMOJ 旅途绿格）。</summary>
        public static bool IsRecipeInAvailableRecipes(Recipe recipe)
        {
            if (recipe == null)
                return false;

            int index = recipe.RecipeIndex;
            for (int n = 0; n < Main.numAvailableRecipes; n++)
            {
                if (Main.availableRecipe[n] == index)
                    return true;
            }

            return false;
        }

        /// <summary>旅途绿格：材料链已研究 + 依赖种子 + 环境/台子满足（不要求背包实体材料）。</summary>
        public static bool IsProductJourneyReady(int productType, int seedType, GreenFaceQuerySession session = null)
        {
            if (seedType <= ItemID.None || !RecipeAnalyzer.IsFullyResearched(seedType) || !IsGreenFaceProduct(productType))
                return false;

            GreenFaceQuerySession scratch = session ?? CreateTintScratchSession(seedType);
            return TryEvaluateJourneyReady(productType, seedType, scratch);
        }

        private static GreenFaceQuerySession CreateTintScratchSession(int seedType)
        {
            var session = new GreenFaceQuerySession
            {
                SeedType = seedType,
                ResearchMemo = new Dictionary<int, bool>(),
                PathViableMemo = new Dictionary<int, bool>(),
                ResearchVisiting = new HashSet<int>(),
                RecipeEnvCache = new Dictionary<int, bool>(),
                NestedListEnvCache = new Dictionary<int, bool>(),
                RecipeEnvCacheSignature = ResearchCraftingPlayer.GetEnvironmentSignature(),
                ProductHasStationMemo = new Dictionary<int, bool>(),
                DirectProducts = BuildDirectProductsForSeed(seedType),
                DirectIntermediateTypes = GetDirectIntermediateTypesForSeed(seedType)
            };
            return session;
        }

        private static HashSet<int> BuildDirectProductsForSeed(int seedType)
        {
            var direct = new HashSet<int>();
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(seedType))
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;
                int product = recipe.createItem.type;
                if (product > ItemID.None)
                    direct.Add(product);
            }

            return direct;
        }

        private static HashSet<int> GetDirectIntermediateTypesForSeed(int seedType)
        {
            var mids = new HashSet<int>();
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(seedType))
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;
                int product = recipe.createItem.type;
                if (product > ItemID.None && !RecipeAnalyzer.IsFullyResearched(product))
                    mids.Add(product);
            }

            return mids;
        }

        /// <summary>一键研究/获取：与 <see cref="IsProductJourneyReady"/> 相同（方向 2，不看背包）。</summary>
        public static bool IsProductImmediatelyCraftable(int productType, int seedType) =>
            IsProductJourneyReady(productType, seedType);

        /// <summary>仅重算当前列表产物的绿/黄（环境变化时，避免整次绿脸查询）。</summary>
        public static void RefreshJourneyReadyTints(
            IReadOnlyList<int> products,
            int seedType,
            HashSet<int> journeyReadyOut)
        {
            journeyReadyOut.Clear();
            if (products == null || products.Count == 0 || seedType <= ItemID.None)
                return;

            var researchMemo = new Dictionary<int, bool>();
            var researchVisiting = new HashSet<int>();
            var envCache = new Dictionary<int, bool>();
            int envSig = ResearchCraftingPlayer.GetEnvironmentSignature();
            var scratchSession = CreateTintScratchSession(seedType);
            scratchSession.ResearchMemo = researchMemo;
            scratchSession.ResearchVisiting = researchVisiting;
            scratchSession.RecipeEnvCache = envCache;
            scratchSession.RecipeEnvCacheSignature = envSig;

            for (int i = 0; i < products.Count; i++)
            {
                int productType = products[i];
                researchVisiting.Clear();
                if (TryEvaluateJourneyReady(productType, seedType, scratchSession))
                    journeyReadyOut.Add(productType);
            }
        }

        /// <summary>旅途绿/黄：仅在与入列相同的 qualifying 配方上，再判 1∨2 环境。</summary>
        private static bool TryEvaluateJourneyReady(int productType, int seedType, GreenFaceQuerySession session)
        {
            if (productType <= ItemID.None || seedType <= ItemID.None)
                return false;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                session?.PathViableMemo?.Clear();
                session?.ResearchVisiting?.Clear();
                if (!RecipeQualifiesForGreenFaceListPath(recipe, seedType, session))
                    continue;

                if (!IsRecipeJourneyGreenEnvironmentUnlocked(recipe, session))
                    continue;

                return true;
            }

            return false;
        }

        private static bool RecipeQualifiesForGreenFaceListPath(Recipe recipe, int seedType, GreenFaceQuerySession session)
        {
            if (!RecipeUsesSeedScope(recipe, seedType, session?.DirectIntermediateTypes))
                return false;

            return RecipeCraftPathViable(
                recipe,
                session,
                session?.PathViableMemo,
                session?.ResearchVisiting,
                0);
        }

        private static void TryEvaluateGreenFaceProduct(
            int productType,
            int seedType,
            GreenFaceQuerySession session,
            out bool onList,
            out bool journeyReady,
            out bool sawMaterialRecipe,
            out bool sawSeedPath,
            out bool sawIngredientsPath)
        {
            onList = false;
            journeyReady = false;
            sawMaterialRecipe = false;
            sawSeedPath = false;
            sawIngredientsPath = false;

            if (productType <= ItemID.None || seedType <= ItemID.None)
                return;

            onList = QualifiesForGreenFaceList(productType, seedType, session);
            if (!onList)
                return;

            sawMaterialRecipe = true;
            sawSeedPath = true;
            sawIngredientsPath = true;
            journeyReady = TryEvaluateJourneyReady(productType, seedType, session);
        }

        /// <summary>
        /// 与 RB 材料查询同源的候选集：索引 <see cref="RecipeAnalyzer.GetRecipesConsumingMaterial"/>（直接耗种子），
        /// 至多再扩 <see cref="NestedIntermediateDepth"/> 层未研究中间物；禁止多层 BFS（会误扫锯木机等配方组牵连物）。
        /// </summary>
        private static List<int> CollectGreenFaceCandidates(int seedType, GreenFaceQuerySession session)
        {
            var candidates = new HashSet<int>();
            session.DirectIntermediateTypes = new HashSet<int>();
            session.DirectProducts = new HashSet<int>();

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(seedType))
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                int productType = recipe.createItem.type;
                if (productType <= ItemID.None)
                    continue;

                candidates.Add(productType);
                session.DirectProducts.Add(productType);
                if (!RecipeAnalyzer.IsFullyResearched(productType))
                    session.DirectIntermediateTypes.Add(productType);
            }

            int nestedDepth = NestedIntermediateDepth;
            if (nestedDepth > 0 && session.DirectIntermediateTypes.Count > 0)
            {
                var mids = session.DirectIntermediateTypes.ToList();
                foreach (int midType in mids)
                {
                    foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(midType))
                    {
                        if (recipe?.createItem == null || recipe.createItem.IsAir)
                            continue;

                        int productType = recipe.createItem.type;
                        if (productType <= ItemID.None || RecipeAnalyzer.IsFullyResearched(productType))
                            continue;

                        candidates.Add(productType);
                    }
                }
            }

            return PrioritizeGreenFaceCandidates(candidates, MaxGreenFaceCandidates);
        }

        /// <summary>配方在材料意义上使用种子：直接耗种子（含配方组）或消耗「直接中间产物」之一。</summary>
        private static bool RecipeUsesSeedScope(Recipe recipe, int seedType, HashSet<int> directIntermediates)
        {
            if (recipe == null)
                return false;

            if (RecipeAnalyzer.RecipeUsesIngredient(recipe, seedType))
                return true;

            if (!OPJourneyConfig.UseGreenFaceExtendedNestedSearch())
                return false;

            if (directIntermediates == null || directIntermediates.Count == 0 || recipe.requiredItem == null)
                return false;

            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;
                if (directIntermediates.Contains(req.type))
                    return true;
            }

            return false;
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
            if (!IsGreenFaceMaterialChainRecipe(recipe))
                return false;

            if (recipe.requiredItem == null)
                return true;

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
                int gid = RecipeAnalyzer.GetAcceptedGroupId(parentRecipe, slotIndex);
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
                    if (!IsGreenFaceMaterialChainRecipe(producer))
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

        private static bool ProductHasStationRecipe(int productType, GreenFaceQuerySession session = null)
        {
            if (session?.ProductHasStationMemo != null
                && session.ProductHasStationMemo.TryGetValue(productType, out bool cached))
            {
                return cached;
            }

            bool has = false;
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                if (RecipeHasRequiredStation(recipe))
                {
                    has = true;
                    break;
                }
            }

            session?.ProductHasStationMemo?.Add(productType, has);
            return has;
        }

        private static bool RecipeHasRequiredStation(Recipe recipe) =>
            recipe?.requiredTile != null && recipe.requiredTile.Any(t => t >= 0);

        internal static bool DebugProductHasStationRecipe(int productType) => ProductHasStationRecipe(productType);

        internal static bool DebugIsShimmerBlocked(Recipe recipe) => IsShimmerBlockedProduct(recipe);

        internal static bool DebugRecipeHasRequiredStation(Recipe recipe) => RecipeHasRequiredStation(recipe);

        internal static bool DebugIngredientsComplete(
            Recipe recipe,
            Dictionary<int, bool> memo,
            HashSet<int> visiting) =>
            RecipeIngredientsResearchComplete(recipe, memo, visiting, 0);

        internal static bool DebugDependsOnSeed(
            Recipe recipe,
            int seedType,
            Dictionary<int, bool> memo,
            HashSet<int> visiting) =>
            RecipeDependsOnSeed(recipe, seedType, memo, visiting, 0);

        private static bool RecipeDependsOnSeed(
            Recipe recipe,
            int seedType,
            Dictionary<int, bool> memo,
            HashSet<int> visiting,
            int depth)
        {
            if (depth > MaterialChainMaxDepth)
                return false;

            if (recipe.requiredItem == null)
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

            int gid = RecipeAnalyzer.GetAcceptedGroupId(parentRecipe, slotIndex);
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

        /// <summary>嵌套材料链：只校验禁用/微光，不要求工作台/液体（环境在终产物配方上单独判定）。</summary>
        private static bool IsGreenFaceMaterialChainRecipe(Recipe recipe)
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
            return MeetsGreenFaceRecipeConditions(recipe);
        }

        private static bool IsGreenFaceRecipeValid(Recipe recipe) =>
            IsGreenFaceRecipeValid(recipe, null);

        private static bool IsRecipeJourneyGreenEnvironmentUnlocked(Recipe recipe, GreenFaceQuerySession session)
        {
            if (recipe == null)
                return false;

            if (session?.JourneyGreenEnvCache != null)
            {
                int key = recipe.RecipeIndex;
                if (session.JourneyGreenEnvCache.TryGetValue(key, out bool cached))
                    return cached;

                bool unlocked = ResearchCraftEnvironment.IsRecipeJourneyGreenEnvironmentUnlocked(recipe);
                session.JourneyGreenEnvCache[key] = unlocked;
                return unlocked;
            }

            return ResearchCraftEnvironment.IsRecipeJourneyGreenEnvironmentUnlocked(recipe);
        }

        private static bool IsRecipeEnvironmentUnlocked(Recipe recipe, GreenFaceQuerySession session)
        {
            if (recipe == null)
                return false;

            if (session?.RecipeEnvCache != null)
            {
                int key = recipe.RecipeIndex;
                if (session.RecipeEnvCache.TryGetValue(key, out bool cached))
                    return cached;

                bool unlocked = ResearchCraftEnvironment.IsRecipeEnvironmentUnlocked(recipe);
                session.RecipeEnvCache[key] = unlocked;
                return unlocked;
            }

            return ResearchCraftEnvironment.IsRecipeEnvironmentUnlocked(recipe);
        }

        private static bool IsGreenFaceRecipeValid(Recipe recipe, GreenFaceQuerySession session) =>
            IsGreenFaceMaterialChainRecipe(recipe) && IsRecipeEnvironmentUnlocked(recipe, session);

        private static void EnsureRecipeEnvCache(GreenFaceQuerySession session)
        {
            if (session == null)
                return;

            int sig = ResearchCraftingPlayer.GetEnvironmentSignature();
            if (session.RecipeEnvCache != null && session.RecipeEnvCacheSignature == sig)
                return;

            session.RecipeEnvCacheSignature = sig;
            session.RecipeEnvCache ??= new Dictionary<int, bool>();
            session.RecipeEnvCache.Clear();
            session.NestedListEnvCache ??= new Dictionary<int, bool>();
            session.NestedListEnvCache.Clear();
            session.JourneyGreenEnvCache ??= new Dictionary<int, bool>();
            session.JourneyGreenEnvCache.Clear();
        }

        /// <summary>绿脸预览不模拟 Boss/事件进度；液体/群系/附近环境由 <see cref="ResearchCraftEnvironment"/> 判定。</summary>
        private static bool MeetsGreenFaceRecipeConditions(Recipe recipe) =>
            recipe != null && !recipe.Disabled;

        /// <summary>
        /// 未接触微光：只挡「必须在微光旁制作」或「无台子且为微光转化专属产物」。
        /// 不用 GetDecraftingRecipeIndex：它指向的是同一条合成配方，会把木墙等全部误判为分解配方。
        /// </summary>
        private static bool IsShimmerBlockedProduct(Recipe recipe)
        {
            if (recipe?.createItem == null)
                return true;
            if (ResearchCraftingPlayer.HasEncounteredShimmer)
                return false;
            if (RecipeRequiresNearShimmer(recipe))
                return true;
            if (RecipeHasRequiredStation(recipe))
                return false;
            return IsItemShimmerBlocked(recipe.createItem.type);
        }

        internal static bool RecipeRequiresNearShimmer(Recipe recipe)
        {
            if (recipe?.Conditions == null || recipe.Conditions.Count == 0)
                return false;

            foreach (Condition condition in recipe.Conditions)
            {
                if (condition == null)
                    continue;
                if (condition == Condition.NearShimmer)
                    return true;

                LocalizedText desc = condition.Description;
                if (desc == null)
                    continue;

                string key = desc.Key ?? string.Empty;
                string value = desc.Value ?? string.Empty;
                if (key.IndexOf("Shimmer", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf("shimmer", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf("微光", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>未接触微光时仅隐藏「微光转化产物」，不按 shimmer 分解索引屏蔽普通合成物（木墙/家具等）。</summary>
        internal static bool IsItemShimmerBlocked(int itemType)
        {
            if (itemType <= ItemID.None || ResearchCraftingPlayer.HasEncounteredShimmer)
                return false;

            EnsureShimmerTransformTargets();
            return _shimmerTransformTargets != null && _shimmerTransformTargets.Contains(itemType);
        }

        /// <summary>
        /// 勿用 GetDecraftingRecipeIndex 判定「分解配方」：返回的是 shimmer 分解时复用的合成配方索引（与 #940 木墙合成等同一条）。
        /// </summary>
        [System.Obsolete("Misidentifies normal craft recipes; use RecipeRequiresNearShimmer for green face.")]
        internal static bool IsShimmerDecraftRecipe(Recipe recipe) => RecipeRequiresNearShimmer(recipe);

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
