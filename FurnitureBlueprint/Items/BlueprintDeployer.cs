using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Placement;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;
using EvenMoreOverpoweredJourney.SuperAdmin;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Items
{
    /// <summary>按住左键在光标处放置当前激活的内置模板（自动模式：使用 ActiveScheme 材料）�?</summary>
    /// <summary>贴图�?<c>FurnitureBlueprint/Items/BlueprintDeployer.png</c>（tModLoader 默认路径）�?</summary>
    public class BlueprintDeployer : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.channel = false;
            Item.noMelee = true;
            Item.value = Item.sellPrice(0, 2);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.MultiplayerClient)
                return true;

            FurnitureBlueprintPlayer fb = player.GetModPlayer<FurnitureBlueprintPlayer>();
            BuiltinBlueprintTemplates.EnsureValidActiveTemplate(fb);
            BlueprintTemplate template = BuiltinBlueprintTemplates.ResolveActiveTemplate(fb);
            if (template == null)
                return false;

            bool consume = fb.ConsumeMaterialsOnPlace && !SuperAdminSession.DebugFillTheBlueprint;
            if (BlueprintTemplatePlacementRunner.IsBusy || fb.RecognitionBusy)
            {
                CombatText.NewText(player.getRect(), Color.Goldenrod, EOPJText.UI("Blueprint.PlaceBusy"));
                return false;
            }

            if (!BlueprintTemplatePlacer.TryPlace(player, template, fb.ActiveScheme, consume, fb.PlacementMode))
            {
                string failKey = BlueprintTemplatePlacer.LastRejectReason switch
                {
                    BlueprintTemplatePlacer.PlaceRejectReason.Busy => "Blueprint.PlaceBusy",
                    BlueprintTemplatePlacer.PlaceRejectReason.Framework => "Blueprint.PlaceFailedFramework",
                    BlueprintTemplatePlacer.PlaceRejectReason.Strict => "Blueprint.PlaceFailedStrict",
                    _ => "Blueprint.PlaceFailed"
                };
                CombatText.NewText(player.getRect(), Color.IndianRed, EOPJText.UI(failKey));
                return false;
            }

            if (template.Width * template.Height >= BlueprintTemplatePlacementRunner.AsyncCellThreshold)
                CombatText.NewText(player.getRect(), Color.LightGreen, EOPJText.UI("Blueprint.PlaceStarted"));

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 30)
                .AddIngredient(ItemID.Book, 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
