namespace EvenMoreOverpoweredJourney.Research
{
    /// <summary>研究页「表情」筛选模式（与 NPC 快乐 UI 四张脸对应）。</summary>
    public enum ResearchFaceMode
    {
        Yellow = 0,
        Green = 1,
        Blue = 2,
        Purple = 3
    }

    /// <summary>产物格子在列表中的强调背景语义。</summary>
    public enum ResearchProductTint
    {
        None,
        BlueResearched,
        GreenResearchable,
        RedUnresearched,
        PurpleCraftable,
        PurpleCannotCraft
    }
}
