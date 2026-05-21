using System;
using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.ItemHub.Filters
{
    public static class HubTagPredicates
    {
        public static bool Matches(int type, string tag, ref HubExtData e)
        {
            try
            {
                if (tag.StartsWith("rare.", StringComparison.Ordinal) && int.TryParse(tag.AsSpan(5), out int rr))
                    return HubRegistry.ByType[type].Rare == rr;

                if (tag.StartsWith("mod.", StringComparison.Ordinal))
                    return string.Equals(HubRegistry.ByType[type].ModKey, tag.Substring(4), StringComparison.Ordinal);

                Item item = HubCatalog.GetDisplayItemReference(type);
                if (item == null || item.type <= ItemID.None)
                    return false;

                return HubCollectibleRules.MatchesHubTag(item, tag);
            }
            catch
            {
                return false;
            }
        }
    }
}
