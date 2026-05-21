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

        public BestiaryFaceMode BestiaryFaceMode = BestiaryFaceMode.ProgressivePlus;
        public BestiaryViewMode BestiaryViewMode = BestiaryViewMode.Card;
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
            if (Instance != null)
                Instance.SwitchToTab(tabIndex);
            else
                _pendingTabOnFirstInit = tabIndex;
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
            BestiarySecondaryPanel?.RebuildActiveFilterStrip();
            if (contentContainer == null)
                return;

            foreach (UIElement child in contentContainer.Children)
            {
                if (child is BestiaryPage page)
                    page.OnFiltersChanged();
            }
        }

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
            if (currentTab > 3) currentTab = 3;
            foreach (UITab t in tabs)
                t.Active = t.ID == currentTab;
            if (currentTab != 3)
                BestiarySecondaryPanel?.SetOpen(false);
            if (currentTab != 2)
                ItemHubSecondaryPanel?.SetOpen(false);
            if (currentTab != 1)
                BuffSecondaryPanel?.SetOpen(false);
            EmojLog.Info(EmojLogChannel.Ui, $"tab switch index={currentTab}");
            SyncResizeMinimums();
            RefreshTabs();
        }

        public override void OnInitialize()
        {
            Instance = this;

            mainPanel = new UIDraggablePanel();
            mainPanel.Width.Set(OPJourneyShellMetrics.DefaultMainWidth, 0);
            mainPanel.Height.Set(OPJourneyShellMetrics.DefaultMainHeight, 0);
            mainPanel.Left.Set(Main.screenWidth / 2f - OPJourneyShellMetrics.DefaultMainWidth * 0.5f, 0f);
            mainPanel.Top.Set(Main.screenHeight / 2f - OPJourneyShellMetrics.DefaultMainHeight * 0.5f, 0f);
            mainPanel.BackgroundColor = new Color(40, 40, 60) * 0.95f;
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
            closeBtn.OnLeftClick += (_, _) =>
            {
                DeactivateItemHubChainOnClose();
                Visible = false;
            };
            mainPanel.Append(closeBtn);

            contentContainer = new UIElement();
            contentContainer.Left.Set(0, 0);
            contentContainer.Top.Set(OPJourneyShellMetrics.TitleBarHeight, 0);
            contentContainer.Width.Set(0, 1f);
            contentContainer.Height.Set(-(OPJourneyShellMetrics.TitleBarHeight + OPJourneyShellMetrics.ResizeHandleSize), 1f);
            mainPanel.Append(contentContainer);

            string[] tabTextKeys = { "TabResearch", "TabBuff", "TabStorage", "TabBestiary" };
            string[] tabHoverKeys = { "TabHoverResearch", "TabHoverBuff", "TabHoverStorage", "TabHoverBestiary" };
            string[] tabIcons =
            {
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.TabIconResearch,
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.TabIconBuff,
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.TabIconStorage,
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.TabIconBestiary
            };
            for (int i = 0; i < 4; i++)
            {
                int id = i;
                var tab = new UITab(id, tabTextKeys[i], tabIcons[i], tabHoverKeys[i]);
                tab.Left.Set(-42, 0);
                tab.Top.Set(27 + i * 45, 0);
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
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;

            mainPanel?.Draw(spriteBatch);

            foreach (UIElement child in Elements)
            {
                if (child == null || child == mainPanel)
                    continue;
                child.Draw(spriteBatch);
            }
        }

        public override void Update(GameTime gameTime)
        {
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

            if (Visible && currentTab == 3 && BestiarySecondaryPanel.IsOpen)
            {
                BestiarySecondaryPanel.Left.Set(d.X + d.Width + 6f, 0f);
                BestiarySecondaryPanel.Top.Set(d.Y, 0f);
                BestiarySecondaryPanel.Width.Set(secW, 0f);
                BestiarySecondaryPanel.Height.Set(secH, 0f);
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
            else
                contentContainer.Append(new BestiaryPage(this));
        }
    }
}
