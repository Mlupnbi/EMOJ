using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.UI.Components
{
    public class UIBuffSearchBar : UIElement
    {
        private const int CornerRadius = 8;
        private const float LeftIconSlotW = 28f;
        private const float LeftIconPad = 8f;

        private readonly Color _background = OPJourneyUiColors.SearchBarBackground;
        private readonly Color _border = OPJourneyUiColors.SearchBarBorder;

        public UISearchBar InnerSearchBar;
        private string _actualText = "";

        public bool Focused => InnerSearchBar?.IsWritingText ?? false;

        public string CurrentSearch => _actualText ?? "";

        public Action<string> OnTextChanged;

        private string _hintText = "";

        public string SearchHint
        {
            get => _hintText;
            set => _hintText = value ?? "";
        }

        public UIBuffSearchBar()
        {
            InnerSearchBar = new UISearchBar(LocalizedText.Empty, 0.8f);
            InnerSearchBar.Left.Set(LeftIconPad + LeftIconSlotW, 0f);
            InnerSearchBar.Width.Set(-(LeftIconPad + LeftIconSlotW + 8f), 1f);
            InnerSearchBar.Height.Set(0f, 1f);
            InnerSearchBar.VAlign = 0.5f;

            InnerSearchBar.OnLeftClick += (_, _) =>
            {
                if (!InnerSearchBar.IsWritingText)
                    InnerSearchBar.ToggleTakingText();
            };
            InnerSearchBar.OnContentsChanged += text =>
            {
                _actualText = text ?? "";
                OnTextChanged?.Invoke(_actualText);
            };

            Append(InnerSearchBar);
            StripInnerSearchChrome();
        }

        private void StripInnerSearchChrome()
        {
            foreach (UIElement child in InnerSearchBar.Children)
            {
                if (child is UIPanel panel)
                {
                    panel.BackgroundColor = Color.Transparent;
                    panel.BorderColor = Color.Transparent;
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle rect = GetDimensions().ToRectangle();
            RoundedRectDrawUtil.DrawBorder(spriteBatch, rect, _border, _background, CornerRadius, 1);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (InnerSearchBar.IsWritingText && Main.mouseLeft && !IsMouseHovering)
                InnerSearchBar.ToggleTakingText();
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            base.DrawChildren(spriteBatch);
            CalculatedStyle dims = GetDimensions();
            DrawLeftCursorIcon(spriteBatch, dims);

            if (string.IsNullOrEmpty(_actualText) && !InnerSearchBar.IsWritingText)
                DrawHintText(spriteBatch, dims);
        }

        private static void DrawLeftCursorIcon(SpriteBatch spriteBatch, CalculatedStyle dims)
        {
            Texture2D icon = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.Cursor2;
            if (icon == null || icon.Width < 1 || icon.Height < 1)
                return;

            float targetH = Math.Min(LeftIconSlotW - 4f, Math.Max(14f, dims.Height - 8f));
            float scale = targetH / icon.Height;
            float drawW = icon.Width * scale;
            float drawH = icon.Height * scale;
            float x = dims.X + LeftIconPad + (LeftIconSlotW - drawW) * 0.5f;
            float y = dims.Y + (dims.Height - drawH) * 0.5f;
            spriteBatch.Draw(icon, new Vector2(x, y), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawHintText(SpriteBatch spriteBatch, CalculatedStyle dims)
        {
            float textLeft = dims.X + LeftIconPad + LeftIconSlotW + 4f;
            float maxTextW = Math.Max(40f, dims.Width - (textLeft - dims.X) - 8f);
            string shown = _hintText ?? "";
            var font = FontAssets.MouseText.Value;
            const float hintScale = 0.8f;
            while (shown.Length > 3 && font.MeasureString(shown).X * hintScale > maxTextW)
                shown = shown.Substring(0, shown.Length - 1);

            if (shown.Length < (_hintText?.Length ?? 0))
                shown += "\u2026";

            Vector2 textPos = new Vector2(
                textLeft,
                dims.Y + dims.Height / 2f - font.MeasureString(shown).Y * hintScale / 2f + 4f);
            Utils.DrawBorderString(spriteBatch, shown, textPos, OPJourneyUiColors.TextHint, hintScale);
        }
    }
}
