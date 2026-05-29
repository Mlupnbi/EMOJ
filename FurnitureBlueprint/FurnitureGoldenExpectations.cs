using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>用户金标准（Data/user_furniture_golden.json）：batch golden_match，不依赖 wiki。</summary>
    internal static class FurnitureGoldenExpectations
    {
        internal readonly struct GoldenSetEntry
        {
            public string Label { get; }
            public int Seed { get; }
            public int ExpectedMaterial { get; }
            public string StylePrefix { get; }
            public Dictionary<FurnitureSlotKind, string> Slots { get; }

            public GoldenSetEntry(
                string label,
                int seed,
                int expectedMaterial,
                string stylePrefix,
                Dictionary<FurnitureSlotKind, string> slots)
            {
                Label = label ?? string.Empty;
                Seed = seed;
                ExpectedMaterial = expectedMaterial;
                StylePrefix = stylePrefix ?? string.Empty;
                Slots = slots ?? new Dictionary<FurnitureSlotKind, string>();
            }
        }

        public readonly struct GoldenMatchReport
        {
            public int Match { get; }
            public int Checked { get; }
            public string Label { get; }

            public GoldenMatchReport(int match, int checkedCount, string label)
            {
                Match = match;
                Checked = checkedCount;
                Label = label ?? string.Empty;
            }
        }

        private static readonly List<GoldenSetEntry> Sets = new List<GoldenSetEntry>();
        private static readonly Dictionary<int, GoldenSetEntry> BySeed = new Dictionary<int, GoldenSetEntry>();

        public static int LoadedSetCount => Sets.Count;
        public static bool IsLoaded => Sets.Count > 0;

        public static void Reload(Mod mod)
        {
            Sets.Clear();
            BySeed.Clear();

            if (!TryLoadCacheJson(mod, out string json, out string sourceLabel))
            {
                FurnitureBlueprintLog.InfoFull("golden-cache missing (GetFileBytes + fallbacks failed)");
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
                    if (!TryParseSet(setEl, out GoldenSetEntry entry))
                        continue;

                    Sets.Add(entry);
                    if (entry.Seed > ItemID.None)
                        BySeed[entry.Seed] = entry;
                }

                FurnitureBlueprintLog.Info($"golden-cache loaded sets={Sets.Count} source={sourceLabel}");
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"golden-cache load failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public static GoldenMatchReport Evaluate(int seedType, int materialBlock, FurnitureScheme scheme)
        {
            if (scheme == null || !BySeed.TryGetValue(seedType, out GoldenSetEntry entry))
                return default;

            int match = 0;
            int checkedCount = 0;

            if (entry.ExpectedMaterial > ItemID.None)
            {
                checkedCount++;
                if (materialBlock == entry.ExpectedMaterial)
                    match++;
            }

            foreach (FurnitureSlotKind slot in FurnitureWikiSlots.RecognitionOrder)
            {
                int pick = scheme.GetSlot(slot);
                if (pick <= ItemID.None)
                    continue;

                checkedCount++;

                if (entry.Slots.TryGetValue(slot, out string expectedInternal)
                    && !string.IsNullOrWhiteSpace(expectedInternal))
                {
                    if (ItemMatchesExpected(pick, expectedInternal))
                        match++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(entry.StylePrefix)
                    && ItemMatchesPrefix(pick, seedType, materialBlock, entry.StylePrefix))
                {
                    match++;
                }
            }

            return new GoldenMatchReport(match, checkedCount, entry.Label);
        }

        public static bool TryGetBySeed(int seedType, out GoldenSetEntry entry) =>
            BySeed.TryGetValue(seedType, out entry);

        private static bool TryParseSet(JsonElement setEl, out GoldenSetEntry entry)
        {
            entry = default;
            string label = setEl.TryGetProperty("label", out JsonElement labelEl) ? labelEl.GetString() : null;
            int seed = setEl.TryGetProperty("seed", out JsonElement seedEl) ? seedEl.GetInt32() : ItemID.None;
            int material = setEl.TryGetProperty("material", out JsonElement matEl) ? matEl.GetInt32() : ItemID.None;
            string prefix = setEl.TryGetProperty("stylePrefix", out JsonElement preEl) ? preEl.GetString() : null;

            if (seed <= ItemID.None)
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

            entry = new GoldenSetEntry(label, seed, material, prefix, slots);
            return true;
        }

        private static bool TryLoadCacheJson(Mod mod, out string json, out string sourceLabel)
        {
            json = null;
            sourceLabel = null;

            if (mod != null)
            {
                byte[] bytes = mod.GetFileBytes("Data/user_furniture_golden.json");
                if (bytes != null && bytes.Length > 0)
                {
                    json = System.Text.Encoding.UTF8.GetString(bytes);
                    sourceLabel = "GetFileBytes(Data/user_furniture_golden.json)";
                    return true;
                }
            }

            string root = Path.GetDirectoryName(typeof(EvenMoreOverpoweredJourney).Assembly.Location);
            if (!string.IsNullOrEmpty(root))
            {
                string asmPath = Path.Combine(root, "Data", "user_furniture_golden.json");
                if (File.Exists(asmPath))
                {
                    json = File.ReadAllText(asmPath);
                    sourceLabel = asmPath;
                    return true;
                }
            }

            return false;
        }

        private static bool ItemMatchesExpected(int pick, string expectedInternal)
        {
            if (pick <= ItemID.None || string.IsNullOrWhiteSpace(expectedInternal))
                return false;

            string actual = FurnitureSchemeSlotFormatter.GetInternalEnglishName(pick);
            if (string.IsNullOrEmpty(actual))
                return false;

            return string.Equals(actual, expectedInternal, StringComparison.OrdinalIgnoreCase)
                || actual.Contains(expectedInternal, StringComparison.OrdinalIgnoreCase)
                || expectedInternal.Contains(actual, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ItemMatchesPrefix(int pick, int seedType, int materialBlock, string prefix)
        {
            ModItem mi = ItemLoader.GetItem(pick);
            if (mi == null || string.IsNullOrWhiteSpace(prefix))
                return false;

            if (mi.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || mi.Name.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                return FurnitureStylePrefixCatalog.ProductMatchesSeedStyle(pick, seedType, materialBlock);

            return false;
        }
    }
}
