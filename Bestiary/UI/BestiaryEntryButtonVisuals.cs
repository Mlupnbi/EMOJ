using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>
    /// 在保留原版 EntryButton 分层的前提下，将背景层 Color 乘以 alpha。
    /// 若背景由 DrawSelf 直绘（无子节点），则无法调色——此时不剥离原版层。
    /// </summary>
    internal static class BestiaryEntryButtonVisuals
    {
        private static FieldInfo _imageColorField;
        private static FieldInfo _colorField;

        public static void ApplyPortraitBackgroundAlpha(UIBestiaryEntryButton button, float alpha)
        {
            if (button == null)
                return;

            float clamped = Math.Clamp(alpha, 0f, 1f);
            bool touched = false;

            foreach (UIElement child in button.Children)
            {
                if (TryTintBackgroundTree(child, clamped))
                    touched = true;
            }

            // 无背景子节点时由 EntryButton.DrawSelf 绘制，保持原版；不额外自绘以免错位
            if (!touched)
                return;
        }

        private static bool TryTintBackgroundTree(UIElement element, float alpha)
        {
            bool touched = false;

            if (IsPortraitBackgroundLayer(element))
            {
                MultiplyAlpha(element, alpha);
                touched = true;
            }

            foreach (UIElement child in element.Children)
            {
                if (TryTintBackgroundTree(child, alpha))
                    touched = true;
            }

            return touched;
        }

        private static void MultiplyAlpha(UIElement element, float alpha)
        {
            if (TryMultiplyFieldColor(element, ref _colorField, "Color", alpha))
                return;

            if (element is UIImage)
                TryMultiplyFieldColor(element, ref _imageColorField, "ImageColor", alpha);
        }

        private static bool TryMultiplyFieldColor(UIElement element, ref FieldInfo field, string name, float alpha)
        {
            field ??= element.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field?.GetValue(element) is not Color c)
                return false;

            field.SetValue(
                element,
                new Color(c.R, c.G, c.B, (byte)(c.A * alpha)));
            return true;
        }

        private static bool IsPortraitBackgroundLayer(UIElement element)
        {
            if (element is UIBestiaryEntryIcon)
                return false;

            if (element is UIText)
                return false;

            string name = element.GetType().Name;
            if (name.Contains("Front", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Border", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Index", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Label", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Icon", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return name.Contains("Back", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Background", StringComparison.OrdinalIgnoreCase);
        }
    }
}
