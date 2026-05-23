using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research.Players;

namespace EvenMoreOverpoweredJourney.Research.Globals
{
    /// <summary>旅途研究完成放置类物品时，视为解锁对应制作环境（对齐 UpgradeResearch researchedTiles）。</summary>
    public sealed class ResearchStationEnvironmentGlobalItem : GlobalItem
    {
        public override void OnResearched(Item item, bool fullyResearched)
        {
            if (!fullyResearched || Main.netMode == NetmodeID.Server || item?.IsAir != false)
                return;

            if (Main.myPlayer < 0 || Main.myPlayer >= Main.player.Length)
                return;

            Player player = Main.player[Main.myPlayer];
            if (player == null || !player.active)
                return;

            player.GetModPlayer<ResearchCraftingPlayer>()?.OnItemFullyResearched(item.type);
        }
    }
}
