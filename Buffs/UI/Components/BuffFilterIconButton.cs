using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.UI.Components
{
    internal sealed class BuffFilterIconButton : UIElement
    {
        private const float UiScale = 0.8f;
        private const float BaseSize = 28f;

        private readonly OPJourneyUI _shell;
        private readonly Action _onClick;

        public static float OuterSize => BaseSize * UiScale;

        public BuffFilterIconButton(OPJourneyUI shell, Action onClick)
        {
            _shell = shell;
            _onClick = onClick;
            Width.Set(OuterSize, 0);
            Height.Set(OuterSize, 0);
            OnLeftClick += (_, __) => _onClick?.Invoke();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (IsMouseHovering && Main.LocalPlayer != null)
                Main.LocalPlayer.mouseInterface = true;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle rect = d.ToRectangle();
            bool open = _shell?.BuffSecondaryPanel?.IsOpen ?? false;

            Color fill = open
                ? OPJourneyUiColors.ButtonBackgroundOpen
                : (IsMouseHovering ? OPJourneyUiColors.ButtonBackgroundHover : OPJourneyUiColors.SearchBarBackground);
            Color border = open ? OPJourneyUiColors.ButtonBorderOpen : OPJourneyUiColors.SearchBarBorder;

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, fill);
            BorderDrawUtil.DrawRectOutline(spriteBatch, rect, border, 1);

            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            Texture2D iconTex = ItemHubUiTextureHelper.TryLoad(
                mod,
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubFilterButton);
            if (iconTex != null)
            {
                float fit = Math.Min(rect.Width, rect.Height) * 0.62f;
                float sc = fit / Math.Max(iconTex.Width, iconTex.Height);
                Vector2 origin = new Vector2(iconTex.Width, iconTex.Height) * 0.5f;
                Vector2 center = new Vector2(rect.Center.X, rect.Center.Y);
                spriteBatch.Draw(iconTex, center, null, Color.White, 0f, origin, sc, SpriteEffects.None, 0f);
            }

            if (IsMouseHovering && Main.LocalPlayer != null)
                Main.instance.MouseText(EOPJText.UI("BuffFilterTitle"));
        }
    }
}
