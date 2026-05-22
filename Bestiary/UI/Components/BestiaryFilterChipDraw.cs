using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.ItemHub.Data;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI.Components
{
    /// <summary>模组/群系筛选 chip 的槽位+图标绘制（主窗摘要行与二级窗共用）。</summary>
    internal static class BestiaryFilterChipDraw
    {
        public static void DrawModChip(
            SpriteBatch spriteBatch,
            Vector2 slotPos,
            float slotPixW,
            float slotPixH,
            string modKey,
            BestiaryVisibilityPolicy.ListAppearance appearance)
        {
            float alpha = appearance == BestiaryVisibilityPolicy.ListAppearance.BlackWithQuestion ? 0.25f : 1f;
            Mod mod = modKey != "Terraria" && ModLoader.TryGetMod(modKey, out Mod m) ? m : null;
            Texture2D iconTex = ItemHubModGridIcons.Resolve(mod, modKey);
            if (iconTex != null)
            {
                float fit = Math.Min(slotPixW, slotPixH) * 0.92f;
                float s = fit / Math.Max(iconTex.Width, iconTex.Height);
                Vector2 origin = new Vector2(iconTex.Width, iconTex.Height) * 0.5f;
                Vector2 center = slotPos + new Vector2(slotPixW * 0.5f, slotPixH * 0.5f);
                spriteBatch.Draw(iconTex, center, null, Color.White * alpha, 0f, origin, s, SpriteEffects.None, 0f);
            }
            else
            {
                string ab = HubModAbbrev.ForGrid(modKey);
                var f = FontAssets.MouseText.Value;
                Vector2 ms = f.MeasureString(ab);
                Vector2 tpos = slotPos + new Vector2((slotPixW - ms.X) * 0.5f, (slotPixH - ms.Y) * 0.5f);
                Color text = appearance == BestiaryVisibilityPolicy.ListAppearance.BlackWithQuestion
                    ? Color.Gray * 0.5f
                    : Color.White;
                Utils.DrawBorderStringFourWay(spriteBatch, f, ab, tpos.X, tpos.Y, text, Color.Black, Vector2.One);
            }
        }

        /// <summary>在物品槽内取居中正方形区域，避免槽位宽高不一导致图标被拉扁。</summary>
        public static Rectangle ComputeSquareIconRect(Vector2 slotPos, float slotPixW, float slotPixH, float inset = 0.88f)
        {
            float side = Math.Min(slotPixW, slotPixH) * inset;
            float ox = slotPos.X + (slotPixW - side) * 0.5f;
            float oy = slotPos.Y + (slotPixH - side) * 0.5f;
            int s = Math.Max(1, (int)side);
            return new Rectangle((int)ox, (int)oy, s, s);
        }

        public static void DrawBiomeChipAtSlot(
            SpriteBatch spriteBatch,
            Vector2 slotPos,
            float slotPixW,
            float slotPixH,
            BestiaryFilterDef def,
            BestiaryVisibilityPolicy.ListAppearance appearance,
            float inset = 0.88f)
        {
            DrawBiomeChip(spriteBatch, ComputeSquareIconRect(slotPos, slotPixW, slotPixH, inset), def, appearance);
        }

        public static void DrawBiomeChip(
            SpriteBatch spriteBatch,
            Rectangle iconRect,
            BestiaryFilterDef def,
            BestiaryVisibilityPolicy.ListAppearance appearance)
        {
            float alpha = appearance == BestiaryVisibilityPolicy.ListAppearance.BlackWithQuestion ? 0.25f : 1f;
            BestiaryFilterIconResolver.DrawDef(spriteBatch, iconRect, def, alpha);
        }

        public static void DrawInventorySlot(SpriteBatch spriteBatch, Vector2 slotPos, float slotScale)
        {
            float old = Main.inventoryScale;
            Main.inventoryScale = slotScale;
            Item[] dummy = new Item[11];
            dummy[10] = new Item();
            ItemSlot.Draw(spriteBatch, dummy, ItemSlot.Context.InventoryItem, 10, slotPos);
            Main.inventoryScale = old;
        }

        public static void ComputeSlotMetrics(float slotScale, out float slotPixW, out float slotPixH)
        {
            Texture2D invBack = global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.InventoryBack;
            slotPixW = invBack.Width * slotScale;
            slotPixH = invBack.Height * slotScale;
        }
    }
}
