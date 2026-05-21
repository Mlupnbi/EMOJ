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
    /// 72px 网格卡：原版 UIBestiaryEntryButton，<b>isAPrettyPortrait 必须为 false</b>（卡片图标模式，悬停才动）。
    /// 详情大图为 true，仅用于详情页（非本类）。
    /// </summary>
    public sealed class UIBestiaryNpcCard : UIElement
    {
        public const float VanillaSlotPx = BestiaryCardLayout.RefOuterPx;

        /// <summary>原版图鉴左侧网格 = 图标模式，非肖像大图模式。</summary>
        public const bool GridIsPortraitMode = false;

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

            _entryButton = new UIBestiaryEntryButton(meta.Entry, isAPrettyPortrait: GridIsPortraitMode)
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

            var custom = new EMOJBestiaryAllVisibleEntryIcon(button.Entry, isPortrait: GridIsPortraitMode);

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
