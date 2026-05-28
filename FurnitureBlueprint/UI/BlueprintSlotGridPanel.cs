using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>按可用宽度自动换行的 22 槽网格容器。</summary>
    public sealed class BlueprintSlotGridPanel : UIElement
    {
        public static float CellSize => BlueprintSlotMetrics.CellSize;
        public const float CellGap = 2f;
        public static float LabelHeight => BlueprintSlotMetrics.LabelHeight;
        public static float CellStride => CellSize + CellGap;

        public static float RowStride => BlueprintSlotGridLayoutMetrics.RowStride;

        private readonly List<UIElement> _cells = new();

        public void SetCells(IEnumerable<UIElement> slotAndLabelPairs)
        {
            RemoveAllChildren();
            _cells.Clear();
            foreach (UIElement el in slotAndLabelPairs)
            {
                _cells.Add(el);
                Append(el);
            }
            Recalculate();
        }

        public override void Recalculate()
        {
            if (_cells.Count == 0)
            {
                base.Recalculate();
                return;
            }

            float width = GetInnerDimensions().Width;
            if (width < CellSize && Parent != null)
                width = Parent.GetInnerDimensions().Width;

            float stride = CellStride;
            int perRow = Math.Max(1, (int)((width + CellGap) / stride));
            if (perRow > _cells.Count)
                perRow = _cells.Count;

            float x0 = 0f;
            float y0 = 0f;

            for (int i = 0; i < _cells.Count; i++)
            {
                int col = i % perRow;
                int row = i / perRow;
                UIElement el = _cells[i];
                el.Left.Set(x0 + col * stride, 0);
                el.Top.Set(y0 + row * RowStride, 0);
            }

            int rows = (_cells.Count + perRow - 1) / perRow;
            Height.Set(rows * RowStride, 0f);
            base.Recalculate();
        }
    }
}
