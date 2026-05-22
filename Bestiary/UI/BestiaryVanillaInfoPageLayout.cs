using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>原版详情页：仅缩放立绘区，不移动内部 UIScrollbar（避免拖滑块无效）。</summary>
    internal static class BestiaryVanillaInfoPageLayout
    {
        public static void Apply(UIBestiaryEntryInfoPage page)
        {
            if (page == null)
                return;

            float innerW = page.GetInnerDimensions().Width;
            if (innerW < 40f)
                innerW = BestiaryVanillaDetailMetrics.Width - BestiaryVanillaDetailMetrics.ContentPadLeft * 2f;

            ScalePortraitHosts(page, innerW);
        }

        private static void ScalePortraitHosts(UIElement root, float panelInnerW)
        {
            float maxW = Math.Max(120f, panelInnerW - 12f);

            foreach (UIElement child in root.Children)
            {
                if (IsPortraitContainer(child))
                    ApplyPortraitScale(child, maxW);

                ScalePortraitHosts(child, panelInnerW);
            }
        }

        private static void ApplyPortraitScale(UIElement host, float targetWidth)
        {
            CalculatedStyle d = host.GetDimensions();
            float srcW = Math.Max(80f, d.Width > 1f ? d.Width : targetWidth);
            float srcH = Math.Max(60f, d.Height > 1f ? d.Height : targetWidth * 0.72f);
            float aspect = srcH / srcW;
            float w = targetWidth;
            float h = w * aspect;

            host.HAlign = 0.5f;
            host.Left.Set(0f, 0f);
            host.Width.Set(w, 0f);
            host.Height.Set(h, 0f);

            foreach (UIElement child in host.Children)
            {
                if (child is UIImage || IsPortraitImageChild(child))
                {
                    child.HAlign = 0.5f;
                    child.Left.Set(0f, 0f);
                    child.Width.Set(0f, 1f);
                    child.Height.Set(0f, 1f);
                }
            }
        }

        private static bool IsPortraitImageChild(UIElement el)
        {
            string name = el.GetType().Name;
            return name.Contains("Portrait", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Pretty", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("IconDisplay", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPortraitContainer(UIElement el)
        {
            if (el == null)
                return false;

            string name = el.GetType().Name;
            return name.Contains("Portrait", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("IconDisplay", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Pretty", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("UIBestiaryNPCEntryPortrait", StringComparison.OrdinalIgnoreCase);
        }
    }
}
