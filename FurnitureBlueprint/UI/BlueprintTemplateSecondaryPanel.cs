using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>样板房二级窗：左 38% 模板卡片列表，右 62% 大预览（≥280px）。</summary>
    public sealed class BlueprintTemplateSecondaryPanel : BlueprintSecondaryWindowBase
    {
        private const float ColumnGap = 8f;
        private const float LeftColumnShare = 0.38f;
        private const float FooterLegendHeight = 36f;
        private const float FooterHintHeight = 40f;
        private const float FooterGap = 6f;
        private const float PickerLabelHeight = 18f;
        private const float PickerLabelGap = 4f;
        private const float HeaderBlockHeight = 44f;
        private const float ModeRowHeight = 28f;
        private const float ModeRowGap = 4f;
        private static float PreviewTopOffset => HeaderBlockHeight + ModeRowHeight + ModeRowGap;

        private UIText _nameText;
        private UIText _partsText;
        private UIPanel _strictBtn;
        private UIText _strictBtnLabel;
        private UIPanel _looseBtn;
        private UIText _looseBtnLabel;
        private UIList _cardList;
        private EojUIScrollbar _cardScroll;
        private BlueprintLayoutPreviewElement _preview;
        private UIText _footerLegendText;
        private UIText _footerHintText;
        private int _lastSchemeSignature = int.MinValue;

        protected override string TitleLocalizationKey => "Blueprint.TemplatePanelTitle";
        protected override string TitleFallback => "\u5efa\u7b51\u65b9\u6848";
        private const string FallbackStrict = "Strict";
        private const string FallbackLoose = "Loose";
        private const string FallbackPreviewLegend =
            "\u6b63\u5e38=\u5df2\u914d\u7f6e\u6750\u6599\uff1b\u7ea2\u8272=\u8be5\u90e8\u4f4d\u7f3a\u4ef6\uff08\u6728\u5bb6\u5177\u5360\u4f4d\uff09";
        private const string FallbackInteractionHint =
            "\u5efa\u7b51\u65b9\u6848=\u6237\u578b\uff1b\u5957\u7ec4=22\u69fd\u6750\u6599\u3002Strict=\u7f3a\u4ef6\u6574\u6b21\u62d2\u7edd\uff1bLoose=\u7f3a\u4ef6\u8df3\u8fc7\u3002";
        private const string FallbackTemplateEmpty = "\u6682\u65e0\u53ef\u7528\u6237\u578b";
        private const string FallbackActivePartsFormat = "{0}\u00d7{1} \u00b7 \u6750\u6599 {2}/{3}";
        private const string FallbackStrictNeedsCoverage = "\uff08\u4e25\u683c\uff1a\u9700\u8865\u5168\u6750\u6599\uff09";

        public override float DefaultWidth => FurnitureBlueprintPageLayout.TemplatePanelWidth;
        public override float DefaultHeight => FurnitureBlueprintPageLayout.TemplatePanelHeight;

        public BlueprintTemplateSecondaryPanel(OPJourneyUI shell) : base(shell) { }

        protected override void BuildContent()
        {
            float footerBlock = FooterLegendHeight + FooterHintHeight + FooterGap * 2f;

            // StyleDimension.Set(pixels, percent)：第二参数为父元素宽度/高度比例，禁止对同一轴连续 Set 覆盖。
            var leftColumn = new UIElement();
            leftColumn.Left.Set(0f, 0f);
            leftColumn.Top.Set(0f, 0f);
            leftColumn.Width.Set(0f, LeftColumnShare);
            leftColumn.Height.Set(0f, 1f);
            ContentHost.Append(leftColumn);

            leftColumn.Append(new UIText(
                EOPJText.UIOr("Blueprint.TemplatePickerLabel", "\u65b9\u6848\u9009\u62e9"), 0.72f)
            {
                Left = { Pixels = 0f },
                Top = { Pixels = 0f },
                TextColor = Color.LightGray
            });

            var listHost = new UIPanel
            {
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            listHost.SetPadding(0);
            listHost.Left.Set(0f, 0f);
            listHost.Top.Set(PickerLabelHeight + PickerLabelGap, 0f);
            listHost.Width.Set(-EojUIScrollbar.DefaultWidth, 1f);
            listHost.Height.Set(-(PickerLabelHeight + PickerLabelGap), 1f);
            leftColumn.Append(listHost);

            _cardList = new UIList();
            _cardList.Width.Set(0f, 1f);
            _cardList.Height.Set(0f, 1f);
            _cardList.ListPadding = 6f;
            listHost.Append(_cardList);

            _cardScroll = new EojUIScrollbar(EojUIScrollbar.DefaultWidth);
            _cardScroll.HAlign = 1f;
            _cardScroll.Height.Set(0f, 1f);
            _cardList.SetScrollbar(_cardScroll);
            listHost.Append(_cardScroll);

            var rightColumn = new UIElement();
            rightColumn.Left.Set(ColumnGap, LeftColumnShare);
            rightColumn.Top.Set(0f, 0f);
            rightColumn.Width.Set(-ColumnGap, 1f - LeftColumnShare);
            rightColumn.Height.Set(0f, 1f);
            ContentHost.Append(rightColumn);

            _nameText = new UIText("", 0.78f)
            {
                Left = { Pixels = 0f },
                Top = { Pixels = 0f },
                Width = { Pixels = 0f, Percent = 1f },
                IsWrapped = false,
                TextColor = Color.White
            };
            rightColumn.Append(_nameText);

            _partsText = new UIText("", 0.62f)
            {
                Left = { Pixels = 0f },
                Top = { Pixels = 22f },
                Width = { Pixels = 0f, Percent = 1f },
                IsWrapped = false,
                TextColor = Color.LightGray
            };
            rightColumn.Append(_partsText);

            (_strictBtn, _strictBtnLabel) = BlueprintUiFlatButton.CreateWithLabel(
                EOPJText.BlueprintOr("BtnPlacementStrict", FallbackStrict),
                88f, ModeRowHeight, () => SetPlacementMode(BlueprintPlacementMode.Strict), 0.62f);
            _strictBtn.Left.Set(0f, 0f);
            _strictBtn.Top.Set(HeaderBlockHeight, 0f);
            rightColumn.Append(_strictBtn);

            (_looseBtn, _looseBtnLabel) = BlueprintUiFlatButton.CreateWithLabel(
                EOPJText.BlueprintOr("BtnPlacementLoose", FallbackLoose),
                88f, ModeRowHeight, () => SetPlacementMode(BlueprintPlacementMode.Loose), 0.62f);
            _looseBtn.Left.Set(96f, 0f);
            _looseBtn.Top.Set(HeaderBlockHeight, 0f);
            rightColumn.Append(_looseBtn);

            var previewFrame = new UIPanel();
            previewFrame.Left.Set(0f, 0f);
            previewFrame.Top.Set(PreviewTopOffset, 0f);
            previewFrame.Width.Set(0f, 1f);
            previewFrame.Height.Set(-(PreviewTopOffset + footerBlock), 1f);
            previewFrame.BackgroundColor = new Color(18, 22, 30) * 0.92f;
            previewFrame.BorderColor = OPJourneyUiColors.PanelBorder;
            rightColumn.Append(previewFrame);

            _preview = new BlueprintLayoutPreviewElement();
            _preview.Left.Set(4f, 0f);
            _preview.Top.Set(4f, 0f);
            _preview.Width.Set(-8f, 1f);
            _preview.Height.Set(-8f, 1f);
            previewFrame.Append(_preview);

            _footerLegendText = new UIText(FallbackPreviewLegend, 0.58f)
            {
                Left = { Pixels = 0f },
                VAlign = 1f,
                Top = { Pixels = -(FooterHintHeight + FooterGap + FooterLegendHeight) },
                Width = { Pixels = 0f, Percent = 1f },
                Height = { Pixels = FooterLegendHeight },
                IsWrapped = true,
                TextColor = Color.DarkGray
            };
            rightColumn.Append(_footerLegendText);

            _footerHintText = new UIText(FallbackInteractionHint, 0.56f)
            {
                Left = { Pixels = 0f },
                VAlign = 1f,
                Top = { Pixels = -FooterHintHeight },
                Width = { Pixels = 0f, Percent = 1f },
                Height = { Pixels = FooterHintHeight },
                IsWrapped = true,
                TextColor = Color.LightSteelBlue
            };
            rightColumn.Append(_footerHintText);
        }

        protected override void OnOpened()
        {
            SyncFromPlayer();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!IsOpen)
                return;

            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            int signature = ComputeSchemeSignature(fb.ActiveScheme);
            if (signature != _lastSchemeSignature)
            {
                _lastSchemeSignature = signature;
                RebuildCards();
            }

            UpdateSelectionSummary();
        }

        public void SyncFromPlayer()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            BuiltinBlueprintTemplates.EnsureValidActiveTemplate(fb);
            _lastSchemeSignature = ComputeSchemeSignature(fb.ActiveScheme);
            BlueprintLayout layout = BuiltinBlueprintTemplates.ResolveActiveLayout(fb);
            if (layout != null && fb.ActiveScheme != null)
                BlueprintLayoutPreviewCache.RequestRebuild(layout, fb.ActiveScheme);
            RebuildCards();
            RefreshModeButtons();
            UpdateSelectionSummary();
            _footerLegendText?.SetText(FallbackPreviewLegend);
            _footerHintText?.SetText(FallbackInteractionHint);
        }

        private void SetPlacementMode(BlueprintPlacementMode mode)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null || fb.PlacementMode == mode)
                return;

            fb.PlacementMode = mode;
            RefreshModeButtons();
            UpdateSelectionSummary();
        }

        private void RefreshModeButtons()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            bool strict = fb.PlacementMode == BlueprintPlacementMode.Strict;
            ApplyModeButtonStyle(_strictBtn, strict);
            ApplyModeButtonStyle(_looseBtn, !strict);

            _strictBtnLabel?.SetText(EOPJText.BlueprintOr("BtnPlacementStrict", FallbackStrict)
                + (strict
                    ? EOPJText.BlueprintOr("PlacementModeActiveShort", FallbackActiveShort)
                    : ""));
            _looseBtnLabel?.SetText(EOPJText.BlueprintOr("BtnPlacementLoose", FallbackLoose)
                + (!strict
                    ? EOPJText.BlueprintOr("PlacementModeActiveShort", FallbackActiveShort)
                    : ""));
        }

        private static void ApplyModeButtonStyle(UIPanel panel, bool active)
        {
            if (panel == null)
                return;

            panel.BackgroundColor = active
                ? OPJourneyUiColors.TabActiveBackground
                : new Color(32, 38, 52) * 0.92f;
            panel.BorderColor = active ? OPJourneyUiColors.AccentGoldOutline : OPJourneyUiColors.PanelBorder;
        }

        private const string FallbackActiveShort = " \u25cf";

        private static int ComputeSchemeSignature(FurnitureScheme scheme) =>
            BlueprintLayoutPreviewCache.ComputeSchemeSignature(scheme);

        private void RebuildCards()
        {
            _cardList?.Clear();
            if (_cardList == null || BuiltinBlueprintTemplates.All == null)
                return;

            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            string activeId = fb?.ActiveTemplateId ?? "";
            FurnitureScheme scheme = fb?.ActiveScheme;

            foreach (BlueprintLayout layout in BuiltinBlueprintTemplates.All)
            {
                if (layout == null)
                    continue;

                (int configured, int required) = BuiltinBlueprintTemplates.CountMaterialCoverage(layout, scheme);
                bool selected = layout.Id == activeId;
                _cardList.Add(new BlueprintTemplateCard(layout, selected, configured, required, OnPickTemplate));
            }

            if (_cardList.Count == 0)
            {
                _cardList.Add(new UIText(
                    EOPJText.BlueprintOr("TemplateLibraryEmpty", FallbackTemplateEmpty),
                    0.68f)
                {
                    TextColor = Color.Gray,
                    Width = { Pixels = 0f, Percent = 1f },
                    IsWrapped = true
                });
            }
        }

        private void OnPickTemplate(BlueprintLayout layout)
        {
            if (layout == null)
                return;

            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            if (string.Equals(fb.ActiveTemplateId, layout.Id, StringComparison.Ordinal))
                return;

            fb.ApplyTemplateDefaults(layout);
            RebuildCards();
            UpdateSelectionSummary();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void UpdateSelectionSummary()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null || _nameText == null)
                return;

            BlueprintLayout layout = BuiltinBlueprintTemplates.ResolveActiveLayout(fb);
            if (layout == null)
            {
                _nameText.SetText(EOPJText.BlueprintOr("TemplateLibraryEmpty", FallbackTemplateEmpty));
                _partsText?.SetText("");
                if (_partsText != null)
                    _partsText.TextColor = Color.LightGray;
                return;
            }

            _nameText.SetText(ResolveTemplateName(layout.DisplayNameKey));

            (int configured, int required) = BuiltinBlueprintTemplates.CountMaterialCoverage(layout, fb.ActiveScheme);
            if (_partsText != null)
            {
                string format = EOPJText.BlueprintOr("TemplateActivePartsFormat", FallbackActivePartsFormat);
                string line = format
                    .Replace("{0}", layout.Width.ToString())
                    .Replace("{1}", layout.Height.ToString())
                    .Replace("{2}", configured.ToString())
                    .Replace("{3}", required.ToString());

                if (fb.PlacementMode == BlueprintPlacementMode.Strict
                    && required > 0
                    && configured < required)
                {
                    line += "  " + EOPJText.BlueprintOr("StrictNeedsFullCoverage", FallbackStrictNeedsCoverage);
                }

                _partsText.SetText(line);
                _partsText.TextColor = fb.PlacementMode == BlueprintPlacementMode.Strict
                    && configured < required
                    ? new Color(230, 170, 120)
                    : Color.LightGray;
            }
        }

        internal static string ResolveTemplateName(string displayNameKey) =>
            EOPJText.UIOr(displayNameKey, displayNameKey switch
            {
                "Blueprint.Template.Preset.RoomA" => "Room A",
                "Blueprint.Template.Preset.RoomB" => "Room B",
                "Blueprint.Template.Preset.RoomC" => "Room C",
                "Blueprint.Template.SimpleNpcRoom" => "NPC room",
                "Blueprint.Template.CompactShelter" => "Compact shelter",
                "Blueprint.Template.IG.BuildingShowcase" => "IG buildingShowcase",
                _ => displayNameKey
            });
    }
}
