using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    internal static class BestiaryFilterIconResolver
    {
        private static Type _byInfoElementType;

        public static UIElement GetFilterIconElement(IBestiaryEntryFilter filter) =>
            InvokeGetImage(filter);

        public static bool DrawInto(SpriteBatch spriteBatch, Rectangle target, IBestiaryEntryFilter filter, float alpha = 1f)
        {
            if (filter == null || target.Width < 2 || target.Height < 2)
                return false;

            if (TryGetIconFrame(filter, out Point frame))
            {
                BestiaryVanillaFilterIcons.DrawFilterIcon(spriteBatch, target, frame, alpha);
                return true;
            }

            UIElement image = InvokeGetImage(filter);
            if (image == null)
                return false;

            return TryDrawUiImage(spriteBatch, target, image, alpha);
        }

        public static bool TryGetIconFrame(IBestiaryEntryFilter filter, out Point frame)
        {
            frame = default;
            if (!TryGetProviderFromByInfo(filter, out IFilterInfoProvider provider))
                return false;

            FieldInfo frameField = typeof(FilterProviderInfoElement).GetField(
                "_filterIconFrame",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (frameField?.GetValue(provider) is Point p)
            {
                frame = p;
                return true;
            }

            return false;
        }

        private static UIElement InvokeGetImage(IBestiaryEntryFilter filter)
        {
            if (filter == null)
                return null;

            MethodInfo method = filter.GetType().GetMethod(
                "GetImage",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            if (method == null || !typeof(UIElement).IsAssignableFrom(method.ReturnType))
                return null;

            try
            {
                return method.Invoke(filter, null) as UIElement;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryDrawUiImage(SpriteBatch spriteBatch, Rectangle target, UIElement image, float alpha)
        {
            if (!TryResolveTexture(image, out Asset<Texture2D> asset, out Rectangle source))
                return false;

            DrawSource(spriteBatch, target, asset.Value, source, alpha);
            return true;
        }

        private static bool TryResolveTexture(UIElement image, out Asset<Texture2D> asset, out Rectangle source)
        {
            asset = null;
            source = default;

            if (image == null)
                return false;

            if (image is UIImageFramed framed)
            {
                asset = FindTextureAsset(framed);
                if (asset == null)
                    return false;

                FieldInfo frameField = typeof(UIImageFramed).GetField(
                    "_frame",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                source = frameField?.GetValue(framed) is Rectangle r && r.Width > 0
                    ? r
                    : asset.Frame();
                return true;
            }

            if (image is UIImage plain)
            {
                asset = FindTextureAsset(plain);
                if (asset == null)
                    return false;

                source = asset.Frame();
                return true;
            }

            asset = FindTextureAsset(image);
            if (asset == null)
                return false;

            source = asset.Frame();
            return true;
        }

        private static Asset<Texture2D> FindTextureAsset(UIElement image)
        {
            for (Type t = image.GetType(); t != null && t != typeof(object); t = t.BaseType)
            {
                FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (FieldInfo field in fields)
                {
                    if (!typeof(Asset<Texture2D>).IsAssignableFrom(field.FieldType))
                        continue;

                    try
                    {
                        if (field.GetValue(image) is Asset<Texture2D> asset && asset != null)
                            return asset;
                    }
                    catch
                    {
                        // UIImage / UIImageFramed 字段声明类型不一致时跳过
                    }
                }
            }

            return null;
        }

        private static void DrawSource(SpriteBatch spriteBatch, Rectangle target, Texture2D tex, Rectangle source, float alpha)
        {
            if (tex == null || source.Width <= 0 || source.Height <= 0)
                return;

            if (source.Width > tex.Width || source.Height > tex.Height)
                source = new Rectangle(0, 0, Math.Min(tex.Width, 32), Math.Min(tex.Height, 32));

            float scale = Math.Min(target.Width / (float)source.Width, target.Height / (float)source.Height);
            scale = Math.Min(scale, 1.25f);
            Vector2 size = new Vector2(source.Width, source.Height) * scale;
            Vector2 pos = new Vector2(target.Center.X, target.Center.Y);
            spriteBatch.Draw(tex, pos, source, Color.White * alpha, 0f, size * 0.5f, scale, SpriteEffects.None, 0f);
        }

        private static bool TryGetProviderFromByInfo(IBestiaryEntryFilter filter, out IFilterInfoProvider provider)
        {
            provider = null;
            _byInfoElementType ??= typeof(BestiaryDatabase).GetNestedType("ByInfoElement", BindingFlags.Public);
            if (_byInfoElementType == null || !_byInfoElementType.IsInstanceOfType(filter))
                return false;

            object element = _byInfoElementType.GetField("_element", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(filter);
            provider = element as IFilterInfoProvider;
            return provider != null;
        }
    }
}
