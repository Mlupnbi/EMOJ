namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>样板房放置策略：Strict 缺件整次拒绝；Loose 缺件跳过其余继续。</summary>
    public enum BlueprintPlacementMode : byte
    {
        Strict = 0,
        Loose = 1
    }
}
