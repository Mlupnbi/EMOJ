using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>主窗页签列表/滚动条统一底部留白，避免滑条占满右下角拉伸区。</summary>
    internal static class ShellUiScrollLayout
    {
        public static float BottomReserve => OPJourneyShellMetrics.ContentBottomSafeMargin;

        public static void ApplyVerticalRange(UIElement list, UIScrollbar scrollbar, float top)
        {
            float heightNeg = top + BottomReserve;
            list.Top.Set(top, 0f);
            list.Height.Set(-heightNeg, 1f);
            if (scrollbar == null)
                return;

            scrollbar.Top.Set(top, 0f);
            scrollbar.Height.Set(-heightNeg, 1f);
        }

        public static void ApplyVerticalRange(UIElement list, UIScrollbar scrollbar, float top, float extraBottom)
        {
            float heightNeg = top + BottomReserve + extraBottom;
            list.Top.Set(top,  0f);
            list.Height.Set(-heightNeg, 1f);
            if (scrollbar == null)
                return;

            scrollbar.Top.Set(top, 0f);
            scrollbar.Height.Set(-heightNeg, 1f);
        }
    }
}
