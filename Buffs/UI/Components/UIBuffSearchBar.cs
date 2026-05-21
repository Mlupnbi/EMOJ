using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Buffs.UI.Components
{
    public class UIBuffSearchBar : UIPanel
    {
        public UISearchBar InnerSearchBar;
        private string _actualText = "";
        public bool Focused => InnerSearchBar?.IsWritingText ?? false;

        /// <summary>��ǰ�������ı����� OnTextChanged ͬ������</summary>
        public string CurrentSearch => _actualText ?? "";

        // ���ؼ��������������ֱ�ӵ�����UI���������ⷢ��һ���źţ�˭��˭��
        public Action<string> OnTextChanged;

        private string _hintText = "����Buffȫ��/Ӣ��/ƴ������ĸ...";

        public string SearchHint
        {
            get => _hintText;
            set => _hintText = value ?? "";
        }

        public UIBuffSearchBar()
        {
            SetPadding(0);
            BackgroundColor = new Color(45, 45, 75);
            BorderColor = new Color(120, 120, 180);

            InnerSearchBar = new UISearchBar(LocalizedText.Empty, 0.8f);
            InnerSearchBar.Width.Set(-40, 1f);
            InnerSearchBar.Left.Set(35, 0);
            InnerSearchBar.Height.Set(0, 1f);
            InnerSearchBar.VAlign = 0.5f;

            InnerSearchBar.OnLeftClick += (evt, el) => {
                if (!InnerSearchBar.IsWritingText) InnerSearchBar.ToggleTakingText();
            };
            InnerSearchBar.OnRightClick += (evt, el) => {
                if (InnerSearchBar.HasContents) InnerSearchBar.SetContents("");
            };
            InnerSearchBar.OnContentsChanged += (text) => {
                _actualText = text ?? "";
                OnTextChanged?.Invoke(_actualText);
            };

            Append(InnerSearchBar);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (InnerSearchBar.IsWritingText)
            {
                if (Main.mouseLeft && !IsMouseHovering)
                {
                    InnerSearchBar.ToggleTakingText();
                }
            }
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            base.DrawChildren(spriteBatch);
            CalculatedStyle dims = GetDimensions();

            Texture2D searchIcon = Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Button_Search").Value;
            spriteBatch.Draw(searchIcon, new Vector2(dims.X + 8, dims.Y + dims.Height / 2f - searchIcon.Height / 2f), Color.White);

            if (string.IsNullOrEmpty(_actualText) && !InnerSearchBar.IsWritingText)
            {
                float maxTextW = Math.Max(40f, dims.Width - 46f);
                string shown = _hintText ?? "";
                var font = FontAssets.MouseText.Value;
                const float hintScale = 0.8f;
                while (shown.Length > 3 && font.MeasureString(shown).X * hintScale > maxTextW)
                    shown = shown.Substring(0, shown.Length - 1);
                if (shown.Length < (_hintText?.Length ?? 0))
                    shown += "\u2026";
                Vector2 textPos = new Vector2(dims.X + 35, dims.Y + dims.Height / 2f - font.MeasureString(shown).Y * hintScale / 2f + 4f);
                Utils.DrawBorderString(spriteBatch, shown, textPos, Color.Gray, hintScale);
            }
        }
    }
}