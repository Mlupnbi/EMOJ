using System;
using EvenMoreOverpoweredJourney.Core.Localization;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    internal static class FurnitureSchemeNaming
    {
        public static string DefaultNewSetBaseName =>
            EOPJText.UIOr("Blueprint.NewSetDefaultName", "New set");

        public static string AllocateUniqueDisplayName(FurnitureBlueprintPlayer player, string baseName = null)
        {
            baseName = string.IsNullOrWhiteSpace(baseName) ? DefaultNewSetBaseName : baseName.Trim();
            if (!DisplayNameExists(player, baseName))
                return baseName;

            for (int i = 1; i < 10000; i++)
            {
                string candidate = $"{baseName}({i})";
                if (!DisplayNameExists(player, candidate))
                    return candidate;
            }

            return baseName + "_" + DateTime.UtcNow.ToString("HHmmss");
        }

        public static bool DisplayNameExists(FurnitureBlueprintPlayer player, string displayName)
        {
            if (player == null || string.IsNullOrEmpty(displayName))
                return false;

            foreach (var pair in player.CustomSchemes)
            {
                if (pair.Value == null)
                    continue;
                if (string.Equals(pair.Value.DisplayName, displayName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
