using System;
using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Virtual
{
    /// <summary>���� <see cref="Player.UpdateBuffs"/> ��ͬһ scratch �����ۼ��ʹ���λ��ÿ֡������ԭ��ǯ�ơ�</summary>
    internal sealed class BuffVirtualEffectSummonGuard
    {
        public const int AbsoluteMaxMinionSlots = 24;
        public const int AbsoluteMaxSentrySlots = 16;

        private readonly int _maxMinions;
        private readonly float _slotsMinions;
        private readonly int _maxTurrets;

        private BuffVirtualEffectSummonGuard(Player player)
        {
            _maxMinions = player.maxMinions;
            _slotsMinions = player.slotsMinions;
            _maxTurrets = player.maxTurrets;
        }

        public static BuffVirtualEffectSummonGuard Capture(Player player) => new BuffVirtualEffectSummonGuard(player);

        public void Restore(Player player)
        {
            player.maxMinions = _maxMinions;
            player.slotsMinions = _slotsMinions;
            player.maxTurrets = _maxTurrets;
        }

        public static void Clamp(Player player)
        {
            if (player == null)
                return;

            player.maxMinions = Math.Clamp(player.maxMinions, 1, AbsoluteMaxMinionSlots);
            player.maxTurrets = Math.Clamp(player.maxTurrets, 0, AbsoluteMaxSentrySlots);
            if (player.slotsMinions > player.maxMinions)
                player.slotsMinions = player.maxMinions;
            if (player.slotsMinions < 0f)
                player.slotsMinions = 0f;
        }
    }
}
