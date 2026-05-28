using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Research
{
    /// <summary>合成配方「环境」一行：工作台本地化名称 + 水体/群系等条件（原版部分字段�? internal，用反射读取）�?</summary>
    public static class RecipeEnvironmentHelper
    {
        private static readonly Dictionary<int, string> TileTypeToItemNameCache = new Dictionary<int, string>();

        private static readonly (string field, string langKey)[] EnvBoolFields =
        {
            ("needWater", "NearWater"),
            ("needLava", "NearLava"),
            ("needHoney", "NearHoney"),
            ("needSnowBiome", "SnowBiome"),
            ("needGraveyardBiome", "Graveyard"),
            ("needEverythingSeed", "ZenithWorld"),
            ("corruption", "CorruptionWorld"),
            ("crimson", "CrimsonWorld"),
        };

        public static string BuildEnvironmentDisplayText(Recipe recipe)
        {
            var parts = new List<string>();
            foreach (int tileType in recipe.requiredTile)
            {
                if (tileType < 0)
                    continue;
                string s = GetCraftingStationDisplayName(tileType);
                if (!string.IsNullOrEmpty(s))
                    parts.Add(s);
            }

            foreach ((string field, string langKey) in EnvBoolFields)
            {
                if (GetRecipeBool(recipe, field))
                    parts.Add(EOPJText.RecipeEnv(langKey));
            }

            return string.Join(EOPJText.UI("ListJoiner"), DistinctPreserveOrder(parts));
        }

        private static List<string> DistinctPreserveOrder(List<string> parts)
        {
            var seen = new HashSet<string>();
            var ordered = new List<string>();
            foreach (string p in parts)
            {
                if (string.IsNullOrEmpty(p) || !seen.Add(p))
                    continue;
                ordered.Add(p);
            }
            return ordered;
        }

        private static bool GetRecipeBool(Recipe r, string name)
        {
            FieldInfo f = typeof(Recipe).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return f?.GetValue(r) is bool b && b;
        }

        public static bool RecipeNeedsWater(Recipe recipe) => GetRecipeBool(recipe, "needWater");
        public static bool RecipeNeedsLava(Recipe recipe) => GetRecipeBool(recipe, "needLava");
        public static bool RecipeNeedsHoney(Recipe recipe) => GetRecipeBool(recipe, "needHoney");
        public static bool RecipeNeedsSnowBiome(Recipe recipe) => GetRecipeBool(recipe, "needSnowBiome");
        public static bool RecipeNeedsGraveyard(Recipe recipe) => GetRecipeBool(recipe, "needGraveyardBiome");

        public static bool RecipeNeedsGraveyardIncludingConditions(Recipe recipe) =>
            RecipeNeedsGraveyard(recipe) || RecipeConditionsRequireGraveyard(recipe);

        public static bool RecipeNeedsSnowBiomeIncludingConditions(Recipe recipe) =>
            RecipeNeedsSnowBiome(recipe) || RecipeConditionsRequireSnowBiome(recipe);

        public static bool RecipeNeedsWaterIncludingConditions(Recipe recipe) =>
            RecipeNeedsWater(recipe) || RecipeConditionsRequireWater(recipe);

        public static bool RecipeNeedsLavaIncludingConditions(Recipe recipe) =>
            RecipeNeedsLava(recipe) || RecipeConditionsRequireLava(recipe);

        public static bool RecipeNeedsHoneyIncludingConditions(Recipe recipe) =>
            RecipeNeedsHoney(recipe) || RecipeConditionsRequireHoney(recipe);

        public static bool ConditionRequiresGraveyard(Condition condition) =>
            ConditionMatchesKnown(condition, Condition.InGraveyard)
            || ConditionDescriptionMatches(condition, "Graveyard", "graveyard", "?");

        public static bool ConditionRequiresSnowBiome(Condition condition) =>
            ConditionMatchesKnown(condition, Condition.InSnow)
            || ConditionDescriptionMatches(condition, "Snow", "snow", "?");

        public static bool ConditionRequiresWater(Condition condition) =>
            ConditionMatchesKnown(condition, Condition.NearWater)
            || ConditionDescriptionMatches(condition, "Water", "water", "?");

        public static bool ConditionRequiresLava(Condition condition) =>
            ConditionMatchesKnown(condition, Condition.NearLava)
            || ConditionDescriptionMatches(condition, "Lava", "lava", "??", "??");

        public static bool ConditionRequiresHoney(Condition condition) =>
            ConditionMatchesKnown(condition, Condition.NearHoney)
            || ConditionDescriptionMatches(condition, "Honey", "honey", "??");

        public static bool ConditionLooksLikeCraftingStation(Condition condition)
        {
            if (condition?.Description == null)
                return false;

            return ConditionDescriptionMatches(
                condition,
                "Work Bench", "Workbench", "Workbench", "Anvil", "Furnace", "Sawmill", "Loom",
                "Table", "Bench", "Altar", "Kiln", "Forge", "Tinker", "Alchemy", "Heavy",
                "Crystal", "Autohammer", "Blend", "Meat", "Dead", "Necro", "Spirit", "Demon",
                "Hellforge", "Tile", "Station", "Near",
                "???", "?", "??", "??", "??", "??", "??", "??", "?", "?", "?", "?");
        }

        /// <summary>Condition ?? Predicate ?????????????? TileId?</summary>
        public static bool TryGetConditionTileIds(Condition condition, out int[] tileIds)
        {
            tileIds = null;
            if (condition == null || !ConditionLooksLikeCraftingStation(condition))
                return false;

            string text = (condition.Description.Value ?? string.Empty)
                + " "
                + (condition.Description.Key ?? string.Empty);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var found = new List<int>();
            EnsureTileDisplayNameCache();
            int bestTile = -1;
            int bestLen = 0;
            foreach (var pair in TileDisplayNameCache)
            {
                if (pair.Value.Length < 2)
                    continue;
                if (text.IndexOf(pair.Value, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (pair.Value.Length > bestLen)
                {
                    bestLen = pair.Value.Length;
                    bestTile = pair.Key;
                }
            }

            if (bestTile > 0)
                found.Add(bestTile);

            if (found.Count == 0)
                return false;

            tileIds = found.ToArray();
            return true;
        }

        private static Dictionary<int, string> TileDisplayNameCache;

        private static void EnsureTileDisplayNameCache()
        {
            if (TileDisplayNameCache != null)
                return;

            TileDisplayNameCache = new Dictionary<int, string>();
            for (int tileId = 0; tileId < TileLoader.TileCount; tileId++)
            {
                string name = GetCraftingStationDisplayName(tileId);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                TileDisplayNameCache[tileId] = name;
            }
        }

        private static bool RecipeConditionsRequireGraveyard(Recipe recipe) =>
            RecipeConditionsMatch(recipe, ConditionRequiresGraveyard);

        private static bool RecipeConditionsRequireSnowBiome(Recipe recipe) =>
            RecipeConditionsMatch(recipe, ConditionRequiresSnowBiome);

        private static bool RecipeConditionsRequireWater(Recipe recipe) =>
            RecipeConditionsMatch(recipe, ConditionRequiresWater);

        private static bool RecipeConditionsRequireLava(Recipe recipe) =>
            RecipeConditionsMatch(recipe, ConditionRequiresLava);

        private static bool RecipeConditionsRequireHoney(Recipe recipe) =>
            RecipeConditionsMatch(recipe, ConditionRequiresHoney);

        private static bool RecipeConditionsMatch(Recipe recipe, System.Func<Condition, bool> predicate)
        {
            if (recipe?.Conditions == null || recipe.Conditions.Count == 0)
                return false;

            foreach (Condition condition in recipe.Conditions)
            {
                if (predicate(condition))
                    return true;
            }

            return false;
        }

        private static bool ConditionMatchesKnown(Condition condition, params Condition[] known)
        {
            if (condition == null || known == null)
                return false;

            foreach (Condition candidate in known)
            {
                if (candidate != null && condition == candidate)
                    return true;
            }

            return false;
        }

        private static bool ConditionDescriptionMatches(Condition condition, params string[] tokens)
        {
            if (condition == null || tokens == null || tokens.Length == 0)
                return false;

            LocalizedText desc = condition.Description;
            if (desc == null)
                return false;

            string key = desc.Key ?? string.Empty;
            string value = desc.Value ?? string.Empty;
            foreach (string token in tokens)
            {
                if (string.IsNullOrEmpty(token))
                    continue;
                if (key.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool RecipeNeedsAlchemyTable(Recipe recipe)
        {
            if (recipe?.requiredTile == null)
                return false;
            foreach (int tile in recipe.requiredTile)
            {
                if (tile == Terraria.ID.TileID.AlchemyTable)
                    return true;
            }
            return false;
        }

        public static string GetCraftingStationDisplayName(int tileType)
        {
            if (TileTypeToItemNameCache.TryGetValue(tileType, out string cached))
                return cached;

            for (int i = 1; i < ItemLoader.ItemCount; i++)
            {
                Item probe = new Item();
                probe.SetDefaults(i);
                if (probe.createTile == tileType && !string.IsNullOrEmpty(probe.Name))
                {
                    TileTypeToItemNameCache[tileType] = probe.Name;
                    return probe.Name;
                }
            }

            ModTile modTile = TileLoader.GetTile(tileType);
            if (modTile != null)
            {
                string mapKey = $"Mods.{modTile.Mod.Name}.MapObject.{modTile.Name}";
                string localized = Terraria.Localization.Language.GetTextValue(mapKey);
                if (!string.IsNullOrEmpty(localized) && localized != mapKey)
                {
                    TileTypeToItemNameCache[tileType] = localized;
                    return localized;
                }
                string typeKey = $"Mods.{modTile.Mod.Name}.{modTile.GetType().Name}.MapEntry";
                localized = Terraria.Localization.Language.GetTextValue(typeKey);
                if (!string.IsNullOrEmpty(localized) && localized != typeKey)
                {
                    TileTypeToItemNameCache[tileType] = localized;
                    return localized;
                }
                TileTypeToItemNameCache[tileType] = modTile.Name;
                return modTile.Name;
            }

            if (Terraria.ID.TileID.Search.TryGetName(tileType, out string key))
            {
                string mapObject = Terraria.Localization.Language.GetTextValue("MapObject." + key);
                if (!string.IsNullOrEmpty(mapObject) && mapObject != "MapObject." + key)
                {
                    TileTypeToItemNameCache[tileType] = mapObject;
                    return mapObject;
                }
                string tiles = Terraria.Localization.Language.GetTextValue("Tiles." + key);
                if (!string.IsNullOrEmpty(tiles) && tiles != "Tiles." + key)
                {
                    TileTypeToItemNameCache[tileType] = tiles;
                    return tiles;
                }
                TileTypeToItemNameCache[tileType] = key;
                return key;
            }

            string unknown = EOPJText.RecipeEnv("UnknownStation");
            TileTypeToItemNameCache[tileType] = unknown;
            return unknown;
        }
    }
}
