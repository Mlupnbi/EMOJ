using System;
using System.Collections.Generic;

namespace EvenMoreOverpoweredJourney.Bestiary.Filters
{
    public sealed class BestiarySecondaryFilterState
    {
        public readonly HashSet<string> ActiveModKeys = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Ò³2£ºÔ­°æÍ¼¼øÉ¸Ñ¡Ïî id£¨Phase 1 Ìî³ä£©¡£</summary>
        public readonly HashSet<string> ActiveBestiaryFilterIds = new HashSet<string>(StringComparer.Ordinal);

        public int MajorTabIndex;

        public void ResetForNewSession()
        {
            ActiveModKeys.Clear();
            ActiveBestiaryFilterIds.Clear();
            MajorTabIndex = 0;
        }

        public void ResetFilters()
        {
            ActiveModKeys.Clear();
            ActiveBestiaryFilterIds.Clear();
        }

        public bool HasActiveConstraints =>
            ActiveModKeys.Count > 0 || ActiveBestiaryFilterIds.Count > 0;
    }
}
