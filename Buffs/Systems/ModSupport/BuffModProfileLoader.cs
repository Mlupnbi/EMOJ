using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport
{
    /// <summary>
    /// ��ȡ mod_profiles.json��ɨ��ʱ�Ǽǵ�ģ��汾�뵱ǰ���ذ汾��һ��ʱ��������ģ�� overrides��
    /// </summary>
    public static class BuffModProfileLoader
    {
        private static readonly Dictionary<string, ModProfile> ProfilesByMod = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> VersionMismatchMods = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<string> VersionMismatchModNames => VersionMismatchMods;

        public static void Reload(Mod mod)
        {
            ProfilesByMod.Clear();
            VersionMismatchMods.Clear();

            string? root = Path.GetDirectoryName(typeof(EvenMoreOverpoweredJourney).Assembly.Location);
            if (string.IsNullOrEmpty(root))
                return;

            string path = Path.Combine(root, "Data", "BuffModSupport", "mod_profiles.json");
            if (!File.Exists(path))
                return;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (!doc.RootElement.TryGetProperty("mods", out JsonElement mods) || mods.ValueKind != JsonValueKind.Object)
                    return;

                foreach (JsonProperty prop in mods.EnumerateObject())
                {
                    var profile = new ModProfile { ModName = prop.Name };
                    JsonElement el = prop.Value;

                    if (el.TryGetProperty("strictVersionMatch", out JsonElement strict))
                        profile.StrictVersionMatch = strict.GetBoolean();

                    if (el.TryGetProperty("acceptedVersions", out JsonElement av) && av.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement v in av.EnumerateArray())
                        {
                            string? s = v.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                                profile.AcceptedVersions.Add(NormalizeVersion(s));
                        }
                    }

                    ProfilesByMod[prop.Name] = profile;
                    ValidateLoadedModVersion(prop.Name, profile);
                }

                EmojLog.Info(EmojLogChannel.Core,
                    $"BuffModProfile: profiles={ProfilesByMod.Count} versionMismatch={VersionMismatchMods.Count}");
            }
            catch (Exception ex)
            {
                EmojLog.Warn(EmojLogChannel.Core, $"BuffModProfile load failed: {ex.Message}");
            }
        }

        public static bool ShouldSkipKey(string stableKey)
        {
            if (string.IsNullOrWhiteSpace(stableKey))
                return true;

            int slash = stableKey.IndexOf('/');
            if (slash <= 0)
                return false;

            string modName = stableKey.Substring(0, slash);
            return VersionMismatchMods.Contains(modName);
        }

        private static void ValidateLoadedModVersion(string modName, ModProfile profile)
        {
            if (profile.AcceptedVersions.Count == 0)
                return;

            if (!ModLoader.TryGetMod(modName, out Mod mod))
                return;

            string loaded = NormalizeVersion(mod.Version.ToString());
            if (profile.AcceptedVersions.Contains(loaded))
                return;

            if (profile.StrictVersionMatch)
            {
                VersionMismatchMods.Add(modName);
                EmojLog.Warn(EmojLogChannel.Core,
                    $"BuffModProfile: skip {modName} overrides �� loaded v{loaded}, accepted [{string.Join(", ", profile.AcceptedVersions)}]");
            }
        }

        private static string NormalizeVersion(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return "";

            v = v.Trim();
            int plus = v.IndexOf('+');
            if (plus > 0)
                v = v.Substring(0, plus);
            return v;
        }

        private sealed class ModProfile
        {
            public string ModName = "";
            public bool StrictVersionMatch = true;
            public List<string> AcceptedVersions = new();
        }
    }
}
