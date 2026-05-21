using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Managed
{
    /// <summary>
    /// tML �� UpdateBuffs ǰ������ buffImmune����ԭ�����ѭ�����ע��������ߣ����� BuffsPlus ��ƽ��ۣ���
    /// </summary>
    public sealed class BuffImmunityHookSystem : ModSystem
    {
        private static uint lastImmunityEnforceGameFrame;

        public override void Load()
        {
            On_Player.UpdateBuffs += EnforceDisabledBuffImmunity;
        }

        public override void Unload()
        {
            On_Player.UpdateBuffs -= EnforceDisabledBuffImmunity;
            lastImmunityEnforceGameFrame = 0;
        }

        private static void EnforceDisabledBuffImmunity(On_Player.orig_UpdateBuffs orig, Player player, int buffIndex)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                BuffResearchPlayer modPlayer = player.GetModPlayer<BuffResearchPlayer>();
                if (modPlayer.IsApplyingVirtualBuffEffects)
                {
                    orig(player, buffIndex);
                    return;
                }

                if (Main.GameUpdateCount != lastImmunityEnforceGameFrame)
                {
                    lastImmunityEnforceGameFrame = Main.GameUpdateCount;
                    modPlayer.EnforceDisabledBuffImmunityBeforeUpdate();
                    if (BuffFedStateCompat.ShouldMaintainSatiety(modPlayer))
                        BuffFedStateCompat.SuppressHungerDebuffs(player);
                }
            }

            orig(player, buffIndex);

            if (player.whoAmI != Main.myPlayer || buffIndex < 0 || buffIndex >= Player.MaxBuffs)
                return;

            BuffResearchPlayer after = player.GetModPlayer<BuffResearchPlayer>();
            int buffType = player.buffType[buffIndex];
            if (buffType > 0 && after.DisabledBuffs.Contains(buffType))
                player.DelBuff(buffIndex);
        }
    }
}
