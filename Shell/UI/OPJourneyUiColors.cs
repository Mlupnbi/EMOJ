using Microsoft.Xna.Framework;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>壳层 UI 语义色 — 全部映射到 <see cref="OPJourneyUiPalette"/> 设计令牌。</summary>
    public static class OPJourneyUiColors
    {
        // —— 窗体 ——
        public static readonly Color MainPanelBackground = OPJourneyUiPalette.PanelGlass(0.7f);
        public static readonly Color PanelBackground = MainPanelBackground;
        public static readonly Color PanelBorder = OPJourneyUiPalette.BorderDefault;
        public static readonly Color GridBackdrop = OPJourneyUiPalette.WithAlpha(OPJourneyUiPalette.SurfaceInset, 0.55f);

        // —— 壳页签 ——
        public static readonly Color TabBackground = OPJourneyUiPalette.Tier3Deep;
        public static readonly Color TabInactiveBackground = OPJourneyUiPalette.SurfacePressed;
        public static readonly Color TabActiveBackground = OPJourneyUiPalette.Tier2Brand;
        public static readonly Color TabInactiveBorder = OPJourneyUiPalette.BorderSubtle;
        public static readonly Color TabActiveBorder = OPJourneyUiPalette.BorderStrong;
        public static readonly Color TabIconInactiveTint = OPJourneyUiPalette.TextOnTealSecondary;

        // —— 通用按钮 ——
        public static readonly Color ButtonBackground = OPJourneyUiPalette.SurfacePressed;
        public static readonly Color ButtonBackgroundHover = OPJourneyUiPalette.SurfaceInset;
        public static readonly Color ButtonBackgroundOpen = OPJourneyUiPalette.Tier2Brand;
        public static readonly Color ButtonBorder = OPJourneyUiPalette.BorderSubtle;
        public static readonly Color ButtonBorderOpen = OPJourneyUiPalette.BorderStrong;
        public static readonly Color ButtonGrayedBackground = OPJourneyUiPalette.WithAlpha(OPJourneyUiPalette.SurfacePressed, 0.5f);

        // —— 筛选窗外挂竖钮 ——
        public static readonly Color SecondaryTabOnBackground = OPJourneyUiPalette.Tier2Brand;
        public static readonly Color SecondaryTabOffBackground = OPJourneyUiPalette.SurfacePressed;
        public static readonly Color SecondaryTabOnBorder = OPJourneyUiPalette.BorderStrong;
        public static readonly Color SecondaryTabOffBorder = OPJourneyUiPalette.BorderSubtle;

        // —— 重置 ——
        public static readonly Color DangerBackground = OPJourneyUiPalette.DangerFill;
        public static readonly Color DangerBorder = OPJourneyUiPalette.DangerBorder;
        public static readonly Color DangerText = OPJourneyUiPalette.TextOnTealPrimary;

        // —— 选中描边 ——
        public static readonly Color AccentGoldOutline = OPJourneyUiPalette.AccentWarm;
        public static readonly Color AccentCyanOutline = OPJourneyUiPalette.Tier1Light;

        // —— 搜索框（嵌入面板，比大底更深）——
        public static readonly Color SearchBarBackground = OPJourneyUiPalette.SurfaceInset;
        public static readonly Color SearchBarBorder = OPJourneyUiPalette.BorderDefault;

        // —— 滚动条（105,139,105 / #4b634b）——
        public static readonly Color ScrollTrack = new Color(105, 139, 105);
        public static readonly Color ScrollBorder = new Color(0x4b, 0x63, 0x4b);
        public static readonly Color ScrollThumb = new Color(120, 158, 120);
        public static readonly Color ScrollThumbHover = new Color(135, 175, 135);

        // —— 详情页 ——
        public static readonly Color DetailPanelBackground = OPJourneyUiPalette.WithAlpha(OPJourneyUiPalette.SurfaceInset, 0.92f);
        public static readonly Color DetailPanelBorder = OPJourneyUiPalette.BorderDefault;
        public static readonly Color DetailDividerLine = new Color(105, 139, 105);
        public static readonly Color DetailSectionText = OPJourneyUiPalette.TextOnTealSecondary;
        public static readonly Color DetailBodyText = OPJourneyUiPalette.TextOnTealPrimary;

        // —— 图鉴格子 ——
        public static readonly Color SlotCellFill = OPJourneyUiPalette.SurfaceSlot;
        public static readonly Color SlotCellBorder = OPJourneyUiPalette.BorderSubtle;

        // —— 研究页语义 rim（保留色相）——
        public static readonly Color RecipeRowBackground = OPJourneyUiPalette.WithAlpha(OPJourneyUiPalette.SurfacePressed, 0.9f);
        public static readonly Color ResearchBlueRim = new Color(50, 140, 220) * 0.65f;
        public static readonly Color ResearchGreenRim = new Color(60, 200, 60) * 0.6f;
        public static readonly Color ResearchRedRim = new Color(220, 70, 70) * 0.65f;

        // —— 关闭钮（与详情分隔线同色）——
        public static readonly Color CloseButtonFill = DetailDividerLine;
        public static readonly Color CloseButtonMark = Color.Transparent;

        // —— 正文 ——
        public static readonly Color TextPrimary = OPJourneyUiPalette.TextOnTealPrimary;
        public static readonly Color TextMuted = OPJourneyUiPalette.TextOnTealMuted;
        public static readonly Color TextHint = OPJourneyUiPalette.WithAlpha(OPJourneyUiPalette.TextOnTealMuted, 0.85f);
        public static readonly Color TextShadow = Color.Black;

        // —— 脸 fallback ——
        public static readonly Color FaceFallback0 = OPJourneyUiPalette.Tier0Highlight;
        public static readonly Color FaceFallback1 = OPJourneyUiPalette.Tier1Light;
        public static readonly Color FaceFallback2 = OPJourneyUiPalette.Tier2Brand;
        public static readonly Color FaceFallback3 = OPJourneyUiPalette.Tier3Deep;

        // —— 槽位遮罩 ——
        public static readonly Color SlotLockedOverlay = Color.Black * 0.5f;
        public static readonly Color SlotDimOverlayStrong = Color.Black * 0.55f;
        public static readonly Color SlotDimOverlayLight = Color.Black * 0.28f;
        public static readonly Color HubSlotSilhouette = Color.Black * 0.58f;

        // —— 研究 Strip ——
        public static readonly Color ResearchStripButton = OPJourneyUiPalette.SurfacePressed;
        public static readonly Color ResearchStripButtonHighlight = OPJourneyUiPalette.Tier1Light;
        public static readonly Color ButtonActionSuccess = new Color(52, 128, 98);
        public static readonly Color ButtonActionWarm = new Color(196, 158, 72);

        // —— 稀有度条 ——
        public static readonly Color RareRangeTrack = OPJourneyUiPalette.WithAlpha(OPJourneyUiPalette.Tier3Deep, 0.8f);
        public static readonly Color RareRangeMarker = OPJourneyUiPalette.AccentWarm;

        // —— Tooltip ——
        public static readonly Color TooltipOverride = OPJourneyUiPalette.TextOnTealMuted;

        // —— 拖拽手柄 ——
        public static readonly Color DragHandleGold = OPJourneyUiPalette.Tier0Highlight;
    }

    public static class BestiaryUiColors
    {
        public static Color PanelBackground => OPJourneyUiColors.PanelBackground;
        public static Color PanelBorder => OPJourneyUiColors.PanelBorder;
        public static Color GridBackdrop => OPJourneyUiColors.GridBackdrop;
        public static Color MainPanelBackground => OPJourneyUiColors.MainPanelBackground;
    }
}
