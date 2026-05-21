using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    public static class BestiaryStableKey
    {
        public static string ToKey(int netId)
        {
            Mod npcMod = NPCLoader.GetNPC(netId)?.Mod;
            string modPart = npcMod != null ? npcMod.Name : "Terraria";
            return $"{modPart}/{netId}";
        }

        public static string ToEntryKey(int catalogIndex, int netId) =>
            netId > 0 ? $"Entry/{catalogIndex}/{netId}" : $"Entry/{catalogIndex}";

        public static bool TryResolve(string key, out int netId)
        {
            netId = 0;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            int slash = key.LastIndexOf('/');
            if (slash < 0 || slash >= key.Length - 1)
                return false;

            return int.TryParse(key.Substring(slash + 1), out netId) && netId > 0;
        }
    }
}
