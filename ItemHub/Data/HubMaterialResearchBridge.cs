using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.ItemHub.Data
{
    /// <summary>
    /// �ӡ���;���о����׼��������ޣ����Ĳ������䷽ requiredItem ���򴫲����������������Ʒ type��
    /// ���ڣ���������Ի��⡢misc.other ��չƥ�䡣
    /// </summary>
    public static class HubMaterialResearchBridge
    {
        private static bool[] _upstreamReach;

        public static void Clear() => _upstreamReach = null;

        public static bool IsUpstreamToResearchable(int type) =>
            type > 0 &&
            _upstreamReach != null &&
            type < _upstreamReach.Length &&
            _upstreamReach[type];

        public static void Rebuild()
        {
            int max = ItemLoader.ItemCount;
            _upstreamReach = new bool[max];
            if (Main.recipe == null || CreativeItemSacrificesCatalog.Instance == null)
                return;

            for (int t = 1; t < max; t++)
            {
                if (CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(t, out int cap) &&
                    cap > 0)
                    _upstreamReach[t] = true;
            }

            bool changed = true;
            int guard = 0;
            while (changed && guard++ < max * 3)
            {
                changed = false;
                int n = Recipe.numRecipes;
                for (int i = 0; i < n; i++)
                {
                    Recipe r = Main.recipe[i];
                    if (r?.createItem == null || r.createItem.IsAir)
                        continue;
                    int c = r.createItem.type;
                    if (c <= 0 || c >= max || !_upstreamReach[c])
                        continue;
                    for (int j = 0; j < r.requiredItem.Count; j++)
                    {
                        Item req = r.requiredItem[j];
                        if (req == null || req.IsAir)
                            continue;
                        int rt = req.type;
                        if (rt > 0 && rt < max && !_upstreamReach[rt])
                        {
                            _upstreamReach[rt] = true;
                            changed = true;
                        }
                    }
                }
            }
        }
    }
}
