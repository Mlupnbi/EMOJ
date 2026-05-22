using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Buffs.Systems.Catalog;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Components;
using EvenMoreOverpoweredJourney.ItemHub.UI;

namespace EvenMoreOverpoweredJourney.Buffs.UI
{
    public sealed class BuffSecondaryPanel : UIElement
    {
        private readonly OPJourneyUI _shell;
        private UIPanel _body;
        private UIList _scroll;
        private UIScrollbar _scrollBar;
        private UIElement _bottomBar;
        private UIElement _activeStrip;
        private bool _open;
        private float _layoutInnerWidthUsed = 280f;
        private float _lastRebuildOuterW = -1f;

        private const float BottomBarH = 44f;
        private const float ActiveFiltersStripH = 36f;
        private const float GapAboveBottomBar = 4f;
        private const float BottomReservedH = BottomBarH + ActiveFiltersStripH + GapAboveBottomBar;
        private const float ContentPadLeft = 5f;
        private const float ContentPadRight = OPJourneyShellMetrics.ScrollSafeMarginRight;
        private const float ContentPadTop = 20f;
        private const float ContentPadBottom = 5f;

        public bool IsOpen => _open;

        public BuffSecondaryPanel(OPJourneyUI shell)
        {
            _shell = shell;
            _body = new UIPanel();
            _body.Width.Set(0, 1f);
            _body.Height.Set(-BottomReservedH, 1f);
            _body.BackgroundColor = BestiaryUiColors.PanelBackground;
            _body.BorderColor = BestiaryUiColors.PanelBorder;
            Append(_body);

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

            _activeStrip = new UIElement();
            _activeStrip.Left.Set(6, 0);
            _activeStrip.Top.Set(-(ActiveFiltersStripH + GapAboveBottomBar), 1f);
            _activeStrip.Width.Set(-12, 1f);
            _activeStrip.Height.Set(ActiveFiltersStripH, 0);
            Append(_activeStrip);

            _bottomBar = new UIElement();
            _bottomBar.Left.Set(6, 0);
            _bottomBar.Top.Set(-(ActiveFiltersStripH + GapAboveBottomBar + BottomBarH), 1f);
            _bottomBar.Width.Set(-12, 1f);
            _bottomBar.Height.Set(BottomBarH, 0);
            Append(_bottomBar);

            BuildBottomResetBar();
        }

        private void BuildBottomResetBar()
        {
            _bottomBar.RemoveAllChildren();

            var resetPanel = new UIPanel();
            resetPanel.SetPadding(0);
            resetPanel.Left.Set(0, 0f);
            resetPanel.Top.Set(4, 0f);
            resetPanel.Width.Set(0, 1f);
            resetPanel.Height.Set(-6, 1f);
            resetPanel.BackgroundColor = OPJourneyUiColors.DangerBackground;
            resetPanel.BorderColor = OPJourneyUiColors.DangerBorder;

            const float resetTextScale = 0.72f * 1.5f;
            var resetTxt = new UIText(EOPJText.UI("BuffFilterReset"), resetTextScale);
            resetTxt.HAlign = 0.5f;
            resetTxt.VAlign = 0.5f;
            resetTxt.TextColor = OPJourneyUiColors.DangerText;
            resetTxt.IgnoresMouseInteraction = true;
            resetPanel.Append(resetTxt);

            resetPanel.OnLeftClick += (_, __) =>
            {
                EmojLog.InfoFull(EmojLogChannel.Buff, "buff mod filter reset");
                _shell.BuffSecondary.Reset();
                SoundEngine.PlaySound(SoundID.MenuClose);
                _shell.NotifyBuffFiltersChanged();
                RebuildScroll();
            };

            _bottomBar.Append(resetPanel);
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

            if (GetDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint()) && Main.LocalPlayer != null)
                Main.LocalPlayer.mouseInterface = true;

