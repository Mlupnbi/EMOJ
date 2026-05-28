using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public readonly struct SchemeLibraryEntry
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public bool IsAuto { get; init; }
        public int SeedType { get; init; }
        public FurnitureScheme Scheme { get; init; }
    }

    public static class FurnitureSchemeLibrary
    {
        public const string AutoIdPrefix = "auto:";

        public static string AutoIdForSeed(int seedType) => AutoIdPrefix + seedType;

        public static bool TryParseAutoSeed(string id, out int seedType)
        {
            seedType = ItemID.None;
            if (string.IsNullOrEmpty(id) || !id.StartsWith(AutoIdPrefix))
                return false;
            return int.TryParse(id.Substring(AutoIdPrefix.Length), out seedType) && seedType > ItemID.None;
        }

        public static List<SchemeLibraryEntry> BuildEntries(FurnitureBlueprintPlayer player)
        {
            var list = new List<SchemeLibraryEntry>();
            if (player == null)
                return list;

            foreach (var pair in player.CustomSchemes)
            {
                if (pair.Value == null)
                    continue;
                list.Add(new SchemeLibraryEntry
                {
                    Id = pair.Key,
                    DisplayName = string.IsNullOrEmpty(pair.Value.DisplayName) ? pair.Key : pair.Value.DisplayName,
                    IsAuto = false,
                    SeedType = pair.Value.SeedType,
                    Scheme = pair.Value
                });
            }

            list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase));
            return list;
        }

        public static bool TryGetScheme(FurnitureBlueprintPlayer player, string id, out FurnitureScheme scheme)
        {
            scheme = null;
            if (player == null || string.IsNullOrEmpty(id))
                return false;

            if (TryParseAutoSeed(id, out int seed) && player.AutoSchemesBySeed.TryGetValue(seed, out scheme))
            {
                scheme = scheme.Clone();
                return true;
            }

            if (player.CustomSchemes.TryGetValue(id, out scheme))
            {
                scheme = scheme.Clone();
                return true;
            }

            return false;
        }
    }
}
