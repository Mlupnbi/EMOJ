using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Integration.ImproveGame;
using EvenMoreOverpoweredJourney.Research.Crafting;
using EvenMoreOverpoweredJourney.Research.Players;

namespace EvenMoreOverpoweredJourney.Research.Systems
{
    /// <summary>注册便携制作环境映射，并在 mod/世界加载时刷新。</summary>
    public sealed class ResearchCraftEnvironmentSystem : ModSystem
    {
        public override void OnModLoad()
        {
            RegisterVanillaPortableMappings();
            ImproveGameIntegration.Refresh();
        }

        public override void PostSetupContent()
        {
            ImproveGameIntegration.Refresh();
        }

        public override void OnWorldLoad()
        {
            if (Main.dedServ)
                return;

            Main.LocalPlayer?.GetModPlayer<ResearchCraftingPlayer>()?.RebuildResearchedCraftEnvironment();
        }

        private static void RegisterVanillaPortableMappings()
        {
            PortableCraftEnvironmentRegistry.Register(1781, TileID.Sinks);
        }
    }
}
