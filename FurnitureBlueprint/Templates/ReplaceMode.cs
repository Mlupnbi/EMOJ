namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    /// <summary>放置时单元格的替换策略（对齐 BLUEPRINT_REFERENCE §4.2）。</summary>
    public enum ReplaceMode : byte
    {
        /// <summary>按 structure 原样写入，不参与套组换色。</summary>
        Fixed = 0,

        /// <summary>用当前套组对应 <see cref="FurnitureSlotKind"/> 的物品替换。</summary>
        Slot = 1,

        /// <summary>同 GroupId 的多格家具共享一次套组槽位（仅首格计件）。</summary>
        SlotGroup = 2
    }
}
