using System.Collections.Generic;
using System.Reflection;
using Terraria;
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
