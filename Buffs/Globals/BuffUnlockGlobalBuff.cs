using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Globals
{
    /// <summary>������ϳ����������¼ Buff ʱ���ý������� BuffsPlus����������¼��һ�£���</summary>
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

            player.GetModPlayer<BuffResearchPlayer>().TryGrantPermanentUnlock(type);
        }
    }
}
