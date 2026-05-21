using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Integration.Browser
{
    /// <summary>�� external item browser item browser UI.PopulateGrid һ�µ�ȫ�� type ���ϣ�1 .. ItemCount-1������ʱ��� Deprecated����</summary>
    internal static class ExternalBrowserCatalog
    {
        public static HashSet<int> BuildFullGridTypes()
        {
            HubCatalog.EnsureBuilt();
            return new HashSet<int>(HubCatalog.AllTypes);
        }
    }
}
