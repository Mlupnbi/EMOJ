using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.FedState
{
    /// <summary>
    /// 饥饿之雨/虚拟食物：原版 <see cref="Player.UpdateStarvingState"/> 只扫 Buff 栏上的 <see cref="BuffID.Sets.IsFedState"/>。
    /// EMOJ 在虚拟食物开启时于栏上保留饱食 Buff，并每帧压制饥饿 debuff。
    /// 注意：<see cref="BuffID.Sets.IsFedState"/> 同时包含饱食与饥饿阶段（332–334），不可用于判断「已饱食」。
    /// </summary>
    public static class BuffFedStateCompat
    {
        private static int[] _hungerDebuffIds;
        private static bool _hungerIdsInitialized;
        private static readonly int[] WellFedTierIds =
        {
            BuffID.WellFed3,
            BuffID.WellFed2,
            BuffID.WellFed
        };

        private static int[] HungerDebuffIds
        {
            get
            {
                if (!_hungerIdsInitialized)
                    EnsureHungerDebuffIds();
                return _hungerDebuffIds ?? Array.Empty<int>();
            }
        }

        private static void EnsureHungerDebuffIds()
        {
            _hungerDebuffIds = new[]
            {
                BuffID.NeutralHunger,
                BuffID.Hunger,
                BuffID.Starving
            };
            _hungerIdsInitialized = true;
        }

        /// <summary>饱食类 Buff（不含饥饿 debuff）。</summary>
        public static bool IsWellFedBuff(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            return buffId < BuffID.Sets.IsWellFed.Length && BuffID.Sets.IsWellFed[buffId];
        }

        /// <summary>饥饿 debuff（NeutralHunger / Hunger / Starving）。</summary>
        public static bool IsHungerDebuff(int buffId)
        {
            if (buffId <= 0)
                return false;

            foreach (int hungerId in HungerDebuffIds)
            {
                if (buffId == hungerId)
                    return true;
            }

            return false;
        }

        public static bool ShouldForcePhysical(int buffId) =>
            IsWellFedBuff(buffId) || BuffVirtualEffectSystem.VanillaPhysicalOnlyBuffs.Contains(buffId);

        /// <summary>与原版 UpdateStarvingState 一致：栏上是否存在任意 IsFedState（含饥饿阶段）。</summary>
        public static bool PlayerBarHasAnyFedStateSlot(Player player)
        {
            if (player == null)
                return false;

            int limit = Math.Min(Player.MaxBuffs, Math.Min(player.buffType.Length, player.buffTime.Length));
            for (int i = 0; i < limit; i++)
            {
                int type = player.buffType[i];
                if (type <= 0 || player.buffTime[i] <= 0)
                    continue;

                if (type < BuffID.Sets.IsFedState.Length && BuffID.Sets.IsFedState[type])
                    return true;
            }

            return false;
        }

        /// <summary>栏上是否已有饱食（WellFed 系列），不含饥饿 debuff。</summary>
        public static bool PlayerBarHasWellFed(Player player)
        {
            if (player == null)
                return false;

            int limit = Math.Min(Player.MaxBuffs, Math.Min(player.buffType.Length, player.buffTime.Length));
            for (int i = 0; i < limit; i++)
            {
                int type = player.buffType[i];
                if (type <= 0 || player.buffTime[i] <= 0)
                    continue;

                if (IsWellFedBuff(type))
                    return true;
            }

            return false;
        }

        /// <summary>EMOJ 是否应维持饱食栏位并压制饥饿 debuff。</summary>
        public static bool ShouldMaintainSatiety(BuffResearchPlayer mp)
        {
            if (mp == null || mp.Player == null)
                return false;

            foreach (int buffId in mp.ActiveBuffs)
            {
                if (mp.DisabledBuffs.Contains(buffId))
                    continue;

                if (IsWellFedBuff(buffId))
                    return true;

                if (BuffSourceIndexSystem.IsPotionFoodBuff(buffId))
                    return true;
            }

            return false;
        }

        public static void ApplySatietyAfterBuffPipeline(Player player, BuffResearchPlayer mp)
        {
            if (player == null || mp == null || player.whoAmI != Main.myPlayer)
                return;

            if (!ShouldMaintainSatiety(mp))
                return;

            EnsureWellFedVisibleOnBar(player, mp);
            SuppressHungerDebuffs(player);
        }

        /// <summary>在饥饿判定前保证栏上有 IsWellFed，避免 UpdateStarvingState 施加饥饿 debuff。</summary>
        public static void EnsureWellFedVisibleOnBar(Player player, BuffResearchPlayer mp)
        {
            if (player == null || mp == null || !ShouldMaintainSatiety(mp))
                return;

            if (PlayerBarHasWellFed(player))
                return;

            EnsureRepresentativeFedBuffOnBar(player, mp);
        }

        public static void ResetSessionDiagnostics() { }

        public static void SuppressHungerDebuffs(Player player)
        {
            foreach (int hungerId in HungerDebuffIds)
            {
                if (hungerId <= 0 || hungerId >= BuffLoader.BuffCount)
                    continue;

                if (hungerId < player.buffImmune.Length)
                    player.buffImmune[hungerId] = true;

                player.ClearBuff(hungerId);
            }
        }

        private static int EnsureRepresentativeFedBuffOnBar(Player player, BuffResearchPlayer mp)
        {
            int fedId = ResolveBestFedBuffToDisplay(mp);
            if (fedId <= 0)
                return 0;

            int existing = player.FindBuffIndex(fedId);
            if (existing >= 0)
            {
                if (player.buffTime[existing] < BuffResearchPlayer.ActiveBuffDurationFrames)
                    player.buffTime[existing] = BuffResearchPlayer.ActiveBuffDurationFrames;

                if (fedId < Main.buffNoTimeDisplay.Length)
                    Main.buffNoTimeDisplay[fedId] = true;

                return fedId;
            }

            if (fedId < player.buffImmune.Length)
                player.buffImmune[fedId] = false;

            player.AddBuff(fedId, BuffResearchPlayer.ActiveBuffDurationFrames);

            if (fedId < Main.buffNoTimeDisplay.Length)
                Main.buffNoTimeDisplay[fedId] = true;

            return fedId;
        }

        private static int ResolveBestFedBuffToDisplay(BuffResearchPlayer mp)
        {
            foreach (int tierId in WellFedTierIds)
            {
                if (tierId > 0 && mp.ActiveBuffs.Contains(tierId) && !mp.DisabledBuffs.Contains(tierId))
                    return tierId;
            }

            int bestId = 0;
            int bestRank = 0;
            foreach (int buffId in mp.ActiveBuffs)
            {
                if (mp.DisabledBuffs.Contains(buffId) || !IsWellFedBuff(buffId))
                    continue;

                int rank = WellFedTierRank(buffId);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    bestId = buffId;
                }
            }

            if (bestId > 0)
                return bestId;

            return BuffID.WellFed3;
        }

        private static int WellFedTierRank(int buffId)
        {
            if (buffId == BuffID.WellFed3)
                return 3;
            if (buffId == BuffID.WellFed2)
                return 2;
            if (buffId == BuffID.WellFed)
                return 1;
            return IsWellFedBuff(buffId) ? 1 : 0;
        }
    }
}
