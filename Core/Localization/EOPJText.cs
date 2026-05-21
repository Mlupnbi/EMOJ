using Terraria.Localization;

namespace EvenMoreOverpoweredJourney.Core.Localization
{
    internal static class EOPJText
    {
        private const string Root = "Mods.EvenMoreOverpoweredJourney.";

        public static string UI(string key) => Language.GetTextValue(Root + "UI." + key);

        public static string UIFormat(string key, params object[] args) => Language.GetTextValue(Root + "UI." + key, args);

        public static string RecipeEnv(string key) => Language.GetTextValue(Root + "RecipeEnv." + key);
    }
}
