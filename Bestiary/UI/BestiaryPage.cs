using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Bestiary;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Bestiary.Filters;
using EvenMoreOverpoweredJourney.Bestiary.UI.Components;
using EvenMoreOverpoweredJourney.Buffs.UI.Components;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    public class BestiaryPage : UIElement
    {
        private const float ToolbarTop = 40f;
        private const float SearchTop = 78f;
        private const float SearchBarH = 28f;
        private const float StatsGapPx = 10f;
        private const float StatsLineH = 18f;
        private const float StatsBlockTop = SearchTop + SearchBarH + StatsGapPx;
        private static float ListTopBase => StatsBlockTop + StatsLineH + StatsGapPx;
        private const float GridScrollPadRight = OPJourneyShellMetrics.ScrollSafeMarginRight;

        private readonly OPJourneyUI _shell;

        private UIText _titleText;
        private UIText _summaryText;
        private UIBestiaryActiveFilterSummaryRow _activeFilterSummary;
        private UIText _pendingText;
        private UIBuffSearchBar _searchBar;
        private UIBestiaryFaceSelector _faceSelector;
        private BestiaryFilterIconButton _filterBtn;
        private UIElement _toolbarRow;
        private UIList _gridList;
        private UIScrollbar _scrollbar;

        private float _lastGridInnerW = -1f;
        private bool _catalogRefreshAttempted;
        private readonly List<BestiaryNpcMeta> _displayed = new List<BestiaryNpcMeta>();
        private readonly List<UIBestiaryNpcCard> _cardPool = new List<UIBestiaryNpcCard>();
        private readonly List<UIElement> _rowPool = new List<UIElement>();

        private int _columns = 5;
        private float _rowH;
        private float _gridOffsetX;
        private UIPanel _gridBackdrop;
        private bool _listDirty = true;
        private bool _appearanceDirty;
        private int _chromeRecoverCooldown;
        private BestiaryFaceMode _lastBuiltFace = (BestiaryFaceMode)(-1);

        public BestiaryPage(OPJourneyUI shell)
        {
            _shell = shell;
            Width.Set(0, 1f);
            Height.Set(0, 1f);
            BestiaryUiAssets.EnsureLoaded();
            BuildChrome();
            RefreshSummary();
            RebuildActiveFilterSummary();
            _listDirty = true;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawGridBackdrop(spriteBatch);
            base.Draw(spriteBatch);
        }

        private void DrawGridBackdrop(SpriteBatch spriteBatch)
        {
            if (_gridBackdrop == null)
                return;

            CalculatedStyle dims = _gridBackdrop.GetDimensions();
            if (dims.Width < 8f || dims.Height < 8f)
            {
                RestoreGridChromeLayout();
                dims = _gridBackdrop.GetDimensions();
            }

            if (dims.Width < 8f || dims.Height < 8f)
                return;

            Rectangle rect = dims.ToRectangle();
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, _gridBackdrop.BackgroundColor);
        }

        private void BuildChrome()
        {
            _titleText = new UIText(EOPJText.UI("BestiaryTitle"), 1.4f);
            _titleText.Left.Set(5, 0);
            _titleText.Top.Set(6, 0);
            _titleText.IgnoresMouseInteraction = true;
            Append(_titleText);

            const float toolbarRowH = 28f;
            _toolbarRow = new UIElement();
            _toolbarRow.Left.Set(8, 0);
            _toolbarRow.Top.Set(ToolbarTop, 0);
            _toolbarRow.Width.Set(OPJourneyShellMetrics.ChromeWidth, 0);
            _toolbarRow.Height.Set(toolbarRowH, 0);
            Append(_toolbarRow);

            _faceSelector = new UIBestiaryFaceSelector(20f);
            _faceSelector.Left.Set(0, 0);
            _faceSelector.Top.Set(0, 0);
            _faceSelector.ActiveFace = _shell.BestiaryFaceMode;
            _faceSelector.OnFaceSelected = face =>
            {
                _shell.BestiaryFaceMode = face;
                _appearanceDirty = true;
                _listDirty = true;
                RefreshSummary();
                RebuildActiveFilterSummary();
            };
            _toolbarRow.Append(_faceSelector);

            _searchBar = new UIBuffSearchBar();
            _searchBar.SearchHint = EOPJText.UI("BestiarySearchHint");
            _searchBar.Left.Set(8, 0);
            _searchBar.Top.Set(SearchTop, 0);
            _searchBar.Width.Set(OPJourneyShellMetrics.ChromeWidth, 0);
            _searchBar.Height.Set(28, 0);
            if (!string.IsNullOrEmpty(_shell.BestiarySearchQueryText))
                _searchBar.InnerSearchBar.SetContents(_shell.BestiarySearchQueryText);
            _searchBar.OnTextChanged += t =>
            {
                _shell.BestiarySearchQueryText = t ?? "";
                _listDirty = true;
                RefreshSummary();
            };
            Append(_searchBar);

            _summaryText = new UIText("", 0.68f);
            _summaryText.Left.Set(8, 0);
            _summaryText.Top.Set(StatsBlockTop, 0);
            _summaryText.Width.Set(0, 1f);
            _summaryText.Height.Set(StatsLineH, 0);
            _summaryText.IsWrapped = false;
            _summaryText.TextColor = OPJourneyUiColors.TextMuted;
            _summaryText.IgnoresMouseInteraction = true;
            Append(_summaryText);

            _activeFilterSummary = new UIBestiaryActiveFilterSummaryRow(_shell);
            _activeFilterSummary.Left.Set(8, 0);
            _activeFilterSummary.Width.Set(OPJourneyShellMetrics.ChromeWidth, 0);
            _activeFilterSummary.IgnoresMouseInteraction = false;
            Append(_activeFilterSummary);

            _gridBackdrop = new UIPanel();
            _gridBackdrop.BackgroundColor = BestiaryUiColors.GridBackdrop;
            _gridBackdrop.BorderColor = Color.Transparent;
            _gridBackdrop.Left.Set(0, 0);
            _gridBackdrop.Width.Set(-GridScrollPadRight, 1f);
            _gridBackdrop.IgnoresMouseInteraction = true;
            Append(_gridBackdrop);

            _gridList = new UIList();
            _gridList.ManualSortMethod = _ => { };
            _gridList.Left.Set(0, 0);
            _gridList.Width.Set(-GridScrollPadRight, 1f);
            _gridList.OnUpdate += _ =>
            {
                float w = _gridList.GetInnerDimensions().Width;
                if (w > 1f && (_listDirty || Math.Abs(w - _lastGridInnerW) > 8f))
                    RebuildGrid(w);
            };
            Append(_gridList);

            _scrollbar = new UIScrollbar();
            _scrollbar.Left.Set(-GridScrollPadRight, 1f);
            ApplyListTopLayout();
            _gridList.SetScrollbar(_scrollbar);
            Append(_scrollbar);

            _pendingText = new UIText("", 0.72f);
            _pendingText.Left.Set(8, 0);
            _pendingText.Top.Set(-OPJourneyShellMetrics.ContentBottomSafeMargin, 1f);
            _pendingText.Width.Set(0, 1f);
            _pendingText.TextColor = OPJourneyUiColors.TextHint;
            _pendingText.IgnoresMouseInteraction = true;
            Append(_pendingText);

            _filterBtn = new BestiaryFilterIconButton(_shell, () =>
            {
                bool open = !(_shell.BestiarySecondaryPanel?.IsOpen ?? false);
                _shell.BestiarySecondaryPanel?.SetOpen(open);
                if (open)
                    _shell.BestiarySecondaryPanel?.RebuildScroll();
            });
            Append(_filterBtn);
            LayoutToolbarRow();
        }

        public void CloseDetail() => _shell.CloseBestiaryDetail();

        public void OpenDetail(BestiaryNpcMeta meta) => _shell.OpenBestiaryDetail(meta);

        private void RestoreGridChromeLayout()
        {
            _gridBackdrop.Width.Set(-GridScrollPadRight, 1f);
            _gridList.Width.Set(-GridScrollPadRight, 1f);
            _gridList.IgnoresMouseInteraction = false;
            _scrollbar.Width.Set(18f, 0f);
            _scrollbar.Left.Set(-GridScrollPadRight, 1f);
            _scrollbar.IgnoresMouseInteraction = false;
            ApplyListTopLayout();
        }

        private float GetListTop()
        {
            float top = ListTopBase;
            if (_activeFilterSummary != null && _activeFilterSummary.HasChips)
                top += UIBestiaryActiveFilterSummaryRow.RowHeight + StatsGapPx;
            return top;
        }

        private void RebuildActiveFilterSummary()
        {
            if (_activeFilterSummary == null)
                return;

            float w = _activeFilterSummary.GetInnerDimensions().Width;
            if (w < 40f)
                w = OPJourneyShellMetrics.ChromeWidth;
            _activeFilterSummary.Rebuild(w);

            float summaryTop = StatsBlockTop + StatsLineH + StatsGapPx;
            if (_activeFilterSummary.HasChips)
            {
                _activeFilterSummary.Top.Set(summaryTop, 0);
                _activeFilterSummary.Height.Set(UIBestiaryActiveFilterSummaryRow.RowHeight, 0);
                _activeFilterSummary.Width.Set(OPJourneyShellMetrics.ChromeWidth, 0);
            }
            else
            {
                _activeFilterSummary.Top.Set(summaryTop, 0);
                _activeFilterSummary.Height.Set(0, 0);
            }

            ApplyListTopLayout();
        }

        private void ApplyListTopLayout()
        {
            float listTop = GetListTop();
            float extraBottom = GetListExtraBottom();
            ShellUiScrollLayout.ApplyVerticalRange(_gridBackdrop, null, listTop, extraBottom);
            ShellUiScrollLayout.ApplyVerticalRange(_gridList, _scrollbar, listTop, extraBottom);
        }

        private float GetListExtraBottom()
        {
            if (_pendingText == null || string.IsNullOrEmpty(_pendingText.Text))
                return 0f;

            return StatsLineH + StatsGapPx;
        }

        public void OnFiltersChanged()
        {
            _listDirty = true;
            RefreshSummary();
            RebuildActiveFilterSummary();
        }

        private void LayoutToolbarRow()
        {
            if (_filterBtn == null)
                return;

            const float toolbarInsetLeft = 8f;
            const float faceHeight = 20f;
            const float rightSafePad = 12f;
            float filterLeft = toolbarInsetLeft + OPJourneyShellMetrics.ChromeWidth - BestiaryFilterIconButton.OuterSize - rightSafePad;
            _filterBtn.Left.Set(filterLeft, 0f);
            _filterBtn.Top.Set(ToolbarTop + faceHeight - BestiaryFilterIconButton.OuterSize, 0f);
        }

        public void OnShellResized()
        {
            Recalculate();
            LayoutToolbarRow();
            RebuildActiveFilterSummary();
            RestoreGridChromeLayout();
            _listDirty = true;
        }

        /// <summary>防止布局被意外置零后蓝色底板消失（如详情关闭、重建列表时）。</summary>
        private void EnsureGridChromeVisible()
        {
            CalculatedStyle bd = _gridBackdrop.GetDimensions();
            float listH = _gridList.GetDimensions().Height;
            if (bd.Height >= 8f && bd.Width >= 8f && listH >= 8f)
            {
                _chromeRecoverCooldown = 0;
                return;
            }

            if (_chromeRecoverCooldown > 0)
            {
                _chromeRecoverCooldown--;
                return;
            }

            RestoreGridChromeLayout();
            _chromeRecoverCooldown = 30;
            if (_gridList.GetDimensions().Height < 8f)
                _listDirty = true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!_catalogRefreshAttempted || BestiaryListCatalog.BestiaryDbEntryCount != (Main.BestiaryDB?.Entries?.Count ?? 0))
            {
                _catalogRefreshAttempted = true;
                BestiaryListCatalog.EnsureFresh();
                BestiaryFilterIndex.Rebuild();
                _listDirty = true;
                RefreshSummary();
            }

            EnsureGridChromeVisible();

            float gridW = _gridList.GetInnerDimensions().Width;
            if (gridW > 1f)
            {
                if (_listDirty)
                    RebuildGrid(gridW);
                else if (_appearanceDirty)
                    RefreshCardAppearances(gridW);
            }
        }

        private void RefreshCardAppearances(float innerWidth)
        {
            _appearanceDirty = false;

            if (_displayed.Count == 0 || _gridList.Count == 0)
            {
                _listDirty = true;
                return;
            }

            if (_lastBuiltFace != _shell.BestiaryFaceMode || Math.Abs(innerWidth - _lastGridInnerW) > 2f)
            {
                _listDirty = true;
                return;
            }

            ComputeCellMetrics(innerWidth);
            for (int i = 0; i < _displayed.Count; i++)
            {
                BestiaryNpcMeta meta = _displayed[i];
                GetCard(i).SetContext(meta, _shell.BestiaryFaceMode);
            }
        }

        private void RebuildGrid(float innerWidth)
        {
            long ms = BestiaryPerfLog.Measure(() => RebuildGridCore(innerWidth));
            BestiaryPerfLog.LogElapsed("grid-rebuild", ms, _displayed.Count);
        }

        private void RebuildGridCore(float innerWidth)
        {
            _listDirty = false;
            _appearanceDirty = false;
            _lastGridInnerW = innerWidth;
            _lastBuiltFace = _shell.BestiaryFaceMode;
            _gridList.Clear();
            RebuildDisplayedList();

            if (_displayed.Count == 0)
            {
                var empty = new UIText(EOPJText.UI("BestiaryNoActiveFilters"), 0.8f);
                empty.HAlign = 0.5f;
                empty.VAlign = 0.35f;
                _gridList.Add(empty);
                return;
            }

            ComputeCellMetrics(innerWidth);
            float cellW = innerWidth / _columns;
            float cardSize = UIBestiaryNpcCard.VanillaSlotPx;

            for (int i = 0; i < _displayed.Count; i++)
            {
                int rowIndex = i / _columns;
                int col = i % _columns;
                UIElement row = GetRow(rowIndex);
                if (col == 0)
                {
                    float gridWidth = _columns * cellW;
                    row.Width.Set(gridWidth, 0f);
                    row.Left.Set(_gridOffsetX, 0f);
                    row.Height.Set(_rowH, 0);
                    row.RemoveAllChildren();
                    _gridList.Add(row);
                }

                BestiaryNpcMeta meta = _displayed[i];

                UIBestiaryNpcCard card = GetCard(i);
                card.SetContext(meta, _shell.BestiaryFaceMode);
                card.OnOpenDetail = () => OpenDetail(meta);
                card.Left.Set(col * cellW + (cellW - cardSize) * 0.5f, 0);
                row.Append(card);
            }
        }

        private void ComputeCellMetrics(float innerWidth)
        {
            float slotPx = UIBestiaryNpcCard.VanillaSlotPx;
            _columns = Math.Max(1, (int)(innerWidth / slotPx));
            float cellW = innerWidth / _columns;
            float gridWidth = _columns * cellW;
            _gridOffsetX = Math.Max(0f, (innerWidth - gridWidth) * 0.5f);
            _rowH = UIBestiaryNpcCard.ComputeRowHeight(cellW);
        }

        private UIElement GetRow(int rowIndex)
        {
            while (_rowPool.Count <= rowIndex)
                _rowPool.Add(new UIElement());
            return _rowPool[rowIndex];
        }

        private UIBestiaryNpcCard GetCard(int index)
        {
            while (_cardPool.Count <= index)
            {
                var card = new UIBestiaryNpcCard();
                card.OnLeftClick += (_, el) => OpenDetail(((UIBestiaryNpcCard)el).Meta);
                _cardPool.Add(card);
            }

            return _cardPool[index];
        }

        private void RebuildDisplayedList()
        {
            _displayed.Clear();
            if (!BestiaryListCatalog.Ready)
                return;

            string q = _shell.BestiarySearchQueryText ?? "";
            foreach (BestiaryNpcMeta meta in BestiaryListCatalog.All)
            {
                if (!meta.HasBestiaryEntry)
                    continue;
                if (!BestiaryFilterPredicates.PassesFace(_shell.BestiaryFaceMode, meta))
                    continue;
                if (!BestiaryFilterPredicates.PassesSecondary(_shell.BestiarySecondary, meta))
                    continue;
                if (!BestiaryFilterPredicates.PassesSearch(q, meta))
                    continue;

                _displayed.Add(meta);
            }

            _displayed.Sort(CompareDisplayedMeta);
        }

        private static int CompareDisplayedMeta(BestiaryNpcMeta a, BestiaryNpcMeta b)
        {
            int bySort = BestiaryNpcMetaSort.Compare(a, b);
            if (bySort != 0)
                return bySort;

            int byCatalog = a.CatalogIndex.CompareTo(b.CatalogIndex);
            if (byCatalog != 0)
                return byCatalog;

            return a.NetId.CompareTo(b.NetId);
        }

        public void RefreshSummary()
        {
            _faceSelector.ActiveFace = _shell.BestiaryFaceMode;
            RebuildActiveFilterSummary();

            if (!BestiaryListCatalog.Ready)
            {
                _summaryText.SetText(EOPJText.UI("BestiaryCatalogLoading"));
                _pendingText.SetText("");
                return;
            }

            int total = BestiaryListCatalog.BestiaryDbEntryCount;
            switch (_shell.BestiaryFaceMode)
            {
                case BestiaryFaceMode.ProgressiveMinus:
                    _summaryText.SetText(EOPJText.UIFormat(
                        "BestiaryFaceSummary_DiscoveredOnly",
                        CountDiscoveredInScope(),
                        total));
                    break;
                case BestiaryFaceMode.UnlockedOnly:
                    _summaryText.SetText(EOPJText.UIFormat(
                        "BestiaryFaceSummary_UnlockedOnly",
                        CountUndiscoveredInScope(),
                        total));
                    break;
                default:
                    int visible = CountVisibleInList();
                    int found = CountFoundInCurrentView();
                    int missing = Math.Max(0, visible - found);
                    float foundPct = total > 0 ? found * 100f / total : 0f;
                    _summaryText.SetText(EOPJText.UIFormat("BestiaryCollectionSummary", found, foundPct, missing, visible));
                    break;
            }

            int pending = CountPendingDiscovery();
            _pendingText.SetText(pending > 0 && _shell.BestiaryFaceMode == BestiaryFaceMode.ProgressiveMinus
                ? EOPJText.UIFormat("BestiaryPendingDiscoveryFmt", pending)
                : "");
            ApplyListTopLayout();
        }

        private int CountDiscoveredInScope() => CountInScope(requireFound: true);

        private int CountUndiscoveredInScope() => CountInScope(requireFound: false, countUndiscoveredOnly: true);

        private int CountInScope(bool requireFound, bool countUndiscoveredOnly = false)
        {
            int n = 0;
            string q = _shell.BestiarySearchQueryText ?? "";
            foreach (BestiaryNpcMeta meta in BestiaryListCatalog.All)
            {
                if (!meta.HasBestiaryEntry)
                    continue;
                if (!BestiaryFilterPredicates.PassesSecondary(_shell.BestiarySecondary, meta))
                    continue;
                if (!BestiaryFilterPredicates.PassesSearch(q, meta))
                    continue;

                bool found = meta.Entry != null && BestiaryProgressResolver.WasEverFound(meta.Entry);
                if (countUndiscoveredOnly)
                {
                    if (!found)
                        n++;
                }
                else if (requireFound && found)
                {
                    n++;
                }
            }

            return n;
        }

        private int CountPendingDiscovery()
        {
            int n = 0;
            string q = _shell.BestiarySearchQueryText ?? "";
            foreach (BestiaryNpcMeta meta in BestiaryListCatalog.All)
            {
                if (!meta.HasBestiaryEntry)
                    continue;
                if (!BestiaryFilterPredicates.PassesSecondary(_shell.BestiarySecondary, meta))
                    continue;
                if (!BestiaryFilterPredicates.PassesSearch(q, meta))
                    continue;
                if (meta.Entry == null || BestiaryProgressResolver.WasEverFound(meta.Entry))
                    continue;
                n++;
            }

            return n;
        }

        private int CountVisibleInList() => CountInCurrentView(requireFound: false);

        private int CountFoundInCurrentView() => CountInCurrentView(requireFound: true);

        private int CountInCurrentView(bool requireFound)
        {
            int n = 0;
            string q = _shell.BestiarySearchQueryText ?? "";
            foreach (BestiaryNpcMeta meta in BestiaryListCatalog.All)
            {
                if (!meta.HasBestiaryEntry)
                    continue;
                if (!BestiaryFilterPredicates.PassesFace(_shell.BestiaryFaceMode, meta))
                    continue;
                if (!BestiaryFilterPredicates.PassesSecondary(_shell.BestiarySecondary, meta))
                    continue;
                if (!BestiaryFilterPredicates.PassesSearch(q, meta))
                    continue;
                if (requireFound && (meta.Entry == null || !BestiaryProgressResolver.WasEverFound(meta.Entry)))
                    continue;
                n++;
            }

            return n;
        }
    }
}
