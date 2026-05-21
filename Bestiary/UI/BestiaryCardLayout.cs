using Microsoft.Xna.Framework;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>与原版 UIBestiaryEntryButton @ 72px 对齐的布局常量。</summary>
    internal static class BestiaryCardLayout
    {
        public const float RefOuterPx = 72f;
        /// <summary>原版 Slot_Front 内缘约 8px（8/72）。</summary>
        public const float RefInsetRatio = 8f / RefOuterPx;

        public static Rectangle InsetBackgroundBounds(Rectangle outer)
        {
            if (outer.Width <= 0 || outer.Height <= 0)
                return outer;

            int inset = (int)(outer.Width * RefInsetRatio);
            if (inset * 2 >= outer.Width || inset * 2 >= outer.Height)
                return outer;

            return new Rectangle(
                outer.X + inset,
                outer.Y + inset,
                outer.Width - inset * 2,
                outer.Height - inset * 2);
        }
    }
}
