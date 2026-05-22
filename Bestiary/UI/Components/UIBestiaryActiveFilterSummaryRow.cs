using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Bestiary.Filters;
using EvenMoreOverpoweredJourney.Bestiary.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI.Components
{
    /// <summary>主窗搜索栏下方：已选模组/群系筛选的图标摘要行（与二级窗底栏 chip 同风格）。</summary>
    public sealed class UIBestiaryActiveFilterSummaryRow : UIElement
    {
        public const float RowHeight = BestiaryFilterTagMetrics.ActiveStripOuterH;

        private readonly OPJourneyUI _shell;
        private readonly List<ChipDraw> _chips = new List<ChipDraw>();

        public bool HasChips => _chips.Count > 0;

        public UIBestiaryActiveFilterSummaryRow(OPJourneyUI shell)
        {
            _shell = shell;
            Height.Set(RowHeight, 0);
            IgnoresMouseInteraction = false;
        }

        public void Rebuild(float innerWidth)
        {
            _chips.Clear();
            BestiarySecondaryFilterState st = _shell.BestiarySecondary;
            int n = st.ActiveModKeys.Count + st.ActiveBestiaryFilterIds.Count;
            if (n <= 0)
                return;

            BestiaryFilterTagMetrics.ComputeActiveStripCell(innerWidth, n, out float cellW, out float rowH);
            float yPad = System.Math.Max(0f, (RowHeight - rowH) * 0.5f);
            float x = 4f;
            BestiaryVisibilityPolicy.ListAppearance chipFace =
                BestiaryVisibilityPolicy.GetFilterChipAppearance(_shell.BestiaryFaceMode);

            foreach (string mk in st.ActiveModKeys)
            {
                _chips.Add(new ChipDraw
                {
                    Kind = ChipKind.Mod,
                    ModKey = mk,
                    Left = x,
                    Top = yPad,
                    CellW = cellW,
                    RowH = rowH,
                    Appearance = chipFace
                });
                x += cellW + 2f;
            }

            foreach (string fid in st.ActiveBestiaryFilterIds)
            {
                _chips.Add(new ChipDraw
                {
                    Kind = ChipKind.Biome,
                    FilterId = fid,
                    Def = BestiaryActiveFiltersStripLayout.FindFilter(fid),
                    Left = x,
                    Top = yPad,
                    CellW = cellW,
                    RowH = rowH,
                    Appearance = chipFace
                });
                x += cellW + 2f;
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            Vector2 pos = evt.MousePosition;
            for (int i = 0; i < _chips.Count; i++)
            {
                ChipDraw c = _chips[i];
                var rect = new Rectangle(
                    (int)(GetDimensions().X + c.Left),
                    (int)(GetDimensions().Y + c.Top),
                    (int)c.CellW,
                    (int)c.RowH);
                if (!rect.Contains(pos.ToPoint()))
                    continue;

                if (c.Kind == ChipKind.Mod)
                    _shell.BestiarySecondary.ActiveModKeys.Remove(c.ModKey);
                else
                    _shell.BestiarySecondary.ActiveBestiaryFilterIds.Remove(c.FilterId);

                SoundEngine.PlaySound(SoundID.MenuTick);
                _shell.NotifyBestiaryFiltersChanged();
                return;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_chips.Count == 0)
                return;

            CalculatedStyle dims = GetDimensions();
            Vector2 origin = dims.Position();
            BestiaryFilterChipDraw.ComputeSlotMetrics(BestiaryFilterTagMetrics.ActiveStripScale, out float slotPixW, out float slotPixH);

            for (int i = 0; i < _chips.Count; i++)
            {
                ChipDraw c = _chips[i];
                Vector2 pos = origin + new Vector2(c.Left, c.Top);
                Vector2 slotPos = pos + new Vector2((c.CellW - slotPixW) * 0.5f, (c.RowH - slotPixH) * 0.5f);
                BestiaryFilterChipDraw.DrawInventorySlot(spriteBatch, slotPos, BestiaryFilterTagMetrics.ActiveStripScale);

                if (c.Kind == ChipKind.Mod)
                {
                    BestiaryFilterChipDraw.DrawModChip(spriteBatch, slotPos, slotPixW, slotPixH, c.ModKey, c.Appearance);
                }
                else if (c.Def != null)
                    BestiaryFilterChipDraw.DrawBiomeChipAtSlot(spriteBatch, slotPos, slotPixW, slotPixH, c.Def, c.Appearance);

                var outline = new Rectangle(
                    (int)slotPos.X - 1,
                    (int)slotPos.Y - 1,
                    (int)slotPixW + 2,
                    (int)slotPixH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, outline, OPJourneyUiColors.AccentCyanOutline, 2);
            }
        }

        private enum ChipKind
        {
            Mod,
            Biome
        }

        private struct ChipDraw
        {
            public ChipKind Kind;
            public string ModKey;
            public string FilterId;
            public BestiaryFilterDef Def;
            public float Left;
            public float Top;
            public float CellW;
            public float RowH;
            public BestiaryVisibilityPolicy.ListAppearance Appearance;
        }
    }
}
