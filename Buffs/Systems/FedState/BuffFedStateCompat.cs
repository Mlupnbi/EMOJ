using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace EvenMoreOverpoweredJourney.Buffs.Systems.FedState
{
    /// <summary>
    /// ����֮��/��������ԭ�� <see cref="Player.UpdateStarvingState"/> ֻ�� Buff ���ϵ� <see cref="BuffID.Sets.IsFedState"/>��
    /// EMOJ ����ʳ�ﲻ������ʱ��Ҵ����ʳ Buff������ÿ֡ѹ�Ƽ��� debuff��
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
                ResolveVanillaBuffId("Peckish", 332),
                ResolveVanillaBuffId("Hungry", 333),
                ResolveVanillaBuffId("Starving", 334)
            };
            _hungerIdsInitialized = true;
        }

        private static int ResolveVanillaBuffId(string name, int fallback)
        {
            if (BuffID.Search.TryGetId(name, out int id) && id > 0)
                return id;

            return fallback;
        }

        public static bool IsFedStateBuff(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            return buffId < BuffID.Sets.IsFedState.Length && BuffID.Sets.IsFedState[buffId];
        }

        public static bool ShouldForcePhysical(int buffId) =>
            IsFedStateBuff(buffId) || BuffVirtualEffectSystem.VanillaPhysicalOnlyBuffs.Contains(buffId);

        /// <summary>��ԭ�� UpdateStarvingState ��ͬ���������ʵ Buff ������������ scratch����</summary>
        public static bool PlayerBarHasFedState(Player player)
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

        /// <summary>EMOJ ����Ϊ���Ӧ���ڱ�ʳ��������ʳ��/��ʳ�����棩��</summary>
        public static bool ShouldMaintainSatiety(BuffResearchPlayer mp)
        {
            if (mp == null || mp.Player == null)
                return false;

            foreach (int buffId in mp.ActiveBuffs)
            {
                if (mp.DisabledBuffs.Contains(buffId))
                    continue;

                if (IsFedStateBuff(buffId))
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

            EnsureFedStateVisibleOnBar(player, mp);
            SuppressHungerDebuffs(player);
        }

        /// <summary>�ڼ����ж�ǰ��֤������ IsFedState������ UpdateStarvingState ʩ�� Peckish��</summary>
        public static void EnsureFedStateVisibleOnBar(Player player, BuffResearchPlayer mp)
        {
            if (player == null || mp == null || !ShouldMaintainSatiety(mp))
                return;

            if (PlayerBarHasFedState(player))
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
                if (mp.DisabledBuffs.Contains(buffId) || !IsFedStateBuff(buffId))
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
            return IsFedStateBuff(buffId) ? 1 : 0;
        }
    }
}
