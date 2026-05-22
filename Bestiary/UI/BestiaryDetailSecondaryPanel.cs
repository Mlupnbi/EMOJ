using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>主窗右侧原版详情窗（284×606 信息区，对齐 <see cref="UIBestiaryEntryInfoPage"/>）。</summary>
    public sealed class BestiaryDetailSecondaryPanel : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly UIPanel _frame;
        private readonly UIText _emptyText;
        private UIBestiaryEntryInfoPage _infoPage;
        private BestiaryDetailPreviewScope _previewScope;
        private bool _open;

        public bool IsOpen => _open;

        public BestiaryDetailSecondaryPanel(OPJourneyUI shell)
        {
            _shell = shell;
            Left.Set(0, 0);
            Top.Set(0, 0);
            Width.Set(0, 0);
            Height.Set(0, 0);

            _frame = new UIPanel();
            _frame.SetPadding(0);
            _frame.Width.Set(0, 1f);
            _frame.Height.Set(0, 1f);
            _frame.BackgroundColor = OPJourneyUiColors.DetailPanelBackground;
            _frame.BorderColor = OPJourneyUiColors.DetailPanelBorder;
            Append(_frame);

            _emptyText = new UIText("", 0.85f);
            _emptyText.HAlign = 0.5f;
            _emptyText.VAlign = 0.5f;
            _emptyText.IgnoresMouseInteraction = true;
            _emptyText.Width.Set(0, 0);
            _emptyText.Height.Set(0, 0);
            _emptyText.TextColor = OPJourneyUiColors.DetailBodyText;
            _frame.Append(_emptyText);

            MountFreshInfoPage();
        }

        public void SetOpen(bool open)
        {
            _open = open;
            if (!open)
            {
                ReleasePreviewScope();
                SetEmptyVisible(false);
            }
        }

        private void ReleasePreviewScope()
        {
            _previewScope?.Dispose();
            _previewScope = null;
        }

        private void MountFreshInfoPage()
        {
            ReleasePreviewScope();

            if (_infoPage != null)
                _frame.RemoveChild(_infoPage);

            _infoPage = new UIBestiaryEntryInfoPage();
            _infoPage.Left.Set(0f, 0f);
            _infoPage.Top.Set(0f, 0f);
            _infoPage.Width.Set(0f, 1f);
            _infoPage.Height.Set(-OPJourneyShellMetrics.ContentBottomSafeMargin, 1f);
            _infoPage.Initialize();
            _frame.Append(_infoPage);
        }

        private void SetEmptyVisible(bool show, string message = "")
        {
            if (show)
            {
                _emptyText.SetText(message);
                _emptyText.Width.Set(-16f, 1f);
                _emptyText.Height.Set(-12f, 1f);
                if (_infoPage != null)
                {
                    _infoPage.Width.Set(0, 0);
                    _infoPage.Height.Set(0, 0);
                }
            }
            else
            {
                _emptyText.Width.Set(0, 0);
                _emptyText.Height.Set(0, 0);
                if (_infoPage != null)
                {
                    _infoPage.Top.Set(0f, 0f);
                    _infoPage.Width.Set(0f, 1f);
                    _infoPage.Height.Set(-OPJourneyShellMetrics.ContentBottomSafeMargin, 1f);
                }
            }
        }

        public void Show(BestiaryNpcMeta meta)
        {
            if (meta == null)
            {
                SetOpen(false);
                return;
            }

            if (meta.Entry == null)
            {
                MountFreshInfoPage();
                SetEmptyVisible(true, EOPJText.UI("BestiaryDetailUnregistered"));
                SetOpen(true);
                return;
            }

            if (_shell.BestiaryFaceMode == BestiaryFaceMode.ProgressiveMinus &&
                !BestiaryProgressResolver.WasEverFound(meta.Entry))
            {
                SetOpen(false);
                return;
            }

            try
            {
                MountFreshInfoPage();
                SetEmptyVisible(false);
                ReleasePreviewScope();
                _previewScope = BestiaryDetailPreviewScope.TryEnter(meta.Entry, _shell.BestiaryFaceMode);

                BestiaryUICollectionInfo collection = BestiaryVanillaDetailBridge.GetBoostedCollectionInfo(
                    meta.Entry,
                    _shell.BestiaryFaceMode);
                ExtraBestiaryInfoPageInformation extra = BestiaryVanillaDetailBridge.CreateExtra(
                    meta.Entry,
                    _shell.BestiaryFaceMode);

                _infoPage.FillInfoForEntry(meta.Entry, extra);
                BestiaryVanillaDetailBridge.TryPushCollectionToInfoPage(_infoPage, collection);
                _infoPage.Recalculate();
                BestiaryVanillaInfoPageLayout.Apply(_infoPage);
                BestiaryVanillaDetailBridge.TryPushCollectionToInfoPage(_infoPage, collection);
                _infoPage.Recalculate();

                SetOpen(true);
            }
            catch (System.Exception ex)
            {
                EmojLog.Warn(EmojLogChannel.Ui, $"Bestiary detail FillInfoForEntry failed: {ex}");
                ReleasePreviewScope();
                SetEmptyVisible(true, EOPJText.UI("BestiaryDetailPlaceholder"));
                SetOpen(true);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!_open || GetDimensions().Width < 2f)
                return;

            CalculatedStyle outer = GetDimensions();
            if (outer.Width > 2f && outer.ToRectangle().Contains(Main.MouseScreen.ToPoint()) && Main.LocalPlayer != null)
                Main.LocalPlayer.mouseInterface = true;

            base.Update(gameTime);
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            if (_open && _infoPage != null && GetDimensions().Width > 2f)
                _infoPage.UpdateScrollbar(evt.ScrollWheelValue);

            base.ScrollWheel(evt);
        }
    }
}
