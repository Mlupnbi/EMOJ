using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Managed
{
    /// <summary>
    /// ���������棨����/����/�����������ȣ�������ʵ <see cref="Player.AddBuff"/> ά�֡�
    /// ��������������� <see cref="BuffVirtualEffectSystem.ApplyAllVirtualEffects"/>��
    /// </summary>
    public static class BuffManagedReapplySystem
    {
        private const int ReapplyCooldownFrames = 30;

        private static readonly Dictionary<int, int> ReapplyCooldownByBuff = new Dictionary<int, int>();
        private static readonly HashSet<int> TimeRulesActive = new HashSet<int>();

        public static void ClearRuntimeState()
        {
            ReapplyCooldownByBuff.Clear();
            foreach (int buffType in TimeRulesActive)
                BuffManagedTimeRules.SetEnabled(buffType, false);
            TimeRulesActive.Clear();
        }

        public static bool ShouldMaintainViaAddBuff(int buffId, BuffResearchPlayer mp)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount || mp.DisabledBuffs.Contains(buffId))
                return false;

            if (VanillaBuffStatRegistry.IsSyntheticStatBuff(buffId))
                return false;

            if (BuffPlayerApplicability.ShouldBlockManagedApplication(buffId))
                return false;

            if (BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId))
                return false;

            string category = BuffPage.GetBuffCategoryStatic(buffId);
            if (BuffPage.IsExclusiveCombatSummonCategory(category))
                return false;

            if (BuffVirtualEffectSystem.UsesVirtualEffect(buffId, mp))
                return false;

            if (BuffVirtualEffectSystem.IsEntityManagedBuff(buffId))
                return false;

            if (!BuffVirtualEffectSafety.IsSafeForVirtualApply(buffId))
                return false;

            return true;
        }

        public static void ApplyMissing(Player player, BuffResearchPlayer mp, bool allowNewAddBuff = true)
        {
            if (player == null || mp == null || player.dead)
                return;

            SyncTimeRules(mp);
            DecayCooldowns();

            bool tryAddMissing = allowNewAddBuff && Main.GameUpdateCount % 15 == player.whoAmI % 15;

            var order = new List<int>();
            BuildReapplyOrder(mp, order);

            foreach (int buffId in order)
            {
                if (!ShouldMaintainViaAddBuff(buffId, mp))
                    continue;

                if (PlayerHasBuff(player, buffId))
                {
                    ClearReapplyCooldown(buffId);
                    RenewIfLow(player, buffId);
                    continue;
                }

                if (!tryAddMissing)
                    continue;

                if (IsReapplyCoolingDown(buffId))
                    continue;

                player.AddBuff(buffId, BuffResearchPlayer.ActiveBuffDurationFrames, quiet: true, foodHack: false);
                if (PlayerHasBuff(player, buffId))
                    ClearReapplyCooldown(buffId);
                else
                    SetReapplyCooldown(buffId, ReapplyCooldownFrames);
            }
        }

        private static void SyncTimeRules(BuffResearchPlayer mp)
        {
            var want = new HashSet<int>();
            foreach (int buffId in mp.ActiveBuffs)
            {
                if (ShouldMaintainViaAddBuff(buffId, mp))
                    want.Add(buffId);
            }

            foreach (int buffId in want)
            {
                if (TimeRulesActive.Add(buffId))
                    BuffManagedTimeRules.SetEnabled(buffId, true);
            }

            var remove = new List<int>();
            foreach (int buffId in TimeRulesActive)
            {
                if (!want.Contains(buffId))
                    remove.Add(buffId);
            }

            foreach (int buffId in remove)
            {
                TimeRulesActive.Remove(buffId);
                BuffManagedTimeRules.SetEnabled(buffId, false);
            }
        }

        private static void BuildReapplyOrder(BuffResearchPlayer mp, List<int> order)
        {
            bool rotate = !OPJourneyConfig.UseVirtualScratchApply() && mp.ActiveBuffs.Count > Player.MaxBuffs;

            foreach (int buffId in mp.PinnedPhysicalBuffs)
            {
                if (mp.ActiveBuffs.Contains(buffId))
                    order.Add(buffId);
            }

            foreach (int buffId in mp.ActiveBuffs)
            {
                if (BuffFedStateCompat.ShouldForcePhysical(buffId))
                    TryAddUnique(order, buffId);
            }

            foreach (int buffId in mp.ActiveBuffs)
            {
                if (BuffVirtualEffectSystem.VanillaPhysicalOnlyBuffs.Contains(buffId))
                    TryAddUnique(order, buffId);
            }

            foreach (int buffId in mp.ActiveBuffs)
            {
                if (BuffResearchPlayer.IsMiscEquipBuff(buffId) ||
                    BuffEntityIndexSystem.IsEntitySpawningBuff(buffId))
                    TryAddUnique(order, buffId);
            }

            if (!rotate)
            {
                foreach (int buffId in mp.ActiveBuffs)
                    TryAddUnique(order, buffId);
                return;
            }

            var active = new List<int>(mp.ActiveBuffs);
            if (active.Count == 0)
                return;

            int offset = (int)(Main.GameUpdateCount / 20) % active.Count;
            for (int i = 0; i < active.Count; i++)
                TryAddUnique(order, active[(i + offset) % active.Count]);
        }

        private static void TryAddUnique(List<int> order, int buffId)
        {
            if (!order.Contains(buffId))
                order.Add(buffId);
        }

        private static void RenewIfLow(Player player, int buffId)
        {
            int idx = player.FindBuffIndex(buffId);
            if (idx == -1)
                return;

            if (player.buffTime[idx] < BuffResearchPlayer.ActiveBuffDurationFrames)
                player.buffTime[idx] = BuffResearchPlayer.ActiveBuffDurationFrames;
        }

        private static bool PlayerHasBuff(Player player, int buffType)
        {
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                if (player.buffType[i] == buffType && player.buffTime[i] > 0)
                    return true;
            }

            return false;
        }

        private static bool IsReapplyCoolingDown(int buffType) =>
            ReapplyCooldownByBuff.TryGetValue(buffType, out int ticks) && ticks > 0;

        private static void SetReapplyCooldown(int buffType, int ticks)
        {
            if (ReapplyCooldownByBuff.TryGetValue(buffType, out int existing))
                ReapplyCooldownByBuff[buffType] = System.Math.Max(existing, ticks);
            else
                ReapplyCooldownByBuff[buffType] = ticks;
        }

        private static void ClearReapplyCooldown(int buffType) => ReapplyCooldownByBuff.Remove(buffType);

        private static void DecayCooldowns()
        {
            if (ReapplyCooldownByBuff.Count == 0)
                return;

            var done = new List<int>();
            foreach (var kv in ReapplyCooldownByBuff)
            {
                if (kv.Value <= 1)
                    done.Add(kv.Key);
                else
                    ReapplyCooldownByBuff[kv.Key] = kv.Value - 1;
            }

            foreach (int key in done)
                ReapplyCooldownByBuff.Remove(key);
        }
    }
}
