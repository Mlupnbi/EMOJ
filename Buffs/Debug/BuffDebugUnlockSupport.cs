using Terraria;

namespace EvenMoreOverpoweredJourney.Buffs.Debug
{
    /// <summary>DEBUG_UNLOCKALLBUFFS �� <see cref="BuffResearchPlayer"/> �浵������Žӡ�</summary>
    public static class BuffDebugUnlockSupport
    {
        public static void OnDebugUnlockAllBuffsEnabled()
        {
            Player player = Main.LocalPlayer;
            if (player?.active != true)
                return;

            player.GetModPlayer<BuffResearchPlayer>()?.OnDebugUnlockAllBuffsEnabled();
        }

        public static void OnDebugUnlockAllBuffsDisabled()
        {
            Player player = Main.LocalPlayer;
            if (player?.active != true)
                return;

            player.GetModPlayer<BuffResearchPlayer>()?.OnDebugUnlockAllBuffsDisabled();
        }
    }
}
