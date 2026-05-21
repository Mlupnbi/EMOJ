using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Content
{
    /// <summary>����������ռλͼ�ꣻЧ���� <see cref="BuffResearchPlayer"/> ��ȫ���ڡ�</summary>
    public class EMOJOmegaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) =>
            player.buffTime[buffIndex] = BuffResearchPlayer.ActiveBuffDurationFrames;
    }
}
