using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Bestiary;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Core.Localization;

namespace EvenMoreOverpoweredJourney.Bestiary.UI.Components
{
    public sealed class UIBestiaryDetailOverlay : UIPanel
    {
        private readonly BestiaryPage _owner;
        private UIText _nameText;
        private UIText _modText;
        private UIText _bodyText;
        private UIPanel _backBtn;
        private bool _visible;

        public UIBestiaryDetailOverlay(BestiaryPage owner)
        {
            _owner = owner;
            Width.Set(0, 1f);
            Height.Set(0, 1f);
            BackgroundColor = new Color(12, 12, 24) * 0.94f;
            BorderColor = new Color(80, 80, 120);

            _backBtn = new UIPanel();
            _backBtn.Width.Set(72, 0);
            _backBtn.Height.Set(28, 0);
            _backBtn.Left.Set(8, 0);
            _backBtn.Top.Set(8, 0);
            _backBtn.BackgroundColor = new Color(50, 50, 75);
            _backBtn.OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                _owner.CloseDetail();
            };
            Append(_backBtn);
            var backT = new UIText(EOPJText.UI("BestiaryDetailBack"), 0.85f);
            backT.HAlign = 0.5f;
            backT.VAlign = 0.5f;
            backT.IgnoresMouseInteraction = true;
            _backBtn.Append(backT);

            _nameText = new UIText("", 1.1f);
            _nameText.Left.Set(12, 0);
            _nameText.Top.Set(44, 0);
            _nameText.IgnoresMouseInteraction = true;
            Append(_nameText);

            _modText = new UIText("", 0.8f);
            _modText.Left.Set(12, 0);
            _modText.Top.Set(68, 0);
            _modText.TextColor = Color.LightGray;
            _modText.IgnoresMouseInteraction = true;
            Append(_modText);

            _bodyText = new UIText("", 0.75f);
            _bodyText.Left.Set(12, 0);
            _bodyText.Top.Set(96, 0);
            _bodyText.Width.Set(-24, 1f);
            _bodyText.Height.Set(-110, 1f);
            _bodyText.IsWrapped = true;
            _bodyText.IgnoresMouseInteraction = true;
            Append(_bodyText);

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            IgnoresMouseInteraction = !visible;

            float btnW = visible ? 72f : 0f;
            float btnH = visible ? 28f : 0f;
            _backBtn.Width.Set(btnW, 0);
            _backBtn.Height.Set(btnH, 0);
            _backBtn.IgnoresMouseInteraction = !visible;

            if (!visible)
            {
                _nameText.SetText("");
                _modText.SetText("");
                _bodyText.SetText("");
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!_visible)
                return;

            base.Update(gameTime);
        }

        protected override void DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            if (!_visible)
                return;

            base.DrawSelf(spriteBatch);
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            if (!_visible)
                return;

            base.Draw(spriteBatch);
        }

        public void Show(BestiaryNpcMeta meta, BestiaryFaceMode face)
        {
            _nameText.SetText(meta?.DisplayName ?? "?");
            _modText.SetText(EOPJText.UIFormat("BestiaryDetailModFmt", meta?.ModKey ?? "?"));

            var sb = new StringBuilder();
            if (meta?.Entry == null)
            {
                sb.AppendLine(EOPJText.UI("BestiaryDetailUnregistered"));
            }
            else if (face == BestiaryFaceMode.ProgressiveMinus && !BestiaryProgressResolver.WasEverFound(meta.Entry))
            {
                sb.AppendLine(EOPJText.UI("BestiaryDetailEmpty"));
            }
            else
            {
                sb.AppendLine(EOPJText.UI("BestiaryDetailPlaceholder"));
                sb.AppendLine();
                sb.AppendLine(EOPJText.UIFormat("BestiaryDetailNetIdFmt", meta.NetId));
                sb.AppendLine(EOPJText.UIFormat("BestiaryDetailUnlockFmt",
                    BestiaryProgressResolver.GetUnlockState(meta.Entry)));
            }

            _bodyText.SetText(sb.ToString());
        }
    }
}
