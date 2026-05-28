using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 套组血统：种子全名前缀（生命红木/干木/无）与已确认槽位共性词，用于压过同材料泛用家具。
    /// </summary>
    public static class FurnitureSetLineageScoring
    {
        public const int LineageStrong = 4_200;
        public const int CommonWordStrong = 2_800;
        public const int MaterialOnlyPartial = 2_800;

        private static readonly string[] SlotPartTokens =
        {
            "椅", "桌", "床", "门", "墙", "箱", "台", "浴缸", "书架", "烛", "灯", "钢琴", "水槽", "沙发", "马桶", "工作台", "平台", "栅栏", "藤架",
            "chair", "table", "bed", "door", "wall", "chest", "lamp", "bookcase", "bathtub", "sink", "sofa", "toilet", "workbench", "platform", "fence"
        };

        public static string ExtractSeedLineageMoniker(int seedType) =>
            FurnitureSetRecognizer.ExtractDisplayLineageMoniker(seedType);

        public static int ScoreSeedLineage(int productType, int seedType, int materialBlock)
        {
            if (productType <= ItemID.None || seedType <= ItemID.None)
                return 0;

            string moniker = ExtractSeedLineageMoniker(seedType);
            if (moniker.Length < 2)
                return 0;

            Item prod = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(prod, productType))
                return 0;
            string prodLower = (prod.Name ?? "").ToLowerInvariant();
            string monikerLower = moniker.ToLowerInvariant();

            if (prodLower.Contains(monikerLower))
                return LineageStrong;

            string seedKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(productType);
            if (FurnitureMaterialKeyNormalizer.StyleKeysMatch(seedKey, productKey))
                return LineageStrong;

            Item mat = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(mat, materialBlock > ItemID.None ? materialBlock : seedType))
                return 0;
            string matToken = FurnitureNameSignals.NormalizeMaterialDisplayName(mat.Name);
            if (string.IsNullOrEmpty(matToken) || matToken.Length < 2)
                return 0;

            if (monikerLower.Length <= matToken.Length)
                return 0;

            if (!monikerLower.StartsWith(matToken, StringComparison.OrdinalIgnoreCase)
                && !matToken.StartsWith(monikerLower, StringComparison.OrdinalIgnoreCase))
                return 0;

            string extra = monikerLower.Substring(matToken.Length).Trim();
            if (extra.Length < 1)
                return 0;

            if (prodLower.Contains(matToken) && !prodLower.Contains(extra))
                return -MaterialOnlyPartial;

            return 0;
        }

        public static IReadOnlyList<string> BuildCommonWords(
            int seedType,
            Dictionary<FurnitureSlotKind, List<int>> perSlot,
            int materialBlock,
            FurnitureScheme scheme = null,
            bool relaxedOccurrence = false)
        {
            var result = new List<string>();
            string seedMoniker = ExtractSeedLineageMoniker(seedType);
            if (seedMoniker.Length >= 2)
                result.Add(seedMoniker);

            var names = new List<string>();
            if (seedMoniker.Length >= 2)
                names.Add(seedMoniker);

            AppendNamesFromScheme(scheme, materialBlock, names);
            AppendNamesFromPerSlot(perSlot, materialBlock, names);

            if (FurnitureGenericWoodLineageRules.ShouldBoostPlaceholderCommonWords(seedType, materialBlock))
            {
                AppendGenericWoodLineageTokens(seedType, materialBlock, names, result);
                relaxedOccurrence = true;
            }

            int minOccurrences = relaxedOccurrence
                ? 1
                : names.Count >= 4 ? 2 : Math.Max(2, names.Count);

            foreach (string token in ExtractSharedTokens(names, minTokenLength: 2, minOccurrences))
            {
                if (!result.Exists(t => t.Equals(token, StringComparison.OrdinalIgnoreCase)))
                    result.Add(token);
            }

            return result;
        }

        private static void AppendNamesFromScheme(FurnitureScheme scheme, int materialBlock, List<string> names)
        {
            if (scheme == null)
                return;

            foreach (FurnitureSlotKind slot in FurnitureWikiSlots.RecognitionOrder)
            {
                int type = scheme.GetSlot(slot);
                if (type <= ItemID.None || type == materialBlock)
                    continue;

                Item probe = new Item();
                if (!FurnitureItemDefaults.TrySetDefaults(probe, type))
                    continue;

                string n = probe.Name ?? "";
                if (!string.IsNullOrWhiteSpace(n))
                    names.Add(n);
            }
        }

        private static void AppendNamesFromPerSlot(
            Dictionary<FurnitureSlotKind, List<int>> perSlot,
            int materialBlock,
            List<string> names)
        {
            if (perSlot == null)
                return;

            foreach (FurnitureSlotKind slot in FurnitureWikiSlots.RecognitionOrder)
            {
                if (slot is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    continue;
                if (!perSlot.TryGetValue(slot, out List<int> list) || list == null || list.Count == 0)
                    continue;

                foreach (int type in list)
                {
                    if (type <= ItemID.None || type == materialBlock)
                        continue;
                    Item probe = new Item();
                    if (!FurnitureItemDefaults.TrySetDefaults(probe, type))
                        continue;
                    string n = probe.Name ?? "";
                    if (!string.IsNullOrWhiteSpace(n))
                        names.Add(n);
                }
            }
        }

        private static void AppendGenericWoodLineageTokens(
            int seedType,
            int materialBlock,
            List<string> names,
            List<string> result)
        {
            string seedMoniker = ExtractSeedLineageMoniker(seedType);
            if (seedMoniker.Length >= 2 && !result.Exists(t => t.Equals(seedMoniker, StringComparison.OrdinalIgnoreCase)))
                result.Add(seedMoniker);

            Item mat = new Item();
            if (FurnitureItemDefaults.TrySetDefaults(mat, materialBlock))
            {
                string matToken = FurnitureNameSignals.NormalizeMaterialDisplayName(mat.Name);
                if (matToken.Length >= 2 && !result.Exists(t => t.Equals(matToken, StringComparison.OrdinalIgnoreCase)))
                    result.Add(matToken);
            }

            if (seedMoniker.Length >= 3)
            {
                string matToken = mat.IsAir ? "" : FurnitureNameSignals.NormalizeMaterialDisplayName(mat.Name);
                if (!string.IsNullOrEmpty(matToken)
                    && seedMoniker.StartsWith(matToken, StringComparison.OrdinalIgnoreCase)
                    && seedMoniker.Length > matToken.Length)
                {
                    string suffix = seedMoniker.Substring(matToken.Length).Trim();
                    if (suffix.Length >= 1 && !result.Exists(t => t.Equals(suffix, StringComparison.OrdinalIgnoreCase)))
                        result.Add(suffix);
                }
            }

            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                foreach (string token in TokenizeName(name))
                {
                    if (token.Length < 2 || IsSlotPartToken(token))
                        continue;
                    if (!result.Exists(t => t.Equals(token, StringComparison.OrdinalIgnoreCase)))
                        result.Add(token);
                }
            }
        }

        public static int ScoreCommonWords(int productType, IReadOnlyList<string> commonWords, bool placeholderBoost = false)
        {
            if (productType <= ItemID.None || commonWords == null || commonWords.Count == 0)
                return 0;

            Item prod = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(prod, productType))
                return 0;
            string prodLower = (prod.Name ?? "").ToLowerInvariant();
            int best = 0;
            int strong = placeholderBoost ? FurnitureGenericWoodLineageRules.PlaceholderCommonWordBoost : CommonWordStrong;

            foreach (string token in commonWords)
            {
                if (string.IsNullOrWhiteSpace(token) || token.Length < 2)
                    continue;
                if (prodLower.Contains(token.Trim().ToLowerInvariant()))
                    best = Math.Max(best, strong);
            }

            return best;
        }

        private static List<string> ExtractSharedTokens(List<string> names, int minTokenLength, int minOccurrences)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in names)
            {
                foreach (string token in TokenizeName(name))
                {
                    if (token.Length < minTokenLength || IsSlotPartToken(token))
                        continue;
                    counts.TryGetValue(token, out int c);
                    counts[token] = c + 1;
                }
            }

            var result = new List<string>();
            foreach (KeyValuePair<string, int> kv in counts)
            {
                if (kv.Value >= minOccurrences)
                    result.Add(kv.Key);
            }

            return result;
        }

        private static IEnumerable<string> TokenizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                yield break;

            name = name.Trim().ToLowerInvariant();
            var current = new StringBuilder();
            foreach (char ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch >= 0x4e00)
                {
                    current.Append(ch);
                    continue;
                }

                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }

            if (current.Length > 0)
                yield return current.ToString();
        }

        private static bool IsSlotPartToken(string token)
        {
            foreach (string part in SlotPartTokens)
            {
                if (token.Equals(part, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
