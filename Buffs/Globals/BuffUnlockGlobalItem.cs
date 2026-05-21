using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Globals
{
    /// <summary>����/ʹ�ô� buffType ����Ʒʱ���ý�����Ӧ Buff��д��浵����</summary>
    public sealed class BuffUnlockGlobalItem : GlobalItem
    {
        public override void OnConsumeItem(Item item, Player player)
        {
            if (item?.buffType > 0)
                player.GetModPlayer<BuffResearchPlayer>().TryGrantPermanentUnlock(item.buffType);
        }
    }
}
