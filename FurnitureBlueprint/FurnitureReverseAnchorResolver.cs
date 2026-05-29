using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// ���ƣ������Ӳ顸�ϳɸ���Ʒ�����䷽ �� �ռ�ԭ���� �� ѡ���������� StyleKey �����ϵ�һ��Ϊê�㡣
    /// ��������ѡ�� <see cref="FurnitureRecipeSetLinker.AddProductsConsumingMaterial"/> ��������ê��Ĳ����
    /// </summary>
    public static class FurnitureReverseAnchorResolver
    {
        private const int MaxRecipeGroupExpand = 10;
        private const int MaxScoreboardSize = 36;

        private static readonly string[] GenericMaterialKeys =
        {
            "Wood", "Stone", "Glass", "Ice", "Sand", "Mud", "Ash", "Iron", "Copper", "Silver", "Gold",
            "Fence", "Chain", "Bar", "Lead", "Any", "Dirt"
        };

        public static int ResolveAnchorFromSeed(int seedType, FurnitureStyleSignature seedSignature) =>
            FurnitureReverseSeedProbeCache.Ensure(seedType).BestAnchorIngredient;

        public static List<int> GetMaterialCandidatesForSeed(int seedType) =>
            new List<int>(FurnitureReverseSeedProbeCache.Ensure(seedType).PickerCandidates);

        public static void ClearReverseCaches() => FurnitureReverseSeedProbeCache.Clear();

        internal static void CollectIngredientScoreboardPublic(
            int seedType,
            string targetStyle,
            FurnitureStyleSignature seedSignature,
            List<(int type, int score)> scoreboard,
            ref int best,
            ref int bestScore) =>
            CollectIngredientScoreboard(seedType, targetStyle, seedSignature, scoreboard, ref best, ref bestScore);

        internal static bool IsPickerEligibleMaterialPublic(Item item) => IsPickerEligibleMaterial(item);

        /// <summary>种子配方 requiredTile 与材料块参与配方是否一致（生命木/锯木台/死木等分流）。</summary>
        public static int ScoreStationMaterialLink(int seedType, int materialType)
        {
            if (seedType <= ItemID.None || materialType <= ItemID.None)
                return 0;

            FurnitureCraftStationProfile stations = FurnitureCraftStationProfile.FromSeed(seedType);
            if (!stations.IsConstrained)
                return 0;

            int best = 0;
            foreach (Recipe recipe in FurnitureRecipeLookup.GetRecipesCreating(seedType))
            {
                if (!RecipeAnalyzer.RecipeUsesIngredient(recipe, materialType))
                    continue;
                best = Math.Max(best, stations.ScoreRecipeMatch(recipe));
            }

            return best;
        }

        public static int CombineMaterialRankScore(int seedType, int materialType)
        {
            if (materialType <= ItemID.None)
                return int.MinValue / 4;

            FurnitureStyleSignature sig = FurnitureStyleSignature.FromItemType(seedType);
            Item probe = new Item();
            probe.SetDefaults(materialType);
            int style = ScoreIngredientNameFit(materialType, sig.StyleKey?.Trim() ?? "", sig, probe);
            if (style <= int.MinValue / 8)
                style = 0;

            int station = ScoreStationMaterialLink(seedType, materialType);
            if (FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
                style += 800;

            return style + station * 150;
        }

        public static int PickBestMaterialFromCandidates(int seedType, IReadOnlyList<int> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return ItemID.None;

            int best = candidates[0];
            int bestRank = int.MinValue;
            foreach (int type in candidates)
            {
                int rank = CombineMaterialRankScore(seedType, type);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = type;
                }
            }

            return best;
        }

        private static void CollectIngredientScoreboard(
            int seedType,
            string targetStyle,
            FurnitureStyleSignature seedSignature,
            List<(int type, int score)> scoreboard,
            ref int best,
            ref int bestScore)
        {
            foreach (Recipe recipe in FurnitureRecipeLookup.GetRecipesCreating(seedType))
            {
                if (recipe?.requiredItem == null)
                    continue;

                for (int i = 0; i < recipe.requiredItem.Count; i++)
                {
                    Item req = recipe.requiredItem[i];
                    if (req == null || req.IsAir)
                        continue;

                    int gid = RecipeAnalyzer.GetAcceptedGroupId(recipe, i);
                    if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
                    {
                        RecipeGroup group = RecipeGroup.recipeGroups[gid];
                        if (group?.ValidItems != null)
                        {
                            foreach (int groupType in ExpandGroupItems(group.ValidItems, targetStyle, MaxRecipeGroupExpand))
                                ConsiderIngredient(seedType, groupType, targetStyle, seedSignature, ref best, ref bestScore, scoreboard);
                        }
                    }

                    ConsiderIngredient(seedType, req.type, targetStyle, seedSignature, ref best, ref bestScore, scoreboard);
                    if (scoreboard.Count >= MaxScoreboardSize)
                        break;
                }

                if (scoreboard.Count >= MaxScoreboardSize)
                    break;
            }

            FurniturePlacementLineMaterialResolver.CollectBlockCandidatesFromPlacementLine(
                seedType, seedSignature, scoreboard);

            foreach ((int type, int score) entry in scoreboard)
            {
                if (entry.score > bestScore)
                {
                    bestScore = entry.score;
                    best = entry.type;
                }
            }
        }

        private static void ConsiderIngredient(
            int seedType,
            int ingredientType,
            string targetStyle,
            FurnitureStyleSignature seedSignature,
            ref int best,
            ref int bestScore,
            List<(int type, int score)> scoreboard)
        {
            if (ingredientType <= ItemID.None)
                return;

            ingredientType = FurnitureVanillaLivingWoodBridge.RedirectIngredientForScoring(seedType, ingredientType);

            Item probe = new Item();
            probe.SetDefaults(ingredientType);
            int score = ScoreIngredientNameFit(ingredientType, targetStyle, seedSignature, probe);
            score += ScoreStationMaterialLink(seedType, ingredientType) * 2;

            if (score <= 0 && FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
            {
                Item seedProbe = new Item();
                seedProbe.SetDefaults(seedType);
                bool seedIsFurniturePiece = seedProbe.createTile >= TileID.Dirt
                    && !FurnitureMaterialAnchor.IsValidAnchorBlock(seedProbe);
                string ingredientKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(ingredientType);
                if (seedIsFurniturePiece && IsGenericMaterialKey(ingredientKey))
                    return;

                ModItem seedMod = ItemLoader.GetItem(seedType);
                ModItem ingMod = ItemLoader.GetItem(ingredientType);
                if (seedMod != null && ingMod != null && seedMod.Mod.Name == ingMod.Mod.Name)
                    score = 1_800;
                else if (!seedIsFurniturePiece)
                    score = 500;
            }

            if (score <= 0)
                return;

            int existing = scoreboard.FindIndex(e => e.type == ingredientType);
            if (existing >= 0)
            {
                if (score > scoreboard[existing].score)
                    scoreboard[existing] = (ingredientType, score);
            }
            else if (scoreboard.Count < MaxScoreboardSize)
            {
                scoreboard.Add((ingredientType, score));
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = ingredientType;
            }
        }

        /// <summary>ԭ�����������������������϶ȣ�Խ��Խ���ȣ���</summary>
        public static int ScoreIngredientNameFit(
            int ingredientType,
            string seedStyleKey,
            FurnitureStyleSignature seedSignature,
            Item probe)
        {
            if (ingredientType <= ItemID.None || probe == null || probe.IsAir)
                return int.MinValue / 4;

            if (string.IsNullOrWhiteSpace(seedStyleKey))
                seedStyleKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(ingredientType);

            string ingredientKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(ingredientType);
            string displayName = probe.Name ?? ItemID.Search.GetName(ingredientType) ?? "";

            int score = ScoreStyleKeyMatch(seedStyleKey, ingredientKey, displayName);
            if (score <= 0)
                return int.MinValue / 4;

            if (IsGenericMaterialKey(ingredientKey) && !ingredientKey.Equals(seedStyleKey, StringComparison.OrdinalIgnoreCase))
                score -= 6_000;

            if (FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
                score += 800;
            else if (IsCraftMaterial(probe))
                score += 200;
            else if (probe.createWall > WallID.None && probe.createTile < TileID.Dirt)
                score -= 8_000;
            else if (IsLikelyWrongIngredient(probe, seedStyleKey))
                score -= 5_000;
            else
                score -= 400;

            ModItem im = ItemLoader.GetItem(ingredientType);
            string mod = im == null ? "Terraria" : im.Mod.Name;
            if (mod == seedSignature.ModKey)
                score += 120;

            return score;
        }

        private static bool IsGenericMaterialKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            foreach (string generic in GenericMaterialKeys)
            {
                if (key.Equals(generic, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static int ScoreStyleKeyMatch(string seedStyleKey, string ingredientKey, string displayName)
        {
            seedStyleKey = seedStyleKey.Trim();
            ingredientKey = ingredientKey?.Trim() ?? "";
            displayName = displayName?.Trim() ?? "";

            if (string.IsNullOrEmpty(seedStyleKey))
                return 100;

            if (string.Equals(ingredientKey, seedStyleKey, StringComparison.OrdinalIgnoreCase))
                return 10_000;

            if (string.Equals(displayName, seedStyleKey, StringComparison.OrdinalIgnoreCase))
                return 9_500;

            if (ingredientKey.StartsWith(seedStyleKey, StringComparison.OrdinalIgnoreCase)
                || seedStyleKey.StartsWith(ingredientKey, StringComparison.OrdinalIgnoreCase))
            {
                if (FurnitureStyleSignature.StyleKeySameMaterialFamily(seedStyleKey, ingredientKey))
                    return 7_000;
            }

            if (displayName.Contains(seedStyleKey, StringComparison.OrdinalIgnoreCase)
                && FurnitureStyleSignature.StyleKeySameMaterialFamily(seedStyleKey, ingredientKey))
                return 5_500;

            if (FurnitureStyleSignature.StyleKeyFuzzyMatch(seedStyleKey, ingredientKey))
                return 3_500;

            return 0;
        }

        private static bool IsLikelyWrongIngredient(Item probe, string seedStyleKey)
        {
            if (probe == null || probe.IsAir || string.IsNullOrWhiteSpace(seedStyleKey))
                return false;

            string name = (probe.Name ?? "").ToLowerInvariant();
            if (name.Contains("fence") || name.Contains("iron") || name.Contains("chain"))
                return true;

            string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(probe.type);
            if (IsGenericMaterialKey(key))
                return true;

            return !FurnitureStyleSignature.StyleKeyFuzzyMatch(seedStyleKey, key)
                && !FurnitureMaterialKeyNormalizer.SameMaterialFamily(seedStyleKey, key);
        }

        private static bool IsPickerEligibleMaterial(Item item)
        {
            if (item == null || item.IsAir)
                return false;
            if (FurnitureMaterialAnchor.IsValidAnchorBlock(item))
                return true;
            if (item.type < ItemID.Sets.IsAMaterial.Length && ItemID.Sets.IsAMaterial[item.type])
                return true;
            return FurnitureTileSafety.IsPhysicallySolidTile(item.createTile);
        }

        private static bool IsCraftMaterial(Item item)
        {
            if (item == null || item.IsAir)
                return false;
            if (item.type < ItemID.Sets.IsAMaterial.Length && ItemID.Sets.IsAMaterial[item.type])
                return true;
            return FurnitureTileSafety.IsPhysicallySolidTile(item.createTile);
        }

        private static IEnumerable<int> ExpandGroupItems(IEnumerable<int> validItems, string targetStyle, int max)
        {
            if (validItems == null)
                yield break;

            var list = validItems as IList<int> ?? new List<int>(validItems);
            if (list.Count == 0)
                yield break;

            if (list.Count <= max)
            {
                foreach (int t in list)
                    yield return t;
                yield break;
            }

            var ranked = new List<(int type, int rank)>();
            foreach (int t in list)
            {
                string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(t);
                int rank = string.IsNullOrEmpty(targetStyle) ? 0
                    : FurnitureStyleSignature.StyleKeyFuzzyMatch(targetStyle, key) ? 100
                    : FurnitureMaterialKeyNormalizer.SameMaterialFamily(targetStyle, key) ? 50
                    : 0;
                ranked.Add((t, rank));
            }

            ranked.Sort((a, b) => b.rank.CompareTo(a.rank));
            int n = 0;
            foreach ((int type, int _) in ranked)
            {
                if (n++ >= max)
                    yield break;
                yield return type;
            }
        }
    }
}
