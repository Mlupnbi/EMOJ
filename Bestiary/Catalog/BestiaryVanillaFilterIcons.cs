using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    internal static class BestiaryVanillaFilterIcons
    {
        public static void DrawFilterIcon(SpriteBatch spriteBatch, Rectangle target, Point frame, float alpha = 1f)
        {
            Texture2D sheet = Bestiary.UI.BestiaryUiAssets.IconTagsShadow;
            if (sheet == null || frame == Point.Zero || target.Width < 2 || target.Height < 2)
                return;

            Asset<Texture2D> asset = Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Icon_Tags_Shadow");
            Rectangle src = asset.Frame(16, 5, frame.X, frame.Y);
            float scale = System.Math.Min(target.Width / (float)src.Width, target.Height / (float)src.Height);
            Vector2 size = new Vector2(src.Width, src.Height) * scale;
            Vector2 pos = new Vector2(target.Center.X, target.Center.Y);
            spriteBatch.Draw(asset.Value, pos, src, Color.White * alpha, 0f, size * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
