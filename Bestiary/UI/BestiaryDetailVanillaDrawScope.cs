using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>仅当图鉴详情三级窗打开且元素位于 <see cref="UIBestiaryEntryInfoPage"/> 子树内。</summary>
    internal static class BestiaryDetailVanillaDrawScope
    {
        public static bool IsActive(UIElement element)
        {
            if (element == null || OPJourneyUI.Instance == null || !OPJourneyUI.Visible)
                return false;

            if (!(OPJourneyUI.Instance.BestiaryDetailPanel?.IsOpen ?? false))
                return false;

            bool inInfoPage = false;
            for (UIElement node = element; node != null; node = node.Parent)
            {
                if (node is UIBestiaryEntryInfoPage)
                    inInfoPage = true;
            }

            return inInfoPage;
        }
    }
}
