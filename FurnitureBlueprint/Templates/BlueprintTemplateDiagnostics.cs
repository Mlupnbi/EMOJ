using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria.ModLoader.IO;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    internal static class BlueprintTemplateDiagnostics
    {
        public static void LogLegacyConversionSummary(BlueprintLayout layout)
        {
            if (layout == null)
                return;

            BlueprintTemplate template = BlueprintTemplate.FromLegacyLayout(layout);
            TagCompound roundTrip = template.ToTag();
            BlueprintTemplate restored = BlueprintTemplate.FromTag(roundTrip);

            IReadOnlyDictionary<FurnitureSlotKind, int> legacyNeed = layout.CountKinds();
            IReadOnlyDictionary<FurnitureSlotKind, int> templateNeed = template.CountRequiredSlots();
            bool countsMatch = DictionaryEquals(legacyNeed, templateNeed);

            var sb = new StringBuilder();
            sb.Append($"template={layout.Id} {layout.Width}x{layout.Height}");
            sb.Append($" slotKinds={templateNeed.Count} tagRoundTripOk={restored.Id == template.Id}");
            sb.Append($" countsMatch={countsMatch}");
            FurnitureBlueprintLog.Info(sb.ToString());
        }

        public static void LogFileRoundTrip(BlueprintTemplate template)
        {
            if (template == null)
                return;

            string tempDir = Path.Combine(Path.GetTempPath(), "emoj-bp-" + Guid.NewGuid().ToString("N"));
            string tempPackage = Path.Combine(Path.GetTempPath(), "emoj-bp-" + Guid.NewGuid().ToString("N") + BlueprintTemplateIO.Extension);
            try
            {
                BlueprintTemplateIO.SaveDirectory(template, tempDir);
                BlueprintTemplate fromDir = BlueprintTemplateIO.LoadDirectory(tempDir);

                BlueprintTemplateIO.SavePackage(template, tempPackage);
                BlueprintTemplate fromPackage = BlueprintTemplateIO.LoadPackage(tempPackage);

                bool dirOk = BlueprintTemplateIO.TemplatesEqual(template, fromDir);
                bool pkgOk = BlueprintTemplateIO.TemplatesEqual(template, fromPackage);
                FurnitureBlueprintLog.Info(
                    $"template file round-trip id={template.Id} dirOk={dirOk} packageOk={pkgOk}");
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"template file round-trip failed id={template.Id}: {ex.Message}");
            }
            finally
            {
                TryDeleteDirectory(tempDir);
                TryDeleteFile(tempPackage);
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // best-effort temp cleanup
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // best-effort temp cleanup
            }
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
