using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics
{
    /// <summary>���桸ȫ�������ʱ��¼����ԭ�򣬱��ڶ��ա�ÿ��������һ������</summary>
    public static class BuffBulkSkipDiagnostics
    {
        public enum SkipReason
        {
            NotUnlocked,
            SetBonusSection,
            ManualEntity,
            ManualOnly,
            UnsafeVirtual
        }

        public readonly struct SkipEntry
        {
            public readonly int BuffId;
            public readonly SkipReason Reason;

            public SkipEntry(int buffId, SkipReason reason)
            {
                BuffId = buffId;
                Reason = reason;
            }
        }

        public static SkipReason? TryGetSkipReason(int buffId, string category, bool enable, BuffResearchPlayer player)
        {
            if (!enable)
                return null;

            if (!player.IsBuffUnlocked(buffId))
                return SkipReason.NotUnlocked;

            if (category == BuffCategories.PositiveEquipment && BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId))
                return SkipReason.SetBonusSection;

            if (BuffEntityIndexSystem.RequiresManualEntityManagement(buffId))
                return SkipReason.ManualEntity;

            if (BuffVirtualEffectSafety.IsManualOnlyBulkEnable(buffId))
                return SkipReason.ManualOnly;

            if (!OPJourneyConfig.AllowBulkEnableUnsafeVirtual() &&
                !BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId) &&
                !BuffVirtualEffectSafety.IsSafeForVirtualApply(buffId))
                return SkipReason.UnsafeVirtual;

            return null;
        }

        public readonly struct SkipCountBreakdown
        {
            public readonly int NotUnlocked;
            public readonly int ManualEntity;
            public readonly int ManualOnly;
            public readonly int UnsafeVirtual;

            public int TotalExcludingSetBonus => NotUnlocked + ManualEntity + ManualOnly + UnsafeVirtual;

            public static SkipCountBreakdown FromEntries(IReadOnlyList<SkipEntry> skipped)
            {
                int notUnlocked = 0;
                int manualEntity = 0;
                int manualOnly = 0;
                int unsafeVirtual = 0;

                if (skipped == null)
                    return default;

                foreach (SkipEntry entry in skipped)
                {
                    switch (entry.Reason)
                    {
                        case SkipReason.NotUnlocked:
                            notUnlocked++;
                            break;
                        case SkipReason.ManualEntity:
                            manualEntity++;
                            break;
                        case SkipReason.ManualOnly:
                            manualOnly++;
                            break;
                        case SkipReason.UnsafeVirtual:
                            unsafeVirtual++;
                            break;
                    }
                }

                return new SkipCountBreakdown(notUnlocked, manualEntity, manualOnly, unsafeVirtual);
            }

            public SkipCountBreakdown(int notUnlocked, int manualEntity, int manualOnly, int unsafeVirtual)
            {
                NotUnlocked = notUnlocked;
                ManualEntity = manualEntity;
                ManualOnly = manualOnly;
                UnsafeVirtual = unsafeVirtual;
            }
        }

        public static void LogSkippedList(string category, bool enable, IReadOnlyList<SkipEntry> skipped)
        {
            if (!EmojLog.IsFullMode || skipped == null || skipped.Count == 0)
                return;

            var counts = new Dictionary<SkipReason, int>();
            foreach (SkipEntry entry in skipped)
            {
                if (!counts.ContainsKey(entry.Reason))
                    counts[entry.Reason] = 0;
                counts[entry.Reason]++;
            }

            var summary = new StringBuilder();
            summary.Append($"BulkSkip category={category} enable={enable} total={skipped.Count}");
            foreach (var pair in counts)
                summary.Append($" {pair.Key}={pair.Value}");

            EmojLog.InfoFull(EmojLogChannel.Buff, summary.ToString());

            const int maxLines = 40;
            for (int i = 0; i < skipped.Count && i < maxLines; i++)
            {
                SkipEntry e = skipped[i];
                string name = BuffDisplayNameHelper.GetDisplayName(e.BuffId);
                EmojLog.InfoFull(EmojLogChannel.Buff, $"  skip buff={e.BuffId} reason={e.Reason} name={name}");
            }

            if (skipped.Count > maxLines)
                EmojLog.InfoFull(EmojLogChannel.Buff, $"  ... and {skipped.Count - maxLines} more");
        }
    }
}
