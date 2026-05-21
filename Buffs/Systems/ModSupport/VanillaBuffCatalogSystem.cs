using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport
{
    /// <summary>���������ء�ȫ��ԭ�� Buff��֧��Ŀ¼�������棩��</summary>
    public sealed class VanillaBuffCatalogSystem : ModSystem
    {
        private static readonly Dictionary<int, VanillaBuffSupportMode> ModeById = new();

        public static int CatalogCount => ModeById.Count;

        public static bool TryGetMode(int buffId, out VanillaBuffSupportMode mode) =>
            ModeById.TryGetValue(buffId, out mode);

        public override void OnModLoad() => RebuildCatalog();

        public override void PostSetupContent() => RebuildCatalog();

        public static void RebuildCatalog()
        {
            ModeById.Clear();

            for (int buffId = 1; buffId < BuffID.Count; buffId++)
            {
                string name = BuffID.Search.GetName(buffId);
                if (string.IsNullOrEmpty(name))
                    continue;

                ModeById[buffId] = Classify(buffId, name);
            }

            EmojLog.Info(EmojLogChannel.Core,
                $"VanillaBuffCatalog: total={ModeById.Count} synthetic={CountMode(VanillaBuffSupportMode.SyntheticStat)} debuff={CountMode(VanillaBuffSupportMode.DebuffPhysical)}");
        }

        private static int CountMode(VanillaBuffSupportMode m)
        {
            int n = 0;
            foreach (var mode in ModeById.Values)
            {
                if (mode == m)
                    n++;
            }

            return n;
        }

        private static VanillaBuffSupportMode Classify(int buffId, string name)
        {
            if (buffId > 0 && buffId < Main.debuff.Length && Main.debuff[buffId])
                return VanillaBuffSupportMode.DebuffPhysical;

            if (VanillaBuffStatRegistry.IsSyntheticStatBuff(buffId))
                return VanillaBuffSupportMode.SyntheticStat;

            if (BuffVirtualEffectSystem.VanillaPhysicalOnlyBuffs.Contains(buffId))
                return VanillaBuffSupportMode.PhysicalMechanic;

            return VanillaBuffSupportMode.StandardPhysical;
        }

        /// <summary>��Ŀ¼д�� Data/BuffModSupport/vanilla_catalog.json����������汾�������</summary>
        public static void ExportCatalogToDisk(Mod mod)
        {
            string? root = Path.GetDirectoryName(typeof(EvenMoreOverpoweredJourney).Assembly.Location);
            if (string.IsNullOrEmpty(root))
                return;

            string path = Path.Combine(root, "Data", "BuffModSupport", "vanilla_catalog.json");
            var list = new List<object>();

            foreach (var kv in ModeById.OrderBy(k => k.Key))
            {
                int id = kv.Key;
                list.Add(new
                {
                    key = BuffStableKey.ToKey(id),
                    buffId = id,
                    name = BuffID.Search.GetName(id),
                    mode = kv.Value.ToString(),
                    isDebuff = id < Main.debuff.Length && Main.debuff[id]
                });
            }

            var doc = new
            {
                schemaVersion = 1,
                exportedAtUtc = DateTime.UtcNow.ToString("o"),
                count = list.Count,
                entries = list
            };

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
