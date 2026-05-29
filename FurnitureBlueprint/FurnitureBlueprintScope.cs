namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 识别范围：严格模式下候选仅来自「材料实心块的配方产物」（+种子本身+墙），不做图格线/二次扩展。
    /// </summary>
    internal static class FurnitureBlueprintScope
    {
        public static bool StrictMaterialOnly { get; set; }
    }
}
