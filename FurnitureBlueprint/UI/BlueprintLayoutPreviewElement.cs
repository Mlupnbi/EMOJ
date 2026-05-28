using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

using EvenMoreOverpoweredJourney;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    public sealed class BlueprintLayoutPreviewElement : UIElement
    {
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();
            if (fb == null || fb.RecognitionBusy)
                return;

            BlueprintLayout layout = BuiltinBlueprintTemplates.ResolveActiveLayout(fb);
            if (layout == null || fb.ActiveScheme == null)
                return;

            Rectangle rect = GetDimensions().ToRectangle();

            if (BlueprintLayoutPreviewCache.HasContent)
            {
                BlueprintLayoutPreviewCache.Draw(spriteBatch, rect, Color.White);
                return;
            }

            string previewAsset = $"Assets/Blueprint/Presets/Preview_{layout.Id}";
            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            if (mod != null && mod.HasAsset(previewAsset))
            {
                Texture2D tex = mod.Assets.Request<Texture2D>(previewAsset).Value;
                spriteBatch.Draw(tex, rect, Color.White * 0.95f);
            }
        }
    }
}
