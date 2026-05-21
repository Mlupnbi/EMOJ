using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.UI.Components
{
    internal sealed class BuffFilterIconButton : UIElement
    {
        private const float UiScale = 0.8f;
        private const float BaseSize = 28f;

        private readonly OPJourneyUI _shell;
        private readonly Action _onClick;
        private readonly Item _placeholder = new Item();

        public static float OuterSize => BaseSize * UiScale;

        public BuffFilterIconButton(OPJourneyUI shell, Action onClick)
        {
            _shell = shell;
            _onClick = onClick;
            _placeholder.TurnToAir();
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
            Vector2 pos = d.Position();
            Texture2D invBack = TextureAssets.InventoryBack.Value;
            float slotScale = ItemHubFilterTagMetrics.SlotScale * UiScale;
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);

            Item[] dummy = new Item[11];
            dummy[10] = _placeholder;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;

            bool open = _shell?.BuffSecondaryPanel?.IsOpen ?? false;
            if (open)
            {
                var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(255, 220, 120), 2);
            }

            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            Texture2D iconTex = ItemHubUiTextureHelper.TryLoad(mod, global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubFilterButton);
            if (iconTex != null)
            {
                float fit = Math.Min(slotPixW, slotPixH) * 0.82f;
                float sc = fit / Math.Max(iconTex.Width, iconTex.Height);
                Vector2 origin = new Vector2(iconTex.Width, iconTex.Height) * 0.5f;
                Vector2 center = slotPos + new Vector2(slotPixW * 0.5f, slotPixH * 0.5f);
                spriteBatch.Draw(iconTex, center, null, Color.White, 0f, origin, sc, SpriteEffects.None, 0f);
            }

            if (IsMouseHovering && Main.LocalPlayer != null)
                Main.instance.MouseText(EOPJText.UI("BuffFilterTitle"));
        }
    }
}
