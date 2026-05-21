using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    /// <summary>与原版 UIBestiaryEntryGrid 默认排序一致：<see cref="SortingSteps.ByBestiarySortingId"/>。</summary>
    internal static class BestiaryVanillaEntrySort
    {
        private static readonly SortingSteps.ByBestiarySortingId VanillaStep = new();

        public static int Compare(BestiaryNpcMeta a, BestiaryNpcMeta b)
        {
            if (a?.Entry != null && b?.Entry != null)
            {
                try
                {
                    return VanillaStep.Compare(a.Entry, b.Entry);
                }
                catch
                {
                    // 模组条目异常时走数值兜底
                }
            }

            int aKey = GetSortKey(a);
            int bKey = GetSortKey(b);
            int byKey = aKey.CompareTo(bKey);
            if (byKey != 0)
                return byKey;

            int byDisplay = a.BestiaryDisplayIndex.CompareTo(b.BestiaryDisplayIndex);
            if (byDisplay != 0)
                return byDisplay;

            return a.CatalogIndex.CompareTo(b.CatalogIndex);
        }

        /// <summary>原版排序 id（<see cref="ContentSamples.NpcBestiarySortingId"/>）。</summary>
        public static int GetSortKey(BestiaryNpcMeta meta)
        {
            if (meta == null)
                return int.MaxValue;

            if (meta.BestiarySortIndex != int.MaxValue)
                return meta.BestiarySortIndex;

            if (meta.NetId > 0 && ContentSamples.NpcBestiarySortingId.TryGetValue(meta.NetId, out int sortId))
                return sortId;

            if (meta.HasBestiaryDisplayLabel)
                return meta.BestiaryDisplayIndex;

            return meta.CatalogIndex >= 0 ? meta.CatalogIndex : int.MaxValue;
        }
    }
}
