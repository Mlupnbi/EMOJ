using EvenMoreOverpoweredJourney.Integration.ImproveGame;
using EvenMoreOverpoweredJourney.Integration.Session;
using EvenMoreOverpoweredJourney.Integration.Browser;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Integration.ImproveGame
{
    public sealed class ImproveGameIntegrationSystem : ModSystem
    {
        public override void OnModLoad() => ImproveGameIntegration.Refresh();

        public override void PostSetupContent() => ImproveGameIntegration.Refresh();

        public override void OnWorldLoad() => ImproveGameIntegration.Refresh();
    }
}
