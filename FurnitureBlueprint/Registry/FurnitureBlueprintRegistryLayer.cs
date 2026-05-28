namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Registry
{
    /// <summary>
    /// Registry 层边界说明（Phase 2+ 建筑/放置专用，与 22 槽识别赋分隔离）。
    /// <list type="bullet">
    /// <item><see cref="FurnitureTileItemRegistry"/> — Tile/Wall → Item 反查</item>
    /// <item><see cref="FurnitureSetMaterialCheckers"/> — 22 槽物品是否匹配（IG CheckersForItem 思路）</item>
    /// <item><see cref="FurnitureSetMaterialValidator"/> — 背包是否具备套组槽位材料（放置路径）</item>
    /// </list>
    /// 识别管线（<c>FurnitureSetRecognizer</c> / <c>FurnitureSlotClassifier</c> / 赋分文件）不得引用本文件夹；
    /// 本文件夹仅只读 <see cref="FurnitureTileSlotRegistry"/> 作 mod 兜底，且不写入识别缓存。
    /// </summary>
    internal static class FurnitureBlueprintRegistryLayer
    {
    }
}
