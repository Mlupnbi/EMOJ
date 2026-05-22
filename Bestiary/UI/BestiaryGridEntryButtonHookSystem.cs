using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Bestiary.UI.Components;
using EvenMoreOverpoweredJourney.Core.Localization;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>
    /// 超级图鉴网格：10% 生态底、悬停动画降速、模组生物悬停加「来源:」。
    /// </summary>
    public sealed class BestiaryGridEntryButtonHookSystem : ModSystem
    {
        public override void Load()
        {
            On_UIBestiaryEntryButton.DrawSelf += OnButtonDrawSelf;
            On_UIBestiaryEntryIcon.Update += OnIconUpdate;
            On_UIBestiaryEntryIcon.GetHoverText += OnIconGetHoverText;
        }

        public override void Unload()
        {
            On_UIBestiaryEntryButton.DrawSelf -= OnButtonDrawSelf;
            On_UIBestiaryEntryIcon.Update -= OnIconUpdate;
            On_UIBestiaryEntryIcon.GetHoverText -= OnIconGetHoverText;
        }

        private static void OnButtonDrawSelf(On_UIBestiaryEntryButton.orig_DrawSelf orig, UIBestiaryEntryButton self, SpriteBatch spriteBatch)
        {
            if (!IsUnderEojGridCard(self))
            {
                orig(self, spriteBatch);
                return;
            }

            Rectangle outer = self.GetDimensions().ToRectangle();
            if (outer.Width > 0 && outer.Height > 0)
            {
                Rectangle inset = BestiaryCardLayout.InsetBackgroundBounds(outer);
                if (self.Entry != null && inset.Width > 0 && inset.Height > 0)
                {
                    BestiaryVanillaSlotRenderer.DrawBackground(
                        spriteBatch,
                        self.Entry,
                        inset,
                        BestiaryCardVisuals.BackgroundImageAlpha);
                }
            }

            orig(self, spriteBatch);

            foreach (UIElement child in self.Children)
                child.Draw(spriteBatch);
        }

        private static void OnIconUpdate(On_UIBestiaryEntryIcon.orig_Update orig, UIBestiaryEntryIcon self, GameTime gameTime)
        {
            if (!IsUnderEojGridCard(self))
            {
                orig(self, gameTime);
                return;
            }

            // 原版 UIBestiaryEntryIcon.Update 每帧都调用 Icon.Update，Draw 才能显示静态帧；
            // 仅在悬停时降频以放慢动画，非悬停必须每帧 Update。
            bool hovered = self.IsMouseHovering || self.ForceHover;
            if (hovered)
            {
                int interval = BestiaryCardVisuals.GridIconAnimationInterval;
                if (interval > 1 && Main.GameUpdateCount % interval != 0)
                    return;
            }

            orig(self, gameTime);
        }

        private static string OnIconGetHoverText(On_UIBestiaryEntryIcon.orig_GetHoverText orig, UIBestiaryEntryIcon self)
        {
            string text = orig(self);
            if (!IsUnderEojGridCard(self))
                return text;

            BestiaryEntry entry = FindHostButton(self)?.Entry;
            if (entry == null)
                return text;

            if (!BestiaryListCatalog.TryFindMetaByEntry(entry, out BestiaryNpcMeta meta) ||
                string.IsNullOrEmpty(meta.ModKey) ||
                meta.ModKey.Equals("Terraria", System.StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }

            return EOPJText.UIFormat("BestiaryHoverSourceFmt", text);
        }

        private static UIBestiaryEntryButton FindHostButton(UIBestiaryEntryIcon icon)
        {
            for (UIElement node = icon?.Parent; node != null; node = node.Parent)
            {
                if (node is UIBestiaryEntryButton button)
                    return button;

                if (node is UIBestiaryNpcCard)
                    break;
            }

            return null;
        }

        private static bool IsUnderEojGridCard(UIElement element)
        {
            for (UIElement node = element?.Parent; node != null; node = node.Parent)
            {
                if (node is UIBestiaryNpcCard)
                    return true;
            }

            return false;
        }
    }
}
