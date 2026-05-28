using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{

    /// <summary>家具蓝图页垂直分区（紧凑抬头 + 套组区 + 已保存区）。</summary>

    internal static class FurnitureBlueprintPageLayout

    {

        public const float QuerySlotSize = 52f;

        public const float QueryRowHeight = QuerySlotSize;



        public const float MaterialSlotSize = 40f;

        public const float MaterialRowHeight = MaterialSlotSize;



        public const float ToolbarGap = 4f;

        public const float ToolbarPadBottom = 2f;



        public static float ToolbarHeight =>

            QueryRowHeight + ToolbarGap + MaterialRowHeight + ToolbarPadBottom;



        public const float MaterialFoldButtonSize = 18f;

        public const float MaterialFoldButtonGap = 6f;



        public const float ToolbarActionButtonWidth = 92f;

        public const float ToolbarActionButtonHeight = 28f;

        public const float ToolbarActionButtonGap = 8f;

        /// <summary>材料折叠键与「保存套组」之间的间距。</summary>
        public const float ToolbarActionGapAfterFold = 8f;



        public const float GridSectionGapBelowToolbar = 24f;

        public const float GridHeaderHeight = 18f;

        public const float GridTitleToGridGap = 2f;

        public const int SlotGridTargetColumns = 8;

        public const int SlotGridVisibleRows = 3;

        public const float GridDescLineHeight = 0f;

        public const float GridDescLines = 0f;

        public const float GridDescGap = 0f;



        public const float GridSavedGap = 12f;

        public const float SlotGridPadBottom = 4f;

        /// <summary>空套组时的最小占位高度（勿过大以免压住工具栏）。</summary>
        public const float SlotGridMinHeight = 56f;

        /// <summary>8 列网格内容区宽度（不含滚动条）。</summary>
        public static float SlotGridContentWidth =>
            BlueprintSlotGridLayoutMetrics.ContentWidthForColumns(SlotGridTargetColumns);

        /// <summary>套组窗体 host 宽度（内容 + 滚动条 + 左右留白）。</summary>
        /// <summary>8 列内容 + 页内滚动条宽度（不含左右 ContentInset，host 实际用满行宽）。</summary>
        public static float SlotGridHostWidth =>
            BlueprintSlotGridLayoutMetrics.HostWidthForColumns(
                SlotGridTargetColumns,
                EojUIScrollbar.DefaultWidth);

        /// <summary>家具蓝图页工具条/套组区锁宽（与大框左对齐）。</summary>
        public static float BlueprintChromeWidth => SlotGridHostWidth;

        /// <summary>3 行槽位+槽名标签的视口高度。</summary>
        public static float SlotGridViewportHeight =>
            BlueprintSlotGridLayoutMetrics.ViewportHeightForRows(SlotGridVisibleRows, SlotGridPadBottom);

        /// <summary>主壳默认宽度：容纳 8 列套组 + 左内边距 + 右滚动安全区。</summary>
        public static float RecommendedShellWidth =>
            BlueprintSlotGridLayoutMetrics.RecommendedShellWidth(
                BlueprintChromeWidth,
                OPJourneyShellMetrics.ContentInsetLeft,
                OPJourneyShellMetrics.ScrollSafeMarginRight);



        public const float SavedPanelMinHeight = 120f;

        public const float NewEmptySetButtonWidth = 96f;

        public const float SchemeRowCoverSize = 48f;
        public const float SchemeRowHeight = 56f;

        public const float BottomBarHeight = 52f;

        public const float ModeHintHeight = 32f;



        public static float GridSectionTop => ToolbarHeight + GridSectionGapBelowToolbar;

        // --- 二级窗体默认尺寸（Phase 0） ---
        public const float SecondaryTitleScale = 0.82f;
        public const float SecondaryTitleBarHeight = 32f;
        public const float SecondaryPanelPadding = 12f;

        public const float SetLibraryPanelWidth = 480f;
        public const float SetLibraryPanelHeight = 520f;
        public const float SetDetailPanelWidth = 560f;
        public const float SetDetailPanelHeight = 480f;
        public const float TemplatePanelWidth = 640f;
        public const float TemplatePanelHeight = 560f;
        public const float TemplateCardHeight = 64f;
        public const float TemplatePreviewMinHeight = 280f;

        // --- 主蓝图页 Hub 区（Phase 0 瘦身后） ---
        public const float HubStatusGap = 12f;
        public const float SummaryRowHeight = 22f;
        public const float SavePromptRowHeight = 18f;
        public const float HubActionRowHeight = 32f;
        public const float HubActionButtonWidth = 92f;
        public const float HubActionButtonHeight = 28f;
        public const float HubActionButtonGap = 8f;
        public static float MainHubContentHeight =>
            ToolbarHeight + HubStatusGap + SummaryRowHeight + SavePromptRowHeight + GridSectionGapBelowToolbar
            + GridHeaderHeight + GridTitleToGridGap + SlotGridViewportHeight
            + GridSavedGap + GridHeaderHeight + SavedPanelMinHeight
            + OPJourneyShellMetrics.ContentBottomSafeMargin;

        public const float BottomActionRowHeight = 0f;

    }

}


