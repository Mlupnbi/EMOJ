using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;

namespace EvenMoreOverpoweredJourney.Shell.Players
{
    public class OPJourneyPlayer : ModPlayer
    {
        /// <summary>???????????? type?0 ????</summary>
        public int PendingResearchQueryType;

        /// <summary>??????????????? type?0 ????</summary>
        public int PendingBlueprintQueryType;

        public override void OnEnterWorld()
        {
            PendingResearchQueryType = 0;
            PendingBlueprintQueryType = 0;
            OPJourneyUI.HideAndResetForWorld();
            if (OPJourneyUI.Instance != null)
            {
                OPJourneyUI.Instance.ItemHubSecondary.ResetForNewSession();
                OPJourneyUI.Instance.BestiarySecondary.ResetForNewSession();
                OPJourneyUI.Instance.ItemHubSortMode = ItemHubSort.ById;
                OPJourneyUI.Instance.ItemHubSortDescending = false;
                OPJourneyUI.Instance.ItemHubCardMode = true;
                OPJourneyUI.Instance.HubSearchQueryText = "";
            }
        }

        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
        {
            if (TryToggleShellTab(EvenMoreOverpoweredJourney.OpenResearchPanelKey, 0))
                return;

            if (TryToggleShellTab(EvenMoreOverpoweredJourney.OpenBuffPanelKey, 1))
                return;

            if (TryToggleShellTab(EvenMoreOverpoweredJourney.OpenItemHubPanelKey, 2))
                return;

            if (TryToggleShellTab(EvenMoreOverpoweredJourney.OpenBestiaryPanelKey, 3))
                return;

            if (TryToggleShellTab(EvenMoreOverpoweredJourney.OpenBlueprintPanelKey, 4))
                return;

            if (EvenMoreOverpoweredJourney.QuickItemQueryKey?.JustPressed == true)
            {
                if (Main.HoverItem == null || Main.HoverItem.IsAir || Main.HoverItem.type == ItemID.None)
                    return;
                int hoverType = Main.HoverItem.type;

                if (OPJourneyUI.Visible && OPJourneyUI.Instance != null && OPJourneyUI.Instance.CurrentTab == 2)
                {
                    HubSecondaryFilterState st = OPJourneyUI.Instance.ItemHubSecondary;
                    if (st != null && st.UpstreamChainActive)
                    {
                        st.SetUpstreamSlotType(hoverType);
                        st.InvalidateUpstream();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        OPJourneyUI.Instance?.ItemHubSecondaryPanel?.SyncChainSlotFromSecondaryState();
                        OPJourneyUI.Instance?.NotifyItemHubFiltersChanged();
                        return;
                    }
                }

                if (OPJourneyUI.Visible && OPJourneyUI.Instance != null && OPJourneyUI.Instance.CurrentTab == 4)
                {
                    PendingBlueprintQueryType = hoverType;
                    OPJourneyUI.Instance.ApplyPendingBlueprintQuickQuery();
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    return;
                }

                PendingResearchQueryType = hoverType;
                OPJourneyUI.ShowAndSwitchTab(0);
                SoundEngine.PlaySound(SoundID.MenuOpen);
                return;
            }
        }

        private static bool TryToggleShellTab(ModKeybind keybind, int tabIndex)
        {
            if (keybind?.JustPressed != true)
                return false;

            bool closing = OPJourneyUI.Visible && OPJourneyUI.Instance != null && OPJourneyUI.Instance.CurrentTab == tabIndex;
            OPJourneyUI.ToggleTab(tabIndex);
            SoundEngine.PlaySound(closing ? SoundID.MenuClose : SoundID.MenuOpen);
            return true;
        }
    }
}
