using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    /// <summary>与 <see cref="StructureCell"/> 一一对应的替换规则（未来 .eopjbp replace.bin）。</summary>
    public struct ReplaceRule
    {
        public ReplaceMode Mode;
        public FurnitureSlotKind SlotKind;
        public byte GroupId;
        public int FixedItemType;

        public static ReplaceRule Fixed(int itemType = ItemID.None) => new()
        {
            Mode = ReplaceMode.Fixed,
            FixedItemType = itemType
        };

        public static ReplaceRule ForSlot(FurnitureSlotKind kind) => new()
        {
            Mode = ReplaceMode.Slot,
            SlotKind = kind
        };

        public static ReplaceRule ForSlotGroup(FurnitureSlotKind kind, byte groupId) => new()
        {
            Mode = ReplaceMode.SlotGroup,
            SlotKind = kind,
            GroupId = groupId
        };

        public readonly bool RequiresSchemeMaterial =>
            Mode is ReplaceMode.Slot or ReplaceMode.SlotGroup
            && SlotKind != FurnitureSlotKind.None;

        public readonly FurnitureSlotKind MaterialSlotKind =>
            Mode is ReplaceMode.Slot or ReplaceMode.SlotGroup ? SlotKind : FurnitureSlotKind.None;
    }
}
