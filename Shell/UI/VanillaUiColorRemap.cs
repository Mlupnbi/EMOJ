using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>在原版 DrawSelf 调用前，把 UICommon 语义色映射为 EOJ 令牌色。</summary>
    internal static class VanillaUiColorRemap
    {
        private static FieldInfo _uiImageTextureField;

        public static Color RemapPanelBackground(Color c)
        {
            if (Matches(c, TerrariaUiCommonMirror.MainPanelBackground) ||
                Matches(c, TerrariaUiCommonMirror.DefaultUIBlue) ||
                Matches(c, TerrariaUiCommonMirror.DefaultUIBlueMouseOver))
                return Color.Transparent;

            return c;
        }

        public static Color RemapPanelBorder(Color c)
        {
            if (Matches(c, TerrariaUiCommonMirror.DefaultUIBorder) ||
                IsVanillaBlueBorder(c))
                return OPJourneyUiColors.DetailDividerLine;

            return c;
        }

        public static Color RemapImageTint(Color c, UIImage image)
        {
            if (IsDividerImage(image))
                return OPJourneyUiColors.DetailDividerLine;

            if (Matches(c, TerrariaUiCommonMirror.DefaultUIBlue) ||
                Matches(c, TerrariaUiCommonMirror.DefaultUIBlueMouseOver) ||
                Matches(c, TerrariaUiCommonMirror.MainPanelBackground))
                return Color.Transparent;

            return c;
        }

        public static Color RemapText(Color c)
        {
            if (c.A < 32)
                return OPJourneyUiColors.DetailBodyText;

            if (c == Color.White || c == Color.LightGray || c == Color.Gray)
                return OPJourneyUiColors.DetailBodyText;

            return c;
        }

        public static bool IsDividerImage(UIImage image)
        {
            Asset<Texture2D> tex = GetUiImageTexture(image);
            if (tex == null || string.IsNullOrEmpty(tex.Name))
                return false;

            return tex.Name.Contains("Divider", StringComparison.OrdinalIgnoreCase);
        }

        private static Asset<Texture2D> GetUiImageTexture(UIImage image)
        {
            if (image == null)
                return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            _uiImageTextureField ??=
                typeof(UIImage).GetField("_imageTexture", flags) ??
                typeof(UIImage).GetField("_texture", flags) ??
                typeof(UIImage).GetField("imageTexture", flags);

            if (_uiImageTextureField == null)
                return null;

            try
            {
                return _uiImageTextureField.GetValue(image) as Asset<Texture2D>;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsVanillaBlueBorder(Color c)
        {
            if (c.A < 8)
                return false;

            return c.B > c.R + 10;
        }

        private static bool Matches(Color a, Color b)
        {
            return Math.Abs(a.R - b.R) <= 2 &&
                   Math.Abs(a.G - b.G) <= 2 &&
                   Math.Abs(a.B - b.B) <= 2 &&
                   Math.Abs(a.A - b.A) <= 2;
        }
    }
}
