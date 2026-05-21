using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using EvenMoreOverpoweredJourney.Core.Logging;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport
{
    /// <summary>��ȡ Data/BuffModSupport/overrides.json����ɨ���������� StableKey ӳ�䵽����ʩ�ӽ׶Ρ�</summary>
    public static class BuffModSupportLoader
    {
        private static readonly Dictionary<int, BuffVirtualEffectPhase> OverridesByBuffId = new();

        public static int OverrideCount => OverridesByBuffId.Count;

        public static void Reload(Mod mod)
        {
            OverridesByBuffId.Clear();

            string? root = Path.GetDirectoryName(typeof(EvenMoreOverpoweredJourney).Assembly.Location);
            if (string.IsNullOrEmpty(root))
                return;

            string path = Path.Combine(root, "Data", "BuffModSupport", "overrides.json");
            if (!File.Exists(path))
                return;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (!doc.RootElement.TryGetProperty("entries", out JsonElement entries) ||
                    entries.ValueKind != JsonValueKind.Array)
                    return;

                foreach (JsonElement item in entries.EnumerateArray())
                {
                    if (!item.TryGetProperty("key", out JsonElement keyEl))
                        continue;

                    string key = keyEl.GetString();
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (BuffModProfileLoader.ShouldSkipKey(key))
                        continue;

                    if (!item.TryGetProperty("phase", out JsonElement phaseEl))
                        continue;

                    string phaseName = phaseEl.GetString();
                    if (!TryParsePhase(phaseName, out BuffVirtualEffectPhase phase))
                        continue;

                    if (!BuffStableKey.TryResolve(key, out int buffId))
                        continue;

                    OverridesByBuffId[buffId] = phase;
                }

                EmojLog.Info(EmojLogChannel.Core, $"BuffModSupport overrides loaded={OverridesByBuffId.Count} from {path}");
            }
            catch (Exception ex)
            {
                EmojLog.Warn(EmojLogChannel.Core, $"BuffModSupport overrides load failed: {ex.Message}");
            }
        }

        public static bool TryGetOverride(int buffId, out BuffVirtualEffectPhase phase) =>
            OverridesByBuffId.TryGetValue(buffId, out phase);

        private static bool TryParsePhase(string name, out BuffVirtualEffectPhase phase)
        {
            phase = BuffVirtualEffectPhase.Stat;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (name.Equals("CombatVisual", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Visual", StringComparison.OrdinalIgnoreCase))
            {
                phase = BuffVirtualEffectPhase.CombatVisual;
                return true;
            }

            if (name.Equals("Stat", StringComparison.OrdinalIgnoreCase))
            {
                phase = BuffVirtualEffectPhase.Stat;
                return true;
            }

            return false;
        }
    }
}
