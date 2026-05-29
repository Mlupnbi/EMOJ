using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Registry;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public sealed class FurnitureBlueprintSystem : ModSystem
    {
        public override void OnModLoad()
        {
            BuiltinBlueprintTemplates.Register();
            FurnitureVanillaLivingWoodBridge.RegisterRecipeGroups();
            FurnitureWikiExpectations.Reload(Mod);
            FurnitureGoldenExpectations.Reload(Mod);
            int count = BuiltinBlueprintTemplates.All?.Count ?? 0;
            FurnitureBlueprintLog.Info($"mod load templates={count}");
            for (int i = 0; i < count; i++)
            {
                BlueprintLayout t = BuiltinBlueprintTemplates.All[i];
                FurnitureBlueprintLog.InfoFull($"  template[{i}] id={t.Id} size={t.Width}x{t.Height} key={t.DisplayNameKey} source={BuiltinBlueprintTemplates.GetLoadSource(t.Id)}");
                BlueprintTemplateDiagnostics.LogLegacyConversionSummary(t);
                if (BuiltinBlueprintTemplates.TryGetTemplate(t.Id, out BlueprintTemplate cached))
                    BlueprintTemplateDiagnostics.LogFileRoundTrip(cached);
            }

            BlueprintTemplateAcceptance.RunBuiltinChecks(Mod);
        }

        public override void PostSetupContent()
        {
            FurnitureTileItemRegistry.Build();
            FurnitureTileSlotRegistry.Build(force: true);
            FurnitureSetMaterialCheckers.Build();
            FurnitureSetCatalog.Build();
            FurnitureWikiExpectations.Reload(Mod);
            FurnitureGoldenExpectations.Reload(Mod);
        }

        public override void OnWorldLoad()
        {
            FurnitureStyleClusterCatalog.ClearCache();
            FurnitureSetCacheSystem.InvalidateAll();
            FurnitureReverseSeedProbeCache.Clear();
            FurnitureTileItemRegistry.Build();
            if (!FurnitureTileSlotRegistry.IsBuilt)
                FurnitureTileSlotRegistry.Build(force: true);
            FurnitureSetMaterialCheckers.Build();
            FurnitureSetCatalog.Build();

            Player local = Main.LocalPlayer;
            if (local != null && local.TryGetModPlayer(out FurnitureBlueprintPlayer fb))
                fb.ClearWorkspaceForWorldEntry();

            if (!EmojLog.IsActive)
                return;
            FurnitureBlueprintLog.Info($"world load templates={BuiltinBlueprintTemplates.All?.Count ?? 0}");
        }
    }
}
