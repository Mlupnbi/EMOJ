namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 维基「家具套装」20 件 + 查询方块 + 官方墙 = 22 槽（与 datamap R/G 编码一致）。
    /// 见 https://terraria.wiki.gg/zh/wiki/%E5%AE%B6%E5%85%B7%E5%A5%97%E8%A3%85
    /// </summary>
    public enum FurnitureSlotKind : byte
    {
        None = 0,
        Block = 1,
        Wall = 2,
        Bathtub = 3,
        Bed = 4,
        Bookcase = 5,
        Candelabra = 6,
        Candle = 7,
        Chandelier = 8,
        Chair = 9,
        Chest = 10,
        Clock = 11,
        Door = 12,
        Dresser = 13,
        Lamp = 14,
        Lantern = 15,
        Piano = 16,
        Platform = 17,
        Sink = 18,
        Sofa = 19,
        Table = 20,
        Toilet = 21,
        Workbench = 22
    }

    public static class FurnitureSlotKinds
    {
        public const int Count = 22;

        public static FurnitureSlotKind FromIndex(int index) =>
            index >= 0 && index < Count ? (FurnitureSlotKind)(index + 1) : FurnitureSlotKind.None;

        public static int ToIndex(FurnitureSlotKind kind) =>
            kind == FurnitureSlotKind.None ? -1 : (int)kind - 1;
    }
}
