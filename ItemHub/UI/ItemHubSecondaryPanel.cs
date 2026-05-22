using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Buffs.UI.Components;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.ItemHub.UI
{
    internal static class ItemHubModGridIcons
    {
        private static readonly Dictionary<string, Texture2D> ModIconCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        internal static Texture2D Resolve(Mod mod, string modKey)
        {
            if (string.IsNullOrEmpty(modKey))
                return null;

            if (modKey == "Terraria")
                return HubModBrandTextures.TryGetVanillaBrandIcon();
            if (modKey == "ModLoader" || modKey == "tModLoader")
                return HubModBrandTextures.TryGetTModBrandIcon();

            if (ModIconCache.TryGetValue(modKey, out Texture2D cached))
                return cached;

            if (mod == null && !ModLoader.TryGetMod(modKey, out mod))
                return null;

            if (mod == null)
                return null;

            foreach (string assetName in new[] { "icon", "Icon" })
            {
                try
                {
                    if (!mod.HasAsset(assetName))
                        continue;

                    Texture2D tex = mod.Assets.Request<Texture2D>(assetName, AssetRequestMode.ImmediateLoad).Value;
                    if (IsUsableModIcon(tex))
                    {
                        ModIconCache[modKey] = tex;
                        return tex;
                    }
                }
                catch
                {
                    /* */
                }
            }

            return null;
        }

        private static bool IsUsableModIcon(Texture2D tex) =>
            tex != null && tex.Width >= 4 && tex.Height >= 4;
    }

    /// <summary>????????????????????????????????????????????????????????????????????????</summary>
    public sealed class ItemHubSecondaryPanel : UIElement
    {
        private readonly OPJourneyUI _shell;
        private UIPanel _body;
        private UIList _scroll;
        private UIScrollbar _scrollBar;
        private UIElement _tabRow;
        private UIElement _activeStrip;
        private bool _open;
        private float _layoutInnerWidthUsed = 280f;
        private float _lastRebuildOuterW = -1f;
        private EmojItemSlot _chainAnchorSlot;
        private UIPanel _chainTogglePanel;
        private int _lastSyncedChainType = int.MinValue;

        private const float BottomTabsH = 44f;
        private const float ActiveFiltersStripH = 36f;
        private const float GapAboveTabs = 4f;
        private const float BottomReservedH = BottomTabsH + ActiveFiltersStripH + GapAboveTabs;
        private const float ContentPadLeft = 5f;
        private const float ContentPadRight = OPJourneyShellMetrics.ScrollSafeMarginRight;
        private const float ContentPadTop = 14f;
        private const float ContentPadBottom = 5f;

        public bool IsOpen => _open;

        public ItemHubSecondaryPanel(OPJourneyUI shell)
        {
            _shell = shell;
            Left.Set(0, 0);
            Top.Set(0, 0);
            Width.Set(0, 0);
            Height.Set(0, 0);

            _body = new UIPanel();
            _body.Left.Set(0, 0);
            _body.Top.Set(0, 0);
            _body.Width.Set(0, 1f);
            _body.Height.Set(-BottomReservedH, 1f);
            _body.BackgroundColor = BestiaryUiColors.PanelBackground;
            _body.BorderColor = BestiaryUiColors.PanelBorder;
            Append(_body);

            _scroll = new UIList();
            _scroll.Left.Set(ContentPadLeft, 0);
            _scroll.Top.Set(ContentPadTop + 3f, 0);
            _scroll.Width.Set(-(ContentPadLeft + ContentPadRight), 1f);
            _scroll.Height.Set(-(ContentPadTop + ContentPadBottom + 3f + OPJourneyShellMetrics.ContentBottomSafeMargin), 1f);
            _body.Append(_scroll);

            _scrollBar = new UIScrollbar();
            _scrollBar.Left.Set(-ContentPadRight, 1f);
            _scrollBar.Top.Set(ContentPadTop + 3f, 0);
            _scrollBar.Height.Set(-(ContentPadTop + ContentPadBottom + 3f + OPJourneyShellMetrics.ContentBottomSafeMargin), 1f);
            _scroll.SetScrollbar(_scrollBar);
            _body.Append(_scrollBar);

            _activeStrip = new UIElement();
            _activeStrip.Left.Set(6, 0);
            _activeStrip.Top.Set(-(ActiveFiltersStripH + GapAboveTabs), 1f);
            _activeStrip.Width.Set(-12, 1f);
            _activeStrip.Height.Set(ActiveFiltersStripH, 0);
            Append(_activeStrip);

            _tabRow = new UIElement();
            _tabRow.Left.Set(6, 0);
            _tabRow.Top.Set(-(ActiveFiltersStripH + GapAboveTabs + BottomTabsH), 1f);
            _tabRow.Width.Set(-12, 1f);
            _tabRow.Height.Set(BottomTabsH, 0);
            Append(_tabRow);

            BuildTabs();
        }

        public void SetOpen(bool open)
        {
            _open = open;
            if (open)
                RebuildScroll();
            else
                _activeStrip?.RemoveAllChildren();
        }

        public override void Update(GameTime gameTime)
        {
            if (!_open || GetDimensions().Width < 2f)
            {
                base.Update(gameTime);
                return;
            }

            CalculatedStyle outer = GetDimensions();
            if (outer.Width > 2f && outer.ToRectangle().Contains(Main.MouseScreen.ToPoint()) && Main.LocalPlayer != null)
                Main.LocalPlayer.mouseInterface = true;

            float ow = outer.Width;
            if (ow > 50f && Math.Abs(ow - _lastRebuildOuterW) > 4f)
                RebuildScroll();

            RefreshChainToggleChrome();
            if (_chainAnchorSlot != null &&
                _shell.ItemHubSecondary.ChainSlotItemType != _lastSyncedChainType)
                SyncChainSlotFromSecondaryState();

            base.Update(gameTime);

            Rectangle box = _body.GetDimensions().ToRectangle();
            if (box.Contains(Main.MouseScreen.ToPoint()))
                PlayerInput.LockVanillaMouseScroll("EvenMoreOverpoweredJourney:ItemHubSecondary");
        }

        private void RefreshChainToggleChrome()
        {
            if (_chainTogglePanel == null)
                return;
            bool on = _shell.ItemHubSecondary.UpstreamChainActive;
            _chainTogglePanel.BorderColor = on ? OPJourneyUiColors.AccentGoldOutline : OPJourneyUiColors.ButtonBorder;
            _chainTogglePanel.BackgroundColor = on ? OPJourneyUiColors.ButtonBackgroundOpen : OPJourneyUiColors.ButtonBackground;
        }

        public void SyncChainSlotFromSecondaryState()
        {
            if (_chainAnchorSlot == null)
                return;
            int t = _shell.ItemHubSecondary.ChainSlotItemType;
            if (t > ItemID.None && t < ItemLoader.ItemCount)
            {
                if (_chainAnchorSlot.item.type != t)
                {
                    _chainAnchorSlot.item = new Item();
                    _chainAnchorSlot.item.SetDefaults(t);
                }
            }
            else if (!_chainAnchorSlot.item.IsAir)
                _chainAnchorSlot.item = new Item();

            _lastSyncedChainType = t;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_open || GetDimensions().Width < 2f)
                return;
            base.Draw(spriteBatch);
        }

        private void BuildTabs()
        {
            _tabRow.RemoveAllChildren();
            string[] keys = { "ItemHubMajor_Mod", "ItemHubMajor_Categories" };
            float gap = 3f;
            float side = 6f;
            float outer = Math.Max(0f, GetDimensions().Width - side * 2f);
            bool usePixelTabs = outer > 120f;
            float tabW = usePixelTabs ? (outer - gap * 2f) / 3f : -1f;
            const float txtScale = 0.62f * 1.5f;

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var p = new UIPanel();
                p.SetPadding(0);
                p.Top.Set(4, 0f);
                p.Height.Set(-6, 1f);
                if (usePixelTabs)
                {
                    p.Left.Set(side + i * (tabW + gap), 0f);
                    p.Width.Set(tabW, 0f);
                }
                else
                {
                    float frac = 1f / 3f;
                    p.Left.Set(0, i * frac);
                    p.Width.Set(0, frac);
                }

                bool on = i < 2 && _shell.ItemHubSecondary.MajorTabIndex == idx;
                if (i < 2)
                {
                    p.BackgroundColor = on ? OPJourneyUiColors.ButtonBackgroundOpen : OPJourneyUiColors.ButtonBackground;
                    p.BorderColor = on ? OPJourneyUiColors.AccentGoldOutline : OPJourneyUiColors.ButtonBorder;
                    var t = new UIText(EOPJText.UI(keys[i]), txtScale);
                    t.HAlign = 0.5f;
                    t.VAlign = 0.5f;
                    t.IgnoresMouseInteraction = true;
                    p.Append(t);
                    p.OnLeftClick += (_, __) =>
                    {
                        _shell.ItemHubSecondary.MajorTabIndex = idx;
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        RebuildScroll();
                    };
                }
                else
                {
                    p.BackgroundColor = OPJourneyUiColors.DangerBackground;
                    p.BorderColor = OPJourneyUiColors.DangerBorder;
                    var t = new UIText(EOPJText.UI("ItemHubResetFilters"), txtScale);
                    t.HAlign = 0.5f;
                    t.VAlign = 0.5f;
                    t.IgnoresMouseInteraction = true;
                    p.Append(t);
                    p.OnLeftClick += (_, __) =>
                    {
                        _shell.ItemHubSecondary.ResetFilters();
                        SoundEngine.PlaySound(SoundID.MenuClose);
                        _shell.NotifyItemHubFiltersChanged();
                        RebuildScroll();
                    };
                }

                _tabRow.Append(p);
            }
        }

        public void RebuildScroll()
        {
            BuildTabs();

            if (!HubRegistry.Ready)
                return;

            if (_shell.ItemHubSecondary.MajorTabIndex > 1)
                _shell.ItemHubSecondary.MajorTabIndex = 1;

            float ow = GetDimensions().Width;
            _layoutInnerWidthUsed = ow > 40f
                ? Math.Max(120f, ow - ContentPadLeft - ContentPadRight - 6f)
                : 280f;

            _scroll.Clear();
            int tab = _shell.ItemHubSecondary.MajorTabIndex;
            if (tab == 0)
                BuildModTab();
            else
                BuildCategoriesTab();

            _lastRebuildOuterW = GetDimensions().Width;
            RebuildActiveFilterStrip();
            _lastSyncedChainType = int.MinValue;
            SyncChainSlotFromSecondaryState();
            RefreshChainToggleChrome();
        }

        public void RebuildActiveFilterStrip()
        {
            if (_activeStrip == null)
                return;
            _activeStrip.RemoveAllChildren();
            if (!_open || GetDimensions().Width < 30f)
                return;
            float inner = Math.Max(40f, GetDimensions().Width - 12f);
            ItemHubActiveFiltersStripLayout.Populate(_shell, _activeStrip, inner);
        }

        private void BuildModTab()
        {
            AddSectionHeader(EOPJText.UI("ItemHubSec_Chain"), false);
            _scroll.Add(BuildChainSection());

            AddSectionGap(6f);
            AddSectionHeader(EOPJText.UI("ItemHubSec_ModPick"), false);
            (string tag, string tip)[] arr = HubRegistry.ModKeys.Select(mk =>
            {
                string tag = "mod." + mk;
                string tip = mk == "Terraria"
                    ? EOPJText.UI("ItemHubModTipVanilla")
                    : EOPJText.UIFormat("ItemHubModTipModFmt", mk);
                return (tag, tip);
            }).ToArray();
            AddModTagGrid(arr);

            AddSectionGap(6f);
            AddSectionHeader(EOPJText.UI("ItemHubSec_Rare"), false);
            _scroll.Add(new ItemHubRareRangeStrip(_shell));
        }

        private void BuildCategoriesTab()
        {
            HubCategoryDefinitions.EnsureInitialized();
            (string tag, string tip)[] arr = HubCategoryDefinitions.All
                .Select(e => (e.Tag, EOPJText.UI(e.LocKey)))
                .ToArray();
            AddTagGrid(arr);
        }

        private UIElement BuildChainSection()
        {
            var panel = new ItemHubChainSectionPanel(_shell);
            _chainAnchorSlot = panel.AnchorSlot;
            _chainTogglePanel = panel.TogglePanel;
            return panel;
        }

        private void AddSectionGap(float pixels)
        {
            var g = new UIElement();
            g.Width.Set(0, 1f);
            g.Height.Set(pixels, 0);
            _scroll.Add(g);
        }

        private void AddSectionHeader(string title, bool relaxedSpacingAfterTitle)
        {
            var row = new UIElement();
            row.Width.Set(0, 1f);
            row.Height.Set(relaxedSpacingAfterTitle ? 28 : 20, 0);

            var tx = new UIText(title, 0.72f * 1.5f);
            tx.Left.Set(0, 0);
            tx.Top.Set(relaxedSpacingAfterTitle ? 4 : 0, 0);
            tx.IgnoresMouseInteraction = true;
            row.Append(tx);
            _scroll.Add(row);
            if (relaxedSpacingAfterTitle)
            {
                var sp = new UIElement();
                sp.Width.Set(0, 1f);
                sp.Height.Set(8, 0);
                _scroll.Add(sp);
            }
        }

        private void AddTagGrid((string tag, string tip)[] items)
        {
            var grid = new HubTagGrid(_shell, _layoutInnerWidthUsed, items);
            grid.Width.Set(0, 1f);
            _scroll.Add(grid);
            UIElement spacer = new UIElement();
            spacer.Height.Set(2, 0);
            spacer.Width.Set(0, 1f);
            _scroll.Add(spacer);
        }

        private void AddModTagGrid((string tag, string tip)[] items)
        {
            var grid = new HubModTagGrid(_shell, _layoutInnerWidthUsed, items);
            grid.Width.Set(0, 1f);
            _scroll.Add(grid);
            UIElement spacer = new UIElement();
            spacer.Height.Set(2, 0);
            spacer.Width.Set(0, 1f);
            _scroll.Add(spacer);
        }
    }

    internal static class ItemHubActiveFilterTips
    {
        internal static string StripRemoveLine() => EOPJText.UI("ItemHubActiveStripRemove");

        internal static string HoverLine(string tag)
        {
            string tip = TipForTag(tag);
            return tip + "\n" + StripRemoveLine();
        }

        private static string TipForTag(string tag)
        {
            if (tag.StartsWith("rare.", StringComparison.Ordinal) && int.TryParse(tag.AsSpan(5), out int rr))
                return EOPJText.UIFormat("ItemHubTip_RareFmt", rr);
            if (tag.StartsWith("mod.", StringComparison.Ordinal))
            {
                string mk = tag.Substring(4);
                return mk == "Terraria"
                    ? EOPJText.UI("ItemHubModTipVanilla")
                    : EOPJText.UIFormat("ItemHubModTipModFmt", mk);
            }

            if (tag.StartsWith("ic.", StringComparison.Ordinal))
            {
                HubCategoryDefinitions.EnsureInitialized();
                foreach (HubCategoryDefinitions.Entry e in HubCategoryDefinitions.All)
                {
                    if (e.Tag == tag)
                        return EOPJText.UI(e.LocKey);
                }
            }

            return tag;
        }
    }

    internal static class ItemHubActiveFiltersStripLayout
    {
        internal static void Populate(OPJourneyUI shell, UIElement parent, float innerWidth)
        {
            HubSecondaryFilterState st = shell.ItemHubSecondary;
            st.NormalizeRareFilterBounds();
            List<string> tags = st.ActiveTags.OrderBy(t => t, StringComparer.Ordinal).ToList();
            bool rareNarrowed = st.IsRareFilterActive;
            int extra = (st.UpstreamChainActive ? 1 : 0) + (rareNarrowed ? 1 : 0);
            int n = tags.Count + extra;
            if (n <= 0)
                return;

            ItemHubFilterTagMetrics.ComputeActiveStripCell(innerWidth, n, out float cellW, out float rowH);
            float yPad = Math.Max(0f, (ItemHubFilterTagMetrics.ActiveStripOuterH - rowH) * 0.5f);
            float x = 4f;
            foreach (string tag in tags)
            {
                var chip = new HubActiveFilterChip(shell, tag, ItemHubActiveFilterTips.HoverLine(tag), cellW, rowH);
                chip.Left.Set(x, 0);
                chip.Top.Set(yPad, 0);
                parent.Append(chip);
                x += cellW + 2f;
            }

            if (st.UpstreamChainActive)
            {
                string chainTip = EOPJText.UI("ItemHubChainActivate") + "\n" + ItemHubActiveFilterTips.StripRemoveLine();
                var c = new HubActiveChainChip(shell, chainTip, cellW, rowH);
                c.Left.Set(x, 0);
                c.Top.Set(yPad, 0);
                parent.Append(c);
                x += cellW + 2f;
            }

            if (rareNarrowed)
            {
                string maxText = st.RareFilterMax >= ItemHubRareRangeStrip.SliderMax
                    ? $"{st.RareFilterMax}+"
                    : st.RareFilterMax.ToString();
                string rareTip = EOPJText.UIFormat("ItemHubActiveRareHoverFmt", st.RareFilterMin, maxText)
                    + "\n" + ItemHubActiveFilterTips.StripRemoveLine();
                var r = new HubActiveRareChip(shell, rareTip, cellW, rowH);
                r.Left.Set(x, 0);
                r.Top.Set(yPad, 0);
                parent.Append(r);
            }
        }
    }

    internal sealed class HubActiveFilterChip : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _tag;
        private readonly string _hover;
        private readonly Item _ph = new Item();

        public HubActiveFilterChip(OPJourneyUI shell, string tag, string hover, float cellW, float rowH)
        {
            _shell = shell;
            _tag = tag;
            _hover = hover ?? "";
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);
            if (tag.StartsWith("mod.", StringComparison.Ordinal))
                _ph.TurnToAir();
            else
                _ph.SetDefaults(HubTagPreviewIds.ForTag(tag));

            OnLeftClick += (_, __) =>
            {
                _shell.ItemHubSecondary.ToggleTag(_tag);
                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyItemHubFiltersChanged();
            };
        }

        private static string AbbrevNoIcon(string modKey, Mod mod) =>
            HubModAbbrev.ForGrid(modKey);

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Vector2 pos = d.Position();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotScale = ItemHubFilterTagMetrics.ActiveStripScale;
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);

            Item[] dummy = new Item[11];
            dummy[10] = _ph;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;

            if (_tag.StartsWith("mod.", StringComparison.Ordinal))
            {
                string mk = _tag.Substring(4);
                Mod mod = mk != "Terraria" && ModLoader.TryGetMod(mk, out Mod m) ? m : null;
                Texture2D iconTex = ItemHubModGridIcons.Resolve(mod, mk);
                if (iconTex != null)
                {
                    float fit = Math.Min(slotPixW, slotPixH) * 0.92f;
                    float s = fit / Math.Max(iconTex.Width, iconTex.Height);
                    Vector2 origin = new Vector2(iconTex.Width, iconTex.Height) * 0.5f;
                    Vector2 center = slotPos + new Vector2(slotPixW * 0.5f, slotPixH * 0.5f);
                    spriteBatch.Draw(iconTex, center, null, Color.White, 0f, origin, s, SpriteEffects.None, 0f);
                }
                else
                {
                    string ab = AbbrevNoIcon(mk, mod);
                    var f = FontAssets.MouseText.Value;
                    Vector2 ms = f.MeasureString(ab);
                    Vector2 tpos = slotPos + new Vector2((slotPixW - ms.X) * 0.5f, (slotPixH - ms.Y) * 0.5f);
                    Utils.DrawBorderStringFourWay(spriteBatch, f, ab, tpos.X, tpos.Y, Color.White, Color.Black, Vector2.One);
                }
            }

            var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
            BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(120, 220, 255), 2);

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (_tag.StartsWith("mod.", StringComparison.Ordinal))
                {
                    string mk = _tag.Substring(4);
                    Mod mod = mk != "Terraria" && ModLoader.TryGetMod(mk, out Mod m) ? m : null;
                    Texture2D iconTex = ItemHubModGridIcons.Resolve(mod, mk);
                    if (iconTex != null)
                    {
                        const float big = 80f;
                        float bigS = big / Math.Max(iconTex.Width, iconTex.Height);
                        Vector2 size = new Vector2(iconTex.Width, iconTex.Height) * bigS;
                        Vector2 topLeft = Main.MouseScreen + new Vector2(-size.X * 0.5f, -size.Y - 14f);
                        spriteBatch.Draw(iconTex, topLeft, null, Color.White, 0f, Vector2.Zero, bigS, SpriteEffects.None, 0f);
                    }
                }

                if (!string.IsNullOrEmpty(_hover))
                    Main.instance.MouseText(_hover);
            }
        }
    }

    internal static class ItemHubChainUiHelper
    {
        internal static string QuickQueryKeyDisplay()
        {
            ModKeybind kb = global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.QuickItemQueryKey;
            if (kb == null || Main.gameMenu)
                return EOPJText.UI("ItemHubChainKeyUnbound");
            try
            {
                List<string> keys = kb.GetAssignedKeys(InputMode.Keyboard);
                if (keys == null || keys.Count == 0)
                    return EOPJText.UI("ItemHubChainKeyUnbound");
                return string.Join(" / ", keys);
            }
            catch (KeyNotFoundException)
            {
                return EOPJText.UI("ItemHubChainKeyUnbound");
            }
            catch
            {
                return EOPJText.UI("ItemHubChainKeyUnbound");
            }
        }
    }

    /// <summary>???????????????????????????????????????????????????????</summary>
    internal sealed class ItemHubChainSectionPanel : UIPanel
    {
        private const float Pad = 6f;
        private const float SlotSize = 52f;
        private const float ColGap = 8f;
        private const float BtnW = 76f;
        private const float BtnH = 26f;
        private const float HintGap = 5f;

        private readonly OPJourneyUI _shell;
        private readonly UIText _keyHint;
        private string _lastKeyDisp = "";

        public EmojItemSlot AnchorSlot { get; }
        public UIPanel TogglePanel { get; }

        public ItemHubChainSectionPanel(OPJourneyUI shell)
        {
            _shell = shell;
            SetPadding(0);
            Width.Set(0, 1f);
            Height.Set(108, 0);
            BackgroundColor = OPJourneyUiColors.ButtonBackground;
            BorderColor = OPJourneyUiColors.ButtonBorder;

            float btnLeft = Pad + SlotSize + ColGap;

            AnchorSlot = new EmojItemSlot();
            AnchorSlot.Left.Set(Pad, 0);
            AnchorSlot.Top.Set(Pad, 0);
            AnchorSlot.OnItemChanged += it =>
            {
                _shell.ItemHubSecondary.SetUpstreamSlotType(it != null && it.type > ItemID.None ? it.type : ItemID.None);
                _shell.NotifyItemHubFiltersChanged();
            };
            Append(AnchorSlot);

            TogglePanel = new UIPanel();
            TogglePanel.Left.Set(btnLeft, 0);
            TogglePanel.Top.Set(Pad, 0);
            TogglePanel.Width.Set(BtnW, 0);
            TogglePanel.Height.Set(BtnH, 0);
            TogglePanel.BackgroundColor = OPJourneyUiColors.ButtonBackground;
            TogglePanel.BorderColor = OPJourneyUiColors.ButtonBorder;
            var btnTxt = new UIText(EOPJText.UI("ItemHubChainActivate"), 0.58f * 1.5f);
            btnTxt.HAlign = 0.5f;
            btnTxt.VAlign = 0.5f;
            btnTxt.IgnoresMouseInteraction = true;
            TogglePanel.Append(btnTxt);
            TogglePanel.OnLeftClick += (_, __) =>
            {
                _shell.ItemHubSecondary.UpstreamChainActive = !_shell.ItemHubSecondary.UpstreamChainActive;
                _shell.ItemHubSecondary.InvalidateUpstream();
                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyItemHubFiltersChanged();
            };
            Append(TogglePanel);

            var activateHint = new UIText(EOPJText.UI("ItemHubChainActivateDesc"), 0.46f * 0.7f);
            activateHint.Left.Set(btnLeft, 0);
            activateHint.Top.Set(Pad + BtnH + HintGap, 0);
            activateHint.Width.Set(-(btnLeft + Pad), 1f);
            activateHint.IsWrapped = true;
            activateHint.TextColor = OPJourneyUiColors.TextMuted;
            activateHint.IgnoresMouseInteraction = true;
            Append(activateHint);

            _keyHint = new UIText("", 0.46f * 0.7f);
            _keyHint.Left.Set(Pad, 0);
            float slotHintTop = Pad + SlotSize + HintGap;
            _keyHint.Top.Set(slotHintTop, 0);
            _keyHint.Width.Set(-Pad * 2f - 4f, 1f);
            _keyHint.IsWrapped = true;
            _keyHint.TextColor = Color.DimGray;
            _keyHint.IgnoresMouseInteraction = true;
            _keyHint.SetText(EOPJText.UIFormat("ItemHubChainSlotHint", EOPJText.UI("ItemHubChainKeyUnbound")));
            Append(_keyHint);
        }

        public override void Update(GameTime gameTime)
        {
            string kd = ItemHubChainUiHelper.QuickQueryKeyDisplay();
            if (kd != _lastKeyDisp)
                RefreshKeyHintText();
            base.Update(gameTime);
        }

        private void RefreshKeyHintText()
        {
            _lastKeyDisp = ItemHubChainUiHelper.QuickQueryKeyDisplay();
            _keyHint.SetText(EOPJText.UIFormat("ItemHubChainSlotHint", _lastKeyDisp));
        }
    }

    internal sealed class HubActiveChainChip : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _hover;
        private readonly Item _item = new Item();

        public HubActiveChainChip(OPJourneyUI shell, string hover, float cellW, float rowH)
        {
            _shell = shell;
            _hover = hover ?? "";
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);
            RefreshItem();

            OnLeftClick += (_, __) =>
            {
                _shell.ItemHubSecondary.UpstreamChainActive = false;
                _shell.ItemHubSecondary.SetUpstreamSlotType(ItemID.None);
                _shell.ItemHubSecondary.InvalidateUpstream();
                SoundEngine.PlaySound(SoundID.MenuClose);
                _shell.NotifyItemHubFiltersChanged();
            };
        }

        private void RefreshItem()
        {
            int t = _shell.ItemHubSecondary.ChainSlotItemType;
            if (t > ItemID.None && t < ItemLoader.ItemCount)
                _item.SetDefaults(t);
            else
                _item.TurnToAir();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            RefreshItem();
            CalculatedStyle d = GetDimensions();
            Vector2 pos = d.Position();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotScale = ItemHubFilterTagMetrics.ActiveStripScale;
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);

            Item[] dummy = new Item[11];
            dummy[10] = _item;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;

            var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
            BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(255, 200, 120), 2);

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (!string.IsNullOrEmpty(_hover))
                    Main.instance.MouseText(_hover);
            }
        }
    }

    internal sealed class HubActiveRareChip : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _hover;

        public HubActiveRareChip(OPJourneyUI shell, string hover, float cellW, float rowH)
        {
            _shell = shell;
            _hover = hover ?? "";
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);

            OnLeftClick += (_, __) =>
            {
                _shell.ItemHubSecondary.ResetRareFilterToDefault();
                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyItemHubFiltersChanged();
            };
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Vector2 pos = d.Position();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotScale = ItemHubFilterTagMetrics.ActiveStripScale;
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);

            Item[] dummy = new Item[11];
            dummy[10] = new Item();
            dummy[10].SetDefaults(ItemID.CopperCoin);
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;

            _shell.ItemHubSecondary.NormalizeRareFilterBounds();
            string hi = _shell.ItemHubSecondary.RareFilterMax >= ItemHubRareRangeStrip.SliderMax
                ? $"{_shell.ItemHubSecondary.RareFilterMax}+"
                : _shell.ItemHubSecondary.RareFilterMax.ToString();
            string lab = $"{_shell.ItemHubSecondary.RareFilterMin}~{hi}";
            var f = FontAssets.MouseText.Value;
            Vector2 ms = f.MeasureString(lab) * 0.45f;
            Utils.DrawBorderString(spriteBatch, lab, slotPos + new Vector2((slotPixW - ms.X) * 0.5f, (slotPixH - ms.Y) * 0.5f + 2f), Color.Gold, 0.45f);

            var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
            BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(200, 200, 255), 2);

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (!string.IsNullOrEmpty(_hover))
                    Main.instance.MouseText(_hover);
            }
        }
    }

    internal static class ItemHubFilterTagMetrics
    {
        public const float SlotScale = 0.56f * 1.2f;
        public const float ActiveStripScale = 0.4f * 1.2f;
        public const float ActiveStripOuterH = 36f;

        public static void ComputeActiveStripCell(float innerWidth, int count, out float cellW, out float rowH)
        {
            Texture2D inv = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float baseCell = inv.Width * ActiveStripScale + 3f;
            rowH = inv.Height * ActiveStripScale + 3f;
            if (count <= 0)
            {
                cellW = baseCell;
                return;
            }

            float per = (innerWidth - 8f) / count - 2f;
            cellW = Math.Max(20f, Math.Min(baseCell, per));
        }

        public static void ComputeGridLayout(float innerWidth, int count, out float cellW, out float rowH, out int cols, out int rows)
        {
            Texture2D inv = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            cellW = inv.Width * SlotScale + 4f;
            rowH = inv.Height * SlotScale + 4f;
            cols = Math.Max(1, (int)(innerWidth / cellW));
            rows = count == 0 ? 0 : (count + cols - 1) / cols;
        }
    }

    internal sealed class HubModTagGrid : UIElement
    {
        public HubModTagGrid(OPJourneyUI shell, float innerWidth, (string tag, string tip)[] items)
        {
            ItemHubFilterTagMetrics.ComputeGridLayout(innerWidth, items.Length, out float cellW, out float rowH, out int cols, out int rows);
            Height.Set(rows <= 0 ? 1f : rows * rowH, 0);
            Width.Set(0, 1f);

            for (int i = 0; i < items.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                var btn = new HubModTagButton(shell, items[i].tag, items[i].tip, cellW, rowH);
                btn.Left.Set(c * cellW, 0);
                btn.Top.Set(r * rowH, 0);
                Append(btn);
            }
        }
    }

    internal sealed class HubModTagButton : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _tag;
        private readonly string _hoverTip;
        private readonly string _modKey;
        private readonly Item _ph = new Item();

        public HubModTagButton(OPJourneyUI shell, string tag, string hoverTip, float cellW, float rowH)
        {
            _shell = shell;
            _tag = tag;
            _hoverTip = hoverTip ?? "";
            _modKey = tag.StartsWith("mod.", StringComparison.Ordinal) ? tag.Substring(4) : tag;
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);
            _ph.TurnToAir();

            OnLeftClick += (_, __) =>
            {
                _shell.ItemHubSecondary.ToggleTag(_tag);
                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyItemHubFiltersChanged();
            };
        }

        private static string AbbrevNoIcon(string modKey, Mod mod) =>
            HubModAbbrev.ForGrid(modKey);

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Vector2 pos = d.Position();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotScale = ItemHubFilterTagMetrics.SlotScale;
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);

            Item[] dummy = new Item[11];
            dummy[10] = _ph;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;

            Mod mod = _modKey != "Terraria" && ModLoader.TryGetMod(_modKey, out Mod m) ? m : null;
            Texture2D iconTex = ItemHubModGridIcons.Resolve(mod, _modKey);
            if (iconTex != null)
            {
                float fit = Math.Min(slotPixW, slotPixH) * 0.92f;
                float s = fit / Math.Max(iconTex.Width, iconTex.Height);
                Vector2 origin = new Vector2(iconTex.Width, iconTex.Height) * 0.5f;
                Vector2 center = slotPos + new Vector2(slotPixW * 0.5f, slotPixH * 0.5f);
                spriteBatch.Draw(iconTex, center, null, Color.White, 0f, origin, s, SpriteEffects.None, 0f);
            }
            else
            {
                string ab = AbbrevNoIcon(_modKey, mod);
                var f = FontAssets.MouseText.Value;
                Vector2 ms = f.MeasureString(ab);
                Vector2 tpos = slotPos + new Vector2((slotPixW - ms.X) * 0.5f, (slotPixH - ms.Y) * 0.5f);
                Utils.DrawBorderStringFourWay(spriteBatch, f, ab, tpos.X, tpos.Y, Color.White, Color.Black, Vector2.One);
            }

            bool on = _shell.ItemHubSecondary.ActiveTags.Contains(_tag);
            if (on)
            {
                var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(255, 220, 120), 2);
            }

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (iconTex != null)
                {
                    const float big = 80f;
                    float bigS = big / Math.Max(iconTex.Width, iconTex.Height);
                    Vector2 size = new Vector2(iconTex.Width, iconTex.Height) * bigS;
                    Vector2 topLeft = Main.MouseScreen + new Vector2(-size.X * 0.5f, -size.Y - 14f);
                    spriteBatch.Draw(iconTex, topLeft, null, Color.White, 0f, Vector2.Zero, bigS, SpriteEffects.None, 0f);
                }

                if (!string.IsNullOrEmpty(_hoverTip))
                    Main.instance.MouseText(_hoverTip);
            }
        }
    }

    internal sealed class HubTagGrid : UIElement
    {
        public HubTagGrid(OPJourneyUI shell, float innerWidth, (string tag, string tip)[] items)
        {
            ItemHubFilterTagMetrics.ComputeGridLayout(innerWidth, items.Length, out float cellW, out float rowH, out int cols, out int rows);
            Height.Set(rows <= 0 ? 1f : rows * rowH, 0);
            Width.Set(0, 1f);

            for (int i = 0; i < items.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                var btn = new HubTagButton(shell, items[i].tag, items[i].tip, cellW, rowH);
                btn.Left.Set(c * cellW, 0);
                btn.Top.Set(r * rowH, 0);
                Append(btn);
            }
        }
    }

    internal sealed class HubTagButton : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _tag;
        private readonly string _hoverTip;
        private readonly Item _ph = new Item();

        public HubTagButton(OPJourneyUI shell, string tag, string hoverTip, float cellW, float rowH)
        {
            _shell = shell;
            _tag = tag;
            _hoverTip = hoverTip ?? "";
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);
            _ph.SetDefaults(HubTagPreviewIds.ForTag(tag));

            OnLeftClick += (_, __) =>
            {
                _shell.ItemHubSecondary.ToggleTag(_tag);
                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyItemHubFiltersChanged();
            };
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Vector2 pos = d.Position();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotScale = ItemHubFilterTagMetrics.SlotScale;
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);

            Item[] dummy = new Item[11];
            dummy[10] = _ph;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;

            bool on = _shell.ItemHubSecondary.ActiveTags.Contains(_tag);
            if (on)
            {
                var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(255, 220, 120), 2);
            }

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (!string.IsNullOrEmpty(_hoverTip))
                    Main.instance.MouseText(_hoverTip);
            }
        }
    }
}
