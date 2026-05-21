using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>原版 Utils.DrawRectangle 的第二个 Vector2 为右下角坐标而非宽高，此处提供按像素描边。</summary>
    internal static class BorderDrawUtil
    {
        public static void DrawRectOutline(SpriteBatch sb, Rectangle r, Color color, int thickness = 2)
        {
            Texture2D px = TextureAssets.MagicPixel.Value;
            int t = System.Math.Max(1, thickness);
            sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, t), color);
            sb.Draw(px, new Rectangle(r.X, r.Bottom - t, r.Width, t), color);
            sb.Draw(px, new Rectangle(r.X, r.Y + t, t, r.Height - 2 * t), color);
            sb.Draw(px, new Rectangle(r.Right - t, r.Y + t, t, r.Height - 2 * t), color);
        }

        /// <summary>仅在物品槽外缘着色条带，不盖住中央图标。</summary>
        public static void DrawInventorySlotRimTint(SpriteBatch sb, Vector2 slotTopLeft, int slotW, int slotH, Color c, int band = 4)
        {
            Texture2D px = TextureAssets.MagicPixel.Value;
            int x = (int)slotTopLeft.X;
            int y = (int)slotTopLeft.Y;
            int w = slotW;
            int h = slotH;
            int b = System.Math.Clamp(band, 2, System.Math.Min(w, h) / 2 - 1);
            sb.Draw(px, new Rectangle(x, y, w, b), c);
            sb.Draw(px, new Rectangle(x, y + h - b, w, b), c);
            sb.Draw(px, new Rectangle(x, y + b, b, h - 2 * b), c);
            sb.Draw(px, new Rectangle(x + w - b, y + b, b, h - 2 * b), c);
        }
    }
}
