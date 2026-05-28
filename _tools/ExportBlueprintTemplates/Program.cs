using System;
using System.Drawing;
using System.IO;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;

namespace EvenMoreOverpoweredJourney.Tools.ExportBlueprintTemplates;

/// <summary>开发工具：从 ImproveGame datamap PNG 或 C# 回退导出 Templates 三文件包。</summary>
internal static class Program
{
    private static readonly (string Id, string SourceFile, string NameKey)[] ImproveGameSources =
    {
        ("npc_room_a", "npc_room_a.png", "Blueprint.Template.Preset.RoomA"),
        ("npc_room_b", "npc_room_b.png", "Blueprint.Template.Preset.RoomB"),
        ("npc_room_c", "npc_room_c.png", "Blueprint.Template.Preset.RoomC"),
        ("simple_npc_room", "simple_npc_room.png", "Blueprint.Template.SimpleNpcRoom"),
        ("ig_building_showcase", "buildingShowcase.png", "Blueprint.Template.IG.BuildingShowcase"),
    };

    public static void Main(string[] args)
    {
        string modRoot = args.Length > 0
            ? args[0]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        string templatesRoot = Path.Combine(modRoot, "Data", "Blueprint", "Templates");
        string igSourcesRoot = Path.Combine(modRoot, "Data", "Blueprint", "Sources", "improvegame");
        Directory.CreateDirectory(templatesRoot);

        int exported = 0;
        foreach ((string id, string sourceFile, string nameKey) in ImproveGameSources)
        {
            string pngPath = Path.Combine(igSourcesRoot, sourceFile);
            if (TryExportFromDatamapFile(pngPath, id, nameKey, templatesRoot))
            {
                Console.WriteLine($"OK improvegame -> {id} ({sourceFile})");
                exported++;
            }
            else
            {
                Console.WriteLine($"SKIP improvegame {id}: {sourceFile} invalid or missing");
            }
        }

        if (BlueprintTemplateAssetExporter.ExportSingleBuiltinFallback("compact_shelter", templatesRoot))
        {
            Console.WriteLine("OK csharp fallback -> compact_shelter");
            exported++;
        }

        Console.WriteLine($"Exported {exported} blueprint templates to {templatesRoot}");
    }

    private static bool TryExportFromDatamapFile(
        string pngPath,
        string id,
        string displayNameKey,
        string templatesRoot)
    {
        if (!File.Exists(pngPath))
            return false;

        using Bitmap bitmap = new Bitmap(pngPath);
        int width = bitmap.Width;
        int height = bitmap.Height;
        var argb = new int[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c = bitmap.GetPixel(x, y);
                argb[x + y * width] = c.ToArgb();
            }
        }

        return BlueprintTemplateAssetExporter.ExportFromDatamapArgb(
            argb, width, height, id, displayNameKey, templatesRoot);
    }
}
