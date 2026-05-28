using System;
using System.Collections.Generic;
using System.Text;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    /// <summary>Phase 3.8：模组加载后自检内置模板来源与数据完整性。</summary>
    public static class BlueprintTemplateAcceptance
    {
        public readonly struct TemplateReport
        {
            public TemplateReport(string id, TemplateLoadSource source, bool assetsComplete, bool dataValid)
            {
                Id = id;
                Source = source;
                AssetsComplete = assetsComplete;
                DataValid = dataValid;
            }

            public string Id { get; }
            public TemplateLoadSource Source { get; }
            public bool AssetsComplete { get; }
            public bool DataValid { get; }
            public bool ReadyForRuntime => AssetsComplete && DataValid;
        }

        public static IReadOnlyList<TemplateReport> RunBuiltinChecks(Mod mod)
        {
            var reports = new List<TemplateReport>();
            foreach ((string id, _, _) in BuiltinBlueprintTemplates.TemplateDefinitionIds)
            {
                TemplateLoadSource source = BuiltinBlueprintTemplates.GetLoadSource(id);
                bool assetsComplete = mod != null && HasCompleteModFiles(mod, id);
                bool dataValid = BuiltinBlueprintTemplates.TryGetTemplate(id, out BlueprintTemplate template)
                    && template != null
                    && LegacyDatamapImporter.ValidateImportedTemplate(
                        template.ToLegacyLayout(),
                        template,
                        out _);

                reports.Add(new TemplateReport(id, source, assetsComplete, dataValid));
            }

            LogSummary(reports);
            return reports;
        }

        public static bool AllTemplatesUseModAssets(IReadOnlyList<TemplateReport> reports)
        {
            if (reports == null || reports.Count == 0)
                return false;

            foreach (TemplateReport report in reports)
            {
                if (!report.ReadyForRuntime || report.Source != TemplateLoadSource.ModAssets)
                    return false;
            }

            return true;
        }

        private static bool HasCompleteModFiles(Terraria.ModLoader.Mod mod, string templateId)
        {
            if (mod == null || string.IsNullOrWhiteSpace(templateId))
                return false;

            string basePath = $"{BlueprintTemplateIO.BuiltinTemplatesModPath}/{templateId}";
            try
            {
                mod.GetFileBytes($"{basePath}/{BlueprintTemplateIO.MetaFileName}");
                mod.GetFileBytes($"{basePath}/{BlueprintTemplateIO.StructureFileName}");
                mod.GetFileBytes($"{basePath}/{BlueprintTemplateIO.ReplaceFileName}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void LogSummary(IReadOnlyList<TemplateReport> reports)
        {
            var sb = new StringBuilder();
            sb.Append("blueprint acceptance: ");
            int ok = 0;
            foreach (TemplateReport report in reports)
            {
                if (report.ReadyForRuntime && report.Source == TemplateLoadSource.ModAssets)
                    ok++;
                sb.Append('[').Append(report.Id).Append('=').Append(report.Source).Append(']');
            }

            sb.Append($" ready={ok}/{reports.Count}");
            sb.Append(" placementOrder=wall>tile>multi>single");
            FurnitureBlueprintLog.Info(sb.ToString());
        }
    }
}
