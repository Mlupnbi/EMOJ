using System;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Bestiary;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Bestiary.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI.Components
{
    /// <summary>
    /// 72px 容器 + 原版 UIBestiaryEntryButton（不拆层、不重画 SpriteBatch）。
    /// 定制：全部显示脸替换 Icon；背景 10% 在 EntryButton 子树建立后处理。
    /// </summary>
    public sealed class UIBestiaryNpcCard : UIElement
    {
        public const float VanillaSlotPx = BestiaryCardLayout.RefOuterPx;

        private static readonly FieldInfo EntryButtonIconField = typeof(UIBestiaryEntryButton).GetField(
            "_icon",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private UIBestiaryEntryButton _entryButton;

        public BestiaryNpcMeta Meta { get; private set; }
        public Action OnOpenDetail;

        public UIBestiaryNpcCard()
        {
            Width.Set(VanillaSlotPx, 0f);
            Height.Set(VanillaSlotPx, 0f);
        }

        public void SetContext(BestiaryNpcMeta meta, BestiaryFaceMode faceMode)
        {
            Meta = meta;
            RemoveAllChildren();
            _entryButton = null;

            Width.Set(VanillaSlotPx, 0f);
            Height.Set(VanillaSlotPx, 0f);

            if (meta?.Entry == null)
                return;

            _entryButton = new UIBestiaryEntryButton(meta.Entry, isAPrettyPortrait: true)
            {
                Width = { Pixels = VanillaSlotPx },
                Height = { Pixels = VanillaSlotPx }
            };
            _entryButton.Left.Set(0f, 0f);
            _entryButton.Top.Set(0f, 0f);
            _entryButton.OnLeftClick += (_, _) =>
            {
                Main.LocalPlayer.mouseInterface = true;
                OnOpenDetail?.Invoke();
            };

            ApplyAllVisibleIconIfNeeded(_entryButton, faceMode);
            BestiaryEntryButtonVisuals.ApplyPortraitBackgroundAlpha(
                _entryButton,
                BestiaryCardVisuals.BackgroundImageAlpha);

            Append(_entryButton);
        }

        private static void ApplyAllVisibleIconIfNeeded(UIBestiaryEntryButton button, BestiaryFaceMode faceMode)
        {
            if (faceMode != BestiaryFaceMode.AllVisible || EntryButtonIconField == null || button?.Entry == null)
                return;

            if (EntryButtonIconField.GetValue(button) is not UIBestiaryEntryIcon oldIcon)
                return;

            var custom = new EMOJBestiaryAllVisibleEntryIcon(button.Entry, isPortrait: true);

            UIElement portraitHost = oldIcon.Parent;
            if (portraitHost != null)
            {
                portraitHost.RemoveChild(oldIcon);
                portraitHost.Append(custom);
            }

            EntryButtonIconField.SetValue(button, custom);
        }

        public static float ComputeRowHeight(float cellWidth) =>
            Math.Max(VanillaSlotPx, cellWidth);
    }
}
