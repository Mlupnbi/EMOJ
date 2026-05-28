using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>套组详情二级窗：22 格 + 保存 + 放置选项。</summary>
    public sealed class BlueprintSetDetailPanel : BlueprintSecondaryWindowBase
    {
        private const float BodyTextScale = 0.68f;
        private const float GridScrollWidth = EojUIScrollbar.DefaultWidth;
        private const float FooterHintHeight = 52f;
        private const float FooterBtnRowHeight = 28f;
        private const float FooterSaveHeight = 28f;
        private const float FooterGap = 6f;
        private const float HeaderHeight = 56f;
        private const float CoverSlotSize = 48f;
        private const float HeaderGapBelow = 4f;

        private const string FallbackTitle = "\u5957\u7ec4\u8be6\u60c5";
        private const string FallbackCover = "\u5c01\u9762";
        private const string FallbackDelete = "\u5220\u9664";
        private const string FallbackToggleConsume = "\u5207\u6362\u6d88\u8017\u6750\u6599";
        private const string FallbackApplyOverlay = "\u4ece\u8bc6\u522b\u8986\u76d6";
        private const string FallbackSaveSet = "\u4fdd\u5b58\u5957\u7ec4";
        private const string FallbackSaveAsNewSet = "\u53e6\u5b58\u4e3a\u65b0\u5957\u7ec4";
        private const float SaveAsNewButtonWidth = 118f;
        private const string FallbackOnShort = "\uff08\u5f00\uff09";
        private const string FallbackOffShort = "\uff08\u5173\uff09";
        private const string FallbackUnsaved = "\u672a\u4fdd\u5b58";
        private const string FallbackCustomScheme = "\u81ea\u5b9a\u4e49\u5bb6\u5177\u65b9\u6848";
        private const string FallbackFooterHint =
            "\u5207\u6362\u6d88\u8017\u6750\u6599\uff1a\u653e\u7f6e\u65f6\u662f\u5426\u6263\u80cc\u5305\u3002\n" +
            "\u4ece\u8bc6\u522b\u8986\u76d6\uff1a\u4e22\u5f03\u624b\u6539\uff0c\u6062\u590d\u4e3a\u6700\u8fd1\u4e00\u6b21\u8bc6\u522b\u7ed3\u679c\u3002\n" +
            "\u4fdd\u5b58\u5957\u7ec4\uff1a\u8986\u76d6\u5f53\u524d\u6b63\u5728\u7f16\u8f91\u7684\u5957\u7ec4\u3002\n" +
            "\u53e6\u5b58\u4e3a\u65b0\u5957\u7ec4\uff1a\u4ee5\u65b0\u540d\u79f0\u5199\u5165\u65b0\u6761\u76ee\uff0c\u539f\u5957\u7ec4\u4e0d\u53d8\u3002";
        private const string FallbackOverlayApplied = "\u5df2\u7528\u6700\u8fd1\u8bc6\u522b\u7ed3\u679c\u8986\u76d6\u5f53\u524d\u5957\u7ec4\u3002";
        private const string FallbackOverlayBusy = "\u8bc6\u522b\u8fdb\u884c\u4e2d\uff0c\u8bf7\u7a0d\u5019\u518d\u8bd5\u3002";
        private const string FallbackOverlayNoSeed = "\u8bf7\u5148\u5728\u4e3b\u7a97\u653e\u5165\u79cd\u5b50\u5e76\u5b8c\u6210\u8bc6\u522b\u3002";
        private const string FallbackOverlayNoMaterial = "\u8bf7\u5148\u786e\u8ba4\u6750\u6599\u5757\u540e\u518d\u8986\u76d6\u3002";
        private const string FallbackOverlayEmpty = "\u6ca1\u6709\u53ef\u7528\u7684\u8bc6\u522b\u7ed3\u679c\u3002";

        private static float FooterTotalHeight =>
            FooterHintHeight + FooterGap + FooterBtnRowHeight + FooterGap + FooterSaveHeight;

        private static float GridTopOffset => HeaderHeight + HeaderGapBelow;

        private static float GridHeightMargin => -(FooterTotalHeight + GridTopOffset);

        private UIPanel _gridHost;
        private UIList _slotGridList;
        private EojUIScrollbar _slotGridScroll;
        private BlueprintSlotGridPanel _slotGrid;
        private BlueprintSeedSlot _coverSlot;
        private UIText _setNameText;
        private UIText _footerHintText;
        private UIPanel _consumeBtn;
        private UIText _consumeBtnLabel;
        private UIPanel _overlayBtn;
        private UIText _overlayBtnLabel;
        private BlueprintRoundedToolbarButton _saveBtn;
        private UIPanel _saveAsNewBtn;
        private UIText _saveAsNewBtnLabel;
        private string _sessionEditingId;
        /// <summary>仅 OpenForScheme 为 true；新建/识别保存会话为 false，禁止误覆盖库内条目。</summary>
        private bool _sessionOverwriteExisting;
        private readonly BlueprintSchemeSlot[] _schemeSlots = new BlueprintSchemeSlot[FurnitureSlotKinds.Count];

        public bool IsEditingExistingLibraryEntry => _sessionOverwriteExisting;

        protected override string TitleLocalizationKey => "Blueprint.SetDetailTitle";
        protected override string TitleFallback => FallbackTitle;

        public override float DefaultWidth => FurnitureBlueprintPageLayout.SetDetailPanelWidth;
        public override float DefaultHeight => FurnitureBlueprintPageLayout.SetDetailPanelHeight;

        public BlueprintSetDetailPanel(OPJourneyUI shell) : base(shell) { }

        /// <summary>打开并绑定要编辑的已保存套组（保存时覆盖该 id）。</summary>
        public void OpenForScheme(string libraryId)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null || string.IsNullOrEmpty(libraryId))
                return;

            _sessionEditingId = libraryId;
            _sessionOverwriteExisting = true;
            fb.LoadCustomScheme(libraryId);
            SetOpen(true);
        }

        /// <summary>打开空白编辑会话（保存时新建）。</summary>
        public void OpenForNewSession(FurnitureScheme source = null)
        {
            _sessionEditingId = null;
            _sessionOverwriteExisting = false;
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb != null)
            {
                fb.SelectedLibrarySchemeId = "";
                if (source != null)
                    fb.ApplyEntireScheme(source.Clone());
            }

            SetOpen(true);
        }

        /// <summary>主窗换种子/识别时解除与旧库条目的绑定，避免保存误覆盖。</summary>
        public void NotifyWorkspaceSeedChanged()
        {
            _sessionEditingId = null;
            _sessionOverwriteExisting = false;
            if (IsOpen)
            {
                RefreshHeader();
                RefreshSlotDisplay();
                RefreshSaveButtonVisibility();
            }
        }

        public void SyncFromActiveScheme()
        {
            RefreshHeader();
            RefreshSlotDisplay();
        }

        public override void SetOpen(bool open)
        {
            if (!open)
            {
                _sessionEditingId = null;
                _sessionOverwriteExisting = false;
            }

            base.SetOpen(open);
        }

        protected override void BuildContent()
        {
            var header = new UIElement();
            header.Left.Set(0f, 0f);
            header.Top.Set(0f, 0f);
            header.Width.Set(0f, 1f);
            header.Height.Set(HeaderHeight, 0f);
            ContentHost.Append(header);

            _coverSlot = new BlueprintSeedSlot
            {
                ReturnPhysicalOnPlace = true,
                ReturnPhysicalOnClear = false,
                Left = { Pixels = 0f },
                VAlign = 0.5f,
                Width = { Pixels = CoverSlotSize },
                Height = { Pixels = CoverSlotSize }
            };
            _coverSlot.OnItemChanged += OnCoverSlotChanged;
            header.Append(_coverSlot);

            header.Append(new UIText(EOPJText.BlueprintOr("CoverSlotLabel", FallbackCover), 0.62f)
            {
                Left = { Pixels = CoverSlotSize + 6f },
                Top = { Pixels = 6f },
                TextColor = Color.LightGray
            });

            _setNameText = new UIText("", 0.74f)
            {
                Left = { Pixels = CoverSlotSize + 6f },
                Top = { Pixels = 24f + BlueprintUiFlatButton.DefaultTextNudgeY },
                Width = { Pixels = -170f, Percent = 1f },
                IsWrapped = true,
                TextColor = Color.White
            };
            header.Append(_setNameText);

            var deleteBtn = BlueprintUiFlatButton.Create(
                EOPJText.BlueprintOr("BtnDeleteSet", FallbackDelete),
                72f, FooterBtnRowHeight, OnDeleteCurrentScheme, 0.62f);
            deleteBtn.HAlign = 1f;
            deleteBtn.VAlign = 0.5f;
            header.Append(deleteBtn);

            _gridHost = new UIPanel
            {
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            _gridHost.SetPadding(0);
            _gridHost.Left.Set(0f, 0f);
            _gridHost.Top.Set(GridTopOffset, 0f);
            _gridHost.Width.Set(0f, 1f);
            _gridHost.Height.Set(GridHeightMargin, 1f);
            ContentHost.Append(_gridHost);

            _slotGridList = new UIList();
            _slotGridList.Left.Set(0f, 0f);
            _slotGridList.Top.Set(0f, 0f);
            _slotGridList.Width.Set(-GridScrollWidth, 1f);
            _slotGridList.Height.Set(0f, 1f);
            _slotGridList.ListPadding = 0f;
            _gridHost.Append(_slotGridList);

            _slotGridScroll = new EojUIScrollbar(GridScrollWidth);
            _slotGridScroll.HAlign = 1f;
            _slotGridScroll.Height.Set(0f, 1f);
            _slotGridList.SetScrollbar(_slotGridScroll);
            _gridHost.Append(_slotGridScroll);

            _slotGrid = new BlueprintSlotGridPanel();
            _slotGrid.Width.Set(0f, 1f);
            _slotGridList.Add(_slotGrid);
            BuildSlotCells();

            var footer = new UIElement();
            footer.VAlign = 1f;
            footer.Width.Set(0f, 1f);
            footer.Height.Set(FooterTotalHeight, 0f);
            ContentHost.Append(footer);

            _footerHintText = new UIText("", BodyTextScale)
            {
                Left = { Pixels = 0f },
                Top = { Pixels = 0f },
                Width = { Pixels = 0f, Percent = 1f },
                Height = { Pixels = FooterHintHeight },
                IsWrapped = true,
                TextColor = Color.LightSteelBlue
            };
            footer.Append(_footerHintText);

            float btnRowTop = FooterHintHeight + FooterGap;
            (_consumeBtn, _consumeBtnLabel) = BlueprintUiFlatButton.CreateWithLabel(
                EOPJText.BlueprintOr("BtnToggleConsume", FallbackToggleConsume),
                130f, FooterBtnRowHeight, OnToggleConsume, 0.62f);
            _consumeBtn.Left.Set(0f, 0f);
            _consumeBtn.Top.Set(btnRowTop, 0f);
            footer.Append(_consumeBtn);

            (_overlayBtn, _overlayBtnLabel) = BlueprintUiFlatButton.CreateWithLabel(
                EOPJText.BlueprintOr("BtnApplyRecognitionOverlay", FallbackApplyOverlay),
                130f, FooterBtnRowHeight, OnApplyRecognitionOverlay, 0.58f);
            _overlayBtn.Left.Set(138f, 0f);
            _overlayBtn.Top.Set(btnRowTop, 0f);
            footer.Append(_overlayBtn);

            float saveTop = btnRowTop + FooterBtnRowHeight + FooterGap;
            _saveBtn = new BlueprintRoundedToolbarButton(
                FurnitureBlueprintPageLayout.ToolbarActionButtonWidth,
                FooterSaveHeight,
                EOPJText.BlueprintOr("BtnSaveSet", FallbackSaveSet),
                OnSaveScheme);
            _saveBtn.Left.Set(0f, 0f);
            _saveBtn.Top.Set(saveTop, 0f);
            footer.Append(_saveBtn);

            (_saveAsNewBtn, _saveAsNewBtnLabel) = BlueprintUiFlatButton.CreateWithLabel(
                EOPJText.BlueprintOr("BtnSaveAsNewSet", FallbackSaveAsNewSet),
                SaveAsNewButtonWidth, FooterSaveHeight, OnSaveAsNewScheme, 0.58f);
            _saveAsNewBtn.Left.Set(FurnitureBlueprintPageLayout.ToolbarActionButtonWidth + 8f, 0f);
            _saveAsNewBtn.Top.Set(saveTop, 0f);
            footer.Append(_saveAsNewBtn);
        }

        protected override void OnOpened()
        {
            RefreshLocalizedStrings();
            RefreshHeader();
            RefreshSlotDisplay();
            RecalculateGridLayout();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (IsOpen)
                RecalculateGridLayout();
        }

        private void RefreshLocalizedStrings()
        {
            _footerHintText?.SetText(EOPJText.BlueprintOr("DetailFooterHint", FallbackFooterHint));
            RefreshToggleButtonLabels();
            _overlayBtnLabel?.SetText(EOPJText.BlueprintOr("BtnApplyRecognitionOverlay", FallbackApplyOverlay));
            _saveBtn?.SetLabel(EOPJText.BlueprintOr("BtnSaveSet", FallbackSaveSet));
            _saveAsNewBtnLabel?.SetText(EOPJText.BlueprintOr("BtnSaveAsNewSet", FallbackSaveAsNewSet));
            RefreshSaveButtonVisibility();
        }

        private void RefreshSaveButtonVisibility()
        {
            if (_saveAsNewBtn == null)
                return;

            bool show = IsOpen && _sessionOverwriteExisting;
            _saveAsNewBtn.IgnoresMouseInteraction = !show;
            _saveAsNewBtn.Width.Set(show ? SaveAsNewButtonWidth : 0f, 0f);
            if (_saveAsNewBtnLabel != null)
                _saveAsNewBtnLabel.TextColor = show ? Color.White : Color.Transparent;
        }

        private void RefreshToggleButtonLabels()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            string consume = EOPJText.BlueprintOr("BtnToggleConsume", FallbackToggleConsume)
                + (fb.ConsumeMaterialsOnPlace
                    ? EOPJText.BlueprintOr("ConsumeOnShort", FallbackOnShort)
                    : EOPJText.BlueprintOr("ConsumeOffShort", FallbackOffShort));

            _consumeBtnLabel?.SetText(consume);
        }

        private string GetSessionLibraryId() => _sessionEditingId;

        private void RecalculateGridLayout() => _slotGrid?.Recalculate();

        private void RefreshHeader()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            string name = fb.ActiveScheme?.DisplayName;
            if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrEmpty(_sessionEditingId)
                && fb.CustomSchemes.TryGetValue(_sessionEditingId, out FurnitureScheme saved))
                name = saved.DisplayName;
            if (string.IsNullOrWhiteSpace(name))
                name = EOPJText.BlueprintOr("HubNoSet", FallbackUnsaved);

            _setNameText?.SetText(name ?? "");

            int coverType = fb.ActiveScheme?.ResolveCoverItemType() ?? ItemID.None;
            if (_coverSlot != null)
            {
                _coverSlot.item = new Item();
                if (coverType > ItemID.None)
                    _coverSlot.item.SetDefaults(coverType);
            }
        }

        private void OnCoverSlotChanged(Item item)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            fb.ActiveScheme.IconItemType = item == null || item.IsAir ? ItemID.None : item.type;
            TryPersistEditingDraft(fb);
            Shell.ActiveBlueprintPage?.RefreshSchemeList();
        }

        private void OnDeleteCurrentScheme()
        {
            string id = GetSessionLibraryId();
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null || string.IsNullOrEmpty(id) || !fb.CustomSchemes.ContainsKey(id))
                return;

            fb.DeleteCustomScheme(id);
            _sessionEditingId = null;
            _sessionOverwriteExisting = false;
            Shell.ActiveBlueprintPage?.RefreshSchemeList();
            Shell.ActiveBlueprintPage?.RefreshSummary();
            RefreshHeader();
            RefreshSlotDisplay();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void BuildSlotCells()
        {
            var cells = new List<UIElement>();
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                FurnitureSlotKind kind = FurnitureSlotKinds.FromIndex(i);
                var slot = new BlueprintSchemeSlot
                {
                    DisplayOnly = false,
                    ReturnPhysicalOnPlace = true,
                    ReturnPhysicalOnClear = true
                };
                int idx = i;
                slot.OnItemChanged += _ => OnSchemeSlotChanged(idx);
                _schemeSlots[i] = slot;

                cells.Add(new BlueprintSlotCell(kind, slot));
            }
            _slotGrid.SetCells(cells);
        }

        public void RefreshSlotDisplay()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            PushSchemeToUi(fb.ActiveScheme);
            fb.SyncPreviewFromActive();
        }

        public void ClearSlotDisplay()
        {
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
                _schemeSlots[i].item = new Item();
        }

        public void PushSchemeToUi(FurnitureScheme scheme)
        {
            if (scheme == null)
                return;

            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                int type = scheme.SlotItemTypes[i];
                _schemeSlots[i].item = new Item();
                if (type > ItemID.None)
                    _schemeSlots[i].item.SetDefaults(type);
            }
        }

        private void PullSchemeFromUi(FurnitureScheme scheme)
        {
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                Item it = _schemeSlots[i].item;
                scheme.SlotItemTypes[i] = it == null || it.IsAir ? ItemID.None : it.type;
            }
        }

        private void SyncActiveAndPreviewFromUi()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            PullSchemeFromUi(fb.ActiveScheme);
            fb.ActiveScheme.IsAutoGenerated = false;
            fb.SyncPreviewFromActive();
        }

        private void OnSchemeSlotChanged(int index)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            Item it = _schemeSlots[index].item;
            fb.ActiveScheme.SlotItemTypes[index] = it == null || it.IsAir ? ItemID.None : it.type;
            fb.ActiveScheme.IsAutoGenerated = false;
            fb.SyncPreviewFromActive();
            TryPersistEditingDraft(fb);
        }

        private void TryPersistEditingDraft(FurnitureBlueprintPlayer fb)
        {
            if (!_sessionOverwriteExisting)
                return;

            string id = GetSessionLibraryId();
            if (string.IsNullOrEmpty(id) || !fb.CustomSchemes.ContainsKey(id))
                return;

            FurnitureScheme draft = fb.ActiveScheme.Clone();
            FurnitureScheme existing = fb.CustomSchemes[id];
            draft.DisplayName = existing.DisplayName;
            draft.IconItemType = fb.ActiveScheme.IconItemType > ItemID.None
                ? fb.ActiveScheme.IconItemType
                : existing.IconItemType;
            fb.CustomSchemes[id] = draft;
        }

        public void OnSaveSchemeFromHub() => SaveSchemeCore(saveAsNew: false);

        private void OnSaveScheme() => SaveSchemeCore(saveAsNew: false);

        private void OnSaveAsNewScheme() => SaveSchemeCore(saveAsNew: true);

        private void SaveSchemeCore(bool saveAsNew)
        {
            Player player = Main.LocalPlayer;
            if (player == null)
                return;

            FurnitureBlueprintPlayer fb = player.GetModPlayer<FurnitureBlueprintPlayer>();
            SyncActiveAndPreviewFromUi();
            PullCoverFromUi(fb.ActiveScheme);

            string overwriteId = _sessionOverwriteExisting ? GetSessionLibraryId() : null;
            string id;
            string name;
            if (saveAsNew)
            {
                if (!_sessionOverwriteExisting)
                    return;

                id = Guid.NewGuid().ToString("N");
                string baseName = ResolveDisplayNameForSave(fb, overwriteId);
                name = FurnitureSchemeNaming.AllocateUniqueDisplayName(fb, baseName);
                fb.ActiveScheme.DisplayName = name;
                FurnitureBlueprintLog.Info(
                    $"detail save-as-new id={id} name={name} sourceId={overwriteId}");
            }
            else if (!string.IsNullOrEmpty(overwriteId) && fb.CustomSchemes.ContainsKey(overwriteId))
            {
                id = overwriteId;
                FurnitureScheme existing = fb.CustomSchemes[id];
                name = string.IsNullOrWhiteSpace(fb.ActiveScheme.DisplayName)
                    ? (string.IsNullOrEmpty(existing.DisplayName)
                        ? EOPJText.BlueprintOr("CustomSchemeName", FallbackCustomScheme)
                        : existing.DisplayName)
                    : fb.ActiveScheme.DisplayName.Trim();
                FurnitureBlueprintLog.Info($"detail save overwrite id={id} name={name}");
            }
            else
            {
                id = Guid.NewGuid().ToString("N");
                name = ResolveDisplayNameForSave(fb, overwriteId);
                FurnitureBlueprintLog.Info($"detail save new id={id} name={name}");
            }

            fb.SaveCustomScheme(id, name);
            _sessionEditingId = id;
            _sessionOverwriteExisting = true;
            fb.SelectedLibrarySchemeId = id;
            Shell.ActiveBlueprintPage?.RefreshSchemeList();
            RefreshHeader();
            RefreshSlotDisplay();
            RefreshSaveButtonVisibility();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private static string ResolveDisplayNameForSave(FurnitureBlueprintPlayer fb, string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(fb.ActiveScheme?.DisplayName))
                return fb.ActiveScheme.DisplayName.Trim();

            if (!string.IsNullOrEmpty(sessionId)
                && fb.CustomSchemes.TryGetValue(sessionId, out FurnitureScheme existing)
                && !string.IsNullOrEmpty(existing.DisplayName))
                return existing.DisplayName;

            int seed = fb.PendingSeedType;
            if (seed > ItemID.None && ContentSamples.ItemsByType.TryGetValue(seed, out Item seedItem))
                return seedItem.Name;

            return EOPJText.BlueprintOr("CustomSchemeName", FallbackCustomScheme);
        }

        private void PullCoverFromUi(FurnitureScheme scheme)
        {
            if (scheme == null || _coverSlot == null)
                return;

            Item it = _coverSlot.item;
            scheme.IconItemType = it == null || it.IsAir ? ItemID.None : it.type;
            if (scheme.IconItemType <= ItemID.None)
            {
                int auto = scheme.ResolveCoverItemType();
                if (auto > ItemID.None)
                    scheme.IconItemType = auto;
            }
        }

        private void OnToggleConsume()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer.GetModPlayer<FurnitureBlueprintPlayer>();
            fb.ConsumeMaterialsOnPlace = !fb.ConsumeMaterialsOnPlace;
            RefreshToggleButtonLabels();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void OnApplyRecognitionOverlay()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            int seed = Shell.ActiveBlueprintPage?.PendingSeedType ?? fb.PendingSeedType;
            FurnitureRecognitionOverlayResult result = FurnitureRecognitionOverlay.TryApply(fb, seed, fb.PendingMaterialBlock);

            switch (result)
            {
                case FurnitureRecognitionOverlayResult.Success:
                    RefreshHeader();
                    RefreshSlotDisplay();
                    Main.NewText(EOPJText.BlueprintOr("StatusOverlayApplied", FallbackOverlayApplied));
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    break;
                case FurnitureRecognitionOverlayResult.Busy:
                    Main.NewText(EOPJText.BlueprintOr("StatusOverlayBusy", FallbackOverlayBusy));
                    break;
                case FurnitureRecognitionOverlayResult.MissingSeed:
                    Main.NewText(EOPJText.BlueprintOr("StatusOverlayNoSeed", FallbackOverlayNoSeed));
                    break;
                case FurnitureRecognitionOverlayResult.MissingMaterial:
                    Main.NewText(EOPJText.BlueprintOr("StatusOverlayNoMaterial", FallbackOverlayNoMaterial));
                    break;
                default:
                    Main.NewText(EOPJText.BlueprintOr("StatusOverlayEmpty", FallbackOverlayEmpty));
                    break;
            }
        }
    }
}
