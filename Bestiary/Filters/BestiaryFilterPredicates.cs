using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Core.Utilities;

namespace EvenMoreOverpoweredJourney.Bestiary.Filters
{
    public static class BestiaryFilterPredicates
    {
        public static bool PassesFace(BestiaryFaceMode face, BestiaryNpcMeta meta)
        {
            bool wasFound = meta.Entry != null && BestiaryProgressResolver.WasEverFound(meta.Entry);
            bool full = meta.Entry != null && BestiaryProgressResolver.IsFullyUnlockedInTracker(meta.Entry);
            if (meta.Entry == null)
            {
                wasFound = false;
                full = false;
            }

            return BestiaryVisibilityPolicy.IsVisibleInList(face, wasFound, full);
        }

        public static bool PassesSecondary(BestiarySecondaryFilterState st, BestiaryNpcMeta meta)
        {
            if (st == null)
                return true;

            if (st.ActiveModKeys.Count > 0 && !st.ActiveModKeys.Contains(meta.ModKey))
                return false;

            if (st.ActiveBestiaryFilterIds.Count == 0)
                return true;

            if (meta.Entry == null)
                return false;

            foreach (string filterId in st.ActiveBestiaryFilterIds)
            {
                BestiaryFilterDef def = FindVanillaFilter(filterId);
                if (def?.Filter != null && BestiaryFilterIndex.EntryMatchesVanillaFilter(meta.Entry, def.Filter))
                    return true;
            }

            return false;
        }

        public static bool PassesSearch(string query, BestiaryNpcMeta meta)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string q = query.Trim().ToLowerInvariant();
            string name = (meta.DisplayName ?? "").ToLowerInvariant();
            if (name.Contains(q))
                return true;

            if (meta.NetId > 0)
            {
                string internalName = NPCID.Search.GetName(meta.NetId).ToLowerInvariant();
                if (internalName.Contains(q))
                    return true;
            }

            string py = PinyinUtils.GetPinyinInitials(meta.DisplayName ?? "");
            if (!string.IsNullOrEmpty(py) && py.Contains(q, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static BestiaryFilterDef FindVanillaFilter(string id)
        {
            foreach (BestiaryFilterDef f in BestiaryFilterIndex.VanillaFilters)
            {
                if (f.Id == id)
                    return f;
            }

            return null;
        }
    }
}
