using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>????? UI ???? <see cref="EojUiTextures.Bestiary"/> ????</summary>
    internal static class BestiaryUiAssets
    {
        public static Texture2D SlotBack => EojUiTextures.Bestiary.SlotBack;

        public static Texture2D SlotFront => EojUiTextures.Bestiary.SlotFront;

        public static Texture2D SlotOverlay => EojUiTextures.Bestiary.SlotOverlay;

        public static Texture2D SlotSelection => EojUiTextures.Bestiary.SlotSelection;

        public static Texture2D IconLocked => EojUiTextures.Bestiary.IconLocked;

        public static Texture2D IconTagsShadow => EojUiTextures.Bestiary.IconTagsShadow;

        public static void EnsureLoaded() => EojUiTextureCache.WarmTab(EojUiTab.Bestiary);
    }
}
