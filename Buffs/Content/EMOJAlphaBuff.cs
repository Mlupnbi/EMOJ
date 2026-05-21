using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Content
{
    /// <summary>�ۺ�ͼ��ռλ����������� <see cref="BuffResearchPlayer.PreUpdateBuffs"/>���˴������ڡ�</summary>
    public class EMOJAlphaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) =>
            player.buffTime[buffIndex] = BuffResearchPlayer.ActiveBuffDurationFrames;
    }
}
