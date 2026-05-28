using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public static class FurnitureBlueprintPreview
    {
        private static readonly Color BorderTint = new(255, 230, 80, 200);

        public static void DrawWorld(SpriteBatch spriteBatch, Player player, BlueprintLayout layout, FurnitureScheme scheme)
        {
            if (player == null || layout == null || scheme == null || Main.gameMenu)
                return;

            Point origin = Main.MouseWorld.ToTileCoordinates() - new Point(layout.Width / 2, layout.Height / 2);

            Rectangle footprint = new Rectangle(
                origin.X * 16 - 2,
                origin.Y * 16 - 2,
                layout.Width * 16 + 4,
                layout.Height * 16 + 4);
            Vector2 footScreen = footprint.TopLeft() - Main.screenPosition;
            DrawBorder(spriteBatch, Terraria.GameContent.TextureAssets.MagicPixel.Value, footScreen, footprint.Width, footprint.Height);

            FurnitureBlueprintPlayer fb = player.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb != null && fb.RecognitionBusy)
                return;

            if (!BlueprintLayoutPreviewCache.HasContent)
                return;

            Rectangle drawRect = new Rectangle(
                (int)footScreen.X,
                (int)footScreen.Y,
                layout.Width * 16,
                layout.Height * 16);

            BlueprintLayoutPreviewCache.Draw(spriteBatch, drawRect, Color.White * 0.88f);
        }

        private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Vector2 pos, int w, int h)
        {
            int thickness = 2;
            spriteBatch.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, w, thickness), BorderTint);
            spriteBatch.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y + h - thickness, w, thickness), BorderTint);
            spriteBatch.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, thickness, h), BorderTint);
            spriteBatch.Draw(pixel, new Rectangle((int)pos.X + w - thickness, (int)pos.Y, thickness, h), BorderTint);
        }
    }
}
