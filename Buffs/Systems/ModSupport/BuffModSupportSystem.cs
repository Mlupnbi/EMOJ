using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport
{
    public sealed class BuffModSupportSystem : ModSystem
    {
        public override void OnModLoad()
        {
            BuffModProfileLoader.Reload(Mod);
            BuffModSupportLoader.Reload(Mod);
        }

        public override void PostSetupContent()
        {
            VanillaBuffCatalogSystem.RebuildCatalog();
            VanillaBuffCatalogSystem.ExportCatalogToDisk(Mod);
            BuffModProfileLoader.Reload(Mod);
            BuffModSupportLoader.Reload(Mod);
            BuffVirtualEffectClassifier.Rebuild();
        }
    }
}
