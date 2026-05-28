using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Shell.UI.Components
{
    /// <summary>与轨道同宽的竖向滚动条，避免原版 UIScrollbar 拉宽后滑块与轨道错位。</summary>
    public sealed class EojUIScrollbar : UIScrollbar
    {
        public const float DefaultWidth = 12f;

        private static FieldInfo _viewSizeField;
        private static FieldInfo _maxViewSizeField;

        public EojUIScrollbar(float width = DefaultWidth)
        {
            Width.Set(width, 0f);
            SetPadding(0);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (IsMouseHovering)
                Main.LocalPlayer.mouseInterface = true;

            CalculatedStyle dims = GetDimensions();
            Rectangle track = dims.ToRectangle();
            if (track.Width <= 0 || track.Height <= 4)
                return;

            RoundedRectDrawUtil.DrawBorder(
                spriteBatch,
                track,
                OPJourneyUiColors.ScrollBorder,
                OPJourneyUiColors.ScrollTrack,
                4,
                1);

            if (!TryGetViewSizes(out float viewSize, out float maxViewSize) || maxViewSize <= viewSize + 1f)
                return;

            float ratio = Math.Clamp(viewSize / maxViewSize, 0.05f, 1f);
            int thumbH = Math.Max(10, (int)((track.Height - 2) * ratio));
            float scrollRange = maxViewSize - viewSize;
            float frac = scrollRange > 0f ? Math.Clamp(ViewPosition / scrollRange, 0f, 1f) : 0f;
            int thumbY = track.Y + 1 + (int)((track.Height - 2 - thumbH) * frac);

            var thumb = new Rectangle(track.X + 1, thumbY, track.Width - 2, thumbH);
            Color thumbColor = IsMouseHovering ? OPJourneyUiColors.ScrollThumbHover : OPJourneyUiColors.ScrollThumb;
            RoundedRectDrawUtil.DrawFilled(spriteBatch, thumb, thumbColor, 3);
        }

        private bool TryGetViewSizes(out float viewSize, out float maxViewSize)
        {
            viewSize = 0f;
            maxViewSize = 0f;
            EnsureReflection();
            if (_viewSizeField == null || _maxViewSizeField == null)
                return false;

            viewSize = (float)_viewSizeField.GetValue(this);
            maxViewSize = (float)_maxViewSizeField.GetValue(this);
            return maxViewSize > 0f;
        }

        private static void EnsureReflection()
        {
            if (_viewSizeField != null)
                return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type t = typeof(UIScrollbar);
            _viewSizeField = t.GetField("_viewSize", flags) ?? t.GetField("viewSize", flags);
            _maxViewSizeField = t.GetField("_maxViewSize", flags) ?? t.GetField("maxViewSize", flags);
        }
    }
}
