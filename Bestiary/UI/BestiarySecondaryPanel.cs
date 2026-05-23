using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.GameContent.Bestiary;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Bestiary.Filters;
using EvenMoreOverpoweredJourney.Bestiary.UI.Components;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    public sealed class BestiarySecondaryPanel : UIElement
    {
        private readonly OPJourneyUI _shell;
        private UIScaledDrawHost _scaleHost;
        private UIElement _filterRoot;
        private UIPanel _body;
        private UIList _scroll;
        private UIScrollbar _scrollBar;
        private UIElement _tabRail;
        private bool _open;
        private float _layoutInnerWidthUsed = 280f;
        private float _lastRebuildOuterW = -1f;

        /// <summary>????????????????????????</summary>
        private const float OutsideTabRailW = 52f;
        private const float OutsideTabGap = 4f;

        private static float S => BestiaryChromeLayout.FilterDisplayScale;
        private static float TabBtnW => 48f;
        private static float TabBtnH => 36f * S;
        private static float TabBtnGap => 4f * S;
        private static float TabRailPad => 2f * S;
        private const float ContentPadLeft = 10f;
        private const float ContentPadRight = OPJourneyShellMetrics.ScrollSafeMarginRight + 12f;
        private const float ContentPadTop = 8f;
        private const float ContentPadBottom = 8f;
        /// <summary>????????????????</summary>
        private const float HeaderToGridGapPx = 3f;
        private const float SectionHeaderRowH = 10f;
        private const float ScrollBarW = 18f;

        public bool IsOpen => _open;

        public BestiarySecondaryPanel(OPJourneyUI shell)
        {
            _shell = shell;
            Left.Set(0, 0);
            Top.Set(0, 0);
            Width.Set(0, 0);
            Height.Set(0, 0);

            float logicalW = OPJourneyShellMetrics.DefaultMainWidth;
            float logicalH = OPJourneyShellMetrics.DefaultMainHeight * 0.8f;

            _filterRoot = new UIElement();
            _filterRoot.Width.Set(0f, 1f);
            _filterRoot.Height.Set(0f, 1f);

            _body = new UIPanel();
            _body.SetPadding(0);
            _body.Width.Set(0f, 1f);
            _body.Height.Set(0f, 1f);
            _body.BackgroundColor = OPJourneyUiColors.MainPanelBackground;
            _body.BorderColor = OPJourneyUiColors.PanelBorder;
            _filterRoot.Append(_body);

            _scroll = new UIList();
            _scroll.Left.Set(ContentPadLeft, 0);
            _scroll.Top.Set(ContentPadTop, 0);
            _scroll.Width.Set(-(ContentPadLeft + ContentPadRight), 1f);
            _scroll.Height.Set(-(ContentPadTop + ContentPadBottom + OPJourneyShellMetrics.ContentBottomSafeMargin), 1f);
            _body.Append(_scroll);

            _scrollBar = new UIScrollbar();
            _scrollBar.Left.Set(-ContentPadRight, 1f);
            _scrollBar.Top.Set(ContentPadTop, 0);
            _scrollBar.Height.Set(-(ContentPadTop + ContentPadBottom + OPJourneyShellMetrics.ContentBottomSafeMargin), 1f);
            _scroll.SetScrollbar(_scrollBar);
            _body.Append(_scrollBar);

            _scaleHost = new UIScaledDrawHost(_filterRoot, logicalW, logicalH, BestiaryChromeLayout.FilterDisplayScale);
            _scaleHost.Left.Set(0f, 0f);
            _scaleHost.Top.Set(0f, 0f);
            Append(_scaleHost);

            _tabRail = new UIElement();
            _tabRail.Width.Set(OutsideTabRailW, 0f);
            _tabRail.Height.Set(0f, 1f);
            _tabRail.Left.Set(-(OutsideTabRailW + OutsideTabGap), 0f);
            _tabRail.Top.Set(0f, 0f);
            Append(_tabRail);
            BuildTabs();
        }

        public override bool ContainsPoint(Vector2 point)
        {
            CalculatedStyle dims = GetDimensions();
            if (dims.Width < 2f || dims.Height < 2f)
                return false;

            Rectangle hit = dims.ToRectangle();
            hit.X -= (int)(OutsideTabRailW + OutsideTabGap);
            hit.Width += (int)(OutsideTabRailW + OutsideTabGap);
            return hit.Contains(point.ToPoint());
        }

        public void SetOpen(bool open)
        {
            _open = open;
            if (open)
                RebuildScroll();
        }

        public override void Update(GameTime gameTime)
        {
            if (!_open || GetDimensions().Width < 2f)
                return;

            CalculatedStyle outer = GetDimensions();
            if (outer.Width > 2f && outer.ToRectangle().Contains(Main.MouseScreen.ToPoint()) && Main.LocalPlayer != null)
                Main.LocalPlayer.mouseInterface = true;

            SyncHostFromOuterDimensions();

            float ow = GetContentLogicalWidth();
            if (ow > 50f && Math.Abs(ow - _lastRebuildOuterW) > 4f)
                RebuildScroll();

            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_open || !OPJourneyUI.Visible || _shell.CurrentTab != 3 || GetDimensions().Width < 2f)
                return;

            base.Draw(spriteBatch);
        }

        private void SyncHostFromOuterDimensions()
        {
            CalculatedStyle outer = GetDimensions();
            if (outer.Width < 2f || outer.Height < 2f || _scaleHost == null)
                return;

            float logicalW = outer.Width / S;
            float logicalH = outer.Height / S;
            _scaleHost.SetLogicalSize(logicalW, logicalH);
        }

        private float GetContentLogicalWidth()
        {
            if (_scroll != null)
            {
                float inner = _scroll.GetInnerDimensions().Width;
                if (inner > 40f)
                    return inner;
            }

            CalculatedStyle outer = GetDimensions();
            if (outer.Width < 2f)
                return _layoutInnerWidthUsed;

            return Math.Max(80f, outer.Width - ContentPadLeft - ContentPadRight - ScrollBarW);
        }

        public void RebuildScroll()
        {
            BuildTabs();
            if (!BestiaryFilterIndex.Ready)
                return;

            float ow = GetContentLogicalWidth();
            _layoutInnerWidthUsed = ow > 40f ? ow : 280f;

            _scroll.Clear();
            if (_shell.BestiarySecondary.MajorTabIndex == 0)
                BuildModTab();
            else
                BuildBiomeTab();

            _lastRebuildOuterW = ow;
        }

        private void BuildTabs()
        {
            _tabRail.RemoveAllChildren();
            string[] keys = { "BestiarySec_Mod", "BestiarySec_Vanilla" };
            float txtScale = 0.62f * 1.5f * S;
            float btnW = TabBtnW - TabRailPad * 2f;

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var p = new UIPanel();
                p.SetPadding(0);
                p.Left.Set(TabRailPad, 0f);
                p.Width.Set(btnW, 0f);
                p.Height.Set(TabBtnH, 0f);
                p.Top.Set(TabRailPad + i * (TabBtnH + TabBtnGap), 0f);

                bool on = i < 2 && _shell.BestiarySecondary.MajorTabIndex == idx;
                if (i < 2)
                {
                    p.SetPadding(0);
                    p.BackgroundColor = on ? OPJourneyUiColors.SecondaryTabOnBackground : OPJourneyUiColors.SecondaryTabOffBackground;
                    p.BorderColor = on ? OPJourneyUiColors.SecondaryTabOnBorder : OPJourneyUiColors.SecondaryTabOffBorder;
                    var t = new UIText(EOPJText.UI(keys[i]), txtScale * 0.92f);
                    t.HAlign = 0.5f;
                    t.VAlign = 0.5f;
                    t.Top.Set(0f, 0f);
                    t.TextColor = OPJourneyUiColors.TextPrimary;
                    t.IgnoresMouseInteraction = true;
                    p.Append(t);
                    p.OnLeftClick += (_, _) =>
                    {
                        _shell.BestiarySecondary.MajorTabIndex = idx;
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        RebuildScroll();
                    };
                }
                else
                {
                    p.SetPadding(0);
                    p.BackgroundColor = OPJourneyUiColors.DangerBackground;
                    p.BorderColor = OPJourneyUiColors.DangerBorder;
                    var t = new UIText(EOPJText.UI("BestiaryFilterReset"), txtScale * 0.85f);
                    t.HAlign = 0.5f;
                    t.VAlign = 0.5f;
                    t.Top.Set(0f, 0f);
                    t.TextColor = OPJourneyUiColors.DangerText;
                    t.IgnoresMouseInteraction = true;
                    p.Append(t);
                    p.OnLeftClick += (_, _) =>
                    {
                        _shell.BestiarySecondary.ResetFilters();
                        SoundEngine.PlaySound(SoundID.MenuClose);
                        _shell.NotifyBestiaryFiltersChanged();
                        RebuildScroll();
                    };
                }

                _tabRail.Append(p);
            }
        }

        private void BuildModTab()
        {
            AddSectionHeader(EOPJText.UI("BestiarySec_ModPick"), false);
            var keys = new List<string>(BestiaryFilterIndex.ModKeys);
            AddModGrid(keys);
        }

        private void BuildBiomeTab()
        {
            AddSectionHeader(EOPJText.UI("BestiarySec_Vanilla"), false);
            var defs = new List<BestiaryFilterDef>(BestiaryFilterIndex.VanillaFilters);
            if (defs.Count == 0)
            {
                AddSectionHeader(EOPJText.UI("BestiaryFilterEmpty"), false);
                return;
            }

            AddBiomeGrid(defs);
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
            row.Height.Set(relaxedSpacingAfterTitle ? 28f : SectionHeaderRowH, 0);

            var tx = new UIText(title, 0.72f * 1.5f);
            tx.TextColor = OPJourneyUiColors.TextPrimary;
            tx.IsWrapped = true;
            tx.Width.Set(-(ContentPadLeft + ContentPadRight), 1f);
            tx.Height.Set(0f, 1f);
            tx.Left.Set(ContentPadLeft, 0);
            tx.Top.Set(1f, 0);
            tx.IgnoresMouseInteraction = true;
            row.Append(tx);
            _scroll.Add(row);

            if (relaxedSpacingAfterTitle)
                AddSectionGap(6f);
            else
                AddSectionGap(HeaderToGridGapPx);
        }

        private void AddModGrid(List<string> modKeys)
        {
            if (modKeys == null || modKeys.Count == 0)
                return;

            var items = new (string key, string tip)[modKeys.Count];
            for (int i = 0; i < modKeys.Count; i++)
            {
                string mk = modKeys[i];
                string tip = mk == "Terraria"
                    ? EOPJText.UI("BestiaryModTipVanilla")
                    : EOPJText.UIFormat("ItemHubModTipModFmt", mk);
                items[i] = (mk, tip);
            }

            var grid = new BestiaryModTagGrid(_shell, _layoutInnerWidthUsed, items);
            grid.Width.Set(0, 1f);
            _scroll.Add(grid);
            AddSectionGap(2f);
        }

        private void AddBiomeGrid(List<BestiaryFilterDef> defs)
        {
            var grid = new BestiaryBiomeTagGrid(_shell, _layoutInnerWidthUsed, defs);
            grid.Width.Set(0, 1f);
            _scroll.Add(grid);
            AddSectionGap(2f);
        }
    }

    internal static class BestiaryActiveFiltersStripLayout
    {
        internal static void Populate(OPJourneyUI shell, UIElement parent, float innerWidth)
        {
            parent.RemoveAllChildren();
            BestiarySecondaryFilterState st = shell.BestiarySecondary;
            int n = st.ActiveModKeys.Count + st.ActiveBestiaryFilterIds.Count;
            if (n <= 0)
                return;

            BestiaryFilterTagMetrics.ComputeActiveStripCell(innerWidth, n, out float cellW, out float rowH);
            float yPad = Math.Max(0f, (BestiaryFilterTagMetrics.ActiveStripOuterH - rowH) * 0.5f);
            float x = 4f;

            foreach (string mk in st.ActiveModKeys)
            {
                string tip = mk == "Terraria"
                    ? EOPJText.UI("BestiaryModTipVanilla")
                    : EOPJText.UIFormat("ItemHubModTipModFmt", mk);
                var chip = new BestiaryActiveModChip(shell, mk, tip, cellW, rowH);
                chip.Left.Set(x, 0);
                chip.Top.Set(yPad, 0);
                parent.Append(chip);
                x += cellW + 2f;
            }

            foreach (string fid in st.ActiveBestiaryFilterIds)
            {
                BestiaryFilterDef def = FindFilter(fid);
                string tip = (def?.DisplayName ?? fid) + "\n" + EOPJText.UI("ItemHubActiveStripRemove");
                var chip = new BestiaryActiveBiomeChip(shell, fid, tip, cellW, rowH);
                chip.Left.Set(x, 0);
                chip.Top.Set(yPad, 0);
                parent.Append(chip);
                x += cellW + 2f;
            }
        }

        internal static BestiaryFilterDef FindFilter(string id)
        {
            foreach (BestiaryFilterDef def in BestiaryFilterIndex.VanillaFilters)
            {
                if (def.Id == id)
                    return def;
            }

            return null;
        }
    }

    internal sealed class BestiaryActiveModChip : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _modKey;
        private readonly string _hover;

        public BestiaryActiveModChip(OPJourneyUI shell, string modKey, string hover, float cellW, float rowH)
        {
            _shell = shell;
            _modKey = modKey;
            _hover = hover ?? "";
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);

            OnLeftClick += (_, _) =>
            {
                _shell.BestiarySecondary.ActiveModKeys.Remove(_modKey);
                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyBestiaryFiltersChanged();
            };
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Vector2 pos = d.Position();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotScale = BestiaryFilterTagMetrics.ActiveStripScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);
            BestiaryFilterSlotDrawHelper.DrawInventorySlot(spriteBatch, slotPos, slotScale, true, IsMouseHovering);

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

            var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
            BorderDrawUtil.DrawRectOutline(spriteBatch, outline, OPJourneyUiColors.AccentCyanOutline, 2);

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(_hover);
            }
        }
    }

    internal sealed class BestiaryActiveBiomeChip : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _filterId;
        private readonly string _hover;
        private readonly IBestiaryEntryFilter _filter;

        public BestiaryActiveBiomeChip(OPJourneyUI shell, string filterId, string hover, float cellW, float rowH)
        {
            _shell = shell;
            _filterId = filterId;
            _hover = hover ?? "";
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);

            BestiaryFilterDef def = BestiaryActiveFiltersStripLayout.FindFilter(filterId);
            _filter = def?.Filter;

            OnLeftClick += (_, _) =>
            {
                _shell.BestiarySecondary.ActiveBestiaryFilterIds.Remove(_filterId);
                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyBestiaryFiltersChanged();
            };
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotScale = BestiaryFilterTagMetrics.ActiveStripScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = d.Position() + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);
            BestiaryFilterSlotDrawHelper.DrawInventorySlot(spriteBatch, slotPos, slotScale, true, IsMouseHovering);

            BestiaryFilterDef def = BestiaryActiveFiltersStripLayout.FindFilter(_filterId);
            BestiaryFilterChipDraw.DrawBiomeChipAtSlot(spriteBatch, slotPos, slotPixW, slotPixH, def, BestiaryVisibilityPolicy.ListAppearance.FullPortraitAndName);

            var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
            BorderDrawUtil.DrawRectOutline(spriteBatch, outline, OPJourneyUiColors.AccentGoldOutline, 2);

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(_hover);
            }
        }
    }

    internal sealed class BestiaryModTagGrid : UIElement
    {
        public BestiaryModTagGrid(OPJourneyUI shell, float innerWidth, (string modKey, string tip)[] items)
        {
            BestiaryFilterTagMetrics.ComputeGridLayout(innerWidth, items.Length, out float cellW, out float rowH, out int cols, out int rows);
            Height.Set(rows <= 0 ? 1f : rows * rowH, 0);
            Width.Set(0, 1f);

            for (int i = 0; i < items.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                var btn = new BestiaryModTagButton(shell, items[i].modKey, items[i].tip, cellW, rowH);
                btn.Left.Set(c * cellW, 0);
                btn.Top.Set(r * rowH, 0);
                Append(btn);
            }
        }
    }

    internal sealed class BestiaryModTagButton : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _modKey;
        private readonly string _hoverTip;

        public BestiaryModTagButton(OPJourneyUI shell, string modKey, string hoverTip, float cellW, float rowH)
        {
            _shell = shell;
            _modKey = modKey;
            _hoverTip = hoverTip ?? "";
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);

            OnLeftClick += (_, _) =>
            {
                if (_shell.BestiarySecondary.ActiveModKeys.Contains(_modKey))
                    _shell.BestiarySecondary.ActiveModKeys.Remove(_modKey);
                else
                    _shell.BestiarySecondary.ActiveModKeys.Add(_modKey);

                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyBestiaryFiltersChanged();
            };
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Vector2 pos = d.Position();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotScale = BestiaryFilterTagMetrics.SlotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);
            bool on = _shell.BestiarySecondary.ActiveModKeys.Contains(_modKey);
            BestiaryFilterSlotDrawHelper.DrawInventorySlot(spriteBatch, slotPos, slotScale, on, IsMouseHovering);

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
                string ab = ItemHub.Data.HubModAbbrev.ForGrid(_modKey);
                var f = FontAssets.MouseText.Value;
                Vector2 ms = f.MeasureString(ab);
                Vector2 tpos = slotPos + new Vector2((slotPixW - ms.X) * 0.5f, (slotPixH - ms.Y) * 0.5f);
                Utils.DrawBorderStringFourWay(spriteBatch, f, ab, tpos.X, tpos.Y, Color.White, Color.Black, Vector2.One);
            }

            if (on)
            {
                var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, outline, OPJourneyUiColors.AccentGoldOutline, 2);
            }

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(_hoverTip);
            }
        }
    }

    internal sealed class BestiaryBiomeTagGrid : UIElement
    {
        public BestiaryBiomeTagGrid(OPJourneyUI shell, float innerWidth, List<BestiaryFilterDef> defs)
        {
            BestiaryFilterTagMetrics.ComputeGridLayout(innerWidth, defs.Count, out float cellW, out float rowH, out int cols, out int rows);
            Height.Set(rows <= 0 ? 1f : rows * rowH, 0);
            Width.Set(0, 1f);

            for (int i = 0; i < defs.Count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                var btn = new BestiaryBiomeTagButton(shell, defs[i], cellW, rowH);
                btn.Left.Set(c * cellW, 0);
                btn.Top.Set(r * rowH, 0);
                Append(btn);
            }
        }
    }

    internal sealed class BestiaryBiomeTagButton : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly BestiaryFilterDef _def;

        public BestiaryBiomeTagButton(OPJourneyUI shell, BestiaryFilterDef def, float cellW, float rowH)
        {
            _shell = shell;
            _def = def;
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);

            OnLeftClick += (_, _) =>
            {
                if (_shell.BestiarySecondary.ActiveBestiaryFilterIds.Contains(_def.Id))
                    _shell.BestiarySecondary.ActiveBestiaryFilterIds.Remove(_def.Id);
                else
                    _shell.BestiarySecondary.ActiveBestiaryFilterIds.Add(_def.Id);

                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyBestiaryFiltersChanged();
            };
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotScale = BestiaryFilterTagMetrics.SlotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = d.Position() + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);
            bool on = _shell.BestiarySecondary.ActiveBestiaryFilterIds.Contains(_def.Id);
            BestiaryFilterSlotDrawHelper.DrawInventorySlot(spriteBatch, slotPos, slotScale, on, IsMouseHovering);

            BestiaryFilterChipDraw.DrawBiomeChipAtSlot(spriteBatch, slotPos, slotPixW, slotPixH, _def, BestiaryVisibilityPolicy.ListAppearance.FullPortraitAndName);
            if (on)
            {
                var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, outline, OPJourneyUiColors.AccentGoldOutline, 2);
            }

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(_def.DisplayName ?? _def.Id);
            }
        }
    }

    internal static class BestiaryFilterSlotDraw
    {
        internal static void DrawSlotBacking(UIElement element, SpriteBatch spriteBatch, float slotScale)
        {
            CalculatedStyle d = element.GetDimensions();
            Vector2 pos = d.Position();
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);
            Item[] dummy = new Item[11];
            dummy[10] = new Item();
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;
        }
    }

    internal static class BestiaryFilterSlotDrawExtensions
    {
        internal static void DrawSlotBacking(this UIElement element, SpriteBatch spriteBatch, float slotScale) =>
            BestiaryFilterSlotDraw.DrawSlotBacking(element, spriteBatch, slotScale);
    }

    internal static class BestiaryFilterSlotDrawHelper
    {
        internal static void DrawThemedBacking(
            SpriteBatch spriteBatch,
            Rectangle slotRect,
            bool selected,
            bool hover)
        {
            Color fill = selected
                ? OPJourneyUiColors.ButtonBackgroundOpen
                : (hover ? OPJourneyUiColors.ButtonBackgroundHover : OPJourneyUiColors.SlotCellFill);
            Color border = selected ? OPJourneyUiColors.ButtonBorderOpen : OPJourneyUiColors.SlotCellBorder;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, slotRect, fill);
            BorderDrawUtil.DrawRectOutline(spriteBatch, slotRect, border, 1);
        }

        internal static void DrawInventorySlot(
            SpriteBatch spriteBatch,
            Vector2 slotPos,
            float slotScale,
            bool selected = false,
            bool hover = false)
        {
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            var slotRect = new Rectangle((int)slotPos.X, (int)slotPos.Y, (int)slotPixW, (int)slotPixH);
            DrawThemedBacking(spriteBatch, slotRect, selected, hover);

            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            Item[] dummy = new Item[11];
            dummy[10] = new Item();
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;
        }
    }

    internal static class BestiaryFilterDrawExtensions
    {
        internal static void DrawInventorySlot(this UIElement _, SpriteBatch spriteBatch, Vector2 slotPos, float slotScale) =>
            BestiaryFilterSlotDrawHelper.DrawInventorySlot(spriteBatch, slotPos, slotScale);
    }
}
