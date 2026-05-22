using System;

namespace EvenMoreOverpoweredJourney.Shell.UI.Assets
{
    /// <summary>一条 UI 贴图：优先模组 <see cref="ModPaths"/>，缺失时回退 <see cref="VanillaFallback"/>。</summary>
    internal readonly struct EojUiAssetEntry
    {
        public EojUiTab Tab { get; }
        public string[] ModPaths { get; }
        public string VanillaFallback { get; }

        public EojUiAssetEntry(EojUiTab tab, string vanillaFallback, params string[] modPaths)
        {
            Tab = tab;
            VanillaFallback = vanillaFallback;
            ModPaths = modPaths ?? Array.Empty<string>();
        }
    }
}
