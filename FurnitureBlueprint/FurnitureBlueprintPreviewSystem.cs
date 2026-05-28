using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Items;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public sealed class FurnitureBlueprintPreviewSystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(layer => layer.Name == "Vanilla: Ruler");
            if (idx < 0)
                idx = layers.FindIndex(layer => layer.Name.Contains("Mouse Text"));
            if (idx < 0)
                return;

            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "EvenMoreOverpoweredJourney: Blueprint Preview",
                DrawPreviewLayer,
                InterfaceScaleType.Game));
        }

        private static bool DrawPreviewLayer()
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || Main.gameMenu || Main.dedServ)
                return true;

            if (player.HeldItem?.type != ModContent.ItemType<BlueprintDeployer>())
                return true;

            FurnitureBlueprintPlayer fb = player.GetModPlayer<FurnitureBlueprintPlayer>();
            BlueprintLayout layout = BuiltinBlueprintTemplates.ResolveActiveLayout(fb);
            FurnitureScheme scheme = fb.ActiveScheme;
            if (layout == null || scheme == null)
                return true;

            FurnitureBlueprintPreview.DrawWorld(Main.spriteBatch, player, layout, scheme);
            return true;
        }
    }
}
