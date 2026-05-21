using System.Collections.Generic;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.ItemHub.Filters
{
    /// <summary>
    /// ���б�ɼ���Ʒ��ѯ��Ŀ¼ȫ�� ������ѡ�������� ������ѡ������ɸѡ��
    /// Ĭ�����������޶���Լ��ʱ�� external item browser δɸѡ����һ�£�ȫ�� type����
    /// </summary>
    public static class HubDisplayQuery
    {
        public static IEnumerable<int> EnumerateVisibleTypes(string mainSearchText, HubSecondaryFilterState secondary)
        {
            HubCatalog.EnsureBuilt();
            if (!HubCatalog.Ready)
                yield break;

            bool useSecondary = secondary != null && secondary.HasActiveConstraints;

            foreach (int type in HubCatalog.AllTypes)
            {
                if (type <= ItemID.None)
                    continue;

                if (!HubSearchQuery.Matches(type, mainSearchText))
                    continue;

                if (useSecondary && !secondary.PassesClassification(type))
                    continue;

                yield return type;
            }
        }
    }
}
