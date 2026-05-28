using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.Players;
using EvenMoreOverpoweredJourney.Buffs.Systems.FedState;

namespace EvenMoreOverpoweredJourney.Buffs.Globals
{
    /// <summary>玩家获得 Buff 时登记永久解锁（与 BuffsPlus 分轨，仅写 UnlockedBuffs）。</summary>
    public sealed class BuffUnlockGlobalBuff : GlobalBuff
    {
        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (Main.netMode == NetmodeID.Server || player == null || player.whoAmI != Main.myPlayer)
                return;

            if (type <= 0 || buffIndex < 0 || buffIndex >= player.buffTime.Length)
                return;

            if (player.buffTime[buffIndex] <= 0)
                return;

            if (BuffFedStateCompat.IsHungerDebuff(type))
                return;

            player.GetModPlayer<BuffResearchPlayer>().TryGrantPermanentUnlock(type);
        }
    }
}
