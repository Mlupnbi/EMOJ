using System;
using System.IO;
using EvenMoreOverpoweredJourney.FurnitureBlueprint;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    /// <summary>将 C# 回退布局导出为 Assets/Blueprint/Templates/&lt;id&gt;/ 三文件包（开发用）。</summary>
    public static class BlueprintTemplateAssetExporter
    {
        private static readonly string[] BuiltinTemplateIds =
        {
            "npc_room_a",
            "npc_room_b",
            "npc_room_c",
            "simple_npc_room",
            "compact_shelter"
        };

        public static int ExportBuiltinFallbacks(string assetsTemplatesRoot)
        {
            if (string.IsNullOrWhiteSpace(assetsTemplatesRoot))
                throw new ArgumentException("Assets templates root required.", nameof(assetsTemplatesRoot));

            Directory.CreateDirectory(assetsTemplatesRoot);
            int exported = 0;
            foreach (string id in BuiltinTemplateIds)
            {
                BlueprintLayout layout = BuildFallbackLayout(id);
                if (layout == null)
                    continue;

                BlueprintTemplate template = BlueprintTemplate.FromLegacyLayout(layout);
                string dir = Path.Combine(assetsTemplatesRoot, id);
                BlueprintTemplateIO.SaveDirectory(template, dir);
                exported++;
            }

            return exported;
        }

        internal static BlueprintLayout BuildFallbackLayout(string id) =>
            BuiltinBlueprintTemplateBuilders.BuildFallbackById(id);

        public static bool ExportSingleBuiltinFallback(string id, string assetsTemplatesRoot)
        {
            BlueprintLayout layout = BuildFallbackLayout(id);
            if (layout == null)
                return false;

            BlueprintTemplate template = BlueprintTemplate.FromLegacyLayout(layout);
            string dir = Path.Combine(assetsTemplatesRoot, id);
            BlueprintTemplateIO.SaveDirectory(template, dir);
            return true;
        }

        /// <summary>ImproveGame datamap PNG（像素已解码）→ Templates 目录。</summary>
        public static bool ExportFromDatamapColors(
            Microsoft.Xna.Framework.Color[] pixels,
            int width,
            int height,
            string id,
            string displayNameKey,
            string assetsTemplatesRoot)
        {
            if (!BlueprintDatamapLoader.TryLoadFromColors(pixels, width, height, id, displayNameKey, out BlueprintLayout layout))
                return false;
            if (!LegacyDatamapImporter.TryImportFromLayout(layout, out BlueprintTemplate template))
                return false;

            string dir = Path.Combine(assetsTemplatesRoot, id);
            Directory.CreateDirectory(dir);
            BlueprintTemplateIO.SaveDirectory(template, dir);
            return true;
        }

        /// <summary>导出工具专用：避免引用 Microsoft.Xna.Framework / FNA。</summary>
        public static bool ExportFromDatamapArgb(
            int[] argbPixels,
            int width,
            int height,
            string id,
            string displayNameKey,
            string assetsTemplatesRoot)
        {
            if (!BlueprintDatamapLoader.TryLoadFromArgb(argbPixels, width, height, id, displayNameKey, out BlueprintLayout layout, log: false))
                return false;

            BlueprintTemplate template = BlueprintTemplate.FromLegacyLayout(layout);
            string dir = Path.Combine(assetsTemplatesRoot, id);
            Directory.CreateDirectory(dir);
            BlueprintTemplateIO.SaveDirectory(template, dir);
            return true;
        }
    }
}
