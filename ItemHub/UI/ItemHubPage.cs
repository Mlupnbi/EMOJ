using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.ItemHub.Rules;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using EvenMoreOverpoweredJourney.Buffs.UI.Components;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.ItemHub.UI
{
    internal static class ItemHubUiTextureHelper
    {
        internal static Texture2D TryLoad(Mod mod, string assetPath) =>
            EojUiTextureCache.TryLoadFirst(assetPath);

        internal static Texture2D TryLoadFirst(Mod mod, params string[] paths) =>
            EojUiTextureCache.TryLoadFirst(paths);

        internal static Texture2D TryLoadSortDirection(Mod mod, bool descending)
        {
            EojUiTextureCache.WarmTab(EojUiTab.ItemHub);
            return descending ? EojUiTextures.ItemHub.SortDesc : EojUiTextures.ItemHub.SortAsc;
        }

        internal static Texture2D FilterButton
        {
            get
            {
                EojUiTextureCache.WarmTab(EojUiTab.ItemHub);
                return EojUiTextures.ItemHub.FilterButton;
            }
        }
    }

    internal sealed class ItemHubSortLabelElement : UIElement
    {
        private string _text = "";

        public void SetLabel(string text) => _text = text ?? "";

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            if (string.IsNullOrEmpty(_text))
                return;
            CalculatedStyle d = GetDimensions();
            const float textScale = 0.7f;
            var font = FontAssets.MouseText.Value;
            Vector2 ms = font.MeasureString(_text) * textScale;
            float x = d.X + 4f;
            float yMid = d.Y + d.Height * 0.5f;
            float textTop = yMid - ms.Y * 0.5f + 5f * textScale;
            Utils.DrawBorderString(spriteBatch, _text, new Vector2(x, textTop), Color.White, textScale);
        }
    }

    public enum ItemHubSort
    {
        ById,
        ByName,
        ByValue,
        ByRare,
        ByDamage,
        ByDefense
    }

    public class ItemHubPage : UIElement
    {
        private OPJourneyUI _shell;
        private HubSecondaryFilterState Secondary => _shell?.ItemHubSecondary;

        private UIText title;
        private UIText progressTitle;
        private UIText progressLine;
        private UIBuffSearchBar searchBar;
        private ItemHubSortDirectionButton sortDirBtn;
        private UIPanel sortFieldHit;
        private ItemHubSortLabelElement sortFieldTxt;
        private ItemHubViewModeButton viewModeBtn;
        private ItemHubFilterOpenButton filterBtnP;
        private UIList itemList;
        private UIScrollbar scrollbar;

        private string searchText = "";
        private ItemHubSort sortMode = ItemHubSort.ById;
        private bool isCardMode = true;
        private List<int> displayed = new List<int>();
        private float lastListWidth;
        private int _lastCardPerRow = -1;
        private readonly List<UIItemHubCard> _cardPool = new List<UIItemHubCard>();
        private readonly List<UIItemHubRow> _rowPool = new List<UIItemHubRow>();
        private readonly List<UIElement> _cardRowCache = new List<UIElement>();

        private int _displayCacheSig = int.MinValue;
        private bool _pendingListAfterRegistry = true;
        private bool _isListDirty = true;
        private int _dirtyWaitFrames;
        private bool _isBuildingList;
        private int _buildProgressIndex;
        private float _buildCellW;
        private float _buildRowH;
        private int _buildPerRow = 1;
        private const int BuildBatchSize = 300;

        public ItemHubPage()
        {
            _shell = OPJourneyUI.Instance;
            if (_shell != null)
            {
                sortMode = _shell.ItemHubSortMode;
                isCardMode = _shell.ItemHubCardMode;
                searchText = _shell.HubSearchQueryText ?? "";
            }

            Width.Set(0, 1f);
            Height.Set(0, 1f);

            title = new UIText(EOPJText.UI("ItemHubTitle"), 1.4f);
            title.Left.Set(5, 0);
            title.Top.Set(6, 0);
            Append(title);

            progressTitle = new UIText(EOPJText.UI("ItemHubProgressTitle"), 0.7f * 1.25f);
            progressTitle.Left.Set(0, 0);
            progressTitle.Top.Set(0, 0);
            progressTitle.IgnoresMouseInteraction = true;

            progressLine = new UIText("", 0.7f);
            progressLine.Left.Set(0, 0);
            progressLine.Top.Set(18, 0);
            progressLine.TextColor = Color.Gray;
            progressLine.IgnoresMouseInteraction = true;

            var progressStack = new UIElement();
            progressStack.Left.Set(188, 0);
            progressStack.Top.Set(4, 0);
            progressStack.Width.Set(260, 0);
            progressStack.Height.Set(40, 0);
            progressStack.Append(progressTitle);
            progressStack.Append(progressLine);
            Append(progressStack);

            searchBar = new UIBuffSearchBar();
            searchBar.SearchHint = EOPJText.UI("ItemHubSearchHint");
            searchBar.Left.Set(5, 0);
            searchBar.Top.Set(40, 0);
            searchBar.Width.Set(OPJourneyShellMetrics.ChromeWidth, 0);
            searchBar.Height.Set(30, 0);
            if (!string.IsNullOrEmpty(searchText))
                searchBar.InnerSearchBar.SetContents(searchText);
            searchBar.OnTextChanged += t =>
            {
                searchText = t ?? "";
                if (_shell != null)
                    _shell.HubSearchQueryText = searchText;
                InvalidateDisplayCache();
                RequestListRebuild();
            };
            Append(searchBar);

            const float stripY = 78f;
            const float sortPanelW = 74f * 1.2f;

            sortFieldHit = new UIPanel();
            sortFieldHit.Left.Set(5, 0);
            sortFieldHit.Top.Set(stripY, 0);
            sortFieldHit.Width.Set(sortPanelW, 0);
            sortFieldHit.Height.Set(28, 0);
            sortFieldHit.BackgroundColor = OPJourneyUiColors.ButtonBackground;
            sortFieldHit.BorderColor = OPJourneyUiColors.ButtonBorder;
            sortFieldHit.SetPadding(4);

            sortDirBtn = new ItemHubSortDirectionButton(_shell, () =>
            {
                InvalidateDisplayCache();
                RequestListRebuild();
            });
            sortDirBtn.Left.Set(1, 0);
            sortDirBtn.VAlign = 0.5f;
            sortDirBtn.Width.Set(22, 0);
            sortDirBtn.Height.Set(22, 0);
            sortFieldHit.Append(sortDirBtn);

            sortFieldTxt = new ItemHubSortLabelElement();
            sortFieldTxt.Left.Set(26, 0);
            sortFieldTxt.Top.Set(0, 0);
            sortFieldTxt.Width.Set(-28, 1f);
            sortFieldTxt.Height.Set(0, 1f);
            sortFieldTxt.VAlign = 0.5f;
            sortFieldTxt.HAlign = 0f;
            sortFieldTxt.IgnoresMouseInteraction = true;
            sortFieldHit.Append(sortFieldTxt);

            var sortCycleHit = new UIElement();
            sortCycleHit.Left.Set(26, 0);
            sortCycleHit.Width.Set(-28, 1f);
            sortCycleHit.Height.Set(0, 1f);
            sortCycleHit.OnLeftClick += (_, __) => CycleSort();
            sortFieldHit.Append(sortCycleHit);

            Append(sortFieldHit);

            filterBtnP = new ItemHubFilterOpenButton(_shell, ToggleFilterPanel);
            filterBtnP.Top.Set(stripY, 0);
            filterBtnP.Height.Set(28, 0);
            Append(filterBtnP);

            viewModeBtn = new ItemHubViewModeButton(_shell, ToggleMode);
            viewModeBtn.Top.Set(stripY, 0);
            Append(viewModeBtn);

            const float listTop = 114f;
            itemList = new UIList();
            itemList.ManualSortMethod = _ => { };
            itemList.Left.Set(0, 0);
            itemList.Width.Set(-OPJourneyShellMetrics.ScrollSafeMarginRight, 1f);
            itemList.OnUpdate += el =>
            {
                float w = el.GetInnerDimensions().Width;
                if (w <= 1f)
                    return;
                if (isCardMode)
                {
                    int pr = ComputePerRowForWidth(w);
                    if (pr != _lastCardPerRow)
                    {
                        _lastCardPerRow = pr;
                        lastListWidth = w;
                        RequestListRebuild();
                    }
                }
                else if (lastListWidth <= 0f || Math.Abs(lastListWidth - w) > 120f)
                {
                    lastListWidth = w;
                    RequestListRebuild();
                }
            };
            Append(itemList);

            scrollbar = new UIScrollbar();
            scrollbar.Left.Set(-OPJourneyShellMetrics.ScrollSafeMarginRight, 1f);
            ShellUiScrollLayout.ApplyVerticalRange(itemList, scrollbar, listTop);
            itemList.SetScrollbar(scrollbar);
            Append(scrollbar);

            LayoutStripButtons();
            RefreshButtonLabels();
            lastListWidth = 0f;
            RequestListRebuild();
        }

        private void LayoutStripButtons()
        {
            const float stripY = 78f;
            const float sortPanelW = 74f * 1.2f;
            float afterSort = 5f + sortPanelW + 4f;
            filterBtnP?.Left.Set(afterSort, 0);
            filterBtnP?.RecalculateFilterWidth();
            float fw = filterBtnP != null ? Math.Max(filterBtnP.Width.Pixels, 48f) : 100f;
            viewModeBtn?.Left.Set(afterSort + fw + 6f, 0);
            viewModeBtn?.Top.Set(stripY, 0);
        }

        public void OnShellResized()
        {
            Recalculate();
            lastListWidth = 0f;
            _lastCardPerRow = -1;
            RequestListRebuild();
        }

        public void OnAdvancedFiltersChanged()
        {
            InvalidateDisplayCache();
            RequestListRebuild();
            RefreshButtonLabels();
        }

        private void InvalidateDisplayCache() => _displayCacheSig = int.MinValue;

        private int ComputeCacheSig()
        {
            unchecked
            {
                int h = Main.myPlayer;
                h = h * 397 ^ sortMode.GetHashCode();
                h = h * 397 ^ (_shell != null && _shell.ItemHubSortDescending ? 31 : 17);
                h = h * 397 ^ (isCardMode ? 1 : 0);
                h = h * 397 ^ (searchText?.GetHashCode() ?? 0);
                h = h * 397 ^ (Secondary?.ComputeHash() ?? 0);
                return h;
            }
        }

        private int ComputePerRowForWidth(float listInnerW)
        {
            const float slotScale = 0.56f * 1.2f;
            int tw = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Width;
            float cellW = tw * slotScale + 4f;
            return Math.Max(1, (int)(listInnerW / cellW));
        }

        private void CycleSort()
        {
            sortMode = (ItemHubSort)(((int)sortMode + 1) % Enum.GetValues(typeof(ItemHubSort)).Length);
            if (_shell != null)
                _shell.ItemHubSortMode = sortMode;
            InvalidateDisplayCache();
            RefreshButtonLabels();
            RequestListRebuild();
        }

        private void ToggleMode()
        {
            isCardMode = !isCardMode;
            if (_shell != null)
                _shell.ItemHubCardMode = isCardMode;
            _lastCardPerRow = -1;
            lastListWidth = 0f;
            InvalidateDisplayCache();
            RefreshButtonLabels();
            RequestListRebuild();
        }

        private void ToggleFilterPanel()
        {
            ItemHubSecondaryPanel p = _shell?.ItemHubSecondaryPanel;
            if (p == null)
                return;
            p.SetOpen(!p.IsOpen);
        }

        public void RefreshButtonLabels()
        {
            LayoutStripButtons();
            sortFieldTxt?.SetLabel(SortLabel());
            viewModeBtn?.RefreshVisual();
            sortDirBtn?.RefreshVisual();
        }

        private string SortLabel() => sortMode switch
        {
            ItemHubSort.ById => EOPJText.UI("ItemHubSortId"),
            ItemHubSort.ByName => EOPJText.UI("ItemHubSortName"),
            ItemHubSort.ByValue => EOPJText.UI("ItemHubSortValue"),
            ItemHubSort.ByRare => EOPJText.UI("ItemHubSortRare"),
            ItemHubSort.ByDamage => EOPJText.UI("ItemHubSortDamage"),
            ItemHubSort.ByDefense => EOPJText.UI("ItemHubSortDefense"),
            _ => ""
        };

        private List<int> BuildDisplayedList()
        {
            if (!HubCatalog.Ready)
            {
                displayed = new List<int>();
                return displayed;
            }

            int sig = ComputeCacheSig();
            if (sig == _displayCacheSig && displayed.Count > 0)
                return displayed;

            IEnumerable<int> q = HubDisplayQuery.EnumerateVisibleTypes(searchText, Secondary);

            bool desc = _shell != null && _shell.ItemHubSortDescending;
            IOrderedEnumerable<int> ordered = sortMode switch
            {
                ItemHubSort.ById => desc
                    ? q.OrderByDescending(t => t)
                    : q.OrderBy(t => t),
                ItemHubSort.ByName => desc
                    ? q.OrderByDescending(t => HubRegistry.ByType[t].NameLower).ThenByDescending(t => t)
                    : q.OrderBy(t => HubRegistry.ByType[t].NameLower).ThenBy(t => t),
                ItemHubSort.ByValue => desc
                    ? q.OrderBy(t => HubRegistry.ByType[t].Value).ThenBy(t => t)
                    : q.OrderByDescending(t => HubRegistry.ByType[t].Value).ThenBy(t => t),
                ItemHubSort.ByRare => desc
                    ? q.OrderBy(t => HubRegistry.ByType[t].Rare).ThenBy(t => t)
                    : q.OrderByDescending(t => HubRegistry.ByType[t].Rare).ThenBy(t => t),
                ItemHubSort.ByDamage => desc
                    ? q.OrderBy(t => HubRegistry.ByType[t].Damage).ThenBy(t => t)
                    : q.OrderByDescending(t => HubRegistry.ByType[t].Damage).ThenBy(t => t),
                ItemHubSort.ByDefense => desc
                    ? q.OrderBy(t => HubRegistry.ByType[t].Defense).ThenBy(t => t)
                    : q.OrderByDescending(t => HubRegistry.ByType[t].Defense).ThenBy(t => t),
                _ => q.OrderBy(t => t)
            };

            displayed = ordered.ToList();
            _displayCacheSig = sig;
            return displayed;
        }

        private void RequestListRebuild()
        {
            _isListDirty = true;
            _dirtyWaitFrames = 0;
            _isBuildingList = false;
        }

        private void BeginListBuild(float width)
        {
            if (!HubCatalog.Ready)
            {
                itemList.Clear();
                return;
            }

            BuildDisplayedList();
            itemList.Clear();

            const float slotScale = 0.56f * 1.2f;
            int tw = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Width;
            int th = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Height;
            _buildCellW = tw * slotScale + 4f;
            _buildRowH = th * slotScale + 4f;
            _buildPerRow = isCardMode ? ComputePerRowForWidth(width) : 1;
            _lastCardPerRow = _buildPerRow;
            _buildProgressIndex = 0;
            _isBuildingList = true;
        }

        private void ContinueListBuild()
        {
            if (!_isBuildingList)
                return;

            int end = Math.Min(_buildProgressIndex + BuildBatchSize, displayed.Count);
            if (isCardMode)
            {
                for (int i = _buildProgressIndex; i < end; i++)
                {
                    int rowIndex = i / _buildPerRow;
                    int col = i % _buildPerRow;
                    UIElement row = GetCachedCardRow(rowIndex);
                    if (col == 0)
                    {
                        row.Width.Set(0, 1f);
                        row.Height.Set(_buildRowH, 0);
                        row.RemoveAllChildren();
                        itemList.Add(row);
                    }

                    int t = displayed[i];
                    var card = GetCachedCard(i, t, 0.56f * 1.2f, _buildCellW, _buildRowH);
                    card.Left.Set(col * _buildCellW, 0);
                    row.Append(card);
                }
            }
            else
            {
                for (int i = _buildProgressIndex; i < end; i++)
                {
                    int t = displayed[i];
                    var rowItem = GetCachedRow(i, t, 0.56f * 1.2f, _buildRowH);
                    rowItem.Left.Set(0, 0);
                    rowItem.Width.Set(0, 1f);
                    itemList.Add(rowItem);
                }
            }

            _buildProgressIndex = end;
            if (_buildProgressIndex >= displayed.Count)
                _isBuildingList = false;
        }

        private UIElement GetCachedCardRow(int rowIndex)
        {
            while (_cardRowCache.Count <= rowIndex)
                _cardRowCache.Add(new UIElement());
            return _cardRowCache[rowIndex];
        }

        private UIItemHubCard GetCachedCard(int poolIndex, int type, float slotScale, float cellW, float rowH)
        {
            while (_cardPool.Count <= poolIndex)
                _cardPool.Add(new UIItemHubCard());

            UIItemHubCard card = _cardPool[poolIndex];
            card.SetItem(type);
            card.UpdateContext(slotScale, cellW, rowH);
            return card;
        }

        private UIItemHubRow GetCachedRow(int poolIndex, int type, float slotScale, float rowH)
        {
            while (_rowPool.Count <= poolIndex)
                _rowPool.Add(new UIItemHubRow());

            UIItemHubRow row = _rowPool[poolIndex];
            row.SetItem(type);
            row.UpdateContext(slotScale, rowH);
            return row;
        }

        public override void Update(GameTime gameTime)
        {
            if (_pendingListAfterRegistry && HubCatalog.Ready)
            {
                _pendingListAfterRegistry = false;
                InvalidateDisplayCache();
                RequestListRebuild();
            }

            HubTooltipGlobal.AppendNotOwnedLine = false;
            base.Update(gameTime);

            if (_isListDirty)
            {
                float w = itemList.GetInnerDimensions().Width;
                if (w > 1f || ++_dirtyWaitFrames > 10)
                {
                    BeginListBuild(w > 1f ? w : 300f);
                    _isListDirty = false;
                    _dirtyWaitFrames = 0;
                }
            }
            ContinueListBuild();

            ItemHubPlayer hub = Main.LocalPlayer?.GetModPlayer<ItemHubPlayer>();
            if (hub != null)
            {
                (int u, int tot) = hub.GetHubUnlockProgressCached();
                int pct = tot > 0 ? (int)(100f * u / tot) : 0;
                progressLine?.SetText(EOPJText.UIFormat("ItemHubProgressFmt", u, tot, pct));
            }
            if (searchBar?.InnerSearchBar != null && searchBar.Focused)
                Main.CurrentInputTextTakerOverride = searchBar.InnerSearchBar;
        }
    }

    internal sealed class UIItemHubCard : UIElement
    {
        private int _type;
        private float _scale;
        private Item _item;

        public UIItemHubCard()
        {
            _type = ItemID.None;
            _item = new Item();
        }

        public void SetItem(int type)
        {
            _type = type;
            _item = HubRegistry.GetDisplayItemReference(type);
            if (_item == null && ContentSamples.ItemsByType.TryGetValue(type, out Item sample))
                _item = sample;
            if (_item == null)
            {
                _item = new Item();
                _item.SetDefaults(ItemID.None);
            }
        }

        public void UpdateContext(float scale, float cellW, float rowH)
        {
            _scale = scale;
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            ItemHubPlayer hub = Main.LocalPlayer?.GetModPlayer<ItemHubPlayer>();
            if (hub == null || Main.LocalPlayer == null)
                return;
            if (!hub.CanClaimFromHub(_type))
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            int amt = ClaimStackSize();
            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward(), _type, amt);
            hub.MarkKnown(_type);
            SoundEngine.PlaySound(SoundID.Grab);
        }

        private int ClaimStackSize() =>
            HubCollectibleRules.IsCapturedNpc(_item) ? 1 : Math.Max(1, _item.maxStack);

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Vector2 pos = GetDimensions().Position();
            float old = Main.inventoryScale;
            Main.inventoryScale = _scale;
            Vector2 slotPos = pos + new Vector2(1, 2);
            int sw = (int)(global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Width * Main.inventoryScale);
            int sh = (int)(global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Height * Main.inventoryScale);
            Item[] dummy = new Item[11];
            dummy[10] = _item;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;

            ItemHubPlayer hub = Main.LocalPlayer?.GetModPlayer<ItemHubPlayer>();
            bool locked = !SuperAdminSession.DebugLetTheLightIn &&
                (hub == null || (!HubRegistry.IsDebugItem(_type) && !hub.IsUnlockedForHub(_type)));
            if (locked)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)slotPos.X, (int)slotPos.Y, sw, sh), Color.Black * 0.62f);

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.HoverItem = _item;
                Main.hoverItemName = _item.Name;
                HubTooltipGlobal.AppendNotOwnedLine = hub == null || !hub.CanClaimFromHub(_type);
                HubTooltipGlobal.AppendForItemType = _type;
            }
        }
    }

    internal sealed class UIItemHubRow : UIElement
    {
        private int _type;
        private float _scale;
        private Item _item;

        public UIItemHubRow()
        {
            _type = ItemID.None;
            _item = new Item();
        }

        public void SetItem(int type)
        {
            _type = type;
            _item = HubRegistry.GetDisplayItemReference(type);
            if (_item == null && ContentSamples.ItemsByType.TryGetValue(type, out Item sample))
                _item = sample;
            if (_item == null)
            {
                _item = new Item();
                _item.SetDefaults(ItemID.None);
            }
        }

        public void UpdateContext(float scale, float rowH)
        {
            _scale = scale;
            Width.Set(0, 1f);
            Height.Set(rowH, 0);
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            ItemHubPlayer hub = Main.LocalPlayer?.GetModPlayer<ItemHubPlayer>();
            if (hub == null || Main.LocalPlayer == null)
                return;
            if (!hub.CanClaimFromHub(_type))
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward(), _type, ClaimStackSize());
            hub.MarkKnown(_type);
            SoundEngine.PlaySound(SoundID.Grab);
        }

        private int ClaimStackSize() =>
            HubCollectibleRules.IsCapturedNpc(_item) ? 1 : Math.Max(1, _item.maxStack);

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle dims = GetDimensions();
            float old = Main.inventoryScale;
            Main.inventoryScale = _scale;
            Vector2 slotPos = dims.Position() + new Vector2(2, 2);
            int sw = (int)(global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Width * Main.inventoryScale);
            int sh = (int)(global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Height * Main.inventoryScale);
            Item[] dummy = new Item[11];
            dummy[10] = _item;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;

            ItemHubPlayer hub = Main.LocalPlayer?.GetModPlayer<ItemHubPlayer>();
            bool locked = !SuperAdminSession.DebugLetTheLightIn &&
                (hub == null || (!HubRegistry.IsDebugItem(_type) && !hub.IsUnlockedForHub(_type)));
            if (locked)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)slotPos.X, (int)slotPos.Y, sw, sh), Color.Black * 0.62f);

            Utils.DrawBorderString(spriteBatch, _item.Name, dims.Position() + new Vector2(sw + 8, 8), Color.White, 0.78f);

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.HoverItem = _item;
                Main.hoverItemName = _item.Name;
                HubTooltipGlobal.AppendNotOwnedLine = hub == null || !hub.CanClaimFromHub(_type);
                HubTooltipGlobal.AppendForItemType = _type;
            }
        }
    }

    internal sealed class ItemHubSortDirectionButton : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly Action _onOrderChanged;

        public ItemHubSortDirectionButton(OPJourneyUI shell, Action onOrderChanged)
        {
            _shell = shell;
            _onOrderChanged = onOrderChanged;
            Width.Set(22, 0);
            Height.Set(22, 0);
            OnLeftClick += (_, __) =>
            {
                if (_shell != null)
                    _shell.ItemHubSortDescending = !_shell.ItemHubSortDescending;
                SoundEngine.PlaySound(SoundID.MenuTick);
                _onOrderChanged?.Invoke();
            };
        }

        public void RefreshVisual()
        {
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle d = GetDimensions();
            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            bool desc = _shell?.ItemHubSortDescending ?? false;
            Texture2D tex = ItemHubUiTextureHelper.TryLoadSortDirection(mod, desc);
            if (tex != null)
            {
                const float visualScale = 0.9f;
                float s = Math.Min(d.Width / tex.Width, d.Height / tex.Height) * visualScale;
                Vector2 pos = d.Position() + new Vector2((d.Width - tex.Width * s) * 0.5f, (d.Height - tex.Height * s) * 0.5f);
                spriteBatch.Draw(tex, pos, null, Color.White, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
            }
            else
            {
                Utils.DrawBorderString(spriteBatch, desc ? "v" : "^", d.Position() + new Vector2(6, 3), Color.White, 0.85f);
            }

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(EOPJText.UI("ItemHubSortDirHint"));
            }
        }
    }

    internal sealed class ItemHubViewModeButton : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly Action _toggle;

        public ItemHubViewModeButton(OPJourneyUI shell, Action toggle)
        {
            _shell = shell;
            _toggle = toggle;
            Width.Set(28, 0);
            Height.Set(28, 0);
            OnLeftClick += (_, __) =>
            {
                _toggle?.Invoke();
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
        }

        public void RefreshVisual()
        {
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle d = GetDimensions();
            bool card = _shell?.ItemHubCardMode ?? true;
            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            string key = card
                ? global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubViewCard
                : global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubViewList;
            Texture2D tex = ItemHubUiTextureHelper.TryLoad(mod, key);
            if (tex != null)
            {
                float s = Math.Min((d.Width - 2f) / tex.Width, (d.Height - 2f) / tex.Height);
                Vector2 pos = d.Position() + new Vector2((d.Width - tex.Width * s) * 0.5f, (d.Height - tex.Height * s) * 0.5f);
                spriteBatch.Draw(tex, pos, null, Color.White, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
            }
            else
            {
                Utils.DrawBorderString(spriteBatch, card ? "C" : "L", d.Position() + new Vector2(8, 4), Color.White, 0.85f);
            }

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(card ? EOPJText.UI("ViewCard") : EOPJText.UI("ViewList"));
            }
        }
    }

    internal sealed class ItemHubFilterOpenButton : UIPanel
    {
        private readonly OPJourneyUI _shell;
        private readonly Action _onClick;

        public ItemHubFilterOpenButton(OPJourneyUI shell, Action onClick)
        {
            _shell = shell;
            _onClick = onClick;
            SetPadding(4);
            BackgroundColor = OPJourneyUiColors.ButtonBackground;
            BorderColor = OPJourneyUiColors.ButtonBorder;
            OnLeftClick += (_, __) => _onClick?.Invoke();
        }

        public void RecalculateFilterWidth()
        {
            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            string lab = EOPJText.UI("ItemHubFilterLabel");
            const float textScale = 0.7f;
            float tw = FontAssets.MouseText.Value.MeasureString(lab).X * textScale;
            float gap = 6f;
            float sidePad = 12f;
            Texture2D icon = ItemHubUiTextureHelper.FilterButton;
            float iw = 22f;
            if (icon != null)
                iw = Math.Min(24f, icon.Width * (22f / icon.Height));
            else
                iw = 0f;
            Width.Set(sidePad * 2f + tw + (icon != null ? iw + gap : 0f), 0);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            bool open = _shell?.ItemHubSecondaryPanel?.IsOpen ?? false;
            BorderColor = open ? OPJourneyUiColors.ButtonBorderOpen : OPJourneyUiColors.ButtonBorder;
            BackgroundColor = open ? OPJourneyUiColors.ButtonBackgroundOpen : OPJourneyUiColors.ButtonBackground;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle d = GetDimensions();
            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            float yMid = d.Y + d.Height * 0.5f;
            bool open = _shell?.ItemHubSecondaryPanel?.IsOpen ?? false;
            Color tint = open ? OPJourneyUiColors.TextPrimary : OPJourneyUiColors.TabIconInactiveTint;
            const float textScale = 0.7f;
            string lab = EOPJText.UI("ItemHubFilterLabel");
            Vector2 ms = FontAssets.MouseText.Value.MeasureString(lab) * textScale;
            Texture2D iconTex = ItemHubUiTextureHelper.FilterButton;
            float ih = 22f;
            float iwIcon = 0f;
            float gap = 6f;
            if (iconTex != null)
            {
                float sc = ih / iconTex.Height;
                iwIcon = iconTex.Width * sc;
            }

            float contentW = ms.X + (iconTex != null ? iwIcon + gap : 0f);
            float startX = d.X + (d.Width - contentW) * 0.5f;
            float x = startX;
            if (iconTex != null)
            {
                spriteBatch.Draw(iconTex, new Vector2(x, yMid - ih * 0.5f), null, tint, 0f, Vector2.Zero, ih / iconTex.Height, SpriteEffects.None, 0f);
                x += iwIcon + gap;
            }

            float textTop = yMid - ms.Y * 0.5f + 5f * textScale;
            Utils.DrawBorderString(spriteBatch, lab, new Vector2(x, textTop), Color.White, textScale);

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(EOPJText.UI("ItemHubFilterTitle"));
            }
        }
    }
}
