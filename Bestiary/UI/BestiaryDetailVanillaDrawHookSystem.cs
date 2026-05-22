using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>
    /// 图鉴详情：Hook 原版 UIPanel / UIImage / UIText 的 DrawSelf，仅在绘制瞬间改色后调用 orig。
    /// </summary>
    public sealed class BestiaryDetailVanillaDrawHookSystem : ModSystem
    {
        public override void Load()
        {
            On_UIPanel.DrawSelf += OnPanelDrawSelf;
            On_UIImage.DrawSelf += OnImageDrawSelf;
            On_UIText.DrawSelf += OnTextDrawSelf;
        }

        public override void Unload()
        {
            On_UIPanel.DrawSelf -= OnPanelDrawSelf;
            On_UIImage.DrawSelf -= OnImageDrawSelf;
            On_UIText.DrawSelf -= OnTextDrawSelf;
            VanillaUiImageTextureUtil.ClearSwapCache();
        }

        private static void OnPanelDrawSelf(On_UIPanel.orig_DrawSelf orig, UIPanel self, SpriteBatch spriteBatch)
        {
            if (!BestiaryDetailVanillaDrawScope.IsActive(self))
            {
                orig(self, spriteBatch);
                return;
            }

            Color bg = self.BackgroundColor;
            Color border = self.BorderColor;
            self.BackgroundColor = VanillaUiColorRemap.RemapPanelBackground(bg);
            self.BorderColor = VanillaUiColorRemap.RemapPanelBorder(border);
            orig(self, spriteBatch);
            self.BackgroundColor = bg;
            self.BorderColor = border;
        }

        private static void OnImageDrawSelf(On_UIImage.orig_DrawSelf orig, UIImage self, SpriteBatch spriteBatch)
        {
            if (!BestiaryDetailVanillaDrawScope.IsActive(self))
            {
                orig(self, spriteBatch);
                return;
            }

            VanillaUiImageTextureUtil.TrySwapToModAsset(self);

            Color tint = self.Color;
            self.Color = VanillaUiColorRemap.RemapImageTint(tint, self);
            orig(self, spriteBatch);
            self.Color = tint;
        }

        private static void OnTextDrawSelf(On_UIText.orig_DrawSelf orig, UIText self, SpriteBatch spriteBatch)
        {
            if (!BestiaryDetailVanillaDrawScope.IsActive(self))
            {
                orig(self, spriteBatch);
                return;
            }

            Color text = self.TextColor;
            self.TextColor = VanillaUiColorRemap.RemapText(text);
            orig(self, spriteBatch);
            self.TextColor = text;
        }
    }
}