            float ow = GetDimensions().Width;
            if (ow > 50f && Math.Abs(ow - _lastRebuildOuterW) > 4f)
                RebuildScroll();

            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_open || GetDimensions().Width < 2f)
                return;
            base.Draw(spriteBatch);
        }

        public void RebuildScroll()
        {
            BuildBottomResetBar();
            float ow = GetDimensions().Width;
            _layoutInnerWidthUsed = ow > 40f
                ? Math.Max(120f, ow - ContentPadLeft - ContentPadRight - 6f)
                : 280f;

            _scroll.Clear();
            AddSectionHeader(EOPJText.UI("BuffFilterModPick"));
            var items = BuffModCatalogSystem.ModKeys
                .Select(mk => (mk, FormatModBuffHover(mk)))
                .ToArray();
            AddModGrid(items);
            _lastRebuildOuterW = ow;
            RebuildActiveStrip();
        }

        public void RebuildActiveFilterStrip() => RebuildActiveStrip();

        public void RebuildActiveStrip()
        {
            _activeStrip.RemoveAllChildren();
            if (!_open || GetDimensions().Width < 30f)
                return;

            var keys = _shell.BuffSecondary.ActiveModKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
            if (keys.Count == 0)
                return;

            ItemHubFilterTagMetrics.ComputeActiveStripCell(GetDimensions().Width - 12f, keys.Count, out float cellW, out float rowH);
            float yPad = Math.Max(0f, (ItemHubFilterTagMetrics.ActiveStripOuterH - rowH) * 0.5f);
            float x = 4f;
            foreach (string modKey in keys)
            {
                var chip = new BuffActiveModChip(_shell, modKey, FormatModBuffHover(modKey), cellW, rowH);
                chip.Left.Set(x, 0);
                chip.Top.Set(yPad, 0);
                _activeStrip.Append(chip);
                x += cellW + 2f;
            }
        }

        private static string FormatModBuffHover(string modKey)
        {
            string display = modKey == "Terraria"
                ? EOPJText.UI("ItemHubModTipVanilla")
                : (ModLoader.TryGetMod(modKey, out Mod mod) ? mod.DisplayName : modKey);
            return EOPJText.UIFormat("BuffFilterModHoverFmt", display);
        }

        private void AddSectionHeader(string title)
        {
            var row = new UIElement();
            row.Width.Set(0, 1f);
            row.Height.Set(26, 0);
            var tx = new UIText(title, 0.72f * 1.5f);
            tx.Top.Set(4, 0);
            tx.IgnoresMouseInteraction = true;
            row.Append(tx);
            _scroll.Add(row);
        }

        private void AddModGrid((string modKey, string tip)[] items)
        {
            var grid = new BuffModFilterGrid(_shell, _layoutInnerWidthUsed, items);
            grid.Width.Set(0, 1f);
            _scroll.Add(grid);
            var spacer = new UIElement();
            spacer.Height.Set(6, 0);
            spacer.Width.Set(0, 1f);
            _scroll.Add(spacer);
        }
    }

    internal static class BuffModFilterMetrics
    {
        public static float CellSizeForPanel(float panelWidth) =>
            Math.Clamp(panelWidth * 0.19f, 44f, 56f);

        public static void ComputeGridLayout(float innerWidth, int count, out float cellW, out float rowH, out int cols, out int rows)
        {
            cellW = rowH = CellSizeForPanel(innerWidth);
            cols = Math.Max(1, (int)(innerWidth / cellW));
            rows = count == 0 ? 0 : (count + cols - 1) / cols;
        }
    }

    internal sealed class BuffModFilterGrid : UIElement
    {
        public BuffModFilterGrid(OPJourneyUI shell, float innerWidth, (string modKey, string tip)[] items)
        {
            BuffModFilterMetrics.ComputeGridLayout(innerWidth, items.Length, out float cellW, out float rowH, out int cols, out int rows);
            Height.Set(rows <= 0 ? 1f : rows * rowH, 0);
            Width.Set(0, 1f);

            for (int i = 0; i < items.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                var btn = new BuffModFilterButton(shell, items[i].modKey, items[i].tip, cellW, rowH);
                btn.Left.Set(c * cellW, 0);
                btn.Top.Set(r * rowH, 0);
                Append(btn);
            }
        }
    }

    internal sealed class BuffModFilterButton : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _modKey;
        private readonly string _hoverTip;
        private readonly Item _placeholder = new Item();

        public BuffModFilterButton(OPJourneyUI shell, string modKey, string hoverTip, float cellW, float rowH)
        {
            _shell = shell;
            _modKey = modKey;
            _hoverTip = hoverTip ?? "";
            _placeholder.TurnToAir();
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);
            OnLeftClick += (_, __) =>
            {
                _shell.BuffSecondary.ToggleMod(_modKey);
                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyBuffFiltersChanged();
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
            dummy[10] = _placeholder;
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
                string ab = HubModAbbrev.ForGrid(_modKey);
                var f = FontAssets.MouseText.Value;
                Vector2 ms = f.MeasureString(ab);
                Vector2 tpos = slotPos + new Vector2((slotPixW - ms.X) * 0.5f, (slotPixH - ms.Y) * 0.5f);
                Utils.DrawBorderStringFourWay(spriteBatch, f, ab, tpos.X, tpos.Y, Color.White, Color.Black, Vector2.One);
            }

            bool active = _shell.BuffSecondary.ActiveModKeys.Contains(_modKey);
            if (active)
            {
                var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(255, 220, 120), 2);
            }

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(_hoverTip + "\n" + EOPJText.UI("ItemHubActiveStripRemove"));
            }
        }
    }

    internal sealed class BuffActiveModChip : UIElement
    {
        private readonly OPJourneyUI _shell;
        private readonly string _modKey;
        private readonly string _hoverTip;
        private readonly Item _placeholder = new Item();

        public BuffActiveModChip(OPJourneyUI shell, string modKey, string hoverTip, float cellW, float rowH)
        {
            _shell = shell;
            _modKey = modKey;
            _hoverTip = hoverTip;
            _placeholder.TurnToAir();
            Width.Set(cellW, 0);
            Height.Set(rowH, 0);
            OnLeftClick += (_, __) =>
            {
                _shell.BuffSecondary.ToggleMod(_modKey);
                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyBuffFiltersChanged();
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
            dummy[10] = _placeholder;
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
                string ab = HubModAbbrev.ForGrid(_modKey);
                var f = FontAssets.MouseText.Value;
                Vector2 ms = f.MeasureString(ab);
                Vector2 tpos = slotPos + new Vector2((slotPixW - ms.X) * 0.5f, (slotPixH - ms.Y) * 0.5f);
                Utils.DrawBorderStringFourWay(spriteBatch, f, ab, tpos.X, tpos.Y, Color.White, Color.Black, Vector2.One);
            }

            var outline = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, (int)slotPixW + 2, (int)slotPixH + 2);
            BorderDrawUtil.DrawRectOutline(spriteBatch, outline, new Color(255, 220, 120), 1);

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(_hoverTip + "\n" + EOPJText.UI("ItemHubActiveStripRemove"));
            }
        }
    }
}
