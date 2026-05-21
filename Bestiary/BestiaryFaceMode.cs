namespace EvenMoreOverpoweredJourney.Bestiary
{
    /// <summary>超级生物图鉴四张脸（左→右）。</summary>
    public enum BestiaryFaceMode
    {
        /// <summary>全部可见：列表/合影均显示立绘+名；详情暂不展示掉落/抗性（见定稿）。</summary>
        AllVisible = 0,

        /// <summary>渐进式解锁+：未遇见=黑色剪影+名（卡片与列表一致）。</summary>
        ProgressivePlus = 1,

        /// <summary>仅已发现（原渐进-）：未遇见不显示；右下角待发现计数。</summary>
        ProgressiveMinus = 2,

        /// <summary>仅未解锁：只显示 Tracker 未完整解锁条目。</summary>
        UnlockedOnly = 3
    }
}
