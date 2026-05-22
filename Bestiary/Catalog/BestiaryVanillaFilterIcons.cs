using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    internal static class BestiaryVanillaFilterIcons
    {
        private const string TagsSheetPath = "Images/UI/Bestiary/Icon_Tags_Shadow";
        private const string ModTagsSheetPath = "Assets/UI/Bestiary/Icon_Tags_Shadow";

        public static void DrawFilterIcon(SpriteBatch spriteBatch, Rectangle target, Point frame, float alpha = 1f)
        {
            if (target.Width < 2 || target.Height < 2 || frame.X < 0 || frame.Y < 0)
                return;

            Asset<Texture2D> asset = GetTagsSheetAsset();
            Rectangle src = asset.Frame(16, 5, frame.X, frame.Y);
            DrawSourceRect(spriteBatch, target, asset.Value, src, alpha);
        }

        public static void DrawSourceRect(
            SpriteBatch spriteBatch,
            Rectangle target,
            Texture2D texture,
            Rectangle source,
            float alpha = 1f)
        {
            if (texture == null || source.Width <= 0 || source.Height <= 0 || target.Width < 2 || target.Height < 2)
                return;

            // 统一按短边缩放，在正方形/矩形目标内保持比例居中（模组 30×30 与原版帧均适用）
            float scale = System.Math.Min(target.Width / (float)source.Width, target.Height / (float)source.Height);
            Vector2 drawSize = new Vector2(source.Width, source.Height) * scale;
            Vector2 pos = new Vector2(
                target.X + (target.Width - drawSize.X) * 0.5f,
                target.Y + (target.Height - drawSize.Y) * 0.5f);
            spriteBatch.Draw(texture, pos, source, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private static Asset<Texture2D> GetTagsSheetAsset()
        {
            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            if (mod != null && mod.HasAsset(ModTagsSheetPath))
                return mod.Assets.Request<Texture2D>(ModTagsSheetPath);

            return Main.Assets.Request<Texture2D>(TagsSheetPath);
        }
    }
}
