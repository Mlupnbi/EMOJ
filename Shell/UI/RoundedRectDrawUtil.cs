using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    internal static class RoundedRectDrawUtil
    {
        public static void DrawFilled(SpriteBatch sb, Rectangle bounds, Color color, int radius)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            Texture2D px = TextureAssets.MagicPixel.Value;
            int r = Math.Clamp(radius, 0, Math.Min(bounds.Width, bounds.Height) / 2);
            if (r <= 0)
            {
                sb.Draw(px, bounds, color);
                return;
            }

            sb.Draw(px, new Rectangle(bounds.X + r, bounds.Y, bounds.Width - 2 * r, bounds.Height), color);
            sb.Draw(px, new Rectangle(bounds.X, bounds.Y + r, bounds.Width, bounds.Height - 2 * r), color);

            int cx = bounds.X + r;
            int cy = bounds.Y + r;
            FillQuarter(sb, px, cx, cy, r, color, left: true, top: true);
            cx = bounds.Right - r - 1;
            FillQuarter(sb, px, cx, cy, r, color, left: false, top: true);
            cy = bounds.Bottom - r - 1;
            cx = bounds.X + r;
            FillQuarter(sb, px, cx, cy, r, color, left: true, top: false);
            cx = bounds.Right - r - 1;
            FillQuarter(sb, px, cx, cy, r, color, left: false, top: false);
        }

        public static void DrawBorder(SpriteBatch sb, Rectangle bounds, Color border, Color fill, int radius, int thickness = 1)
        {
            DrawFilled(sb, bounds, border, radius);
            var inner = new Rectangle(
                bounds.X + thickness,
                bounds.Y + thickness,
                bounds.Width - 2 * thickness,
                bounds.Height - 2 * thickness);
            DrawFilled(sb, inner, fill, Math.Max(0, radius - thickness));
        }

        private static void FillQuarter(
            SpriteBatch sb,
            Texture2D px,
            int cornerPivotX,
            int cornerPivotY,
            int radius,
            Color color,
            bool left,
            bool top)
        {
            int r2 = radius * radius;
            for (int iy = 0; iy < radius; iy++)
            {
                for (int ix = 0; ix < radius; ix++)
                {
                    if (ix * ix + iy * iy > r2)
                        continue;

                    int x = left ? cornerPivotX - ix : cornerPivotX + ix;
                    int y = top ? cornerPivotY - iy : cornerPivotY + iy;
                    sb.Draw(px, new Rectangle(x, y, 1, 1), color);
                }
            }
        }
    }
}
