using System;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>UIPanel 文本按钮，修正 Terraria 字体基线偏上问题。</summary>
    internal static class BlueprintUiFlatButton
    {
        public const float DefaultTextNudgeY = 2f;

        public static UIPanel Create(
            string label,
            float width,
            float height,
            Action onClick,
            float textScale = 0.62f,
            Color? fill = null,
            Color? border = null)
        {
            return CreateWithLabel(label, width, height, onClick, textScale, fill, border).Panel;
        }

        public static (UIPanel Panel, UIText Label) CreateWithLabel(
            string label,
            float width,
            float height,
            Action onClick,
            float textScale = 0.62f,
            Color? fill = null,
            Color? border = null)
        {
            var panel = new UIPanel();
            panel.SetPadding(0);
            panel.Width.Set(width, 0f);
            panel.Height.Set(height, 0f);
            panel.BackgroundColor = fill ?? new Color(50, 70, 95) * 0.95f;
            panel.BorderColor = border ?? new Color(100, 130, 170);

            var txt = new UIText(label, textScale)
            {
                HAlign = 0.5f,
                VAlign = 0.5f
            };
            txt.Top.Set(DefaultTextNudgeY, 0f);
            panel.Append(txt);

            panel.OnLeftClick += (_, _) =>
            {
                onClick?.Invoke();
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            return (panel, txt);
        }
    }
}
