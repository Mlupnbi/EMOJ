using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>折叠/展开贴图按钮，逻辑与 Buff 分类折叠键一致。</summary>
    public sealed class BlueprintUiFoldButton : UIElement
    {
        private static Texture2D expandedTexture;

        private readonly Func<bool> isExpanded;

        public BlueprintUiFoldButton(Func<bool> isExpanded)
        {
            this.isExpanded = isExpanded;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            if (IsMouseHovering)
                Main.LocalPlayer.mouseInterface = true;

            Texture2D texture = isExpanded() ? GetExpandedTexture() : GetCollapsedTexture();
            CalculatedStyle dims = GetDimensions();
            Color color = IsMouseHovering ? Color.White : Color.White * 0.75f;
            spriteBatch.Draw(texture, dims.ToRectangle(), color);
        }

        private static Texture2D GetCollapsedTexture()
        {
            global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextureCache.WarmTab(
                global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTab.Buff);
            return global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Buff.ButtonPlay;
        }

        private static Texture2D GetExpandedTexture()
        {
            Texture2D source = GetCollapsedTexture();
            if (expandedTexture != null && !expandedTexture.IsDisposed &&
                expandedTexture.Width == source.Height && expandedTexture.Height == source.Width)
                return expandedTexture;

            var sourceData = new Color[source.Width * source.Height];
            var rotatedData = new Color[source.Width * source.Height];
            source.GetData(sourceData);

            int rotatedWidth = source.Height;
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    int destX = source.Height - 1 - y;
                    int destY = x;
                    rotatedData[destY * rotatedWidth + destX] = sourceData[y * source.Width + x];
                }
            }

            expandedTexture = new Texture2D(Main.graphics.GraphicsDevice, source.Height, source.Width);
            expandedTexture.SetData(rotatedData);
            return expandedTexture;
        }
    }
}
