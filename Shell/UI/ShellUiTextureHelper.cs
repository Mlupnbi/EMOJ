using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    internal static class ShellUiTextureHelper
    {
        internal static Texture2D TryLoadHandle(Mod mod)
        {
            EojUiTextureCache.WarmTab(EojUiTab.Shell);
            return EojUiTextures.Shell.ResizeHandle;
        }

        internal static Texture2D TryLoad(Mod mod, params string[] assetPaths) =>
            EojUiTextureCache.TryLoadFirst(assetPaths);
    }
}
