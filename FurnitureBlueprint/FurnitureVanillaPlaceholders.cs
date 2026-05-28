using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>套组缺件时用于预览的原版「木材」占位物品（染红提示缺失）。</summary>
    public static class FurnitureVanillaPlaceholders
    {
        public static int Get(FurnitureSlotKind kind) => kind switch
        {
            FurnitureSlotKind.Block => ItemID.Wood,
            FurnitureSlotKind.Platform => ItemID.WoodPlatform,
            FurnitureSlotKind.Wall => ItemID.WoodWall,
            FurnitureSlotKind.Workbench => ItemID.WorkBench,
            FurnitureSlotKind.Table => ItemID.WoodenTable,
            FurnitureSlotKind.Chair => ItemID.WoodenChair,
            FurnitureSlotKind.Door => ItemID.WoodenDoor,
            FurnitureSlotKind.Chest => ItemID.Chest,
            FurnitureSlotKind.Bed => ItemID.Bed,
            FurnitureSlotKind.Bookcase => ItemID.Bookcase,
            FurnitureSlotKind.Bathtub => ItemID.Bathtub,
            FurnitureSlotKind.Candelabra => ItemID.Candelabra,
            FurnitureSlotKind.Candle => ItemID.Candle,
            FurnitureSlotKind.Chandelier => ItemID.Candelabra,
            FurnitureSlotKind.Clock => ItemID.GrandfatherClock,
            FurnitureSlotKind.Dresser => ItemID.Dresser,
            FurnitureSlotKind.Lamp => ItemID.Candle,
            FurnitureSlotKind.Lantern => ItemID.Torch,
            FurnitureSlotKind.Piano => ItemID.Piano,
            FurnitureSlotKind.Sink => ItemID.BottledWater,
            FurnitureSlotKind.Sofa => ItemID.Bench,
            FurnitureSlotKind.Toilet => ItemID.Toilet,
            _ => ItemID.Wood
        };
    }
}
