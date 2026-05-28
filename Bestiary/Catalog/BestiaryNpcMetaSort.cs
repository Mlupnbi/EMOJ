namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    internal static class BestiaryNpcMetaSort
    {
        /// <summary>°´¿¨Æ¬×óÉÏ½ÇÐòºÅ£¨<see cref="BestiaryNpcMeta.BestiaryDisplayIndex"/>£©ÉýÐò¡£</summary>
        public static int Compare(BestiaryNpcMeta a, BestiaryNpcMeta b)
        {
            if (a == null && b == null)
                return 0;
            if (a == null)
                return 1;
            if (b == null)
                return -1;

            int aKey = GetDisplaySortKey(a);
            int bKey = GetDisplaySortKey(b);
            int byLabel = aKey.CompareTo(bKey);
            if (byLabel != 0)
                return byLabel;

            int byCatalog = a.CatalogIndex.CompareTo(b.CatalogIndex);
            if (byCatalog != 0)
                return byCatalog;

            return a.NetId.CompareTo(b.NetId);
        }

        private static int GetDisplaySortKey(BestiaryNpcMeta meta)
        {
            if (meta == null)
                return int.MaxValue;

            if (BestiaryDisplayIndexResolver.TryGetLabelSortKey(meta.Entry, meta.NetId, out int label))
                return label;

            if (meta.HasBestiaryDisplayLabel)
                return meta.BestiaryDisplayIndex;

            return int.MaxValue;
        }
    }
}
