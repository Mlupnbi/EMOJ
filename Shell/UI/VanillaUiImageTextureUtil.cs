using System;
using System.Collections.Generic;
using System.Reflection;
using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>将原版 <see cref="UIImage"/> 绑定的资源替换为模组 <c>Assets/UI</c> 中的 PNG（若存在）。</summary>
    internal static class VanillaUiImageTextureUtil
    {
        private static PropertyInfo _textureProperty;
        private static FieldInfo _textureField;
        private static readonly HashSet<UIImage> Swapped = new();

        public static void ClearSwapCache() => Swapped.Clear();

        /// <summary>若存在同名模组贴图，则替换 UIImage 内部 Asset（每个实例仅尝试一次）。</summary>
        public static bool TrySwapToModAsset(UIImage image)
        {
            if (image == null || Swapped.Contains(image))
                return false;

            Swapped.Add(image);

            Asset<Texture2D> current = TryGetAsset(image);
            string vanillaName = current?.Name;
            if (string.IsNullOrEmpty(vanillaName))
                return false;

            string modPath = EojUiAssetCatalog.TryGetPrimaryModPath(vanillaName) ??
                             EojUiAssetCatalog.InferModPathFromVanillaUi(vanillaName);
            if (string.IsNullOrEmpty(modPath))
                return false;

            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            if (mod == null || !mod.HasAsset(modPath))
                return false;

            try
            {
                Asset<Texture2D> modAsset = mod.Assets.Request<Texture2D>(modPath);
                return TrySetAsset(image, modAsset);
            }
            catch
            {
                return false;
            }
        }

        public static Asset<Texture2D> TryGetAsset(UIImage image)
        {
            if (image == null)
                return null;

            EnsureAccessors();

            if (_textureProperty != null)
            {
                try
                {
                    if (_textureProperty.GetValue(image) is Asset<Texture2D> fromProp)
                        return fromProp;
                }
                catch
                {
                    // ignored
                }
            }

            if (_textureField != null)
            {
                try
                {
                    if (_textureField.GetValue(image) is Asset<Texture2D> fromField)
                        return fromField;
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }

        private static bool TrySetAsset(UIImage image, Asset<Texture2D> asset)
        {
            if (image == null || asset == null)
                return false;

            EnsureAccessors();

            try
            {
                if (_textureProperty?.CanWrite == true)
                {
                    _textureProperty.SetValue(image, asset);
                    return true;
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                if (_textureField != null)
                {
                    _textureField.SetValue(image, asset);
                    return true;
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        private static void EnsureAccessors()
        {
            if (_textureProperty != null || _textureField != null)
                return;

            Type imageType = typeof(UIImage);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _textureProperty = imageType.GetProperty("Texture", flags) ??
                               imageType.GetProperty("Image", flags);

            _textureField = imageType.GetField("_imageTexture", flags) ??
                            imageType.GetField("_texture", flags) ??
                            imageType.GetField("_image", flags) ??
                            imageType.GetField("imageTexture", flags) ??
                            imageType.GetField("texture", flags);
        }
    }
}
