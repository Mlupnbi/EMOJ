using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Combat
{
    /// <summary>�����ٻ��ﵯĻ�ж���Ʒ/ Buff �����ʹӻ����ڱ����� <see cref="Item.sentry"/> ���ɿ�����</summary>
    public static class BuffSummonProjectileHelper
    {
        public static bool TryGetShootProjectile(Item item, out Projectile sample)
        {
            sample = null;
            if (item == null || item.IsAir || item.shoot <= ProjectileID.None)
                return false;

            return ContentSamples.ProjectilesByType.TryGetValue(item.shoot, out sample) && sample != null;
        }

        public static bool ItemShootIsSentry(Item item) =>
            TryGetShootProjectile(item, out Projectile sample) && sample.sentry;

        public static bool ItemShootIsCombatMinion(Item item) =>
            TryGetShootProjectile(item, out Projectile sample) && sample.minion && !sample.sentry;

        public static bool BuffHasSentryItem(int buffId)
        {
            if (buffId <= 0)
                return false;

            foreach (var pair in ContentSamples.ItemsByType)
            {
                Item item = pair.Value;
                if (item == null || item.IsAir || item.buffType != buffId)
                    continue;

                if (ItemShootIsSentry(item))
                    return true;
            }

            return false;
        }
    }
}
