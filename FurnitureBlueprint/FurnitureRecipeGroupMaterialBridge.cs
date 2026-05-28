using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>RecipeGroup（任意木/锭等）→ 与种子同套的具体材料，避免闭包串套。</summary>
    public static class FurnitureRecipeGroupMaterialBridge
    {
        private static readonly Dictionary<int, int> GroupToRepresentative = new();

        public static void RegisterRepresentative(int recipeGroupId, int itemType)
        {
            if (recipeGroupId < 0 || itemType <= ItemID.None)
                return;
            GroupToRepresentative[recipeGroupId] = itemType;
        }

        public static bool TryResolveGroupIngredient(int groupId, FurnitureStyleSignature signature, out int concreteItemType)
        {
            concreteItemType = ItemID.None;
            if (groupId < 0)
                return false;

            if (GroupToRepresentative.TryGetValue(groupId, out int rep) && signature.MatchesItem(rep))
            {
                concreteItemType = rep;
                return true;
            }

            int best = ItemID.None;
            int bestScore = int.MinValue;
            if (groupId < 0 || groupId >= RecipeGroup.recipeGroups.Count)
                return false;

            RecipeGroup group = RecipeGroup.recipeGroups[groupId];
            if (group?.ValidItems == null)
                return false;

            string hint = signature.StyleKey?.Trim() ?? "";
            foreach (int type in FurnitureRecipeGroupSampling.Sample(group.ValidItems, hint, maxItems: 24))
            {
                if (!signature.MatchesItem(type))
                    continue;
                int score = ScoreRepresentative(type, signature);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = type;
                }
            }

            concreteItemType = best;
            return best > ItemID.None;
        }

        private static int ScoreRepresentative(int type, FurnitureStyleSignature signature)
        {
            int score = 0;
            if (signature.MatchesItem(type))
                score += 300;
            string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(type);
            if (!string.IsNullOrEmpty(signature.StyleKey)
                && string.Equals(key, signature.StyleKey, System.StringComparison.OrdinalIgnoreCase))
                score += 500;
            ModItem mi = ItemLoader.GetItem(type);
            if (mi != null && mi.Mod.Name == signature.ModKey)
                score += 100;
            return score;
        }
    }
}
