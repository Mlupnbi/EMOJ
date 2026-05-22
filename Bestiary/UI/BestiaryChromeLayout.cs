using System;

using Terraria.UI;

using EvenMoreOverpoweredJourney.Shell.UI;



namespace EvenMoreOverpoweredJourney.Bestiary.UI

{

    /// <summary>

    /// 三窗：筛选贴主窗上方（宽=主窗、高减 1/3、间距 10px）；详情与主窗等高并排。

    /// </summary>

    internal static class BestiaryChromeLayout

    {

        public const float ColumnGap = 6f;

        /// <summary>筛选窗与主窗顶缘的安全间距。</summary>
        public const float FilterAboveMainGap = 16f;

        /// <summary>筛选窗 1:1 布局，避免逻辑宽大于视觉宽导致裁切。</summary>
        public const float FilterDisplayScale = 1f;

        /// <summary>筛选窗宽度 = 主窗宽度（恢复满宽；勿再缩小 X）。</summary>
        public const float FilterWidthRatio = 1f;

        /// <summary>筛选窗高度 = 默认二级窗高度 × 本系数（在 1/2 基础上再减 1/3 ≈ 原高度的 1/3）。</summary>
        public const float FilterHeightRetainRatio = 1f / 3f;



        public readonly struct Layout

        {

            public readonly float FilterLeft;

            public readonly float FilterTop;

            public readonly float FilterWidth;

            public readonly float FilterHeight;

            public readonly float FilterLogicalWidth;

            public readonly float FilterLogicalHeight;

            public readonly float DetailLeft;

            public readonly float DetailTop;

            public readonly float DetailWidth;

            public readonly float DetailHeight;



            public bool HasFilter => FilterWidth > 1f;

            public bool HasDetail => DetailWidth > 1f;



            public Layout(

                float filterLeft,

                float filterTop,

                float filterWidth,

                float filterHeight,

                float filterLogicalWidth,

                float filterLogicalHeight,

                float detailLeft,

                float detailTop,

                float detailWidth,

                float detailHeight)

            {

                FilterLeft = filterLeft;

                FilterTop = filterTop;

                FilterWidth = filterWidth;

                FilterHeight = filterHeight;

                FilterLogicalWidth = filterLogicalWidth;

                FilterLogicalHeight = filterLogicalHeight;

                DetailLeft = detailLeft;

                DetailTop = detailTop;

                DetailWidth = detailWidth;

                DetailHeight = detailHeight;

            }

        }



        public static Layout Compute(CalculatedStyle main, bool filterOpen, bool detailOpen)

        {

            float detailW = BestiaryVanillaDetailMetrics.Width;
            float detailLeft = main.X + main.Width + ColumnGap;
            float detailTop = main.Y;
            float detailH = System.Math.Min(main.Height, BestiaryVanillaDetailMetrics.Height);



            if (!filterOpen && !detailOpen)

            {

                return new Layout(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            }



            float filterVisualW = 0f;

            float filterVisualH = 0f;

            float filterLogicalW = 0f;

            float filterLogicalH = 0f;

            float filterLeft = 0f;

            float filterTop = 0f;



            if (filterOpen)

            {

                OPJourneyShellMetrics.EnsureSecondarySize();

                float baseVisualH = OPJourneyShellMetrics.FixedSecondaryHeight * FilterDisplayScale;

                float mainW = System.Math.Max(main.Width, OPJourneyShellMetrics.DefaultMainWidth);
                filterVisualW = mainW * FilterWidthRatio;
                filterVisualH = baseVisualH * FilterHeightRetainRatio;
                filterLogicalW = filterVisualW / FilterDisplayScale;
                filterLogicalH = filterVisualH / FilterDisplayScale;
                filterLeft = main.X;

                filterTop = main.Y - filterVisualH - FilterAboveMainGap;

            }



            if (!detailOpen)

            {

                return new Layout(

                    filterLeft,

                    filterTop,

                    filterVisualW,

                    filterVisualH,

                    filterLogicalW,

                    filterLogicalH,

                    0,

                    0,

                    0,

                    0);

            }



            return new Layout(

                filterLeft,

                filterTop,

                filterVisualW,

                filterVisualH,

                filterLogicalW,

                filterLogicalH,

                detailLeft,

                detailTop,

                detailW,

                detailH);

        }

    }

}


