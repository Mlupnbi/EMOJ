using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>??? Slot_Back / ???? / Slot_Front???????????</summary>
    internal static class BestiaryVanillaSlotRenderer
    {
        private static MethodInfo _getBackgroundMethod;

        /// <summary>仅 Slot_Back + 生态图（不含 Slot_Front），Draw 时乘 alpha。</summary>
        public static void DrawBackground(
            SpriteBatch spriteBatch,
            BestiaryEntry entry,
            Rectangle bounds,
            float backgroundAlpha = BestiaryCardVisuals.BackgroundImageAlpha)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            BestiaryUiAssets.EnsureLoaded();
            Texture2D slotBack = BestiaryUiAssets.SlotBack;
            if (slotBack == null)
                return;

            if (!TryResolveBackground(entry, out var bg))
                bg = default;

            Color tint = bg.Color;
            if (tint.A == 0)
                tint = new Color(45, 48, 72);

            float alpha = Math.Clamp(backgroundAlpha, 0f, 1f);
            tint = new Color(
                (byte)(tint.R * alpha),
                (byte)(tint.G * alpha),
                (byte)(tint.B * alpha),
                (byte)(tint.A * alpha));

            if (!string.IsNullOrEmpty(bg.ImagePath))
            {
                try
                {
                    Texture2D bgTex = Main.Assets.Request<Texture2D>(bg.ImagePath).Value;
                    if (bgTex != null)
                    {
                        spriteBatch.Draw(bgTex, bounds, tint);
                        return;
                    }
                }
                catch
                {
                    // fallback to slot back
                }
            }

            spriteBatch.Draw(slotBack, bounds, tint);
        }

        public static void DrawSlot(
            SpriteBatch spriteBatch,
            BestiaryEntry entry,
            Rectangle contentBounds,
            float backgroundAlpha = BestiaryCardVisuals.BackgroundImageAlpha)
        {
            if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
                return;

            BestiaryUiAssets.EnsureLoaded();
            Texture2D slotBack = BestiaryUiAssets.SlotBack;
            Texture2D slotFront = BestiaryUiAssets.SlotFront;
            if (slotBack == null && slotFront == null)
                return;

            if (!TryResolveBackground(entry, out var bg))
                bg = default;

            Color tint = bg.Color;
            if (tint.A == 0)
                tint = new Color(45, 48, 72);

            float alpha = Math.Clamp(backgroundAlpha, 0f, 1f);
            tint = new Color(
                (byte)(tint.R * alpha),
                (byte)(tint.G * alpha),
                (byte)(tint.B * alpha),
                (byte)(tint.A * alpha));

            if (slotBack != null)
            {
                if (!string.IsNullOrEmpty(bg.ImagePath))
                {
                    try
                    {
                        Texture2D bgTex = Main.Assets.Request<Texture2D>(bg.ImagePath).Value;
                        if (bgTex != null)
                            spriteBatch.Draw(bgTex, contentBounds, tint);
                    }
                    catch
                    {
                        spriteBatch.Draw(slotBack, contentBounds, tint);
                    }
                }
                else
                {
                    spriteBatch.Draw(slotBack, contentBounds, tint);
                }
            }

            if (slotFront != null)
                spriteBatch.Draw(slotFront, contentBounds, Color.White);
        }

        private static bool TryResolveBackground(BestiaryEntry entry, out (string ImagePath, Color Color) result)
        {
            result = default;
            if (entry?.Info == null)
                return false;

            for (int i = 0; i < entry.Info.Count; i++)
            {
                IBestiaryInfoElement el = entry.Info[i];
                if (el == null)
                    continue;

                if (TryReadBackgroundProvider(el, out result))
                    return true;

                string typeName = el.GetType().Name;
                if (!typeName.Contains("BestiaryPortraitBackground", StringComparison.OrdinalIgnoreCase))
                    continue;

                object provider = TryInvokeGetProvider(el);
                if (provider != null && TryReadProvider(provider, out result))
                    return true;
            }

            return false;
        }

        private static bool TryReadBackgroundProvider(IBestiaryInfoElement el, out (string ImagePath, Color Color) result)
        {
            result = default;
            Type t = el.GetType();
            if (!t.Name.Contains("Background", StringComparison.OrdinalIgnoreCase))
                return false;

            object provider = TryInvokeGetProvider(el);
            return provider != null && TryReadProvider(provider, out result);
        }

        private static object TryInvokeGetProvider(object el)
        {
            Type t = el.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            MethodInfo getProvider = _getBackgroundMethod ??= FindProviderMethod(t);
            if (getProvider == null)
            {
                foreach (MethodInfo m in t.GetMethods(flags))
                {
                    if (m.Name.Contains("Background", StringComparison.OrdinalIgnoreCase) &&
                        m.GetParameters().Length == 0 &&
                        m.ReturnType != typeof(void))
                    {
                        getProvider = m;
                        break;
                    }
                }
            }

            if (getProvider == null)
                return null;

            try
            {
                return getProvider.Invoke(el, null);
            }
            catch
            {
                return null;
            }
        }

        private static MethodInfo FindProviderMethod(Type t)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return t.GetMethod("GetPreferredBackgroundProvider", flags) ??
                   t.GetMethod("GetBackgroundProvider", flags);
        }

        private static bool TryReadProvider(object provider, out (string ImagePath, Color Color) result)
        {
            result = default;
            if (provider == null)
                return false;

            Type t = provider.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            string path = null;
            PropertyInfo pathProp = t.GetProperty("BackgroundImagePath", flags) ??
                                    t.GetProperty("GetBackgroundImagePath", flags);
            if (pathProp != null)
            {
                if (pathProp.PropertyType == typeof(string))
                    path = pathProp.GetValue(provider) as string;
                else if (pathProp.GetMethod != null)
                {
                    try
                    {
                        path = pathProp.GetGetMethod(true)?.Invoke(provider, null) as string;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                MethodInfo getPath = t.GetMethod("GetBackgroundImagePath", flags);
                if (getPath?.ReturnType == typeof(string))
                {
                    try
                    {
                        path = getPath.Invoke(provider, null) as string;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            Color color = Color.White;
            PropertyInfo colorProp = t.GetProperty("BackgroundColor", flags);
            if (colorProp?.PropertyType == typeof(Color))
            {
                try
                {
                    color = (Color)colorProp.GetValue(provider);
                }
                catch
                {
                    color = new Color(45, 48, 72);
                }
            }
            else
            {
                MethodInfo getColor = t.GetMethod("GetBackgroundColor", flags);
                if (getColor?.ReturnType == typeof(Color))
                {
                    try
                    {
                        color = (Color)getColor.Invoke(provider, null);
                    }
                    catch
                    {
                        color = new Color(45, 48, 72);
                    }
                }
            }

            result = (path ?? "", color);
            return true;
        }
    }
}
