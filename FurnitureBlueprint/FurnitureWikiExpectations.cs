using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 加载 Data/wiki_furniture_cache.json（由 Tools/ScrapeWikiFurnitureSets.js 生成），供 batch 打 wiki_match。
    /// </summary>
    internal static class FurnitureWikiExpectations
    {
        internal readonly struct WikiSetEntry
        {
            public string ModKey { get; }
            public string SetName { get; }
            public string StylePrefix { get; }
            public Dictionary<FurnitureSlotKind, string> Slots { get; }

            public WikiSetEntry(string modKey, string setName, string stylePrefix, Dictionary<FurnitureSlotKind, string> slots)
            {
                ModKey = modKey;
                SetName = setName;
                StylePrefix = stylePrefix;
                Slots = slots;
            }

            public string Label => string.IsNullOrEmpty(ModKey) ? SetName : ModKey + "/" + SetName;
        }

        public readonly struct WikiMatchReport
        {
            public int Match { get; }
            public int Checked { get; }
            public string SetLabel { get; }

            public WikiMatchReport(int match, int checkedCount, string setLabel)
            {
                Match = match;
                Checked = checkedCount;
                SetLabel = setLabel ?? string.Empty;
            }
        }

        private static readonly List<WikiSetEntry> Sets = new List<WikiSetEntry>();
        private static readonly Dictionary<string, List<WikiSetEntry>> ByPrefix =
            new Dictionary<string, List<WikiSetEntry>>(StringComparer.OrdinalIgnoreCase);

        public static int LoadedSetCount => Sets.Count;
        public static bool IsLoaded => Sets.Count > 0;

        public static void Reload(Mod mod)
        {
            Sets.Clear();
            ByPrefix.Clear();

            if (!TryLoadCacheJson(mod, out string json, out string sourceLabel))
            {
                FurnitureBlueprintLog.InfoFull("wiki-cache missing (GetFileBytes + fallbacks failed)");
                return;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("sets", out JsonElement setsEl)
                    || setsEl.ValueKind != JsonValueKind.Array)
                    return;

                foreach (JsonElement setEl in setsEl.EnumerateArray())
                {
                    if (!TryParseSet(setEl, out WikiSetEntry entry))
                        continue;

                    Sets.Add(entry);
                    if (!string.IsNullOrWhiteSpace(entry.StylePrefix))
                    {
                        if (!ByPrefix.TryGetValue(entry.StylePrefix, out List<WikiSetEntry> list))
                        {
                            list = new List<WikiSetEntry>();
                            ByPrefix[entry.StylePrefix] = list;
                        }

                        list.Add(entry);
                    }
                }

                FurnitureBlueprintLog.Info(
                    $"wiki-cache loaded sets={Sets.Count} source={sourceLabel}");
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"wiki-cache load failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        private static bool TryLoadCacheJson(Mod mod, out string json, out string sourceLabel)
        {
            json = null;
            sourceLabel = null;

            if (mod != null)
            {
                byte[] bytes = mod.GetFileBytes("Data/wiki_furniture_cache.json");
                if (bytes != null && bytes.Length > 0)
                {
                    json = System.Text.Encoding.UTF8.GetString(bytes);
                    sourceLabel = "GetFileBytes(Data/wiki_furniture_cache.json)";
                    return true;
                }
            }

            string root = Path.GetDirectoryName(typeof(EvenMoreOverpoweredJourney).Assembly.Location);
            if (!string.IsNullOrEmpty(root))
            {
                string asmPath = Path.Combine(root, "Data", "wiki_furniture_cache.json");
                if (File.Exists(asmPath))
                {
                    json = File.ReadAllText(asmPath);
                    sourceLabel = asmPath;
                    return true;
                }
            }

            return false;
        }

        public static WikiMatchReport Evaluate(int seedType, FurnitureScheme scheme)
        {
            if (scheme == null || !TryResolveSet(seedType, scheme, out WikiSetEntry entry))
                return default;

            int match = 0;
            int checkedCount = 0;
            foreach (FurnitureSlotKind slot in FurnitureWikiSlots.RecognitionOrder)
            {
                if (!entry.Slots.TryGetValue(slot, out string expected)
                    || string.IsNullOrWhiteSpace(expected))
                    continue;

                checkedCount++;
                int pick = scheme.GetSlot(slot);
                if (pick > ItemID.None && ItemMatchesExpected(pick, expected))
                    match++;
            }

            return new WikiMatchReport(match, checkedCount, entry.Label);
        }

        public static bool TryResolveSet(int seedType, FurnitureScheme scheme, out WikiSetEntry entry)
        {
            entry = default;
            if (Sets.Count == 0)
                return false;

            string styleKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            if (string.IsNullOrWhiteSpace(styleKey))
                styleKey = FurnitureSetRecognizer.ExtractDisplayLineageMoniker(seedType);

            if (!string.IsNullOrWhiteSpace(styleKey) && TryPickBestByPrefix(styleKey, out entry))
                return true;

            if (scheme != null && TryPickBestFromScheme(scheme, out entry))
                return true;

            return false;
        }

        private static bool TryParseSet(JsonElement setEl, out WikiSetEntry entry)
        {
            entry = default;
            string mod = setEl.TryGetProperty("mod", out JsonElement modEl) ? modEl.GetString() : null;
            string set = setEl.TryGetProperty("set", out JsonElement setEl2) ? setEl2.GetString() : null;
            string prefix = setEl.TryGetProperty("stylePrefix", out JsonElement preEl) ? preEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(set))
                return false;

            var slots = new Dictionary<FurnitureSlotKind, string>();
            if (setEl.TryGetProperty("slots", out JsonElement slotsEl)
                && slotsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty prop in slotsEl.EnumerateObject())
                {
                    if (!Enum.TryParse(prop.Name, ignoreCase: true, out FurnitureSlotKind kind))
                        continue;

                    string val = prop.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                        slots[kind] = val.Trim();
                }
            }

            if (slots.Count == 0)
                return false;

            entry = new WikiSetEntry(mod ?? "Unknown", set, prefix ?? string.Empty, slots);
            return true;
        }

        private static bool TryPickBestByPrefix(string styleKey, out WikiSetEntry entry)
        {
            entry = default;
            WikiSetEntry best = default;
            int bestScore = 0;

            foreach (KeyValuePair<string, List<WikiSetEntry>> kv in ByPrefix)
            {
                if (!StyleKeyMatchesPrefix(styleKey, kv.Key))
                    continue;

                foreach (WikiSetEntry candidate in kv.Value)
                {
                    int score = candidate.Slots.Count;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
            }

            if (bestScore <= 0)
                return false;

            entry = best;
            return true;
        }

        private static bool TryPickBestFromScheme(FurnitureScheme scheme, out WikiSetEntry entry)
        {
            entry = default;
            var hits = new Dictionary<WikiSetEntry, int>();

            foreach (FurnitureSlotKind slot in FurnitureWikiSlots.RecognitionOrder)
            {
                int pick = scheme.GetSlot(slot);
                if (pick <= ItemID.None)
                    continue;

                string internalName = GetInternalName(pick);
                if (string.IsNullOrEmpty(internalName))
                    continue;

                foreach (WikiSetEntry candidate in Sets)
                {
                    if (string.IsNullOrEmpty(candidate.StylePrefix))
                        continue;

                    if (internalName.StartsWith(candidate.StylePrefix, StringComparison.OrdinalIgnoreCase)
                        || internalName.Contains(candidate.StylePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!hits.ContainsKey(candidate))
                            hits[candidate] = 0;
                        hits[candidate]++;
                    }
                }
            }

            WikiSetEntry best = default;
            int bestHits = 0;
            foreach (KeyValuePair<WikiSetEntry, int> kv in hits)
            {
                if (kv.Value > bestHits)
                {
                    bestHits = kv.Value;
                    best = kv.Key;
                }
            }

            if (bestHits < 3)
                return false;

            entry = best;
            return true;
        }

        private static bool StyleKeyMatchesPrefix(string styleKey, string prefix)
        {
            if (string.IsNullOrWhiteSpace(styleKey) || string.IsNullOrWhiteSpace(prefix))
                return false;

            if (string.Equals(styleKey, prefix, StringComparison.OrdinalIgnoreCase))
                return true;

            if (styleKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;

            return FurnitureStyleSignature.StyleKeyFuzzyMatch(prefix, styleKey)
                || FurnitureMaterialKeyNormalizer.StyleKeysMatch(prefix, styleKey);
        }

        internal static bool ItemMatchesExpected(int pick, string expectedInternal)
        {
            if (pick <= ItemID.None || string.IsNullOrWhiteSpace(expectedInternal))
                return false;

            string actual = GetInternalName(pick);
            if (string.IsNullOrEmpty(actual))
                return false;

            if (string.Equals(actual, expectedInternal, StringComparison.OrdinalIgnoreCase))
                return true;

            if (actual.Contains(expectedInternal, StringComparison.OrdinalIgnoreCase)
                || expectedInternal.Contains(actual, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static string GetInternalName(int type) =>
            FurnitureSchemeSlotFormatter.GetInternalEnglishName(type);
    }
}
