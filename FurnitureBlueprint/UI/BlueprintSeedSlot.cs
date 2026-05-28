using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    public sealed class BlueprintSeedSlot : EmojItemSlot
    {
        /// <summary>仅点击选取，不交换鼠标物品（材料原料浮层用）。</summary>
        public bool PickOnly { get; set; }

        public Action<int> OnPickOnly;

        public BlueprintSeedSlot()
        {
            Width.Set(52f, 0);
            Height.Set(52f, 0);
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            if (PickOnly && item.type > ItemID.None)
            {
                OnPickOnly?.Invoke(item.type);
                return;
            }

            // 查询槽：左键仅清空虚拟格，不再发还物品（拖入时已 ReturnPhysicalOnPlace）
            if ((Main.mouseItem == null || Main.mouseItem.IsAir) && item.type > ItemID.None)
            {
                item = new Item();
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                NotifyItemChanged();
                return;
            }

            base.LeftMouseDown(evt);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            float oldScale = Main.inventoryScale;
            float invW = EojUiTextures.Common.InventoryBack.Width;
            Main.inventoryScale = dims.Width / invW;

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
