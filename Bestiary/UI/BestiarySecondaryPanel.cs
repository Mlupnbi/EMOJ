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
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    public sealed class BestiarySecondaryPanel : UIElement
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

        private const float BottomTabsH = 44f;
        private const float ActiveFiltersStripH = 36f;
        private const float GapAboveTabs = 4f;
        private const float BottomReservedH = BottomTabsH + ActiveFiltersStripH + GapAboveTabs;
        private const float ContentPadLeft = 5f;
        private const float ContentPadRight = OPJourneyShellMetrics.ScrollSafeMarginRight;
        private const float ContentPadTop = 14f;
        private const float ContentPadBottom = 5f;

        public bool IsOpen => _open;

        public BestiarySecondaryPanel(OPJourneyUI shell)
        {
            _shell = shell;
            Left.Set(0, 0);
            Top.Set(0, 0);
            Width.Set(0, 0);
            Height.Set(0, 0);

            _body = new UIPanel();
            _body.Width.Set(0, 1f);
            _body.Height.Set(-BottomReservedH, 1f);
            _body.BackgroundColor = new Color(28, 28, 48) * 0.98f;
            _body.BorderColor = new Color(130, 130, 200);
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

            base.Update(gameTime);
        }

        public void RebuildActiveFilterStrip() => BestiaryActiveFiltersStripLayout.Populate(_shell, _activeStrip, Math.Max(40f, GetDimensions().Width - 12f));

        public void RebuildScroll()
        {
            BuildTabs();
            if (!BestiaryFilterIndex.Ready)
                return;

            float ow = GetDimensions().Width;
            _layoutInnerWidthUsed = ow > 40f
                ? Math.Max(120f, ow - ContentPadLeft - ContentPadRight - 6f)
                : 280f;

            _scroll.Clear();
            if (_shell.BestiarySecondary.MajorTabIndex == 0)
                BuildModTab();
            else
                BuildBiomeTab();

            _lastRebuildOuterW = ow;
            RebuildActiveFilterStrip();
        }

        private void BuildTabs()
        {
            _tabRow.RemoveAllChildren();
            string[] keys = { "BestiarySec_Mod", "BestiarySec_Vanilla" };
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

                bool on = i < 2 && _shell.BestiarySecondary.MajorTabIndex == idx;
                if (i < 2)
                {
                    p.BackgroundColor = on ? new Color(62, 62, 98) : new Color(38, 38, 60);
                    p.BorderColor = on ? new Color(255, 210, 120) : new Color(55, 55, 85);
                    var t = new UIText(EOPJText.UI(keys[i]), txtScale);
                    t.HAlign = 0.5f;
                    t.VAlign = 0.5f;
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
                    p.BackgroundColor = new Color(120, 32, 32);
                    p.BorderColor = new Color(180, 70, 70);
                    var t = new UIText(EOPJText.UI("BestiaryFilterReset"), txtScale);
                    t.HAlign = 0.5f;
                    t.VAlign = 0.5f;
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

                _tabRow.Append(p);
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
            Texture2D invBack = TextureAssets.InventoryBack.Value;
            float slotScale = BestiaryFilterTagMetrics.ActiveStripScale;
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);

            Item[] dummy = new Item[11];
            dummy[10] = new Item();
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

            var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
            BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(120, 220, 255), 2);

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
            Texture2D invBack = TextureAssets.InventoryBack.Value;
            float slotScale = BestiaryFilterTagMetrics.ActiveStripScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = d.Position() + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);
            BestiaryFilterSlotDrawHelper.DrawInventorySlot(spriteBatch, slotPos, slotScale);

            var iconRect = new Rectangle(
                (int)(slotPos.X + slotPixW * 0.12f),
                (int)(slotPos.Y + slotPixH * 0.12f),
                (int)(slotPixW * 0.76f),
                (int)(slotPixH * 0.76f));
            BestiaryFilterDef def = BestiaryActiveFiltersStripLayout.FindFilter(_filterId);
            if (def != null && def.IconFrame != Point.Zero)
                BestiaryVanillaFilterIcons.DrawFilterIcon(spriteBatch, iconRect, def.IconFrame);
            else
                BestiaryFilterIconResolver.DrawInto(spriteBatch, iconRect, _filter);

            var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
            BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(255, 220, 120), 2);

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
            Texture2D invBack = TextureAssets.InventoryBack.Value;
            float slotScale = BestiaryFilterTagMetrics.SlotScale;
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = pos + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);

            Item[] dummy = new Item[11];
            dummy[10] = new Item();
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
                string ab = ItemHub.Data.HubModAbbrev.ForGrid(_modKey);
                var f = FontAssets.MouseText.Value;
                Vector2 ms = f.MeasureString(ab);
                Vector2 tpos = slotPos + new Vector2((slotPixW - ms.X) * 0.5f, (slotPixH - ms.Y) * 0.5f);
                Utils.DrawBorderStringFourWay(spriteBatch, f, ab, tpos.X, tpos.Y, Color.White, Color.Black, Vector2.One);
            }

            bool on = _shell.BestiarySecondary.ActiveModKeys.Contains(_modKey);
            if (on)
            {
                var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(255, 220, 120), 2);
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
            Texture2D invBack = TextureAssets.InventoryBack.Value;
            float slotScale = BestiaryFilterTagMetrics.SlotScale;
            float slotPixW = invBack.Width * slotScale;
            float slotPixH = invBack.Height * slotScale;
            Vector2 slotPos = d.Position() + new Vector2((d.Width - slotPixW) * 0.5f, (d.Height - slotPixH) * 0.5f);
            BestiaryFilterSlotDrawHelper.DrawInventorySlot(spriteBatch, slotPos, slotScale);

            var iconRect = new Rectangle(
                (int)(slotPos.X + slotPixW * 0.12f),
                (int)(slotPos.Y + slotPixH * 0.12f),
                (int)(slotPixW * 0.76f),
                (int)(slotPixH * 0.76f));
            if (_def.IconFrame != Point.Zero)
                BestiaryVanillaFilterIcons.DrawFilterIcon(spriteBatch, iconRect, _def.IconFrame);
            else
                BestiaryFilterIconResolver.DrawInto(spriteBatch, iconRect, _def.Filter);

            bool on = _shell.BestiarySecondary.ActiveBestiaryFilterIds.Contains(_def.Id);
            if (on)
            {
                var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(255, 220, 120), 2);
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
            Texture2D invBack = TextureAssets.InventoryBack.Value;
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
        internal static void DrawInventorySlot(SpriteBatch spriteBatch, Vector2 slotPos, float slotScale)
        {
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
