using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Managed
{
    public sealed class BuffManagedLifecycleSystem : ModSystem
    {
        public override void OnWorldUnload()
        {
            if (!Main.dedServ && Main.LocalPlayer != null)
            {
                BuffResearchPlayer mp = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
                BuffWorldTransitionCleanup.OnPlayerLeaveWorld(Main.LocalPlayer, mp);
            }
        }

        public override void Unload()
        {
            BuffManagedTimeRules.RestoreAll();
            BuffManagedReapplySystem.ClearRuntimeState();
        }
    }
}
