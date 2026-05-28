using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Buffs.UI.Components;
using EvenMoreOverpoweredJourney.Shell.UI.Components;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Bestiary;
using EvenMoreOverpoweredJourney.Bestiary.Filters;
using EvenMoreOverpoweredJourney.Bestiary.UI;
using EvenMoreOverpoweredJourney.Bestiary.UI.Components;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    public class OPJourneyUI : UIState
    {
        public static OPJourneyUI Instance;
        public static bool Visible;
        private static int? _pendingTabOnFirstInit;

        public readonly HubSecondaryFilterState ItemHubSecondary = new HubSecondaryFilterState();
        public ItemHubSecondaryPanel ItemHubSecondaryPanel;

        public readonly BuffSecondaryFilterState BuffSecondary = new BuffSecondaryFilterState();
        public BuffSecondaryPanel BuffSecondaryPanel;

        public readonly BestiarySecondaryFilterState BestiarySecondary = new BestiarySecondaryFilterState();
        public BestiarySecondaryPanel BestiarySecondaryPanel;
        public BestiaryDetailSecondaryPanel BestiaryDetailPanel;

        public BlueprintTemplateSecondaryPanel BlueprintTemplatePanel;
        public BlueprintSetLibraryPanel BlueprintSetLibraryPanel;
        public BlueprintSetDetailPanel BlueprintSetDetailPanel;
        public BlueprintMaterialSecondaryPanel BlueprintMaterialPanel;
        public FurnitureBlueprintPage ActiveBlueprintPage;

        public BestiaryFaceMode BestiaryFaceMode = BestiaryFaceMode.ProgressivePlus;
        public string BestiarySearchQueryText = "";

        public ItemHubSort ItemHubSortMode = ItemHubSort.ById;
        public bool ItemHubSortDescending;
        public bool ItemHubCardMode = true;
        public string HubSearchQueryText = "";


        private UIDraggablePanel mainPanel;
        private UIResizeHandle resizeHandle;
        private UIElement contentContainer;
        private readonly List<UITab> tabs = new List<UITab>();
        private int currentTab;

        public int CurrentTab => currentTab;

        public static void ShowAndSwitchTab(int tabIndex)
        {
            Visible = true;
            ModContent.GetInstance<OPJourneyUISystem>()?.SyncInterfaceVisibility();
            if (Instance != null)
                Instance.SwitchToTab(tabIndex);
            else
                _pendingTabOnFirstInit = tabIndex;
        }

        /// <summary>??????????????????</summary>
        public static void Close()
        {
            Instance?.DeactivateItemHubChainOnClose();
            Hide();
        }

        /// <summary>??????????? <paramref name="tabIndex"/> ??????????????????</summary>
        public static void ToggleTab(int tabIndex)
        {
            if (Visible && Instance != null && Instance.CurrentTab == tabIndex)
            {
                Close();
                return;
            }

            ShowAndSwitchTab(tabIndex);
        }

        public static void Hide()
        {
            Visible = false;
            ModContent.GetInstance<OPJourneyUISystem>()?.SyncInterfaceVisibility();
        }

        public static void HideAndResetForWorld()
        {
            Hide();
            Instance?.ResetForWorldLoad();
        }

        /// <summary>ģ������ʱ�ͷž�̬���ã����� UI ���޷��� GC ���ա�</summary>
        public static void ClearStatics()
        {
            Instance = null;
            Visible = false;
            _pendingTabOnFirstInit = null;
        }

        public void NotifyBuffFiltersChanged()
        {
            BuffSecondaryPanel?.RebuildActiveFilterStrip();
            if (contentContainer != null)
            {
                foreach (UIElement child in contentContainer.Children)
                {
                    if (child is BuffPage page)
                        page.OnModFiltersChanged();
                }
            }
        }

        public void NotifyBestiaryFiltersChanged()
        {
            if (contentContainer == null)
                return;

            foreach (UIElement child in contentContainer.Children)
            {
                if (child is BestiaryPage page)
                    page.OnFiltersChanged();
            }
        }

        public void OpenBestiaryDetail(Bestiary.Catalog.BestiaryNpcMeta meta)
        {
            BestiaryDetailPanel?.Show(meta);
        }

        public void CloseBestiaryDetail() => BestiaryDetailPanel?.SetOpen(false);

        public void NotifyItemHubFiltersChanged()
        {
            Main.LocalPlayer?.GetModPlayer<ItemHubPlayer>()?.InvalidateHubProgressCache();
            ItemHubSecondaryPanel?.SyncChainSlotFromSecondaryState();
            ItemHubSecondaryPanel?.RebuildActiveFilterStrip();
            if (contentContainer != null)
            {
                foreach (UIElement child in contentContainer.Children)
                {
                    if (child is ItemHubPage page)
                        page.OnAdvancedFiltersChanged();
                }
            }
        }

        /// <summary>? <see cref="OPJourneyPlayer.PendingBlueprintQueryType"/> ??????????????</summary>
        public void ApplyPendingBlueprintQuickQuery()
        {
            if (contentContainer == null)
                return;

            foreach (UIElement child in contentContainer.Children)
            {
                if (child is FurnitureBlueprintPage page)
                {
                    page.TryApplyPendingQuickQuery();
                    return;
                }
            }
        }

        /// <summary>�ر�������ʱ�ر���Ʒ��ɸѡ����̬�������λ������ɸѡ�ɻỰ�߼���������?</summary>
        public void DeactivateItemHubChainOnClose()
        {
            ItemHubSecondary.UpstreamChainActive = false;
            ItemHubSecondary.InvalidateUpstream();
            NotifyItemHubFiltersChanged();
        }

        private void SwitchToTab(int tabIndex)
        {
            currentTab = tabIndex;
            if (currentTab < 0) currentTab = 0;
            if (currentTab > 4) currentTab = 4;
            foreach (UITab t in tabs)
                t.Active = t.ID == currentTab;
            if (currentTab != 3)
            {
                BestiarySecondaryPanel?.SetOpen(false);
                BestiaryDetailPanel?.SetOpen(false);
            }
            if (currentTab != 2)
                ItemHubSecondaryPanel?.SetOpen(false);
            if (currentTab != 1)
                BuffSecondaryPanel?.SetOpen(false);
            if (currentTab != 4)
            {
                BlueprintTemplatePanel?.SetOpen(false);
                BlueprintSetLibraryPanel?.SetOpen(false);
                BlueprintSetDetailPanel?.SetOpen(false);
                BlueprintMaterialPanel?.SetOpen(false);
            }
            EmojLog.Info(EmojLogChannel.Ui, $"tab switch index={currentTab}");
            SyncResizeMinimums();
            RefreshTabs();
            if (currentTab == 4)
                ActiveBlueprintPage?.OnShellResized();
        }

        public override void OnInitialize()
        {
            Instance = this;

            mainPanel = new UIDraggablePanel();
            mainPanel.Width.Set(OPJourneyShellMetrics.DefaultMainWidth, 0);
            mainPanel.Height.Set(OPJourneyShellMetrics.DefaultMainHeight, 0);
            mainPanel.Left.Set(Main.screenWidth / 2f - OPJourneyShellMetrics.DefaultMainWidth * 0.5f, 0f);
            mainPanel.Top.Set(Main.screenHeight / 2f - OPJourneyShellMetrics.DefaultMainHeight * 0.5f, 0f);
            mainPanel.BackgroundColor = OPJourneyUiColors.MainPanelBackground;
            mainPanel.BorderColor = OPJourneyUiColors.PanelBorder;
            Append(mainPanel);

            UIDragHandle titleBar = new UIDragHandle();
            titleBar.ParentPanel = mainPanel;
            mainPanel.Append(titleBar);

            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            UIText winTitle = new UIText(mod.DisplayName, 1.05f);
            winTitle.Left.Set(8, 0f);
            winTitle.Top.Set(4, 0f);
            mainPanel.Append(winTitle);

            UICloseButton closeBtn = new UICloseButton();
            closeBtn.Left.Set(-28, 1f);
            closeBtn.Top.Set(6, 0f);
            closeBtn.OnLeftClick += (_, _) => Close();
            mainPanel.Append(closeBtn);

            contentContainer = new UIElement();
            contentContainer.Left.Set(0, 0);
            contentContainer.Top.Set(OPJourneyShellMetrics.TitleBarHeight, 0);
            contentContainer.Width.Set(0, 1f);
            contentContainer.Height.Set(-(OPJourneyShellMetrics.TitleBarHeight + OPJourneyShellMetrics.ContentLayoutBottomInset), 1f);
            mainPanel.Append(contentContainer);

            string[] tabTextKeys = { "TabResearch", "TabBuff", "TabStorage", "TabBestiary", "TabBlueprint" };
            string[] tabHoverKeys = { "TabHoverResearch", "TabHoverBuff", "TabHoverStorage", "TabHoverBestiary", "TabHoverBlueprint" };
            for (int i = 0; i < 5; i++)
            {
                int id = i;
                var tab = new UITab(id, tabTextKeys[i], tabHoverKeys[i]);
                tab.Left.Set(-42, 0);
                tab.Top.Set(22 + i * 42, 0);
                tab.OnLeftClick += (_, el) => { SwitchToTab(((UITab)el).ID); };
                tabs.Add(tab);
            }

            ItemHubSecondaryPanel = new ItemHubSecondaryPanel(this);
            Append(ItemHubSecondaryPanel);
            ItemHubSecondaryPanel.SetOpen(false);

            BuffSecondaryPanel = new BuffSecondaryPanel(this);
            Append(BuffSecondaryPanel);
            BuffSecondaryPanel.SetOpen(false);

            BestiarySecondaryPanel = new BestiarySecondaryPanel(this);
            Append(BestiarySecondaryPanel);
            BestiarySecondaryPanel.SetOpen(false);

            BestiaryDetailPanel = new BestiaryDetailSecondaryPanel(this);
            Append(BestiaryDetailPanel);
            BestiaryDetailPanel.SetOpen(false);

            BlueprintTemplatePanel = new BlueprintTemplateSecondaryPanel(this);
            Append(BlueprintTemplatePanel);
            BlueprintTemplatePanel.SetOpen(false);

            BlueprintSetLibraryPanel = new BlueprintSetLibraryPanel(this);
            Append(BlueprintSetLibraryPanel);
            BlueprintSetLibraryPanel.SetOpen(false);

            BlueprintSetDetailPanel = new BlueprintSetDetailPanel(this);
            Append(BlueprintSetDetailPanel);
            BlueprintSetDetailPanel.SetOpen(false);

            BlueprintMaterialPanel = new BlueprintMaterialSecondaryPanel();
            Append(BlueprintMaterialPanel);
            BlueprintMaterialPanel.SetOpen(false);

            resizeHandle = new UIResizeHandle(mainPanel);
            resizeHandle.OnResized = RecalculateMainLayout;
            mainPanel.Append(resizeHandle);

            for (int i = 0; i < tabs.Count; i++)
                Append(tabs[i]);

            if (_pendingTabOnFirstInit.HasValue)
            {
                int t = _pendingTabOnFirstInit.Value;
                _pendingTabOnFirstInit = null;
                SwitchToTab(t);
            }
            else
                RefreshTabs();

            SyncResizeMinimums();
        }

        private void SyncResizeMinimums()
        {
            if (resizeHandle == null || mainPanel == null)
                return;

            resizeHandle.MinWindowWidth = OPJourneyShellMetrics.MinMainWidth;
            resizeHandle.MinWindowHeight = OPJourneyShellMetrics.MinMainHeight;

            CalculatedStyle dims = mainPanel.GetOuterDimensions();
            bool changed = false;
            if (dims.Width < OPJourneyShellMetrics.MinMainWidth)
            {
                mainPanel.Width.Set(OPJourneyShellMetrics.MinMainWidth, 0f);
                changed = true;
            }
            if (dims.Height < OPJourneyShellMetrics.MinMainHeight)
            {
                mainPanel.Height.Set(OPJourneyShellMetrics.MinMainHeight, 0f);
                changed = true;
            }
            if (changed)
                mainPanel.Recalculate();
        }

        private void RecalculateMainLayout()
        {
            mainPanel?.Recalculate();
            contentContainer?.Recalculate();
            Recalculate();

            if (contentContainer == null)
                return;

            foreach (UIElement child in contentContainer.Children)
            {
                switch (child)
                {
                    case ResearchPage research:
                        research.OnShellResized();
                        break;
                    case BuffPage buff:
                        buff.OnShellResized();
                        break;
                    case ItemHubPage hub:
                        hub.OnShellResized();
                        break;
                    case BestiaryPage bestiary:
                        bestiary.OnShellResized();
                        break;
                    case FurnitureBlueprintPage blueprint:
                        blueprint.OnShellResized();
                        break;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;

            if (mainPanel == null)
                return;

            mainPanel.Draw(spriteBatch);

            foreach (UIElement child in Elements)
            {
                if (child == null || child == mainPanel)
                    continue;

                if (child.GetDimensions().Width < 2f || child.GetDimensions().Height < 2f)
                    continue;

                child.Draw(spriteBatch);
            }

            DrawResizeGrip(spriteBatch);
        }

        /// <summary>??? UI ?????????????????????????????</summary>
        private void DrawResizeGrip(SpriteBatch spriteBatch)
        {
            if (resizeHandle == null || mainPanel == null)
                return;

            Rectangle gripRect = UIResizeHandle.GetGripScreenRect(mainPanel);
            if (gripRect.Width <= 0 || gripRect.Height <= 0)
                return;

            UIResizeHandle.DrawGripAt(
                spriteBatch,
                gripRect,
                resizeHandle.GetCursorTexture(),
                resizeHandle.IsGripHighlighted);
        }

        public void ResetForWorldLoad()
        {
            DeactivateItemHubChainOnClose();
            BestiaryDetailPanel?.SetOpen(false);
            BestiarySecondaryPanel?.SetOpen(false);
            ItemHubSecondaryPanel?.SetOpen(false);
            BuffSecondaryPanel?.SetOpen(false);
            BlueprintTemplatePanel?.SetOpen(false);
            BlueprintSetLibraryPanel?.SetOpen(false);
            BlueprintSetDetailPanel?.SetOpen(false);
            BlueprintMaterialPanel?.SetOpen(false);
            BestiarySearchQueryText = "";

            if (currentTab != 0)
                SwitchToTab(0);
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
                return;

            base.Update(gameTime);
            SyncChromePositions();

            if (Visible && resizeHandle != null && mainPanel != null)
                resizeHandle.UpdateGripInteraction(UIResizeHandle.GetGripScreenRect(mainPanel));

            if (ItemHubSecondaryPanel == null || mainPanel == null)
                return;
            CalculatedStyle d = mainPanel.GetDimensions();
            OPJourneyShellMetrics.EnsureSecondarySize();
            float secW = OPJourneyShellMetrics.FixedSecondaryWidth;
            float secH = OPJourneyShellMetrics.FixedSecondaryHeight;

            bool onBestiary = Visible && currentTab == 3;
            BestiaryChromeLayout.Layout chrome = BestiaryChromeLayout.Compute(
                d,
                onBestiary && BestiarySecondaryPanel.IsOpen,
                onBestiary && BestiaryDetailPanel.IsOpen);

            if (onBestiary && chrome.HasDetail)
            {
                BestiaryDetailPanel.Left.Set(chrome.DetailLeft, 0f);
                BestiaryDetailPanel.Top.Set(chrome.DetailTop, 0f);
                BestiaryDetailPanel.Width.Set(chrome.DetailWidth, 0f);
                BestiaryDetailPanel.Height.Set(chrome.DetailHeight, 0f);
                BestiaryDetailPanel.IgnoresMouseInteraction = false;
            }
            else
            {
                BestiaryDetailPanel.Left.Set(0f, 0f);
                BestiaryDetailPanel.Top.Set(0f, 0f);
                BestiaryDetailPanel.Width.Set(0f, 0f);
                BestiaryDetailPanel.Height.Set(0f, 0f);
                BestiaryDetailPanel.IgnoresMouseInteraction = true;
            }

            if (onBestiary && chrome.HasFilter)
            {
                BestiarySecondaryPanel.Left.Set(chrome.FilterLeft, 0f);
                BestiarySecondaryPanel.Top.Set(chrome.FilterTop, 0f);
                BestiarySecondaryPanel.Width.Set(chrome.FilterWidth, 0f);
                BestiarySecondaryPanel.Height.Set(chrome.FilterHeight, 0f);
                BestiarySecondaryPanel.IgnoresMouseInteraction = false;
            }
            else
            {
                BestiarySecondaryPanel.Left.Set(0f, 0f);
                BestiarySecondaryPanel.Top.Set(0f, 0f);
                BestiarySecondaryPanel.Width.Set(0f, 0f);
                BestiarySecondaryPanel.Height.Set(0f, 0f);
                BestiarySecondaryPanel.IgnoresMouseInteraction = true;
            }

            if (Visible && currentTab == 2 && ItemHubSecondaryPanel.IsOpen)
            {
                ItemHubSecondaryPanel.Left.Set(d.X + d.Width + 6f, 0f);
                ItemHubSecondaryPanel.Top.Set(d.Y, 0f);
                ItemHubSecondaryPanel.Width.Set(secW, 0f);
                ItemHubSecondaryPanel.Height.Set(secH, 0f);
                ItemHubSecondaryPanel.IgnoresMouseInteraction = false;
            }
            else
            {
                ItemHubSecondaryPanel.Left.Set(0f, 0f);
                ItemHubSecondaryPanel.Top.Set(0f, 0f);
                ItemHubSecondaryPanel.Width.Set(0f, 0f);
                ItemHubSecondaryPanel.Height.Set(0f, 0f);
                ItemHubSecondaryPanel.IgnoresMouseInteraction = true;
            }

            if (Visible && currentTab == 1 && BuffSecondaryPanel.IsOpen)
            {
                BuffSecondaryPanel.Left.Set(d.X + d.Width + 6f, 0f);
                BuffSecondaryPanel.Top.Set(d.Y, 0f);
                BuffSecondaryPanel.Width.Set(secW, 0f);
                BuffSecondaryPanel.Height.Set(secH, 0f);
                BuffSecondaryPanel.IgnoresMouseInteraction = false;
            }
            else
            {
                BuffSecondaryPanel.Left.Set(0f, 0f);
                BuffSecondaryPanel.Top.Set(0f, 0f);
                BuffSecondaryPanel.Width.Set(0f, 0f);
                BuffSecondaryPanel.Height.Set(0f, 0f);
                BuffSecondaryPanel.IgnoresMouseInteraction = true;
            }

            if (Visible && currentTab == 4)
                LayoutBlueprintSecondaryPanels(d);
            else
                HideBlueprintSecondaryPanels();
        }

        private void HideBlueprintSecondaryPanels()
        {
            SetBlueprintPanelClosed(BlueprintTemplatePanel);
            SetBlueprintPanelClosed(BlueprintSetLibraryPanel);
            SetBlueprintPanelClosed(BlueprintSetDetailPanel);
            SetBlueprintPanelClosed(BlueprintMaterialPanel);
        }

        private static void SetBlueprintPanelClosed(UIElement panel)
        {
            if (panel == null)
                return;
            panel.Left.Set(0f, 0f);
            panel.Top.Set(0f, 0f);
            panel.Width.Set(0f, 0f);
            panel.Height.Set(0f, 0f);
            panel.IgnoresMouseInteraction = true;
        }

        private void LayoutBlueprintSecondaryPanels(CalculatedStyle mainDims)
        {
            float baseX = mainDims.X + mainDims.Width + 6f;
            float baseY = mainDims.Y;
            float cursorX = baseX;

            if (BlueprintSetLibraryPanel?.IsOpen == true)
            {
                float w = BlueprintSetLibraryPanel.DefaultWidth;
                float h = BlueprintSetLibraryPanel.DefaultHeight;
                PositionBlueprintPanel(BlueprintSetLibraryPanel, cursorX, baseY, w, h);
                cursorX += w + 6f;
            }
            else
                SetBlueprintPanelClosed(BlueprintSetLibraryPanel);

            if (BlueprintSetDetailPanel?.IsOpen == true)
            {
                float w = BlueprintSetDetailPanel.DefaultWidth;
                float h = BlueprintSetDetailPanel.DefaultHeight;
                PositionBlueprintPanel(BlueprintSetDetailPanel, cursorX, baseY, w, h);
                cursorX += w + 6f;
            }
            else
                SetBlueprintPanelClosed(BlueprintSetDetailPanel);

            if (BlueprintTemplatePanel?.IsOpen == true)
            {
                float w = BlueprintTemplatePanel.DefaultWidth;
                float h = BlueprintTemplatePanel.DefaultHeight;
                PositionBlueprintPanel(BlueprintTemplatePanel, cursorX, baseY, w, h);
            }
            else
                SetBlueprintPanelClosed(BlueprintTemplatePanel);

            if (BlueprintMaterialPanel?.IsOpen == true)
            {
                float matW = BlueprintMaterialPanel.GetPreferredWidth();
                float matH = BlueprintMaterialPanel.GetPreferredHeight();
                if (matW < 1f || matH < 1f)
                {
                    SetBlueprintPanelClosed(BlueprintMaterialPanel);
                }
                else
                {
                    float matX = mainDims.X + OPJourneyShellMetrics.ContentInsetLeft;
                    float matY = mainDims.Y + FurnitureBlueprintPageLayout.ToolbarHeight + 2f;
                    PositionBlueprintPanel(BlueprintMaterialPanel, matX, matY, matW, matH);
                }
            }
            else
                SetBlueprintPanelClosed(BlueprintMaterialPanel);
        }

        private static void PositionBlueprintPanel(UIElement panel, float x, float y, float w, float h)
        {
            panel.Left.Set(x, 0f);
            panel.Top.Set(y, 0f);
            panel.Width.Set(w, 0f);
            panel.Height.Set(h, 0f);
            panel.IgnoresMouseInteraction = false;
        }

        private void SyncChromePositions()
        {
            if (mainPanel == null)
                return;

            CalculatedStyle d = mainPanel.GetOuterDimensions();
            for (int i = 0; i < tabs.Count; i++)
            {
                UITab tab = tabs[i];
                tab.Left.Set(d.X - 42f, 0f);
                tab.Top.Set(d.Y + 27f + i * 45f, 0f);
            }

        }

        private void RefreshTabs()
        {
            contentContainer.RemoveAllChildren();
            foreach (UITab t in tabs)
                t.Active = t.ID == currentTab;

            if (currentTab == 0)
                contentContainer.Append(new ResearchPage());
            else if (currentTab == 1)
                contentContainer.Append(new BuffPage(this));
            else if (currentTab == 2)
                contentContainer.Append(new ItemHubPage());
            else if (currentTab == 3)
                contentContainer.Append(new BestiaryPage(this));
            else
            {
                var blueprintPage = new FurnitureBlueprintPage(this);
                ActiveBlueprintPage = blueprintPage;
                contentContainer.Append(blueprintPage);
            }
        }
    }
}
