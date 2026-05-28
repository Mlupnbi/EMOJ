using EvenMoreOverpoweredJourney.FurnitureBlueprint.Placement;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;
using Terraria;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>Legacy 布局入口：转为 <see cref="BlueprintTemplate"/> 后委托 <see cref="BlueprintTemplatePlacer"/>。</summary>
    public static class FurnitureBlueprintPlacer
    {
        public static bool TryPlace(
            Player player,
            BlueprintLayout layout,
            FurnitureScheme scheme,
            bool consumeMaterials,
            BlueprintPlacementMode mode = BlueprintPlacementMode.Strict)
        {
            if (layout == null)
                return false;

            BlueprintTemplate template = BlueprintTemplate.FromLegacyLayout(layout);
            return BlueprintTemplatePlacer.TryPlace(player, template, scheme, consumeMaterials, mode);
        }
    }
}
