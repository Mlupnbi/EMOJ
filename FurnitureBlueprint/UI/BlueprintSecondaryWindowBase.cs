using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>蓝图二级窗基类：标题栏 + 关闭键 + 内容区，视觉对齐 OPJourney 主壳。</summary>
    public abstract class BlueprintSecondaryWindowBase : UIElement
    {
        protected readonly OPJourneyUI Shell;
        protected readonly UIPanel Chrome;
        protected readonly UIElement ContentHost;

        private readonly UIText _titleText;
        private readonly UICloseButton _closeButton;
        private bool _open;

        public bool IsOpen => _open;

        protected abstract string TitleLocalizationKey { get; }
        protected abstract string TitleFallback { get; }

        public virtual float DefaultWidth => FurnitureBlueprintPageLayout.SetLibraryPanelWidth;
        public virtual float DefaultHeight => FurnitureBlueprintPageLayout.SetLibraryPanelHeight;

        protected BlueprintSecondaryWindowBase(OPJourneyUI shell)
        {
            Shell = shell;

            Chrome = new UIPanel
            {
                BackgroundColor = OPJourneyUiColors.PanelBackground,
                BorderColor = OPJourneyUiColors.PanelBorder
            };
            Chrome.Width.Set(0f, 1f);
            Chrome.Height.Set(0f, 1f);
            Append(Chrome);

            _titleText = new UIText(ResolveTitle(), FurnitureBlueprintPageLayout.SecondaryTitleScale)
            {
                Left = { Pixels = FurnitureBlueprintPageLayout.SecondaryPanelPadding },
                Top = { Pixels = 6f }
            };
            Chrome.Append(_titleText);

            _closeButton = new UICloseButton();
            _closeButton.Left.Set(-FurnitureBlueprintPageLayout.SecondaryPanelPadding - 4f, 1f);
            _closeButton.Top.Set(6f, 0f);
            _closeButton.OnClose += () => SetOpen(false);
            Chrome.Append(_closeButton);

            ContentHost = new UIElement();
            ContentHost.Left.Set(FurnitureBlueprintPageLayout.SecondaryPanelPadding, 0f);
            ContentHost.Top.Set(FurnitureBlueprintPageLayout.SecondaryTitleBarHeight, 0f);
            ContentHost.Width.Set(-FurnitureBlueprintPageLayout.SecondaryPanelPadding * 2f, 1f);
            ContentHost.Height.Set(-(FurnitureBlueprintPageLayout.SecondaryTitleBarHeight
                + FurnitureBlueprintPageLayout.SecondaryPanelPadding
                + OPJourneyShellMetrics.ContentBottomSafeMargin), 1f);
            Chrome.Append(ContentHost);

            BuildContent();
        }

        protected abstract void BuildContent();

        public virtual void SetOpen(bool open)
        {
            _open = open;
            if (open)
            {
                _titleText?.SetText(ResolveTitle());
                OnOpened();
            }
        }

        protected virtual void OnOpened() { }

        protected string ResolveTitle()
        {
            string key = TitleLocalizationKey;
            if (key.StartsWith("Blueprint.", System.StringComparison.Ordinal))
                key = key.Substring("Blueprint.".Length);
            return EOPJText.BlueprintOr(key, TitleFallback);
        }

        public override void Update(GameTime gameTime)
        {
            if (!_open || GetDimensions().Width < 2f)
            {
                base.Update(gameTime);
                return;
            }

            if (GetDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint()) && Main.LocalPlayer != null)
                Main.LocalPlayer.mouseInterface = true;

            base.Update(gameTime);

            Rectangle box = Chrome.GetDimensions().ToRectangle();
            if (box.Contains(Main.MouseScreen.ToPoint()))
                PlayerInput.LockVanillaMouseScroll("EvenMoreOverpoweredJourney:BlueprintSecondary");
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_open || GetDimensions().Width < 2f)
                return;
            base.Draw(spriteBatch);
        }
    }
}
