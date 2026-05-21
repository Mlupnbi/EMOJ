using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.Research.UI
{
    public class UIProductEntryCardView : UIElement
    {
        public int ItemType;
        private Item _item;
        public bool Highlighted;
        public ResearchProductTint Tint;

        public UIProductEntryCardView(int type)
        {
            ItemType = type;
            _item = new Item();
            _item.SetDefaults(type);
            Width.Set(44, 0);
            Height.Set(44, 0);
        }

        private static Color? GetRimTint(ResearchProductTint tint) => tint switch
        {
            ResearchProductTint.BlueResearched => new Color(50, 140, 220) * 0.65f,
            ResearchProductTint.GreenResearchable => new Color(60, 200, 60) * 0.6f,
            ResearchProductTint.RedUnresearched => new Color(220, 70, 70) * 0.65f,
            ResearchProductTint.PurpleCraftable => new Color(60, 200, 60) * 0.55f,
            ResearchProductTint.PurpleCannotCraft => new Color(220, 70, 70) * 0.55f,
            _ => null
        };

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Vector2 pos = GetDimensions().Position();
            float oldScale = Main.inventoryScale;
            Main.inventoryScale = 0.8f;

            Vector2 slotPos = pos + new Vector2(2, 2);
            int slotW = (int)(TextureAssets.InventoryBack.Value.Width * Main.inventoryScale);
            int slotH = (int)(TextureAssets.InventoryBack.Value.Height * Main.inventoryScale);

            Item[] dummy = new Item[11];
            dummy[10] = _item;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = oldScale;

            Color? rim = GetRimTint(Tint);
            if (rim.HasValue)
                BorderDrawUtil.DrawInventorySlotRimTint(spriteBatch, slotPos, slotW, slotH, rim.Value, 4);

            if (Highlighted)
            {
                Rectangle slotRect = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, slotW + 2, slotH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, slotRect, Color.Gold, 2);
            }

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.HoverItem = _item.Clone();
                Main.hoverItemName = _item.Name;
            }
        }
    }

    public class UIProductEntryListView : UIElement
    {
        public int ItemType;
        private Item _item;
        public bool Highlighted;
        public ResearchProductTint Tint;

        public UIProductEntryListView(int type)
        {
            ItemType = type;
            _item = new Item();
            _item.SetDefaults(type);
            Width.Set(0, 1f);
            Height.Set(44, 0);
        }

        private static Color? GetRimTint(ResearchProductTint tint) => tint switch
        {
            ResearchProductTint.BlueResearched => new Color(50, 140, 220) * 0.55f,
            ResearchProductTint.GreenResearchable => new Color(60, 200, 60) * 0.5f,
            ResearchProductTint.RedUnresearched => new Color(220, 70, 70) * 0.55f,
            ResearchProductTint.PurpleCraftable => new Color(60, 200, 60) * 0.48f,
            ResearchProductTint.PurpleCannotCraft => new Color(220, 70, 70) * 0.48f,
            _ => null
        };

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle dims = GetDimensions();
            Vector2 pos = dims.Position();
            float oldScale = Main.inventoryScale;
            Main.inventoryScale = 0.8f;

            Vector2 slotPos = pos + new Vector2(4, 2);
            int slotW = (int)(TextureAssets.InventoryBack.Value.Width * Main.inventoryScale);
            int slotH = (int)(TextureAssets.InventoryBack.Value.Height * Main.inventoryScale);

            Item[] dummy = new Item[11];
            dummy[10] = _item;
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = oldScale;

            Color? rim = GetRimTint(Tint);
            if (rim.HasValue)
                BorderDrawUtil.DrawInventorySlotRimTint(spriteBatch, slotPos, slotW, slotH, rim.Value, 4);

            Utils.DrawBorderString(spriteBatch, _item.Name, pos + new Vector2(50, 12), Color.White);

            if (Highlighted)
            {
                Rectangle slotRect = new Rectangle((int)slotPos.X - 1, (int)slotPos.Y - 1, slotW + 2, slotH + 2);
                BorderDrawUtil.DrawRectOutline(spriteBatch, slotRect, Color.Gold, 2);
            }

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.HoverItem = _item.Clone();
                Main.hoverItemName = _item.Name;
            }
        }
    }

    public class UIResearchRecipeRow : UIPanel
    {
        private readonly Recipe _recipe;
        private readonly float _containerWidth;
        private readonly bool _tintMaterialsByResearch;

        public UIResearchRecipeRow(Recipe r, float containerWidth, bool tintMaterialsByResearch)
        {
            _recipe = r;
            _containerWidth = containerWidth;
            _tintMaterialsByResearch = tintMaterialsByResearch;
            Width.Set(0, 1f);
            SetPadding(8);
            BackgroundColor = new Color(30, 30, 50);
            float textX = 55;
            float x = textX;
            float matY = 30;
            foreach (Item mat in _recipe.requiredItem)
            {
                if (mat == null || mat.IsAir) continue;
                if (x + 36 > _containerWidth - 20)
                {
                    x = textX;
                    matY += 36;
                }
                x += 36;
            }
            Height.Set(matY + 45, 0);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle dims = GetInnerDimensions();
            float left = dims.X;
            float top = dims.Y;
            float oldScale = Main.inventoryScale;
            Main.inventoryScale = 0.75f;

            Vector2 resPos = new Vector2(left, top);
            Item[] resDummy = new Item[11];
            resDummy[10] = _recipe.createItem;
            ItemSlot.Draw(spriteBatch, resDummy, ItemSlot.Context.InventoryItem, 10, resPos);

            float textX = left + 45;
            float textY = top;
            string envLabel = EOPJText.UI("EnvRequiredLabel");
            Vector2 labelSize = FontAssets.MouseText.Value.MeasureString(envLabel);
            Utils.DrawBorderString(spriteBatch, envLabel, new Vector2(textX, textY), Color.White, 0.8f);

            string envLine = RecipeEnvironmentHelper.BuildEnvironmentDisplayText(_recipe);
            if (!string.IsNullOrEmpty(envLine))
                Utils.DrawBorderString(spriteBatch, envLine, new Vector2(textX + labelSize.X * 0.8f, textY), Color.Yellow, 0.8f);

            var materials = _recipe.requiredItem.Where(i => i != null && !i.IsAir).ToList();
            float matY = top + 20;
            float x = textX;
            int matSlotW = (int)(TextureAssets.InventoryBack.Value.Width * Main.inventoryScale);
            int matSlotH = (int)(TextureAssets.InventoryBack.Value.Height * Main.inventoryScale);
            foreach (Item mat in materials)
            {
                if (x + 36 > dims.X + _containerWidth - 20)
                {
                    x = textX;
                    matY += 36;
                }

                Vector2 mpos = new Vector2(x, matY);
                Item[] matDummy = new Item[11];
                matDummy[10] = mat;
                ItemSlot.Draw(spriteBatch, matDummy, ItemSlot.Context.InventoryItem, 10, mpos);

                if (_tintMaterialsByResearch)
                {
                    bool ok = RecipeAnalyzer.IsFullyResearched(mat.type);
                    Color c = ok ? new Color(50, 140, 220) * 0.55f : new Color(220, 70, 70) * 0.55f;
                    BorderDrawUtil.DrawInventorySlotRimTint(spriteBatch, mpos, matSlotW, matSlotH, c, 4);
                }

                Rectangle matRect = new Rectangle((int)x, (int)matY, matSlotW, matSlotH);
                if (matRect.Contains(Main.MouseScreen.ToPoint()))
                {
                    Main.HoverItem = mat.Clone();
                    Main.hoverItemName = mat.Name;
                }
                x += 36;
            }
            Main.inventoryScale = oldScale;
        }
    }
}
