using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ObjectData;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>用原版图格贴�? + IG framing 绘制预览（写�? RenderTarget 或调试）�?</summary>
    public static class FurnitureBlueprintTilePreviewDraw
    {
        private const int WallSpriteSize = 32;
        private const int TileSpriteSize = 16;

        private static readonly Dictionary<int, Color> WallTintByWallId = new();

        public static bool DrawLayout(
            SpriteBatch spriteBatch,
            Rectangle dest,
            BlueprintLayout layout,
            FurnitureScheme scheme,
            BlueprintTemplate template,
            int pixelsPerCell)
        {
            if (layout == null || dest.Width < 4 || dest.Height < 4)
                return false;

            if (pixelsPerCell > 0)
                return DrawLayoutFixed(spriteBatch, dest, layout, scheme, template, pixelsPerCell);

            if (!TryGetMetrics(dest, layout, out float ox, out float oy, out float cellPx))
                return false;

            return DrawLayoutCore(spriteBatch, layout, scheme, template, ox, oy, cellPx);
        }

        private static bool DrawLayoutFixed(
            SpriteBatch spriteBatch,
            Rectangle dest,
            BlueprintLayout layout,
            FurnitureScheme scheme,
            BlueprintTemplate template,
            int pixelsPerCell)
        {
            const float pad = 6f;
            float cellPx = pixelsPerCell;
            float ox = dest.X + pad;
            float oy = dest.Y + pad;
            return DrawLayoutCore(spriteBatch, layout, scheme, template, ox, oy, cellPx);
        }

        private static bool DrawLayoutCore(
            SpriteBatch spriteBatch,
            BlueprintLayout layout,
            FurnitureScheme scheme,
            BlueprintTemplate template,
            float ox,
            float oy,
            float cellPx)
        {
            BlueprintCell[,] grid = BlueprintDatamapPreviewGrid.BuildVisualGrid(layout);
            int width = layout.Width;
            int height = layout.Height;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            var bg = new Rectangle(
                (int)ox - 2,
                (int)oy - 2,
                Math.Max(1, (int)(cellPx * width) + 4),
                Math.Max(1, (int)(cellPx * height) + 4));
            spriteBatch.Draw(pixel, bg, new Color(10, 12, 18, 240));

            bool drewContent = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!grid[x, y].HasWall)
                        continue;

                    int idx = x + y * width;
                    var cellRect = CellRect(ox, oy, cellPx, x, y);
                    drewContent |= DrawWall(spriteBatch, cellRect, scheme, template, idx, grid, width, height, x, y);
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    BlueprintCell cell = grid[x, y];
                    if (cell.Kind == FurnitureSlotKind.None)
                        continue;

                    int idx = x + y * width;
                    var cellRect = CellRect(ox, oy, cellPx, x, y);
                    drewContent |= DrawKind(
                        spriteBatch,
                        cellRect,
                        scheme,
                        template,
                        idx,
                        cell,
                        grid,
                        width,
                        height,
                        x,
                        y);
                }
            }

            return drewContent;
        }

        private static Rectangle CellRect(float ox, float oy, float cellPx, int x, int y)
        {
            int cell = Math.Max(1, (int)cellPx);
            return new Rectangle(
                (int)(ox + x * cellPx),
                (int)(oy + y * cellPx),
                cell,
                cell);
        }

        private static int ResolvePreviewItem(
            FurnitureScheme scheme,
            BlueprintTemplate template,
            int cellIndex,
            BlueprintCell cell,
            out bool missing)
        {
            if (template != null)
                return FurnitureBlueprintMaterialResolver.ResolveTemplateCell(template, cellIndex, scheme, out missing);

            return FurnitureBlueprintMaterialResolver.ResolveItemType(scheme, cell.Kind, out missing);
        }

        private static bool DrawWall(
            SpriteBatch spriteBatch,
            Rectangle cellRect,
            FurnitureScheme scheme,
            BlueprintTemplate template,
            int cellIndex,
            BlueprintCell[,] grid,
            int width,
            int height,
            int x,
            int y)
        {
            int type = ResolvePreviewItem(scheme, template, cellIndex, grid[x, y], out bool missing);
            if (type <= ItemID.None)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, cellRect, new Color(70, 75, 95, 160));
                return true;
            }

            Item item = new Item();
            item.SetDefaults(type);
            if (item.createWall <= WallID.None || item.createWall >= TextureAssets.Wall.Length)
                return false;

            if (missing)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, cellRect, new Color(255, 80, 80, 160));
                return true;
            }

            Texture2D sheet = TextureAssets.Wall[item.createWall].Value;
            if (sheet == null)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, cellRect, GetWallTintForItem(type));
                return true;
            }

            BlueprintDatamapPreviewGrid.GetWallFrame(grid, width, height, x, y, out int frameX, out int frameY);
            var src = new Rectangle(frameX, frameY, WallSpriteSize, WallSpriteSize);
            ClampSource(sheet, ref src);
            spriteBatch.Draw(sheet, cellRect, src, Color.White * 0.92f);
            return true;
        }

        private static Color GetWallTintForItem(int itemType)
        {
            if (itemType <= ItemID.None)
                return new Color(70, 75, 95, 140);

            Item item = new Item();
            item.SetDefaults(itemType);
            if (item.createWall <= WallID.None)
                return new Color(70, 75, 95, 140);

            return GetWallTint(item.createWall);
        }

        private static Color GetWallTint(int wallId)
        {
            if (WallTintByWallId.TryGetValue(wallId, out Color cached))
                return cached;

            Color tint = new Color(70, 75, 95, 140);
            if (wallId > WallID.None && wallId < TextureAssets.Wall.Length)
            {
                Texture2D wallTex = TextureAssets.Wall[wallId].Value;
                if (wallTex != null && wallTex.Width > 0 && wallTex.Height > 0)
                {
                    Color[] sample = new Color[1];
                    wallTex.GetData(0, new Rectangle(0, 0, 1, 1), sample, 0, 1);
                    Color c = sample[0];
                    if (c.A >= 8)
                        tint = new Color(c.R, c.G, c.B, 140);
                }
            }

            WallTintByWallId[wallId] = tint;
            return tint;
        }

        private static bool DrawKind(
            SpriteBatch spriteBatch,
            Rectangle cellRect,
            FurnitureScheme scheme,
            BlueprintTemplate template,
            int cellIndex,
            BlueprintCell cell,
            BlueprintCell[,] grid,
            int width,
            int height,
            int x,
            int y)
        {
            int type = ResolvePreviewItem(scheme, template, cellIndex, cell, out bool missing);
            if (type <= ItemID.None)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, cellRect, GhostColor(cell.Kind, missing));
                return true;
            }

            Item item = new Item();
            item.SetDefaults(type);
            Color tint = missing ? new Color(255, 70, 70, 220) : Color.White;

            if (item.createTile >= TileID.Dirt
                && TryDrawTileSprite(spriteBatch, cellRect, item, cell.Kind, grid, width, height, x, y, tint))
                return true;

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, cellRect, GhostColor(cell.Kind, missing));
            return true;
        }

        private static Color GhostColor(FurnitureSlotKind kind, bool missing) =>
            kind switch
            {
                FurnitureSlotKind.Block => new Color(120, 120, 130, missing ? 200 : 140),
                FurnitureSlotKind.Platform => new Color(150, 130, 100, missing ? 200 : 140),
                FurnitureSlotKind.Wall => new Color(70, 75, 95, missing ? 200 : 140),
                _ => new Color(180, 120, 90, missing ? 220 : 160)
            };

        private static bool TryDrawTileSprite(
            SpriteBatch spriteBatch,
            Rectangle cellRect,
            Item item,
            FurnitureSlotKind slotKind,
            BlueprintCell[,] grid,
            int width,
            int height,
            int x,
            int y,
            Color tint)
        {
            int tileType = item.createTile;
            if (tileType < TileID.Dirt || tileType >= TextureAssets.Tile.Length)
                return false;

            Texture2D sheet = TextureAssets.Tile[tileType].Value;
            if (sheet == null)
                return false;

            int frameX = item.placeStyle * 18;
            int frameY = 0;
            TileObjectData data = FurnitureTileSafety.TryGetTileData(tileType, item.placeStyle);
            if (data != null)
            {
                if (data.Style > 0)
                    frameX = data.Style * 18;
                if (slotKind is not (FurnitureSlotKind.Block or FurnitureSlotKind.Platform))
                    BlueprintDatamapPreviewGrid.GetTileFrame(grid, width, height, x, y, out frameX, out frameY);
            }
            else if (slotKind is FurnitureSlotKind.Block or FurnitureSlotKind.Platform)
            {
                BlueprintDatamapPreviewGrid.GetTileFrame(grid, width, height, x, y, out frameX, out frameY);
            }

            var src = new Rectangle(frameX, frameY, TileSpriteSize, TileSpriteSize);
            ClampSource(sheet, ref src);
            spriteBatch.Draw(sheet, cellRect, src, tint);
            return true;
        }

        private static void ClampSource(Texture2D sheet, ref Rectangle src)
        {
            if (src.Right > sheet.Width)
                src.Width = Math.Max(1, sheet.Width - src.X);
            if (src.Bottom > sheet.Height)
                src.Height = Math.Max(1, sheet.Height - src.Y);
            if (src.X < 0)
                src.X = 0;
            if (src.Y < 0)
                src.Y = 0;
        }

        private static bool TryGetMetrics(Rectangle rect, BlueprintLayout layout, out float ox, out float oy, out float cell)
        {
            ox = oy = cell = 0f;
            const float pad = 6f;
            float innerW = rect.Width - pad * 2f;
            float innerH = rect.Height - pad * 2f;
            if (innerW < 1f || innerH < 1f)
                return false;

            cell = (float)Math.Floor(Math.Min(innerW / layout.Width, innerH / layout.Height));
            if (cell < 1f)
                return false;

            float drawW = cell * layout.Width;
            float drawH = cell * layout.Height;
            ox = rect.X + pad + (innerW - drawW) * 0.5f;
            oy = rect.Y + pad + (innerH - drawH) * 0.5f;
            return true;
        }
    }
}
