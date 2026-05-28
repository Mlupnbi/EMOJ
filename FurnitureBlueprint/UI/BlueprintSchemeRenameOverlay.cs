using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>套组重命名浮层（主窗内）。</summary>
    public sealed class BlueprintSchemeRenameOverlay : UIElement
    {
        private const string FallbackRenameTitle = "\u91cd\u547d\u540d\u5957\u7ec4";
        private const string FallbackCancel = "\u53d6\u6d88";
        private const string FallbackConfirm = "\u786e\u5b9a";

        private readonly UISearchBar _nameBar;
        private string _draftName = "";
        private readonly string _schemeId;
        private readonly Action<string, string> _onConfirm;
        private readonly Action _onClose;

        public BlueprintSchemeRenameOverlay(
            string schemeId,
            string initialName,
            Action<string, string> onConfirm,
            Action onClose)
        {
            _schemeId = schemeId;
            _onConfirm = onConfirm;
            _onClose = onClose;

            Width.Set(0, 1f);
            Height.Set(0, 1f);

            var dim = new UIPanel
            {
                Width = { Percent = 1f },
                Height = { Percent = 1f },
                BackgroundColor = new Color(0, 0, 0) * 0.45f,
                BorderColor = Color.Transparent
            };
            dim.OnLeftClick += (_, _) => Close();
            Append(dim);

            var dialog = new UIPanel
            {
                HAlign = 0.5f,
                VAlign = 0.35f,
                Width = { Pixels = 300f },
                Height = { Pixels = 118f },
                BackgroundColor = OPJourneyUiColors.PanelBackground,
                BorderColor = OPJourneyUiColors.PanelBorder
            };
            Append(dialog);

            dialog.Append(new UIText(EOPJText.BlueprintOr("RenameSetTitle", FallbackRenameTitle), 0.78f)
            {
                Left = { Pixels = 10f },
                Top = { Pixels = 8f }
            });

            _nameBar = new UISearchBar(LocalizedText.Empty, 0.72f);
            _nameBar.Left.Set(10f, 0f);
            _nameBar.Top.Set(34f, 0f);
            _nameBar.Width.Set(-20f, 1f);
            _nameBar.Height.Set(28f, 0f);
            _draftName = initialName ?? "";
            _nameBar.SetContents(_draftName);
            _nameBar.OnLeftClick += (_, _) =>
            {
                if (!_nameBar.IsWritingText)
                    _nameBar.ToggleTakingText();
            };
            _nameBar.OnContentsChanged += text => _draftName = text ?? "";
            dialog.Append(_nameBar);

            float btnW = 72f;
            var cancel = MakeDialogButton(10f, 78f, btnW, EOPJText.BlueprintOr("RenameSetCancel", FallbackCancel), Close);
            var ok = MakeDialogButton(10f + btnW + 8f, 78f, btnW, EOPJText.BlueprintOr("RenameSetConfirm", FallbackConfirm), Confirm);
            dialog.Append(cancel);
            dialog.Append(ok);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_nameBar.IsWritingText)
                Main.CurrentInputTextTakerOverride = _nameBar;
        }

        private void Confirm()
        {
            string text = _draftName?.Trim() ?? "";
            if (string.IsNullOrEmpty(text))
                return;
            _onConfirm?.Invoke(_schemeId, text);
            Close();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void Close()
        {
            if (_nameBar.IsWritingText)
                _nameBar.ToggleTakingText();
            _onClose?.Invoke();
            Remove();
        }

        private static UIPanel MakeDialogButton(float x, float y, float w, string label, Action click)
        {
            var panel = BlueprintUiFlatButton.Create(label, w, 28f, click, 0.68f);
            panel.Left.Set(x, 0f);
            panel.Top.Set(y, 0f);
            return panel;
        }
    }
}
