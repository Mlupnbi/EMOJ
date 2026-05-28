using EvenMoreOverpoweredJourney.Shell.UI.Assets;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>与物品研究中枢列表格一致：InventoryBack × 0.56 × 1.2。</summary>
    public static class BlueprintSlotMetrics
    {
        public const float SlotScale = 0.56f * 1.2f;

        public static float CellSize => EojUiTextures.Common.InventoryBack.Width * SlotScale + 4f;

        public static float LabelHeight => 14f;

        public static float RowStride => CellSize + LabelHeight + 4f;
    }
}
