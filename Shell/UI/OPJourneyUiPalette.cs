using Microsoft.Xna.Framework;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>
    /// 碧绿色系设计令牌（参考 Aquamarine / MediumAquamarine #66CDAA 同类配色与 UI 分层实践）。
    /// 非“整屏叠色遮罩”：各角色使用不同 RGB，仅面板大底用透明度。
    /// </summary>
    public static class OPJourneyUiPalette
    {
        // —— 品牌阶梯（用户指定）——
        public static readonly Color Tier0Highlight = new Color(127, 255, 212);
        public static readonly Color Tier1Light = new Color(118, 238, 198);
        public static readonly Color Tier2Brand = new Color(102, 205, 170);
        public static readonly Color Tier3Deep = new Color(69, 139, 116);

        // —— 衍生表面（同色相、不同明度，用于按钮/卡片/搜索框）——
        public static readonly Color SurfaceCanvas = Tier2Brand;
        public static readonly Color SurfaceRaised = Tier1Light;
        public static readonly Color SurfaceInset = new Color(52, 112, 94);
        public static readonly Color SurfacePressed = new Color(45, 98, 82);
        public static readonly Color SurfaceSlot = new Color(58, 118, 100);

        // —— 描边与分隔线 ——
        public static readonly Color BorderStrong = Tier0Highlight;
        public static readonly Color BorderDefault = new Color(92, 188, 158);
        public static readonly Color BorderSubtle = new Color(78, 158, 132);
        public static readonly Color LineDivider = new Color(72, 148, 124);
        public static readonly Color LineDetailRule = new Color(88, 168, 142);

        // —— 文字（青底上）——
        public static readonly Color TextOnTealPrimary = new Color(245, 255, 250);
        public static readonly Color TextOnTealSecondary = new Color(210, 240, 228);
        public static readonly Color TextOnTealMuted = new Color(170, 210, 195);

        // —— 强调（互补暖色，仅用于选中/危险）——
        public static readonly Color AccentWarm = new Color(255, 210, 120);
        public static readonly Color DangerFill = new Color(168, 58, 58);
        public static readonly Color DangerBorder = new Color(220, 100, 100);

        public static Color WithAlpha(Color c, float alpha) => c * alpha;

        /// <summary>面板大底：品牌色 + 透明度（仅此处用 alpha，非全局遮罩）。</summary>
        public static Color PanelGlass(float alpha = 0.7f) => WithAlpha(SurfaceCanvas, alpha);
    }
}
