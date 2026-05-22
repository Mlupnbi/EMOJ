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
    /// <summary>通过原版/模组筛选项的 <c>GetImage()</c> / <c>GetFilterImage()</c> 解析图标。</summary>
    internal static class BestiaryFilterIconResolver
    {
        private const string TagsSheetPath = "Images/UI/Bestiary/Icon_Tags_Shadow";
        private const int TagsCols = 16;
        private const int TagsRows = 5;

        private static readonly FieldInfo FramedTextureField = typeof(UIImageFramed).GetField(
            "_texture",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo FramedFrameField = typeof(UIImageFramed).GetField(
            "_frame",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ImageTextureField = typeof(UIImage).GetField(
            "_texture",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ImageNonReloadField = typeof(UIImage).GetField(
            "_nonReloadingTexture",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool TryResolveIcon(IBestiaryEntryFilter filter, out Point gridFrame, out Texture2D texture, out Rectangle sourceRect)
        {
            gridFrame = Point.Zero;
            texture = null;
            sourceRect = Rectangle.Empty;

            if (filter == null)
                return false;

            if (TryInvokeGetImage(filter, out UIElement imageEl) && TryExtractIcon(imageEl, out gridFrame, out texture, out sourceRect))
                return true;

            if (TryGetFilterImageFromProvider(filter, out imageEl) && TryExtractIcon(imageEl, out gridFrame, out texture, out sourceRect))
                return true;

            return false;
        }

        public static bool TryGetIconFrame(IBestiaryEntryFilter filter, out Point frame)
        {
            frame = Point.Zero;
            return TryResolveIcon(filter, out frame, out _, out _);
        }

        public static void DrawInto(SpriteBatch spriteBatch, Rectangle target, IBestiaryEntryFilter filter, float alpha = 1f)
        {
            if (target.Width <= 0 || target.Height <= 0 || filter == null)
                return;

            if (!TryResolveIcon(filter, out Point gridFrame, out Texture2D tex, out Rectangle src))
                return;

            DrawResolved(spriteBatch, target, gridFrame, tex, src, alpha);
        }

        public static void DrawDef(SpriteBatch spriteBatch, Rectangle target, BestiaryFilterDef def, float alpha = 1f)
        {
            if (def == null || target.Width <= 0 || target.Height <= 0)
                return;

            if (def.HasResolvedIcon && def.IconTexture != null)
            {
                DrawResolved(spriteBatch, target, def.IconFrame, def.IconTexture, def.IconSourceRect, alpha, def.UsesTagsShadowSheet);
                return;
            }

            DrawInto(spriteBatch, target, def.Filter, alpha);
        }

        private static void DrawResolved(
            SpriteBatch spriteBatch,
            Rectangle target,
            Point gridFrame,
            Texture2D tex,
            Rectangle src,
            float alpha,
            bool? forceTagsSheet = null)
        {
            if (tex == null || src.Width <= 0 || src.Height <= 0)
                return;

            bool tags = forceTagsSheet ?? IsTagsShadowTexture(tex);
            if (tags)
                BestiaryVanillaFilterIcons.DrawFilterIcon(spriteBatch, target, gridFrame, alpha);
            else
                BestiaryVanillaFilterIcons.DrawSourceRect(spriteBatch, target, tex, src, alpha);
        }

        private static bool TryExtractIcon(UIElement el, out Point gridFrame, out Texture2D texture, out Rectangle sourceRect)
        {
            gridFrame = Point.Zero;
            texture = null;
            sourceRect = Rectangle.Empty;

            if (el == null)
                return false;

            if (el is UIImageFramed framed)
                return TryExtractFramed(framed, out gridFrame, out texture, out sourceRect);

            if (el is UIImage image)
                return TryExtractUIImage(image, out texture, out sourceRect);

            return false;
        }

        private static bool TryExtractFramed(UIImageFramed framed, out Point gridFrame, out Texture2D texture, out Rectangle sourceRect)
        {
            gridFrame = Point.Zero;
            texture = null;
            sourceRect = Rectangle.Empty;

            if (FramedTextureField?.GetValue(framed) is not Asset<Texture2D> asset || asset?.Value == null)
                return false;

            if (FramedFrameField?.GetValue(framed) is not Rectangle frame || frame.Width <= 0 || frame.Height <= 0)
                return false;

            texture = asset.Value;
            sourceRect = frame;

            if (IsTagsShadowSheet(asset))
            {
                Rectangle cell = asset.Frame(TagsCols, TagsRows, 0, 0);
                if (cell.Width > 0 && cell.Height > 0)
                {
                    gridFrame = new Point(frame.X / cell.Width, frame.Y / cell.Height);
                    gridFrame.X = Math.Clamp(gridFrame.X, 0, TagsCols - 1);
                    gridFrame.Y = Math.Clamp(gridFrame.Y, 0, TagsRows - 1);
                }
            }

            return true;
        }

        private static bool TryExtractUIImage(UIImage image, out Texture2D texture, out Rectangle sourceRect)
        {
            texture = null;
            sourceRect = Rectangle.Empty;

            if (ImageTextureField?.GetValue(image) is Asset<Texture2D> asset && asset?.Value != null)
                texture = asset.Value;
            else if (ImageNonReloadField?.GetValue(image) is Texture2D nonReload && nonReload != null)
                texture = nonReload;

            if (texture == null || texture.Width <= 0 || texture.Height <= 0)
                return false;

            sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
            return true;
        }

        private static bool TryInvokeGetImage(IBestiaryEntryFilter filter, out UIElement image)
        {
            image = null;
            MethodInfo getImage = filter.GetType().GetMethod(
                "GetImage",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            if (getImage == null)
                return false;

            try
            {
                if (getImage.Invoke(filter, null) is UIElement el)
                {
                    image = el;
                    return el != null;
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        /// <summary><see cref="Filters.ByInfoElement"/> 等：从嵌套的 InfoElement 取 <c>GetFilterImage()</c>。</summary>
        private static bool TryGetFilterImageFromProvider(IBestiaryEntryFilter filter, out UIElement image)
        {
            image = null;
            try
            {
                FieldInfo elementField = filter.GetType().GetField(
                    "_element",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (elementField?.GetValue(filter) is not IBestiaryInfoElement element)
                    return false;

                if (element is not IFilterInfoProvider provider)
                    return false;

                image = provider.GetFilterImage();
                return image != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTagsShadowSheet(Asset<Texture2D> asset)
        {
            Texture2D sheet = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Bestiary.IconTagsShadow;
            return asset?.Value != null && ReferenceEquals(asset.Value, sheet);
        }

        private static bool IsTagsShadowTexture(Texture2D tex)
        {
            Texture2D sheet = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Bestiary.IconTagsShadow;
            return tex != null && ReferenceEquals(tex, sheet);
        }
    }
}
