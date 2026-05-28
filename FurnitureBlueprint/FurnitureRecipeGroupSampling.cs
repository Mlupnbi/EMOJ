using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>配方组 ValidItems 限量采样，避免「任意木/锭」等大组在识别路径上扫全表卡死。</summary>
    public static class FurnitureRecipeGroupSampling
    {
        public const int DefaultMaxItems = 16;

        public static IEnumerable<int> Sample(
            IEnumerable<int> validItems,
            string styleHint = null,
            int maxItems = DefaultMaxItems)
        {
            if (validItems == null || maxItems <= 0)
                yield break;

            var list = validItems as IList<int> ?? new List<int>(validItems);
            if (list.Count == 0)
                yield break;

            if (list.Count <= maxItems)
            {
                foreach (int t in list)
                    yield return t;
                yield break;
            }

            if (string.IsNullOrWhiteSpace(styleHint))
            {
                for (int i = 0; i < maxItems; i++)
                    yield return list[i];
                yield break;
            }

            styleHint = styleHint.Trim();
            var ranked = new List<(int type, int rank)>(maxItems + 4);
            foreach (int t in list)
            {
                string key = FurnitureSetRecognizer.ExtractStyleKeyPublic(t);
                int rank = FurnitureStyleSignature.StyleKeyFuzzyMatch(styleHint, key) ? 100
                    : FurnitureMaterialKeyNormalizer.SameMaterialFamily(styleHint, key) ? 50
                    : 0;
                ranked.Add((t, rank));
            }

            ranked.Sort((a, b) => b.rank.CompareTo(a.rank));
            int n = 0;
            foreach ((int type, int _) in ranked)
            {
                if (n++ >= maxItems)
                    yield break;
                yield return type;
            }
        }
    }
}
