using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.ItemHub.Filters
{
    /// <summary>
    /// ��ģ��ɸѡ������ CollectibleChecklist CollectibleChecklistUI.modnames / PassModFilter��
    /// �ο���reference collectible-checklist repository
    /// </summary>
    public static class HubModFilters
    {
        /// <summary>���� UI ԭ��ɸѡ������Ӧ IC ModnamesVanilla��item.ModItem == null����</summary>
        public const string VanillaModKey = "Terraria";

        public static bool IsVanillaFilterKey(string modKey) =>
            string.Equals(modKey, VanillaModKey, System.StringComparison.Ordinal);

        /// <summary>
        /// �������塸��ģ��ɸѡ����ԭ�� + ӵ�� ModItem ��ģ�飨�� IC modnames ����ȫ������һ�£�˳��Ϊ ModLoader.Mods����
        /// </summary>
        public static List<string> BuildFilterModKeys()
        {
            var keys = new List<string> { VanillaModKey };
            foreach (Mod mod in ModLoader.Mods)
            {
                if (!mod.GetContent<ModItem>().Any())
                    continue;
                if (keys.Contains(mod.Name))
                    continue;
                keys.Add(mod.Name);
            }

            return keys;
        }

        /// <summary>��Ʒ�Ƿ����ڸ���ģ��ɸѡ����ί�� CollectibleChecklist PassModFilter����</summary>
        public static bool MatchesModKey(int type, string modKey) =>
            HubCollectibleRules.PassModFilter(type, modKey);
    }
}
