using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    internal static class BestiaryFilterTagMetrics
    {
        public const float SlotScale = 0.56f * 1.2f;
        public const float ActiveStripScale = 0.4f * 1.2f;
        public const float ActiveStripOuterH = 36f;

        public static void ComputeActiveStripCell(float innerWidth, int count, out float cellW, out float rowH)
        {
            Texture2D inv = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            float baseCell = inv.Width * ActiveStripScale + 3f;
            rowH = inv.Height * ActiveStripScale + 3f;
            if (count <= 0)
            {
                cellW = baseCell;
                return;
            }

            float per = (innerWidth - 8f) / count - 2f;
            cellW = System.Math.Max(20f, System.Math.Min(baseCell, per));
        }

        public static void ComputeGridLayout(float innerWidth, int count, out float cellW, out float rowH, out int cols, out int rows)
        {
            Texture2D inv = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            cellW = inv.Width * SlotScale + 4f;
            rowH = inv.Height * SlotScale + 4f;
            cols = System.Math.Max(1, (int)(innerWidth / cellW));
            rows = count == 0 ? 0 : (count + cols - 1) / cols;
        }
    }
}
