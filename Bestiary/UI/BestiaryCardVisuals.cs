namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>图鉴网格卡片视觉常量。</summary>
    internal static class BestiaryCardVisuals
    {
        /// <summary>Slot_Back / 生态背景图不透明度（10%）。</summary>
        public const float BackgroundImageAlpha = 0.1f;

        /// <summary>网格悬停肖像动画：每 N 游戏帧才 Update 一次（60FPS 下 N=2 ≈ 30 步/秒，接近原版体感）。</summary>
        public const int GridIconAnimationInterval = 2;
    }
}
