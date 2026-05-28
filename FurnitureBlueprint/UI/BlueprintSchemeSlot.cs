using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>蓝图 22 格：默认可设为仅展示；详情窗内可拖入编辑。</summary>
    public sealed class BlueprintSchemeSlot : EmojItemSlot
    {
        public static float DrawScale => BlueprintSlotMetrics.SlotScale;

        /// <summary>为 true 时禁止拖入/取出（仅悬停 tooltip）。</summary>
        public bool DisplayOnly { get; set; } = true;

        public BlueprintSchemeSlot()
        {
            Width.Set(BlueprintSlotGridPanel.CellSize, 0);
            Height.Set(BlueprintSlotGridPanel.CellSize, 0);
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            if (DisplayOnly)
                return;

            base.LeftMouseDown(evt);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            float oldScale = Main.inventoryScale;
            Main.inventoryScale = DrawScale;

            Item[] dummy = new Item[11];
            dummy[10] = item;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, dims.Position());

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (item.type > ItemID.None)
                {
                    Main.HoverItem = item.Clone();
                    Main.hoverItemName = item.Name;
                }
            }

            Main.inventoryScale = oldScale;
        }
    }
}
