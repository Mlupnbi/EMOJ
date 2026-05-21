using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.Systems.Catalog;
using EvenMoreOverpoweredJourney.Buffs.Systems.Virtual;
using EvenMoreOverpoweredJourney.Buffs.Systems.Managed;
using EvenMoreOverpoweredJourney.Buffs.Systems.Combat;
using EvenMoreOverpoweredJourney.Buffs.Systems.Spawning;
using EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus;
using EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport;
using EvenMoreOverpoweredJourney.Buffs.Systems.FedState;
using EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics;
using EvenMoreOverpoweredJourney.Buffs.Systems.Display;

namespace EvenMoreOverpoweredJourney.Core.Logging
{
    public sealed class EmojLogSystem : ModSystem
    {
        public override void OnModLoad() => EmojLog.RefreshFromConfig();

        public override void OnModUnload()
        {
            EmojLog.EndSession();
            EmojLog.RefreshFromConfig();
        }

        public override void OnWorldLoad()
        {
            if (Main.dedServ)
                return;

            EmojLog.RefreshFromConfig();
            EmojLog.ResetDedupeKeys();
            EmojLogDiagnostics.ResetBuffTrace();

            if (!EmojLog.IsActive)
                return;

            EmojLog.EnsureSession();

            if (Main.LocalPlayer != null)
            {
                BuffResearchPlayer mp = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
                EmojLog.Info(EmojLogChannel.Core,
                    $"WorldLoad world={Main.worldName} active={mp.ActiveBuffs.Count} virtual={mp.GetVirtualBarBuffCount()} aggregate={mp.UseAggregatedVirtualBarDisplay()}");

                if (BuffFedStateCompat.ShouldMaintainSatiety(mp))
                    EmojLog.InfoOnce(EmojLogChannel.Buff, "fed:world", "satiety maintenance active");
            }

            if (EmojLog.IsFullMode)
            {
                EmojLog.WriteEnvironmentManifest();
                if (Main.LocalPlayer != null)
                    EmojLogDiagnostics.LogBuffPlayerSnapshot(Main.LocalPlayer,
                        Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>(), "world-load");
            }
        }

        public override void OnWorldUnload()
        {
            if (EmojLog.IsActive)
                EmojLog.Info(EmojLogChannel.Core, "WorldUnload");

            EmojLog.EndSession();
        }
    }
}
