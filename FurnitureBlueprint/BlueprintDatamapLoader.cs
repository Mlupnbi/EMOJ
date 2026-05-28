using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>从 datamap PNG 解码为 legacy 布局；转 Template 请用 <see cref="Templates.LegacyDatamapImporter"/>。</summary>
    public static class BlueprintDatamapLoader
    {
        private const int MinWidth = 6;
        private const int MinHeight = 6;
        private const int MaxDatamapExtent = 48;

        public static bool TryLoadFromModAsset(string assetPath, string id, string displayNameKey, out BlueprintLayout layout)
        {
            layout = null;
            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            if (mod == null || !mod.HasAsset(assetPath))
                return false;

            try
            {
                Texture2D tex = mod.Assets.Request<Texture2D>(assetPath).Value;
                return TryLoadFromTexture(tex, id, displayNameKey, out layout);
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"datamap load failed {assetPath}: {ex.Message}");
                return false;
            }
        }

        public static bool TryLoadFromTexture(Texture2D tex, string id, string displayNameKey, out BlueprintLayout layout)
        {
            layout = null;
            if (tex == null)
                return false;

            Color[] pixels = new Color[tex.Width * tex.Height];
            tex.GetData(pixels);
            return TryLoadFromColors(pixels, tex.Width, tex.Height, id, displayNameKey, out layout);
        }

        public static bool TryLoadFromArgb(
            int[] argbPixels,
            int width,
            int height,
            string id,
            string displayNameKey,
            out BlueprintLayout layout,
            bool log = true)
        {
            layout = null;
            if (argbPixels == null || argbPixels.Length != width * height)
                return false;

            if (width < MinWidth || height < MinHeight)
            {
                if (log)
                    FurnitureBlueprintLog.InfoFull($"datamap rejected {id}: too small {width}x{height}");
                return false;
            }

            if (width > MaxDatamapExtent || height > MaxDatamapExtent)
            {
                if (log)
                    FurnitureBlueprintLog.InfoFull($"datamap rejected {id}: likely preview image {width}x{height}");
                return false;
            }

            if (!LooksLikeDatamapArgb(argbPixels))
            {
                if (log)
                    FurnitureBlueprintLog.InfoFull($"datamap rejected {id}: pixels not datamap palette");
                return false;
            }

            var cells = new BlueprintCell[argbPixels.Length];
            for (int i = 0; i < argbPixels.Length; i++)
                cells[i] = BlueprintCell.FromArgb(argbPixels[i]);

            layout = new BlueprintLayout(id, displayNameKey, width, height, cells);
            if (log)
                FurnitureBlueprintLog.Info($"datamap loaded {id} {width}x{height}");
            return true;
        }

        public static bool TryLoadFromColors(
            Color[] pixels,
            int width,
            int height,
            string id,
            string displayNameKey,
            out BlueprintLayout layout)
        {
            layout = null;
            if (pixels == null || pixels.Length != width * height)
                return false;

            if (width < MinWidth || height < MinHeight)
            {
                FurnitureBlueprintLog.InfoFull($"datamap rejected {id}: too small {width}x{height}");
                return false;
            }

            if (width > MaxDatamapExtent || height > MaxDatamapExtent)
            {
                FurnitureBlueprintLog.InfoFull($"datamap rejected {id}: likely preview image {width}x{height}");
                return false;
            }

            if (!LooksLikeDatamapPixels(pixels))
            {
                FurnitureBlueprintLog.InfoFull($"datamap rejected {id}: pixels not datamap palette");
                return false;
            }

            var cells = new BlueprintCell[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                cells[i] = BlueprintCell.FromColor(pixels[i]);

            layout = new BlueprintLayout(id, displayNameKey, width, height, cells);
            FurnitureBlueprintLog.Info($"datamap loaded {id} {width}x{height}");
            return true;
        }

        /// <summary>datamap 像素应落在调色板索引上（兼容旧 24 色 ImproveGame datamap）。</summary>
        private static bool LooksLikeDatamapPixels(Color[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                return false;

            int hits = 0;
            foreach (Color c in pixels)
            {
                int index = (c.R + 1) / 64 * 5 + (c.G + 1) / 64;
                if (index is >= 0 and <= 23)
                    hits++;
            }

            return hits >= Math.Max(4, pixels.Length / 8);
        }

        private static bool LooksLikeDatamapArgb(int[] argbPixels)
        {
            if (argbPixels == null || argbPixels.Length == 0)
                return false;

            int hits = 0;
            foreach (int argb in argbPixels)
            {
                int r = (argb >> 16) & 0xFF;
                int g = (argb >> 8) & 0xFF;
                int index = (r + 1) / 64 * 5 + (g + 1) / 64;
                if (index is >= 0 and <= 23)
                    hits++;
            }

            return hits >= Math.Max(4, argbPixels.Length / 8);
        }
    }
}
