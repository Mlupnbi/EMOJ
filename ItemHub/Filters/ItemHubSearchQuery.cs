using Terraria.ID;

namespace EvenMoreOverpoweredJourney.ItemHub.Filters
{
    /// <summary>���б���������ƥ������/�ڲ���/ƴ������ĸ��������������ǩɸѡ�޹ء�</summary>
    public static class HubSearchQuery
    {
        public static bool HasQuery(string searchText) => !string.IsNullOrWhiteSpace(searchText);

        public static bool Matches(int type, string searchText)
        {
            if (!HasQuery(searchText))
                return true;
            if (type <= ItemID.None || !HubClassificationIndex.Ready)
                return false;

            ref HubRegistry.Meta m = ref HubClassificationIndex.ByType[type];
            string s = searchText.ToLowerInvariant().Trim();
            return m.NameLower.Contains(s) ||
                m.InternalLower.Contains(s) ||
                PinyinUtils.GetPinyinInitials(m.NameLower).Contains(s);
        }
    }
}
