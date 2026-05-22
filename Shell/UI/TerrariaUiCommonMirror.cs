using Microsoft.Xna.Framework;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>与 tModLoader 公开 <c>UICommon</c> 字段一致的常量镜像（编译期不依赖 UICommon 类型）。</summary>
    internal static class TerrariaUiCommonMirror
    {
        public static readonly Color DefaultUIBlue = new Color(73, 94, 171);
        public static readonly Color DefaultUIBlueMouseOver = new Color(63, 82, 151) * 0.7f;
        public static readonly Color MainPanelBackground = new Color(33, 43, 79) * 0.8f;
        public static readonly Color DefaultUIBorder = Color.Black;
    }
}
