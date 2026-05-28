using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.FurnitureBlueprint;
using EvenMoreOverpoweredJourney.Shell.Players;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>家具蓝图主窗：查询 + 识别 22 格 + 已保存套组列表。</summary>
    public sealed class FurnitureBlueprintPage : UIElement
    {
        private const float ContentPadLeft = OPJourneyShellMetrics.ContentInsetLeft;
        private const float ContentPadRight = OPJourneyShellMetrics.ScrollSafeMarginRight;
        private const float SectionTitleScale = 1.05f;
        private const float BodyTextScale = 0.68f;
        private const float GridScrollWidth = EojUIScrollbar.DefaultWidth;

        private readonly OPJourneyUI _shell;
        private UIPanel _toolbar;
        private UIElement _queryRow;
        private UIPanel _materialRow;
        private UIText _queryHintText;
        private BlueprintSeedSlot _seedSlot;
        private BlueprintSeedSlot _materialSlot;
        private BlueprintUiFoldButton _materialFoldBtn;
        private UIText _statusText;
        private UIText _savePromptText;
        private UIText _gridTitle;
        private UIText _savedTitle;
        private UIElement _savedHeaderRow;
        private BlueprintRoundedToolbarButton _btnNewEmptySet;
        private UIPanel _slotGridHost;
        private UIList _slotGridList;
        private EojUIScrollbar _slotGridScroll;
        private BlueprintSlotGridPanel _slotGrid;
        private UIPanel _savedPanel;
        private UIList _schemeList;
        private EojUIScrollbar _schemeScroll;
        private readonly BlueprintSchemeSlot[] _schemeSlots = new BlueprintSchemeSlot[FurnitureSlotKinds.Count];
        private BlueprintSchemeRenameOverlay _renameOverlay;
        private bool _triedApplyPendingQuickQuery;
        private float _lastLayoutHeight = -1f;

        public int PendingSeedType => _seedSlot?.item?.type ?? ItemID.None;

        public FurnitureBlueprintPage(OPJourneyUI shell)
        {
            _shell = shell;
            Width.Set(0, 1f);
            Height.Set(0, 1f);
            BuildUi();
            FurnitureBlueprintUiBridge.SchemeApplied += OnSchemeAppliedFromRecognition;
        }

        private void OnSchemeAppliedFromRecognition()
        {
            if (Main.gameMenu)
                return;
            SyncMaterialSlotFromPlayer();
            RefreshRecognitionGrid();
            RefreshSummary();
        }

        private static float ContentWidthMargin => -(ContentPadLeft + ContentPadRight);

        private void BuildUi()
        {
            float left = ContentPadLeft;
            float top = 4f;

            BuildToolbar(left, top);

            float statusTop = top + FurnitureBlueprintPageLayout.ToolbarHeight + FurnitureBlueprintPageLayout.HubStatusGap;
            _statusText = new UIText(EOPJText.UIOr("Blueprint.HubStatusIdle", "识别：拖入种子物品开始"), BodyTextScale)
            {
                Left = { Pixels = left },
                Top = { Pixels = statusTop },
                Width = { Pixels = ContentWidthMargin, Percent = 1f },
                IsWrapped = true,
                TextColor = Color.LightGray
            };
            Append(_statusText);

            _savePromptText = new UIText("", BodyTextScale)
            {
                Left = { Pixels = left },
                Top = { Pixels = statusTop + FurnitureBlueprintPageLayout.SummaryRowHeight },
                Width = { Pixels = ContentWidthMargin, Percent = 1f },
                IsWrapped = true,
                TextColor = Color.Khaki
            };
            Append(_savePromptText);

            float gridTitleTop = statusTop + FurnitureBlueprintPageLayout.SummaryRowHeight
                + FurnitureBlueprintPageLayout.SavePromptRowHeight + 4f;
            _gridTitle = MakeSectionTitle(left, gridTitleTop, "Blueprint.SetGridTitle", "家具套组识别");
            Append(_gridTitle);

            _slotGridHost = new UIPanel
            {
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            _slotGridHost.SetPadding(0);
            Append(_slotGridHost);

            _slotGridList = new UIList();
            _slotGridList.ListPadding = 0f;
            _slotGridHost.Append(_slotGridList);

            _slotGridScroll = new EojUIScrollbar(GridScrollWidth);
            _slotGridList.SetScrollbar(_slotGridScroll);
            _slotGridHost.Append(_slotGridScroll);

            _slotGrid = new BlueprintSlotGridPanel();
            _slotGrid.Width.Set(0f, 1f);
            _slotGridList.Add(_slotGrid);
            BuildRecognitionSlotCells();

            _savedHeaderRow = new UIElement();
            Append(_savedHeaderRow);

            _savedTitle = MakeSectionTitle(0f, 0f, "Blueprint.SavedSetsTitle", "已保存的套组");
            _savedHeaderRow.Append(_savedTitle);

            _btnNewEmptySet = new BlueprintRoundedToolbarButton(
                FurnitureBlueprintPageLayout.NewEmptySetButtonWidth,
                FurnitureBlueprintPageLayout.HubActionButtonHeight,
                EOPJText.BlueprintOr("BtnNewEmptySet", "新建空套组"),
                OnCreateEmptySet);
            _btnNewEmptySet.HAlign = 1f;
            _btnNewEmptySet.VAlign = 0.5f;
            _savedHeaderRow.Append(_btnNewEmptySet);

            _savedPanel = new UIPanel
            {
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            Append(_savedPanel);

            _schemeList = new UIList();
            _schemeList.ListPadding = 4f;
            _savedPanel.Append(_schemeList);

            _schemeScroll = new EojUIScrollbar(GridScrollWidth);
            _schemeList.SetScrollbar(_schemeScroll);
            _savedPanel.Append(_schemeScroll);

            RefreshFromPlayer();
            RefreshMaterialFoldVisibility();
            RelayoutContent(top);
        }

        private void BuildToolbar(float left, float top)
        {
            const float querySlotSize = FurnitureBlueprintPageLayout.QuerySlotSize;
            const float materialSlotSize = FurnitureBlueprintPageLayout.MaterialSlotSize;
            const float hintLeft = querySlotSize - 4f + 15f;

            _toolbar = new UIPanel
            {
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            _toolbar.SetPadding(0);
            _toolbar.Left.Set(left, 0);
            _toolbar.Top.Set(top, 0);
            _toolbar.Width.Set(ContentWidthMargin, 1f);
            _toolbar.Height.Set(FurnitureBlueprintPageLayout.ToolbarHeight, 0f);
            Append(_toolbar);

            _queryRow = new UIElement();
            _queryRow.Height.Set(FurnitureBlueprintPageLayout.QueryRowHeight, 0f);
            _queryRow.Width.Set(0f, 1f);
            _toolbar.Append(_queryRow);

            _seedSlot = new BlueprintSeedSlot { ReturnPhysicalOnPlace = true, ReturnPhysicalOnClear = false };
            _seedSlot.Width.Set(querySlotSize, 0);
            _seedSlot.Height.Set(querySlotSize, 0);
            _seedSlot.OnItemChanged += OnSeedChanged;
            _queryRow.Append(_seedSlot);

            _queryHintText = new UIText(EOPJText.UIOr("Blueprint.DragSeedHint", "拖入物品查询家具套组"), 1.4f);
            _queryHintText.TextOriginX = 0f;
            _queryHintText.Left.Set(hintLeft, 0);
            _queryHintText.VAlign = 0.5f;
            _queryHintText.Width.Set(240f, 0);
            _queryHintText.IgnoresMouseInteraction = true;
            _queryRow.Append(_queryHintText);

            float materialTop = FurnitureBlueprintPageLayout.QueryRowHeight + FurnitureBlueprintPageLayout.ToolbarGap;
            _materialRow = new UIPanel
            {
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            _materialRow.Top.Set(materialTop, 0f);
            _materialRow.Width.Set(0f, 1f);
            _materialRow.Height.Set(FurnitureBlueprintPageLayout.MaterialRowHeight, 0f);
            _materialRow.SetPadding(0);
            _toolbar.Append(_materialRow);

            _materialSlot = new BlueprintSeedSlot { ReturnPhysicalOnPlace = true, ReturnPhysicalOnClear = true };
            _materialSlot.VAlign = 0.5f;
            _materialSlot.Width.Set(materialSlotSize, 0);
            _materialSlot.Height.Set(materialSlotSize, 0);
            _materialSlot.OnItemChanged += OnMaterialBlockChanged;
            _materialRow.Append(_materialSlot);

            float foldLeft = materialSlotSize + FurnitureBlueprintPageLayout.MaterialFoldButtonGap;
            _materialFoldBtn = new BlueprintUiFoldButton(() => _shell?.BlueprintMaterialPanel?.IsOpen == true);
            _materialFoldBtn.Left.Set(foldLeft, 0);
            _materialFoldBtn.Top.Set((materialSlotSize - FurnitureBlueprintPageLayout.MaterialFoldButtonSize) * 0.5f, 0);
            _materialFoldBtn.Width.Set(FurnitureBlueprintPageLayout.MaterialFoldButtonSize, 0);
            _materialFoldBtn.Height.Set(FurnitureBlueprintPageLayout.MaterialFoldButtonSize, 0);
            _materialFoldBtn.OnLeftClick += (_, _) => ToggleMaterialPicker();
            _materialRow.Append(_materialFoldBtn);

            float btnW = FurnitureBlueprintPageLayout.HubActionButtonWidth;
            float btnH = FurnitureBlueprintPageLayout.HubActionButtonHeight;
            float btnGap = FurnitureBlueprintPageLayout.HubActionButtonGap;
            float actionLeft = foldLeft + FurnitureBlueprintPageLayout.MaterialFoldButtonSize
                + FurnitureBlueprintPageLayout.ToolbarActionGapAfterFold;

            var templateBtn = MakeHubButton(actionLeft, 0f, btnW, btnH,
                EOPJText.BlueprintOr("BtnBuildingPlan", "建筑方案"),
                ToggleBuildingPlanPanel);
            templateBtn.VAlign = 0.5f;
            _materialRow.Append(templateBtn);

            var saveBtn = MakeHubButton(actionLeft + btnW + btnGap, 0f, btnW, btnH,
                EOPJText.BlueprintOr("BtnSaveSet", "保存套组"),
                OnSaveCurrentScheme);
            saveBtn.VAlign = 0.5f;
            _materialRow.Append(saveBtn);

            if (_shell?.BlueprintMaterialPanel != null)
                _shell.BlueprintMaterialPanel.OnMaterialPicked += OnMaterialPickedFromPicker;
        }

        private static BlueprintRoundedToolbarButton MakeHubButton(
            float left, float top, float w, float h, string label, Action onClick)
        {
            var btn = new BlueprintRoundedToolbarButton(w, h, label, onClick);
            btn.Left.Set(left, 0f);
            btn.Top.Set(top, 0f);
            return btn;
        }

        private static UIText MakeSectionTitle(float left, float top, string key, string fallback)
        {
            return new UIText(EOPJText.UIOr(key, fallback), SectionTitleScale)
            {
                Left = { Pixels = left },
                Top = { Pixels = top },
                TextColor = Color.White
            };
        }

        private void RelayoutContent(float top)
        {
            float left = ContentPadLeft;
            float pageHeight = GetInnerDimensions().Height;

            float statusTop = top + FurnitureBlueprintPageLayout.ToolbarHeight + FurnitureBlueprintPageLayout.HubStatusGap;
            float gridTitleTop = statusTop + FurnitureBlueprintPageLayout.SummaryRowHeight
                + FurnitureBlueprintPageLayout.SavePromptRowHeight + 4f;
            float gridTop = gridTitleTop + FurnitureBlueprintPageLayout.GridHeaderHeight
                + FurnitureBlueprintPageLayout.GridTitleToGridGap;
            float gridHeight = FurnitureBlueprintPageLayout.SlotGridViewportHeight;
            float savedTitleTop = gridTop + gridHeight + FurnitureBlueprintPageLayout.GridSavedGap;
            float savedHeaderH = FurnitureBlueprintPageLayout.GridHeaderHeight;
            float savedTop = savedTitleTop + savedHeaderH;
            float savedHeight = pageHeight - savedTop - OPJourneyShellMetrics.ContentBottomSafeMargin;
            savedHeight = Math.Max(FurnitureBlueprintPageLayout.SavedPanelMinHeight, savedHeight);

            _gridTitle?.Top.Set(gridTitleTop, 0);
            _savePromptText?.Top.Set(statusTop + FurnitureBlueprintPageLayout.SummaryRowHeight, 0);
            _savePromptText?.Left.Set(left, 0);

            _slotGridHost?.Left.Set(left, 0);
            _slotGridHost?.Top.Set(gridTop, 0);
            _slotGridHost?.Width.Set(ContentWidthMargin, 1f);
            _slotGridHost?.Height.Set(gridHeight, 0f);

            _slotGridList?.Left.Set(0f, 0f);
            _slotGridList?.Top.Set(0f, 0f);
            _slotGridList?.Width.Set(-GridScrollWidth, 1f);
            _slotGridList?.Height.Set(0f, 1f);
            if (_slotGridScroll != null)
            {
                _slotGridScroll.HAlign = 1f;
                _slotGridScroll.Height.Set(0f, 1f);
            }

            _savedHeaderRow?.Left.Set(left, 0);
            _savedHeaderRow?.Top.Set(savedTitleTop, 0);
            _savedHeaderRow?.Width.Set(ContentWidthMargin, 1f);
            _savedHeaderRow?.Height.Set(savedHeaderH, 0f);

            _savedPanel?.Left.Set(left, 0);
            _savedPanel?.Top.Set(savedTop, 0);
            _savedPanel?.Width.Set(ContentWidthMargin, 1f);
            _savedPanel?.Height.Set(savedHeight, 0f);

            _schemeList?.Left.Set(0f, 0f);
            _schemeList?.Width.Set(-GridScrollWidth, 1f);
            _schemeList?.Height.Set(0f, 1f);
            if (_schemeScroll != null)
            {
                _schemeScroll.HAlign = 1f;
                _schemeScroll.Height.Set(0f, 1f);
            }

            _slotGrid?.Recalculate();
            Recalculate();
        }

        private void BuildRecognitionSlotCells()
        {
            var cells = new List<UIElement>();
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                FurnitureSlotKind kind = FurnitureSlotKinds.FromIndex(i);
                var slot = new BlueprintSchemeSlot
                {
                    DisplayOnly = true,
                    ReturnPhysicalOnPlace = false,
                    ReturnPhysicalOnClear = false
                };
                _schemeSlots[i] = slot;
                cells.Add(new BlueprintSlotCell(kind, slot));
            }
            _slotGrid.SetCells(cells);
        }

        public void RefreshRecognitionGrid()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            FurnitureScheme display = GetRecognitionDisplayScheme(fb);
            if (display != null)
                PushSchemeToUi(display);
            else
                ClearRecognitionGrid(clearQueryResult: true);
        }

        public void RefreshSchemeList()
        {
            _schemeList?.Clear();
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (_schemeList == null || fb == null)
                return;

            int rowCount = 0;
            foreach (SchemeLibraryEntry entry in FurnitureSchemeLibrary.BuildEntries(fb))
            {
                rowCount++;
                _schemeList.Add(new FurnitureBlueprintSchemeRow(
                    entry,
                    () => fb.SelectedLibrarySchemeId,
                    OnSchemeRowSelect,
                    OnSchemeRowRename,
                    OnSchemeRowEdit,
                    OnSchemeRowDelete));
            }

            if (rowCount == 0)
            {
                _schemeList.Add(new UIText(EOPJText.UIOr("Blueprint.SchemeLibraryEmpty", "保存套组后会显示在此"), BodyTextScale)
                {
                    TextColor = Color.Gray,
                    Width = { Pixels = -GridScrollWidth, Percent = 1f },
                    IsWrapped = true
                });
            }

            RelayoutContent(4f);
            _schemeList?.Recalculate();
            Recalculate();
        }

        private void OnSchemeRowSelect(SchemeLibraryEntry entry)
        {
            if (_shell?.BlueprintTemplatePanel?.IsOpen == true)
            {
                ApplySchemeForBuildingPreview(entry);
                return;
            }

            OpenSchemeForEditing(entry.Id);
        }

        /// <summary>建筑方案窗打开时：套用套组材料并刷新预览，不进入编辑窗。</summary>
        private void ApplySchemeForBuildingPreview(SchemeLibraryEntry entry)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null || string.IsNullOrEmpty(entry.Id))
                return;

            fb.ApplyEntireSchemeById(entry.Id);
            _shell?.BlueprintSetDetailPanel?.SetOpen(false);
            PushSchemeToUi(fb.ActiveScheme);
            SyncMaterialSlotFromPlayer();
            RefreshRecognitionGrid();
            RefreshSummary();
            RefreshSchemeList();
            _shell?.BlueprintTemplatePanel?.SyncFromPlayer();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void OpenSchemeForEditing(string libraryId)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null || string.IsNullOrEmpty(libraryId))
                return;

            fb.SelectedLibrarySchemeId = libraryId;
            CloseOtherBlueprintPanels(exceptDetail: true);
            _shell?.BlueprintSetDetailPanel?.OpenForScheme(libraryId);
            RefreshSchemeList();
            RefreshSummary();
        }

        private void OnSchemeRowRename(SchemeLibraryEntry entry)
        {
            _renameOverlay?.Remove();
            _renameOverlay = new BlueprintSchemeRenameOverlay(
                entry.Id,
                entry.DisplayName,
                OnRenameConfirmed,
                () => _renameOverlay = null);
            Append(_renameOverlay);
        }

        private void OnRenameConfirmed(string id, string newName)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;
            fb.RenameCustomScheme(id, newName);
            RefreshSchemeList();
            RefreshSummary();
        }

        private void OnSchemeRowEdit(SchemeLibraryEntry entry) => OpenSchemeForEditing(entry.Id);

        private void OnSchemeRowDelete(SchemeLibraryEntry entry)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            fb.DeleteCustomScheme(entry.Id);
            RefreshSchemeList();
            RefreshSummary();
        }

        private void OnCreateEmptySet()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            FurnitureScheme draft = fb.CreateEmptySchemeDraft();
            RefreshSchemeList();
            RefreshSummary();
            CloseOtherBlueprintPanels(exceptDetail: true);
            _shell?.BlueprintSetDetailPanel?.OpenForNewSession(draft);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void OnSaveCurrentScheme()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            CloseOtherBlueprintPanels(exceptDetail: true);

            if (_shell?.BlueprintSetDetailPanel == null || fb == null)
                return;

            var detail = _shell.BlueprintSetDetailPanel;
            if (detail.IsOpen && detail.IsEditingExistingLibraryEntry && !fb.RecognitionAwaitingSave)
            {
                detail.SetOpen(true);
                return;
            }

            FurnitureScheme source = SchemeForHubSave(fb);
            detail.OpenForNewSession(source);
        }

        private static FurnitureScheme SchemeForHubSave(FurnitureBlueprintPlayer fb)
        {
            if (CountSchemeSlots(fb.QueryResultScheme) > 0)
                return fb.QueryResultScheme;
            return fb.ActiveScheme;
        }

        private void ToggleBuildingPlanPanel()
        {
            if (_shell == null)
                return;
            CloseOtherBlueprintPanels(exceptTemplate: true);
            _shell.BlueprintMaterialPanel?.SetOpen(false);
            bool open = !(_shell.BlueprintTemplatePanel?.IsOpen ?? false);
            _shell.BlueprintTemplatePanel?.SetOpen(open);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void CloseOtherBlueprintPanels(
            bool exceptDetail = false,
            bool exceptTemplate = false)
        {
            _shell?.BlueprintSetLibraryPanel?.SetOpen(false);
            if (!exceptDetail)
                _shell?.BlueprintSetDetailPanel?.SetOpen(false);
            if (!exceptTemplate)
                _shell?.BlueprintTemplatePanel?.SetOpen(false);
        }

        private void SyncMaterialSlotFromPlayer()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null || fb.PendingMaterialBlock <= ItemID.None)
                return;

            if (_materialSlot.item == null || _materialSlot.item.type != fb.PendingMaterialBlock)
            {
                _materialSlot.item = new Item();
                _materialSlot.item.SetDefaults(fb.PendingMaterialBlock);
            }
        }

        private void ClearRecognitionGrid(bool clearQueryResult = true)
        {
            if (clearQueryResult)
                Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>()?.ClearQueryResultScheme();
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
                _schemeSlots[i].item = new Item();
        }

        /// <summary>识别区展示用：优先最近一次识别结果，否则用当前激活套组（含已保存套组）。</summary>
        private static FurnitureScheme GetRecognitionDisplayScheme(FurnitureBlueprintPlayer fb)
        {
            if (fb == null)
                return null;
            if (CountSchemeSlots(fb.QueryResultScheme) > 0)
                return fb.QueryResultScheme;
            if (CountSchemeSlots(fb.ActiveScheme) > 0)
                return fb.ActiveScheme;
            return null;
        }

        private void PushSchemeToUi(FurnitureScheme scheme)
        {
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                int type = scheme.SlotItemTypes[i];
                _schemeSlots[i].item = new Item();
                if (type > ItemID.None)
                    _schemeSlots[i].item.SetDefaults(type);
            }
        }

        public void RefreshSummary()
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null || _statusText == null)
                return;

            int filled = CountSchemeSlots(GetRecognitionDisplayScheme(fb) ?? fb.ActiveScheme);
            string seedName = _seedSlot?.item != null && !_seedSlot.item.IsAir
                ? _seedSlot.item.Name
                : EOPJText.UIOr("Blueprint.HubNoSeed", "未设置");
            string format = EOPJText.UIOr("Blueprint.HubStatusFormat", "识别：{0} → {1}/{2}");
            _statusText.SetText(format
                .Replace("{0}", seedName)
                .Replace("{1}", filled.ToString())
                .Replace("{2}", FurnitureWikiSlots.TotalCount.ToString()));
            _statusText.TextColor = Color.LightGray;

            if (_savePromptText != null)
            {
                if (fb.RecognitionAwaitingSave && filled > 0)
                {
                    _savePromptText.SetText(EOPJText.BlueprintOr(
                        "StatusSavePrompt",
                        "识别完成 — 点击材料行「保存套组」以保留当前结果。"));
                    _savePromptText.TextColor = Color.Khaki;
                }
                else
                    _savePromptText.SetText("");
            }
        }

        private static string TruncateForHint(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            const int max = 26;
            return name.Length <= max ? name : name.Substring(0, max - 1) + "\u2026";
        }

        private void UpdateQueryHint(Item item)
        {
            if (_queryHintText == null)
                return;
            _queryHintText.SetText(item == null || item.IsAir
                ? EOPJText.UIOr("Blueprint.DragSeedHint", "拖入物品查询家具套组")
                : TruncateForHint(item.Name));
        }

        private void RefreshMaterialFoldVisibility()
        {
            if (_materialFoldBtn == null)
                return;
            int seed = _seedSlot?.item?.type ?? ItemID.None;
            var candidates = FurnitureReverseAnchorResolver.GetMaterialCandidatesForSeed(seed);
            _materialFoldBtn.IgnoresMouseInteraction = !(seed > ItemID.None && candidates.Count > 1);
        }

        private void OnSeedChanged(Item item)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            try
            {
                fb.SelectedLibrarySchemeId = "";
                _shell?.BlueprintSetDetailPanel?.NotifyWorkspaceSeedChanged();
                fb.CancelQueuedRecognition();
                UpdateQueryHint(item);

                if (item == null || item.IsAir)
                {
                    fb.AwaitingMaterialConfirm = false;
                    fb.PendingSeedType = ItemID.None;
                    fb.PendingMaterialBlock = ItemID.None;
                    fb.RecognitionAwaitingSave = false;
                    _materialSlot.item = new Item();
                    _shell?.BlueprintMaterialPanel?.SetOpen(false);
                    RefreshMaterialCandidates();
                    ClearRecognitionGrid();
                    RefreshSchemeList();
                    RefreshMaterialFoldVisibility();
                    RefreshSummary();
                    return;
                }

                ClearRecognitionGrid();
                fb.QueueSeedProbe(item.type);
                RefreshSummary();
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"seed changed failed type={item?.type ?? ItemID.None}: {ex}");
            }
        }

        private void RefreshMaterialCandidates()
        {
            int seed = _seedSlot.item?.type ?? ItemID.None;
            _shell?.BlueprintMaterialPanel?.SetCandidates(
                FurnitureReverseAnchorResolver.GetMaterialCandidatesForSeed(seed));
        }

        private void ToggleMaterialPicker()
        {
            if (_shell?.BlueprintMaterialPanel == null)
                return;

            int seed = _seedSlot.item?.type ?? ItemID.None;
            if (seed <= ItemID.None)
                return;

            CloseOtherBlueprintPanels();
            _shell.BlueprintMaterialPanel.SetCandidates(
                FurnitureReverseAnchorResolver.GetMaterialCandidatesForSeed(seed));
            _shell.BlueprintMaterialPanel.SetOpen(!_shell.BlueprintMaterialPanel.IsOpen);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void OnMaterialPickedFromPicker(int itemType)
        {
            if (itemType <= ItemID.None)
                return;

            _shell?.BlueprintMaterialPanel?.SetOpen(false);
            _materialSlot.item = new Item();
            _materialSlot.item.SetDefaults(itemType);
            OnMaterialBlockChanged(_materialSlot.item);
        }

        private void OnMaterialBlockChanged(Item item)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            int block = item == null || item.IsAir ? ItemID.None : item.type;
            int seed = fb.PendingSeedType > ItemID.None ? fb.PendingSeedType : _seedSlot.item?.type ?? ItemID.None;
            if (block > ItemID.None && seed > ItemID.None)
            {
                block = FurnitureVanillaLivingWoodBridge.RedirectReverseAnchor(seed, block);
                block = FurnitureSetMaterialRules.ResolveModMaterialBlock(seed, block);
                FurnitureSetMaterialRules.ApplyLivingWoodRecipeMaterial(seed, ref block);
            }

            fb.PendingMaterialBlock = block;

            if (block <= ItemID.None)
            {
                ClearRecognitionGrid();
                RefreshSummary();
                return;
            }

            ClearRecognitionGrid();
            if (_materialSlot.item?.type != block)
            {
                _materialSlot.item = new Item();
                _materialSlot.item.SetDefaults(block);
            }

            if (seed > ItemID.None)
                ApplyAutoRecognition(seed, block);
            else
                RefreshSummary();
        }

        private void ApplyAutoFromSeed()
        {
            int seed = _seedSlot.item?.type ?? ItemID.None;
            if (seed <= ItemID.None)
                return;

            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            fb?.QueueSeedProbe(seed);
        }

        private void ApplyDeferredSeedProbeToUi(FurnitureBlueprintPlayer fb)
        {
            int seed = fb.PendingSeedType;
            int block = fb.PendingMaterialBlock;

            _materialSlot.item = new Item();
            if (block > ItemID.None)
                _materialSlot.item.SetDefaults(block);

            RefreshMaterialCandidates();
            RefreshMaterialFoldVisibility();

            IReadOnlyList<int> candidates = fb.SeedProbeMaterialCandidates;
            if (candidates != null && candidates.Count > 1)
            {
                CloseOtherBlueprintPanels();
                _shell?.BlueprintMaterialPanel?.SetCandidates(new List<int>(candidates));
            }
            else
                _shell?.BlueprintMaterialPanel?.SetOpen(false);

            if (fb.SeedProbeOpenMaterialPicker)
            {
                CloseOtherBlueprintPanels();
                if (candidates != null)
                    _shell?.BlueprintMaterialPanel?.SetCandidates(new List<int>(candidates));
                _shell?.BlueprintMaterialPanel?.SetOpen(true);
            }

            if (fb.NeedsBlueprintUiRefresh)
            {
                fb.NeedsBlueprintUiRefresh = false;
                SyncMaterialSlotFromPlayer();
                RefreshRecognitionGrid();
            }

            RefreshSummary();
        }

        private void ApplyAutoRecognition(int seedType, int anchorBlock)
        {
            FurnitureBlueprintPlayer fbQuick = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fbQuick != null
                && FurnitureSetCacheSystem.TryGetCachedSchemeForItem(
                    seedType, anchorBlock, out FurnitureScheme cached, out int cachedMat))
            {
                int block = anchorBlock > ItemID.None ? anchorBlock : cachedMat;
                FurnitureScheme hit = cached.Clone();
                hit.SeedType = seedType;
                fbQuick.ApplyRecognitionToActive(hit, rememberAsOverlaySource: true);
                fbQuick.SetQueryResultScheme(hit);
                fbQuick.PendingMaterialBlock = block;
                fbQuick.NeedsBlueprintUiRefresh = true;
                RefreshRecognitionGrid();
                FurnitureBlueprintUiBridge.NotifySchemeApplied();
                RefreshSummary();
                return;
            }

            Player player = Main.LocalPlayer;
            FurnitureBlueprintPlayer fb = player?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null)
                return;

            ClearRecognitionGrid();
            fb.QueueRecognition(seedType, anchorBlock);
            RefreshSummary();
        }

        public void RefreshFromPlayer()
        {
            RefreshRecognitionGrid();
            RefreshSchemeList();
            _shell?.BlueprintTemplatePanel?.SyncFromPlayer();
            RefreshSummary();
        }

        public void TryApplyPendingQuickQuery()
        {
            if (Main.dedServ || Main.gameMenu)
                return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;

            OPJourneyPlayer plr = player.GetModPlayer<OPJourneyPlayer>();
            int type = plr.PendingBlueprintQueryType;
            if (type <= ItemID.None)
                return;

            if (type >= ItemLoader.ItemCount || !ContentSamples.ItemsByType.ContainsKey(type))
            {
                plr.PendingBlueprintQueryType = 0;
                return;
            }

            plr.PendingBlueprintQueryType = 0;
            _seedSlot.item = new Item();
            _seedSlot.item.SetDefaults(type);
            OnSeedChanged(_seedSlot.item);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!_triedApplyPendingQuickQuery)
            {
                _triedApplyPendingQuickQuery = true;
                TryApplyPendingQuickQuery();
            }

            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb != null && fb.ClearWorkspaceUiOnNextTick)
            {
                fb.ClearWorkspaceUiOnNextTick = false;
                ClearRecognitionGrid();
                RefreshSummary();
            }

            if (fb != null && fb.NeedsSeedProbeUiApply)
            {
                fb.NeedsSeedProbeUiApply = false;
                ApplyDeferredSeedProbeToUi(fb);
            }

            if (fb != null && fb.NeedsBlueprintUiRefresh)
            {
                fb.NeedsBlueprintUiRefresh = false;
                SyncMaterialSlotFromPlayer();
                RefreshRecognitionGrid();
                RefreshSummary();
                if (_shell?.BlueprintSetDetailPanel?.IsOpen == true
                    && !_shell.BlueprintSetDetailPanel.IsEditingExistingLibraryEntry)
                    _shell.BlueprintSetDetailPanel.SyncFromActiveScheme();
            }

            float innerH = GetInnerDimensions().Height;
            if (System.Math.Abs(innerH - _lastLayoutHeight) > 0.5f)
            {
                _lastLayoutHeight = innerH;
                RelayoutContent(4f);
            }
        }

        public void OnShellResized() => RelayoutContent(4f);

        private static int CountSchemeSlots(FurnitureScheme scheme)
        {
            if (scheme?.SlotItemTypes == null)
                return 0;
            int n = 0;
            for (int i = 0; i < scheme.SlotItemTypes.Length; i++)
            {
                if (scheme.SlotItemTypes[i] > ItemID.None)
                    n++;
            }
            return n;
        }
    }
}
