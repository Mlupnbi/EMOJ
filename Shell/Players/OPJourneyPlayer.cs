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
        /// <summary>?зр???????????? type??0 ??????????</summary>
        public int PendingResearchQueryType;

        public override void OnEnterWorld()
        {
            PendingResearchQueryType = 0;
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
            if (EvenMoreOverpoweredJourney.OpenResearchPanelKey?.JustPressed == true)
            {
                OPJourneyUI.ShowAndSwitchTab(0);
                SoundEngine.PlaySound(SoundID.MenuOpen);
                return;
            }

            if (EvenMoreOverpoweredJourney.OpenBuffPanelKey?.JustPressed == true)
            {
                OPJourneyUI.ShowAndSwitchTab(1);
                SoundEngine.PlaySound(SoundID.MenuOpen);
                return;
            }

            if (EvenMoreOverpoweredJourney.OpenItemHubPanelKey?.JustPressed == true)
            {
                OPJourneyUI.ShowAndSwitchTab(2);
                SoundEngine.PlaySound(SoundID.MenuOpen);
                return;
            }

            if (EvenMoreOverpoweredJourney.OpenBestiaryPanelKey?.JustPressed == true)
            {
                OPJourneyUI.ShowAndSwitchTab(3);
                SoundEngine.PlaySound(SoundID.MenuOpen);
                return;
            }

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

                PendingResearchQueryType = hoverType;
                OPJourneyUI.ShowAndSwitchTab(0);
                SoundEngine.PlaySound(SoundID.MenuOpen);
                return;
            }
        }
    }
}
