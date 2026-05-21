namespace EvenMoreOverpoweredJourney.ItemHub.Data
{
    /// <summary>模组筛选格无图标时的缩写；原版用本地化短名，其它模组用内部名前两字符。</summary>
    internal static class HubModAbbrev
    {
        internal static string ForGrid(string modKey)
        {
            if (modKey == "Terraria")
                return EOPJText.UI("ItemHubModVanillaShort");

            if (modKey == "EvenMoreOverpoweredJourney")
                return "EMOJ";

            if (modKey.Length >= 2)
                return modKey.Substring(0, 2).ToUpperInvariant();
            return modKey.Length > 0 ? modKey.Substring(0, 1).ToUpperInvariant() : "?";
        }
    }
}
