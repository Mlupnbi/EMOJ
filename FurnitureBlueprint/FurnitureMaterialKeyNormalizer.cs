using System;
using System.Collections.Generic;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>材料/套组 StyleKey 规范化，减少「Sandstone / Sand Stone / SandstoneBrick」误判为不同套组。</summary>
    public static class FurnitureMaterialKeyNormalizer
    {
        private static readonly string[] StripSuffixes =
        {
            "Wall", "Walls", "Block", "Blocks", "Brick", "Bricks", "Plank", "Planks",
            "Slab", "Slabs", "Tile", "Tiles", "Panel", "Panels", "Ore", "Bar", "Bars", "Item"
        };

        private static readonly Dictionary<string, string[]> StyleAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Drywood"] = new[] { "DrywoodItem", "干木" },
            ["Pumpkin"] = new[] { "Pumpkin", "南瓜" },
            ["LivingWood"] = new[] { "LivingWood", "生命木" },
            ["LivingRichMahogany"] = new[] { "LivingRichMahogany", "生命红木" },
            ["Nothing"] = new[] { "Nothing", "无" },
            ["Dead"] = new[] { "Dead", "死" },
            ["Ancient"] = new[] { "Ancient", "远古", "古代" }
        };

        public static string Normalize(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "";

            key = key.Trim().Replace(" ", "", StringComparison.Ordinal);

            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (string suffix in StripSuffixes)
                {
                    if (key.Length > suffix.Length
                        && key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        key = key.Substring(0, key.Length - suffix.Length);
                        changed = true;
                        break;
                    }
                }
            }

            return key;
        }

        public static bool StyleKeysMatch(string a, string b)
        {
            if (SameMaterialFamily(a, b))
                return true;

            string na = Normalize(a);
            string nb = Normalize(b);
            if (string.IsNullOrEmpty(na) || string.IsNullOrEmpty(nb))
                return false;

            foreach (KeyValuePair<string, string[]> alias in StyleAliases)
            {
                if (MatchesAliasGroup(na, alias.Value) && MatchesAliasGroup(nb, alias.Value))
                    return true;
            }

            return false;
        }

        public static bool SameMaterialFamily(string a, string b)
        {
            string na = Normalize(a);
            string nb = Normalize(b);
            if (string.IsNullOrEmpty(na) || string.IsNullOrEmpty(nb))
                return false;

            if (na.Equals(nb, StringComparison.OrdinalIgnoreCase))
                return true;

            if (na.Length >= 4 && nb.Length >= 4)
            {
                if (na.StartsWith(nb, StringComparison.OrdinalIgnoreCase)
                    || nb.StartsWith(na, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool MatchesAliasGroup(string normalizedKey, string[] aliases)
        {
            foreach (string alias in aliases)
            {
                string normalizedAlias = Normalize(alias);
                if (normalizedKey.Equals(normalizedAlias, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (normalizedAlias.Length >= 3
                    && normalizedKey.StartsWith(normalizedAlias, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
