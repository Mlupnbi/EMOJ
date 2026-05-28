using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    /// <summary>
    /// ImproveGame 风格 datamap PNG → <see cref="BlueprintTemplate"/>。
    /// 读取仍走 <see cref="BlueprintDatamapLoader"/>；本类负责转换、校验与导出。
    /// </summary>
    public static class LegacyDatamapImporter
    {
        public static bool TryImportFromModAsset(
            string assetPath,
            string id,
            string displayNameKey,
            out BlueprintTemplate template)
        {
            template = null;
            if (!BlueprintDatamapLoader.TryLoadFromModAsset(assetPath, id, displayNameKey, out BlueprintLayout layout))
                return false;

            return TryImportFromLayout(layout, out template);
        }

        public static bool TryImportFromTexture(
            Texture2D texture,
            string id,
            string displayNameKey,
            out BlueprintTemplate template)
        {
            template = null;
            if (!BlueprintDatamapLoader.TryLoadFromTexture(texture, id, displayNameKey, out BlueprintLayout layout))
                return false;

            return TryImportFromLayout(layout, out template);
        }

        public static bool TryImportFromLayout(BlueprintLayout layout, out BlueprintTemplate template, bool log = true)
        {
            template = null;
            if (layout == null)
                return false;

            template = BlueprintTemplate.FromLegacyLayout(layout);
            if (!ValidateImportedTemplate(layout, template, out string error))
            {
                if (log)
                    FurnitureBlueprintLog.Warn($"datamap import rejected id={layout.Id}: {error}");
                template = null;
                return false;
            }

            if (log)
            {
                FurnitureBlueprintLog.Info(
                    $"datamap imported id={layout.Id} {layout.Width}x{layout.Height} slots={template.CountRequiredSlots().Count}");
            }

            return true;
        }

        public static bool ValidateImportedTemplate(
            BlueprintLayout layout,
            BlueprintTemplate template,
            out string error)
        {
            error = null;
            if (layout == null || template == null)
            {
                error = "null layout or template";
                return false;
            }

            if (template.Width != layout.Width || template.Height != layout.Height)
            {
                error = "dimension mismatch";
                return false;
            }

            if (!DictionaryEquals(layout.CountKinds(), template.CountRequiredSlots()))
            {
                error = "slot counts mismatch between legacy layout and template rules";
                return false;
            }

            try
            {
                BlueprintTemplate roundTrip = BlueprintTemplate.FromTag(template.ToTag());
                if (roundTrip.Id != template.Id
                    || roundTrip.Width != template.Width
                    || roundTrip.Height != template.Height)
                {
                    error = "tag round-trip mismatch";
                    return false;
                }
            }
            catch (Exception)
            {
                // 导出工具等无 tModLoader 宿主时跳过 TagIO 校验
            }

            return true;
        }

        private static bool DictionaryEquals(
            IReadOnlyDictionary<FurnitureSlotKind, int> a,
            IReadOnlyDictionary<FurnitureSlotKind, int> b)
        {
            if (a.Count != b.Count)
                return false;

            foreach (var pair in a)
            {
                if (!b.TryGetValue(pair.Key, out int n) || n != pair.Value)
                    return false;
            }

            return true;
        }
    }
}
