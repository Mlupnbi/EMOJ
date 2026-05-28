using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Bestiary;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Bestiary.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI.Components
{
    /// <summary>
    /// 图鉴网格卡：内部 <see cref="UIBestiaryEntryButton"/> 按 72px 布局，显示尺寸可矩阵缩放（方案 C）。
    /// 入口按钮承接原版悬停高亮/动画；点击由 <see cref="BestiaryGridEntryButtonHookSystem"/> 开详情。
    /// </summary>
    public sealed class UIBestiaryNpcCard : UIElement
    {
        public const float VanillaSlotPx = BestiaryCardLayout.RefOuterPx;

        public const bool GridIsPortraitMode = false;

        private const float MinDisplayPx = 8f;
        private const float ScaleUnityEpsilon = 0.005f;

        private static readonly FieldInfo EntryButtonIconField = typeof(UIBestiaryEntryButton).GetField(
            "_icon",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private UIBestiaryEntryButton _entryButton;

        public BestiaryNpcMeta Meta { get; private set; }
        public Action OnOpenDetail;

        public float DisplayPx { get; private set; } = VanillaSlotPx;

        public UIBestiaryNpcCard()
        {
            SetDisplaySize(VanillaSlotPx);
        }

        public void SetDisplaySize(float pixels)
        {
            DisplayPx = Math.Max(MinDisplayPx, pixels);
            Width.Set(DisplayPx, 0f);
            Height.Set(DisplayPx, 0f);
            SyncEntryButtonHitbox();
        }

        public void SetContext(BestiaryNpcMeta meta, BestiaryFaceMode faceMode)
        {
            Meta = meta;
            RemoveAllChildren();
            _entryButton = null;

            if (meta?.Entry == null)
                return;

            _entryButton = new UIBestiaryEntryButton(meta.Entry, isAPrettyPortrait: GridIsPortraitMode);
            _entryButton.Width.Set(VanillaSlotPx, 0f);
            _entryButton.Height.Set(VanillaSlotPx, 0f);
            _entryButton.Left.Set(0f, 0f);
            _entryButton.Top.Set(0f, 0f);

            ApplyAllVisibleIconIfNeeded(_entryButton, faceMode);
            BestiaryEntryButtonVisuals.StripVanillaBackgroundLayers(_entryButton);
            _entryButton.OnLeftClick += OnEntryButtonLeftClick;

            Append(_entryButton);
            SyncEntryButtonHitbox();
        }

        private void OnEntryButtonLeftClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (evt.Target != listeningElement)
                return;

            SoundEngine.PlaySound(SoundID.MenuTick);
            OnOpenDetail?.Invoke();
        }

        private void SyncEntryButtonHitbox()
        {
            if (_entryButton == null)
                return;

            // 命中区与卡片外廓一致；绘制仍按 72px 逻辑树 + 矩阵缩放。
            _entryButton.Width.Set(DisplayPx, 0f);
            _entryButton.Height.Set(DisplayPx, 0f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_entryButton == null)
                return;

            float scale = DisplayPx / VanillaSlotPx;
            if (Math.Abs(scale - 1f) < ScaleUnityEpsilon)
            {
                base.Draw(spriteBatch);
                return;
            }

            Vector2 origin = GetDimensions().Position();
            ShellUiDrawUtil.DrawScaledAbout(spriteBatch, origin, scale, () => _entryButton.Draw(spriteBatch));
        }

        private static void ApplyAllVisibleIconIfNeeded(UIBestiaryEntryButton button, BestiaryFaceMode faceMode)
        {
            if (faceMode != BestiaryFaceMode.AllVisible || button?.Entry == null)
                return;

            if (!TryGetEntryIcon(button, out UIBestiaryEntryIcon oldIcon))
                return;

            var custom = new EMOJBestiaryAllVisibleEntryIcon(button.Entry, isPortrait: GridIsPortraitMode);

            UIElement portraitHost = oldIcon.Parent;
            if (portraitHost != null)
            {
                portraitHost.RemoveChild(oldIcon);
                portraitHost.Append(custom);
            }

            EntryButtonIconField?.SetValue(button, custom);
        }

        internal static bool TryGetEntryIcon(UIBestiaryEntryButton button, out UIBestiaryEntryIcon icon)
        {
            icon = null;
            if (button == null)
                return false;

            if (EntryButtonIconField?.GetValue(button) is UIBestiaryEntryIcon fromField)
            {
                icon = fromField;
                return true;
            }

            return TryFindEntryIconInTree(button, out icon);
        }

        private static bool TryFindEntryIconInTree(UIElement node, out UIBestiaryEntryIcon icon)
        {
            icon = null;
            if (node == null)
                return false;

            foreach (UIElement child in node.Children)
            {
                if (child is UIBestiaryEntryIcon entryIcon)
                {
                    icon = entryIcon;
                    return true;
                }

                if (TryFindEntryIconInTree(child, out icon))
                    return true;
            }

            return false;
        }

        public static float ComputeRowHeight(float cellWidth) =>
            Math.Max(MinDisplayPx, cellWidth);
    }
}
