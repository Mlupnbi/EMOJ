using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.FedState
{
    /// <summary>
    /// ����֮�꣺<see cref="Player.UpdateStarvingState"/> ֻɨ����ʵ Buff ���ϵ� <see cref="BuffID.Sets.IsFedState"/>��
    /// ���ж�ǰ�Ҵ�����? Buff���ж����������? debuff��tML ÿ֡������ buffImmune����
    /// </summary>
    public sealed class BuffFedStateHookSystem : ModSystem
    {
        public override void Load() => On_Player.UpdateStarvingState += GuardStarvingState;

        public override void Unload() => On_Player.UpdateStarvingState -= GuardStarvingState;

        private static void GuardStarvingState(On_Player.orig_UpdateStarvingState orig, Player player, bool withEmote)
        {
            if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
            {
                orig(player, withEmote);
                return;
            }

            BuffResearchPlayer mp = player.GetModPlayer<BuffResearchPlayer>();
            if (!BuffFedStateCompat.ShouldMaintainSatiety(mp))
            {
                orig(player, withEmote);
                return;
            }

            BuffFedStateCompat.EnsureWellFedVisibleOnBar(player, mp);
            orig(player, false);
            BuffFedStateCompat.SuppressHungerDebuffs(player);
            BuffFedStateCompat.EnsureWellFedVisibleOnBar(player, mp);
        }
    }
}
