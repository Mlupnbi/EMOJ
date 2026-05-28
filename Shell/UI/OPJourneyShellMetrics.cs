using System;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.UI;
using Terraria.GameContent;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>主壳默认尺寸、锁死抬头区宽度、二级窗体固定尺寸（不随主窗拉伸）。</summary>
    public static class OPJourneyShellMetrics
    {
        /// <summary>默认宽度：按家具蓝图 8 列套组 + 滚动条推导（见 <see cref="FurnitureBlueprintPageLayout.RecommendedShellWidth"/>）。</summary>
        public static readonly float DefaultMainWidth = FurnitureBlueprintPageLayout.RecommendedShellWidth;

        /// <summary>主窗默认高度（与底边留白联动调整）。</summary>
        public const float DefaultMainHeight = 600f;

        public static readonly float MinMainWidth = DefaultMainWidth;
        public const float MinMainHeight = 600f;
        /// <summary>右下角拉伸手柄命中/绘制区域（相对初版 12px 放大 1.5 倍）。</summary>
        public const float ResizeHandleSize = 18f;
        public const int HandleTexturePx = 12;
        /// <summary>页内列表距内容区底边的留白（与壳层底栏对齐，各页签须共用）。</summary>
        public const float ContentBottomSafeMargin = 8f;
        /// <summary>主窗内容区为右下角拉伸手柄预留的可视高度（列表可延伸至此线）。</summary>
        public const float ContentLayoutBottomInset = 6f;
        public const float TitleBarHeight = 32f;
        /// <summary>页签内抬头/工具条锁死宽度（家具蓝图 8 列套组窗体同宽）。</summary>
        public static readonly float ChromeWidth = FurnitureBlueprintPageLayout.BlueprintChromeWidth;
        public const float ContentInsetLeft = 10f;
        public const float ResearchBottomStripHeight = 28f;
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

            int invW = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Width;
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
