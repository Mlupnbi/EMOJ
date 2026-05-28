using System;
using System.Collections.Generic;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public enum TemplateLoadSource : byte
    {
        ModAssets = 0,
        LegacyDatamap = 1,
        CSharpFallback = 2
    }

    /// <summary>内置建筑方案：优先 ModAssets 三文件，运行时不再依赖 RoomPreset datamap PNG。</summary>
    public static class BuiltinBlueprintTemplates
    {
        private static readonly Dictionary<string, BlueprintTemplate> TemplatesById = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, TemplateLoadSource> LoadSourcesById = new(StringComparer.Ordinal);

        internal static readonly (string Id, string NameKey, Func<BlueprintLayout> Fallback)[] TemplateDefinitionIds =
        {
            ("npc_room_a", "Blueprint.Template.Preset.RoomA", BuiltinBlueprintTemplateBuilders.BuildNpcRoomA),
            ("npc_room_b", "Blueprint.Template.Preset.RoomB", BuiltinBlueprintTemplateBuilders.BuildNpcRoomB),
            ("npc_room_c", "Blueprint.Template.Preset.RoomC", BuiltinBlueprintTemplateBuilders.BuildNpcRoomC),
            ("simple_npc_room", "Blueprint.Template.SimpleNpcRoom", BuiltinBlueprintTemplateBuilders.BuildSimpleNpcRoom),
            ("ig_building_showcase", "Blueprint.Template.IG.BuildingShowcase", () => null),
            ("compact_shelter", "Blueprint.Template.CompactShelter", BuiltinBlueprintTemplateBuilders.BuildCompactShelter)
        };

        public static IReadOnlyList<BlueprintLayout> All { get; private set; }

        public static BlueprintLayout SimpleNpcRoom { get; private set; }
        public static BlueprintLayout CompactShelter { get; private set; }
        public static BlueprintLayout NpcRoomA { get; private set; }
        public static BlueprintLayout NpcRoomB { get; private set; }
        public static BlueprintLayout NpcRoomC { get; private set; }
        public static BlueprintLayout IgBuildingShowcase { get; private set; }

        internal static void Register()
        {
            var list = new List<BlueprintLayout>(TemplateDefinitionIds.Length);
            foreach ((string id, string nameKey, Func<BlueprintLayout> fallback) in TemplateDefinitionIds)
            {
                BlueprintLayout layout = LoadTemplate(id, nameKey, fallback);
                if (layout == null)
                    continue;

                list.Add(layout);
                switch (id)
                {
                    case "npc_room_a": NpcRoomA = layout; break;
                    case "npc_room_b": NpcRoomB = layout; break;
                    case "npc_room_c": NpcRoomC = layout; break;
                    case "simple_npc_room": SimpleNpcRoom = layout; break;
                    case "compact_shelter": CompactShelter = layout; break;
                    case "ig_building_showcase": IgBuildingShowcase = layout; break;
                }
            }

            All = list;
        }

        public static TemplateLoadSource GetLoadSource(string id)
        {
            id = MigrateLegacyTemplateId(id);
            return LoadSourcesById.TryGetValue(id, out TemplateLoadSource source)
                ? source
                : TemplateLoadSource.CSharpFallback;
        }

        public static bool TryGetTemplate(string id, out BlueprintTemplate template)
        {
            id = MigrateLegacyTemplateId(id);
            return TemplatesById.TryGetValue(id, out template);
        }

        /// <summary>一次性迁移：ImproveGame datamap PNG → 缓存 Template（非 Register 主路径）。</summary>
        public static bool TryImportLegacyDatamap(string datamapAssetPath, string id, string displayNameKey)
        {
            if (!LegacyDatamapImporter.TryImportFromModAsset(datamapAssetPath, id, displayNameKey, out BlueprintTemplate template))
                return false;

            TemplatesById[id] = template;
            LoadSourcesById[id] = TemplateLoadSource.LegacyDatamap;
            FurnitureBlueprintLog.Info($"legacy datamap registered id={id} asset={datamapAssetPath}");
            return true;
        }

        public static string MigrateLegacyTemplateId(string id) =>
            id switch
            {
                "ig_prison_1" => "npc_room_a",
                "ig_prison_2" => "npc_room_b",
                "ig_prison_3" => "npc_room_c",
                "building_showcase" => "ig_building_showcase",
                _ => id
            };

        public static BlueprintLayout GetById(string id)
        {
            id = MigrateLegacyTemplateId(id);
            if (All == null || string.IsNullOrEmpty(id))
                return null;

            foreach (BlueprintLayout layout in All)
            {
                if (layout != null && layout.Id == id)
                    return layout;
            }

            return null;
        }

        public static BlueprintLayout GetDefaultLayout()
        {
            if (SimpleNpcRoom != null)
                return SimpleNpcRoom;
            if (All != null && All.Count > 0)
                return All[0];
            return null;
        }

        public static BlueprintLayout ResolveActiveLayout(FurnitureBlueprintPlayer player)
        {
            BlueprintLayout layout = GetById(player?.ActiveTemplateId);
            return layout ?? GetDefaultLayout();
        }

        public static BlueprintTemplate ResolveActiveTemplate(FurnitureBlueprintPlayer player)
        {
            string id = MigrateLegacyTemplateId(player?.ActiveTemplateId);
            if (!string.IsNullOrEmpty(id) && TemplatesById.TryGetValue(id, out BlueprintTemplate cached))
                return cached;

            BlueprintLayout layout = ResolveActiveLayout(player);
            if (layout == null)
                return null;

            if (TemplatesById.TryGetValue(layout.Id, out cached))
                return cached;

            BlueprintTemplate converted = BlueprintTemplate.FromLegacyLayout(layout);
            TemplatesById[layout.Id] = converted;
            return converted;
        }

        public static void EnsureValidActiveTemplate(FurnitureBlueprintPlayer player)
        {
            if (player == null)
                return;
            if (GetById(player.ActiveTemplateId) != null)
                return;

            BlueprintLayout fallback = GetDefaultLayout();
            if (fallback != null)
                player.ApplyTemplateDefaults(fallback);
        }

        public static (int configuredKinds, int requiredKinds) CountMaterialCoverage(
            BlueprintLayout layout,
            FurnitureScheme scheme)
        {
            if (layout == null || scheme == null)
                return (0, 0);

            IReadOnlyDictionary<FurnitureSlotKind, int> need = TemplatesById.TryGetValue(layout.Id, out BlueprintTemplate template)
                ? template.CountRequiredSlots()
                : layout.CountKinds();

            int required = need.Count;
            int configured = 0;
            foreach (var pair in need)
            {
                if (scheme.GetSlot(pair.Key) > ItemID.None)
                    configured++;
            }

            return (configured, required);
        }

        private static BlueprintLayout LoadTemplate(
            string id,
            string nameKey,
            Func<BlueprintLayout> fallback)
        {
            BlueprintTemplate template = null;
            TemplateLoadSource source = TemplateLoadSource.CSharpFallback;

            global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney mod =
                Terraria.ModLoader.ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            if (mod != null
                && BlueprintTemplateIO.TryLoadFromModAssets(mod, id, out BlueprintTemplate assetTemplate))
            {
                FurnitureBlueprintLog.Info($"template {id} loaded from {BlueprintTemplateIO.BuiltinTemplatesModPath}");
                template = assetTemplate;
                source = TemplateLoadSource.ModAssets;
            }
            else if (TryLoadImproveGameDatamap(id, nameKey, out template))
            {
                FurnitureBlueprintLog.Info($"template {id} loaded from ImproveGame datamap");
                source = TemplateLoadSource.LegacyDatamap;
            }
            else if (fallback != null)
            {
                FurnitureBlueprintLog.Info($"template {id} using builtin C# layout (export with _tools/ExportBlueprintTemplates)");
                BlueprintLayout built = fallback();
                if (built == null)
                    return null;

                if (!LegacyDatamapImporter.TryImportFromLayout(built, out template))
                    return null;
                source = TemplateLoadSource.CSharpFallback;
            }
            else
            {
                FurnitureBlueprintLog.Warn(
                    $"template {id} missing ModAssets and datamap; run ExportBlueprintTemplates with ImproveGame buildingShowcase.png");
                return null;
            }

            TemplatesById[id] = template;
            LoadSourcesById[id] = source;
            return template.ToLegacyLayout();
        }

        private static bool TryLoadImproveGameDatamap(string id, string nameKey, out BlueprintTemplate template)
        {
            template = null;
            if (string.Equals(id, "compact_shelter", StringComparison.Ordinal))
                return false;

            string assetPath = $"Data/Blueprint/Sources/improvegame/{id}.png";
            return LegacyDatamapImporter.TryImportFromModAsset(assetPath, id, nameKey, out template);
        }
    }
}
