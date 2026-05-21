using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Managed
{
    /// <summary>��������ʱ��ԭ misc ��Ʒ�ۡ����������ﵯĻ���������/NPC ��ʵ����ˡ�</summary>
    public static class BuffWorldTransitionCleanup
    {
        public static void OnPlayerEnterWorld(Player player, BuffResearchPlayer mp)
        {
            if (player == null || mp == null || player.whoAmI != Main.myPlayer)
                return;

            RestoreMiscEquips(player, mp);
            PruneDuplicatePetProjectiles(player);
            mp.BeginWorldTransitionGrace();
        }

        public static void OnPlayerLeaveWorld(Player player, BuffResearchPlayer mp)
        {
            if (player == null || mp == null || player.whoAmI != Main.myPlayer)
                return;

            RestoreMiscEquips(player, mp);
            PruneDuplicatePetProjectiles(player);
        }

        private static void RestoreMiscEquips(Player player, BuffResearchPlayer mp)
        {
            const int slotCount = 4;
            for (int slot = 0; slot < slotCount && slot < mp.HasSavedMisc.Length; slot++)
            {
                if (!mp.HasSavedMisc[slot])
                    continue;

                if (mp.SavedMiscEquips[slot] != null && !mp.SavedMiscEquips[slot].IsAir)
                    player.miscEquips[slot] = mp.SavedMiscEquips[slot].Clone();

                mp.HasSavedMisc[slot] = false;
            }
        }

        /// <summary>ͬ���� projPet ֻ����һ��������������ʱ�ظ����ɵĳ���ʵ�塣</summary>
        private static void PruneDuplicatePetProjectiles(Player player)
        {
            var seenTypes = new System.Collections.Generic.HashSet<int>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.owner != player.whoAmI)
                    continue;

                bool isPetLike = p.type != ProjectileID.None && p.type < Main.projPet.Length && Main.projPet[p.type];
                if (!isPetLike)
                    continue;

                if (!seenTypes.Add(p.type))
                    p.Kill();
            }
        }
    }
}
