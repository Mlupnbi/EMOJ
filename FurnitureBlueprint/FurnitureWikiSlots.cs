namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>维基家具套装 + 方块/墙 的 22 项识别顺序。</summary>
    public static class FurnitureWikiSlots
    {
        public static readonly FurnitureSlotKind[] RecognitionOrder =
        {
            FurnitureSlotKind.Block,
            FurnitureSlotKind.Wall,
            FurnitureSlotKind.Bathtub,
            FurnitureSlotKind.Bed,
            FurnitureSlotKind.Bookcase,
            FurnitureSlotKind.Candelabra,
            FurnitureSlotKind.Candle,
            FurnitureSlotKind.Chandelier,
            FurnitureSlotKind.Chair,
            FurnitureSlotKind.Chest,
            FurnitureSlotKind.Clock,
            FurnitureSlotKind.Door,
            FurnitureSlotKind.Dresser,
            FurnitureSlotKind.Lamp,
            FurnitureSlotKind.Lantern,
            FurnitureSlotKind.Piano,
            FurnitureSlotKind.Platform,
            FurnitureSlotKind.Sink,
            FurnitureSlotKind.Sofa,
            FurnitureSlotKind.Table,
            FurnitureSlotKind.Toilet,
            FurnitureSlotKind.Workbench
        };

        public const int TotalCount = 22;

        public static bool IsRecognitionSlot(FurnitureSlotKind kind)
        {
            for (int i = 0; i < RecognitionOrder.Length; i++)
            {
                if (RecognitionOrder[i] == kind)
                    return true;
            }
            return false;
        }

        public static FurnitureSlotKind NormalizeClassified(FurnitureSlotKind kind) => kind switch
        {
            FurnitureSlotKind.None => FurnitureSlotKind.None,
            _ when IsRecognitionSlot(kind) => kind,
            _ => FurnitureSlotKind.None
        };
    }
}
