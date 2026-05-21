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
    /// <summary>������־ר�ã����ص�״̬���գ��� <see cref="EmojLog.IsFullMode"/>����</summary>
    public static class EmojLogDiagnostics
    {
        public static void ResetBuffTrace() { }

        public static void LogBuffPlayerSnapshot(Player player, BuffResearchPlayer mp, string reason)
        {
            if (!EmojLog.IsFullMode || player == null || mp == null)
                return;

            EmojLog.InfoFull(EmojLogChannel.Buff,
                $"{reason} | active={mp.ActiveBuffs.Count} disabled={mp.DisabledBuffs.Count} pinned={mp.PinnedPhysicalBuffs.Count} " +
                $"virtualQ={mp.VirtualEffectQueue.Count} stat={mp.VirtualStatQueueCount} combat={mp.VirtualCombatVisualQueueCount} " +
                $"scratch={OPJourneyConfig.UseVirtualScratchApply()} aggregate={mp.UseAggregatedVirtualBarDisplay()} " +
                $"maxMinions={player.maxMinions} slotsMinions={player.slotsMinions:F2}");
        }

        public static void LogBuffToggle(string action, int buffId, BuffResearchPlayer mp)
        {
            if (!EmojLog.IsActive || mp == null)
                return;

            bool virtualized = BuffVirtualEffectSystem.UsesVirtualEffect(buffId, mp);
            bool setBonus = BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId);
            EmojLog.Info(EmojLogChannel.Buff,
                $"{action} buff={buffId} virtual={virtualized} setBonus={setBonus} pinned={mp.PinnedPhysicalBuffs.Contains(buffId)}");
        }

        public static void LogBulkToggle(string category, bool enable, int enabled, int skipped, int skippedSetBonus)
        {
            if (!EmojLog.IsActive)
                return;

            EmojLog.Info(EmojLogChannel.Buff,
                $"BulkToggle category={category} enable={enable} enabled={enabled} skipped={skipped} skippedSetBonus={skippedSetBonus}");

            if (EmojLog.IsFullMode && Main.LocalPlayer != null)
                LogBuffPlayerSnapshot(Main.LocalPlayer, Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>(), "after-bulk");
        }
    }
}
