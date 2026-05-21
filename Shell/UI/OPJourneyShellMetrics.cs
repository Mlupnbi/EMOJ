using System;
using Terraria.GameContent;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>主壳默认尺寸、锁死抬头区宽度、二级窗体固定尺寸（不随主窗拉伸）。</summary>
    public static class OPJourneyShellMetrics
    {
        public const float DefaultMainWidth = 400f;
        public const float DefaultMainHeight = 600f;
        public const float MinMainWidth = 400f;
        public const float MinMainHeight = 600f;
        /// <summary>右下角拉伸手柄命中/绘制区域（14×14）。</summary>
        public const float ResizeHandleSize = 14f;
        public const int HandleTexturePx = 14;
        /// <summary>主窗内容区底部留白，滚动条不贴底以露出拉伸手柄。</summary>
        public const float ContentBottomSafeMargin = 12f;
        public const float TitleBarHeight = 32f;
        /// <summary>页签内抬头/工具条锁死宽度（不随主窗变宽而拉伸）。</summary>
        public const float ChromeWidth = 370f;
        public const float ContentInsetLeft = 10f;
        public const float ResearchBottomStripHeight = 40f;
        /// <summary>列表滚动条距内容区右缘的安全留白（像素）。</summary>
        public const float ScrollSafeMarginRight = 14f;

        private static float _fixedSecondaryWidth = -1f;
        private static float _fixedSecondaryHeight = -1f;

        public static float FixedSecondaryWidth
        {
            get { EnsureSecondarySize(); return _fixedSecondaryWidth; }
        }

        public static float FixedSecondaryHeight
        {
            get { EnsureSecondarySize(); return _fixedSecondaryHeight; }
        }

        public static void EnsureSecondarySize()
        {
            if (_fixedSecondaryWidth > 0f)
                return;

            int invW = TextureAssets.InventoryBack.Value.Width;
            float cellW = invW * 0.56f * 1.2f + 4f;
            const float padL = 5f;
            const float padR = 15f;
            const float scrollBarW = 18f;
            float minInnerFive = 5f * cellW + padL + padR + scrollBarW + 8f;
            _fixedSecondaryWidth = Math.Max(DefaultMainWidth * (2f / 3f) * (5f / 6f), minInnerFive);
            _fixedSecondaryHeight = DefaultMainHeight * 0.8f;
        }
    }
}
