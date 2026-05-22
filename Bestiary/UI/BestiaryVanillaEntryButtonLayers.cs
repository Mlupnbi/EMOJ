using System;
using System.Collections.Generic;
using System.Reflection;
using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>
    /// 原版 <see cref="UIBestiaryEntryButton"/> 子节点结构（tModLoader 反编译）：
    /// <list type="bullet">
    /// <item>内层 UIElement：Slot_Back UIImage →（肖像模式时生态 UIImage）→ UIBestiaryEntryIcon</item>
    /// <item>UIText 角标（可选）</item>
    /// <item>Slot_Overlay、Slot_Front、Slot_Selection（_borders / _bordersOverlay / _bordersGlow）</item>
    /// </list>
    /// 定制 10% 生态底时，只移除贴图为 <see cref="SlotBackAssetPath"/> 的 UIImage，不碰肖像与其它层。
    /// </summary>
    internal static class BestiaryVanillaEntryButtonLayers
    {
        public const string SlotBackAssetPath = EojUiAssetCatalog.BestiaryVanillaPaths.SlotBack;
        public const string SlotFrontAssetPath = EojUiAssetCatalog.BestiaryVanillaPaths.SlotFront;
        public const string SlotOverlayAssetPath = EojUiAssetCatalog.BestiaryVanillaPaths.SlotOverlay;
        public const string SlotSelectionAssetPath = EojUiAssetCatalog.BestiaryVanillaPaths.SlotSelection;

        private static Texture2D _slotBackTexture;
        private static PropertyInfo _uiImageTextureProperty;
        private static FieldInfo _uiImageTextureField;

        public static void StripOpaqueSlotBack(UIBestiaryEntryButton button)
        {
            if (button == null)
                return;

            EnsureSlotBackReference();
            foreach (UIElement child in button.Children)
                RemoveSlotBackImagesInTree(child);
        }

        private static void RemoveSlotBackImagesInTree(UIElement node)
        {
            if (node == null)
                return;

            var children = new List<UIElement>();
            foreach (UIElement child in node.Children)
                children.Add(child);

            foreach (UIElement child in children)
            {
                RemoveSlotBackImagesInTree(child);

                if (child is UIImage image && IsSlotBackImage(image))
                    node.RemoveChild(child);
            }
        }

        private static bool IsSlotBackImage(UIImage image)
        {
            if (image == null)
                return false;

            Texture2D texture = TryGetUIImageTexture(image);
            if (texture != null && _slotBackTexture != null && ReferenceEquals(texture, _slotBackTexture))
                return true;

            string assetName = TryGetUIImageAssetName(image);
            if (string.IsNullOrEmpty(assetName))
                return false;

            return assetName.Equals(SlotBackAssetPath, StringComparison.OrdinalIgnoreCase) ||
                   assetName.EndsWith("/Slot_Back", StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureSlotBackReference()
        {
            if (_slotBackTexture != null)
                return;

            EojUiTextureCache.WarmTab(EojUiTab.Bestiary);
            _slotBackTexture = EojUiTextures.Bestiary.SlotBack;
        }

        private static Texture2D TryGetUIImageTexture(UIImage image)
        {
            Asset<Texture2D> asset = TryGetUIImageAsset(image);
            return asset?.Value;
        }

        private static string TryGetUIImageAssetName(UIImage image)
        {
            Asset<Texture2D> asset = TryGetUIImageAsset(image);
            return asset?.Name;
        }

        private static Asset<Texture2D> TryGetUIImageAsset(UIImage image)
        {
            CacheUIImageAccessors();

            if (_uiImageTextureProperty != null)
            {
                try
                {
                    if (_uiImageTextureProperty.GetValue(image) is Asset<Texture2D> fromProp)
                        return fromProp;
                }
                catch
                {
                    // ignored
                }
            }

            if (_uiImageTextureField != null)
            {
                try
                {
                    if (_uiImageTextureField.GetValue(image) is Asset<Texture2D> fromField)
                        return fromField;
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }

        private static void CacheUIImageAccessors()
        {
            if (_uiImageTextureProperty != null || _uiImageTextureField != null)
                return;

            Type imageType = typeof(UIImage);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _uiImageTextureProperty = imageType.GetProperty("Texture", flags) ??
                                      imageType.GetProperty("Image", flags);

            _uiImageTextureField = imageType.GetField("_image", flags) ??
                                   imageType.GetField("_texture", flags) ??
                                   imageType.GetField("image", flags) ??
                                   imageType.GetField("texture", flags);
        }
    }
}
