using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Assets;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>
    /// 家具套组 8×3 网格尺寸（由 InventoryBack 贴图宽推导，与 <see cref="BlueprintSlotMetrics"/> 一致）。
    /// <para>公式（列数 C、行数 R）：</para>
    /// <para>cellSize = invW × 0.56 × 1.2 + 4；stride = cellSize + cellGap；</para>
    /// <para>内容宽 = C×stride ? cellGap；窗体宽 = 内容宽 + 滚动条 + hostPadX；</para>
    /// <para>行高 = cellSize + labelH + rowGap；视口高 = R×行高 + padBottom。</para>
    /// </summary>
    internal static class BlueprintSlotGridLayoutMetrics
    {
        public const float CellGap = BlueprintSlotGridPanel.CellGap;
        public const float LabelHeight = 14f;
        public const float RowGapBelowSlot = 4f;
        /// <summary>套组窗体右侧留白（滚动条已单独占宽，此处仅微调）。</summary>
        public const float HostPadX = 0f;
        public const float ShellExtraMarginX = 36f;

        public static float InventoryBackWidth => EojUiTextures.Common.InventoryBack.Width;

        public static float CellSize => BlueprintSlotMetrics.CellSize;

        public static float CellStride => CellSize + CellGap;

        public static float RowStride => CellSize + LabelHeight + RowGapBelowSlot;

        public static float ContentWidthForColumns(int columns) =>
            columns * CellStride - CellGap;

        public static float HostWidthForColumns(int columns, float scrollBarWidth) =>
            ContentWidthForColumns(columns) + scrollBarWidth + HostPadX;

        public static float ViewportHeightForRows(int rows, float padBottom) =>
            rows * RowStride + padBottom;

        public static float RecommendedShellWidth(float chromeWidth, float contentInsetLeft, float scrollSafeRight) =>
            contentInsetLeft + chromeWidth + scrollSafeRight + ShellExtraMarginX;
    }
}
