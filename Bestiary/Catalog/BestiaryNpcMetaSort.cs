namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    internal static class BestiaryNpcMetaSort
    {
        public static int Compare(BestiaryNpcMeta a, BestiaryNpcMeta b) =>
            BestiaryVanillaEntrySort.Compare(a, b);
    }
}
