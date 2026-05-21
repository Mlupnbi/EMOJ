using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    internal static class ShellUiTextureHelper
    {
        internal static Texture2D TryLoadHandle(Mod mod) =>
            TryLoad(mod, global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ResizeHandleCursor, "Assets/UI/Handle");

        internal static Texture2D TryLoad(Mod mod, params string[] assetPaths)
        {
            if (mod == null || assetPaths == null)
                return null;

            foreach (string path in assetPaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                Texture2D tex = TryLoadAsset(mod, path);
                if (tex != null)
                    return tex;
            }

            return null;
        }

        private static Texture2D TryLoadAsset(Mod mod, string path)
        {
            try
            {
                if (mod.HasAsset(path))
                    return mod.Assets.Request<Texture2D>(path).Value;
            }
            catch
            {
                // fall through
            }

            try
            {
                return mod.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
