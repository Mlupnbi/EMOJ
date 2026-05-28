using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>家具蓝图工具条圆角按钮（自绘，避免 UIPanel 九宫格拉伸）。</summary>
    public sealed class BlueprintRoundedToolbarButton : UIElement
    {
        private const int CornerRadius = 8;
        private const float TextScale = 0.7f;

        private readonly Action _onClick;
        private string _text;

        public BlueprintRoundedToolbarButton(float width, float height, string text, Action onClick)
        {
            _text = text;
            _onClick = onClick;
            Width.Set(width, 0f);
            Height.Set(height, 0f);
            OnLeftClick += (_, _) =>
            {
                _onClick?.Invoke();
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (IsMouseHovering)
                Main.LocalPlayer.mouseInterface = true;

            CalculatedStyle dims = GetDimensions();
            Rectangle rect = dims.ToRectangle();
            Color border = IsMouseHovering ? OPJourneyUiColors.ButtonBorderOpen : OPJourneyUiColors.ButtonBorder;
            Color fill = IsMouseHovering ? OPJourneyUiColors.ButtonBackgroundHover : OPJourneyUiColors.ButtonBackground;
            RoundedRectDrawUtil.DrawBorder(spriteBatch, rect, border, fill, CornerRadius, 1);

            Vector2 size = FontAssets.MouseText.Value.MeasureString(_text) * TextScale;
            Vector2 pos = new Vector2(
                dims.X + (dims.Width - size.X) * 0.5f,
                dims.Y + (dims.Height - size.Y) * 0.5f + BlueprintUiFlatButton.DefaultTextNudgeY);
            Utils.DrawBorderString(spriteBatch, _text, pos, Color.White, TextScale);
        }

        public void SetLabel(string text) => _text = text ?? "";
    }
}
