using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.Shell.UI.Components
{
    /// <summary>EMOJ ?????????????????????????????????</summary>
    public class EmojItemSlot : UIElement
    {
        public Item item = new Item();
        public event Action<Item> OnItemChanged;
        public bool isRecipeNode = false;
        /// <summary>????????????????????/????? true??</summary>
        public bool ReturnPhysicalOnPlace { get; set; } = true;
        /// <summary>????????????????????? false???????</summary>
        public bool ReturnPhysicalOnClear { get; set; } = false;

        public EmojItemSlot() { Width.Set(52, 0); Height.Set(52, 0); }

        protected void NotifyItemChanged() => OnItemChanged?.Invoke(item);

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            if (evt.Target != this)
            {
                base.LeftMouseDown(evt);
                return;
            }

            if (Main.mouseItem.type > ItemID.None)
            {
                Item dragged = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();

                item = new Item();
                item.SetDefaults(dragged.type);
                item.stack = Math.Min(dragged.stack, item.maxStack);
                if (dragged.prefix != 0)
                    item.Prefix(dragged.prefix);

                SoundEngine.PlaySound(SoundID.Grab);
                OnItemChanged?.Invoke(item);

                if (ReturnPhysicalOnPlace)
                    ReturnPhysicalItemToPlayer(dragged);
            }
            else if (item.type > ItemID.None)
            {
                Item was = ReturnPhysicalOnClear ? item.Clone() : null;
                item = new Item();
                SoundEngine.PlaySound(SoundID.Grab);
                OnItemChanged?.Invoke(item);
                if (ReturnPhysicalOnClear)
                    ReturnPhysicalItemToPlayer(was);
            }
        }

        /// <summary>?????????????0?49???????????????</summary>
        private static void ReturnPhysicalItemToPlayer(Item physical)
        {
            if (physical == null || physical.IsAir)
                return;
            Player plr = Main.LocalPlayer;
            if (plr == null || !plr.active)
                return;

            Item give = physical.Clone();

            for (int i = 0; i < 50; i++)
            {
                ref Item slot = ref plr.inventory[i];
                if (slot.IsAir || slot.type != give.type)
                    continue;
                if (!ItemLoader.CanStack(slot, give))
                    continue;
                int space = slot.maxStack - slot.stack;
                if (space <= 0)
                    continue;
                int move = Math.Min(space, give.stack);
                slot.stack += move;
                give.stack -= move;
                if (give.stack <= 0)
                    return;
            }

            for (int i = 0; i < 50; i++)
            {
                if (!plr.inventory[i].IsAir)
                    continue;
                plr.inventory[i] = give.Clone();
                return;
            }

            plr.QuickSpawnItem(plr.GetSource_GiftOrReward(), give.type, give.stack);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            float oldScale = Main.inventoryScale;
            Main.inventoryScale = 1f;

            Item[] dummy = new Item[11];
            dummy[10] = item;

            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, dims.Position());

            if (isRecipeNode && item.type > ItemID.None && !RecipeAnalyzer.IsResearched(item.type))
            {
                Vector2 p = dims.Position();
                int sw = (int)(global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Width * Main.inventoryScale);
                int sh = (int)(global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack.Height * Main.inventoryScale);
                BorderDrawUtil.DrawInventorySlotRimTint(spriteBatch, p, sw, sh, Color.Red * 0.65f, 4);
            }

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
