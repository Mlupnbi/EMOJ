using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.ItemHub.Rules
{
    public static class HubRecipeClosure
    {
        /// <summary>
        /// �䷽������ center Ϊê����(1) ���С�����Ϊ center�����䷽�Ĳ��ϣ�(2) ���С����� center Ϊ���ϡ����䷽�Ĳ�������� center �����
        /// ��������ɸѡ���� AND��������ɸѡʱ��Ϊ�˼��ϡ�
        /// </summary>
        public static HashSet<int> BuildRecipeNeighborhood(int center)
        {
            var set = new HashSet<int>();
            if (center <= ItemID.None)
                return set;
            set.Add(center);
            if (Main.recipe == null)
                return set;

            int n = Recipe.numRecipes;
            for (int i = 0; i < n; i++)
            {
                Recipe r = Main.recipe[i];
                if (r?.createItem == null || r.createItem.IsAir)
                    continue;
                int prod = r.createItem.type;
                if (prod != center)
                    continue;
                for (int j = 0; j < r.requiredItem.Count; j++)
                {
                    Item req = r.requiredItem[j];
                    if (req != null && !req.IsAir && req.type > ItemID.None)
                        set.Add(req.type);
                }
            }

            for (int i = 0; i < n; i++)
            {
                Recipe r = Main.recipe[i];
                if (r?.createItem == null || r.createItem.IsAir)
                    continue;
                int prod = r.createItem.type;
                bool usesCenter = false;
                for (int j = 0; j < r.requiredItem.Count; j++)
                {
                    Item req = r.requiredItem[j];
                    if (req != null && !req.IsAir && req.type == center)
                    {
                        usesCenter = true;
                        break;
                    }
                }

                if (usesCenter)
                    set.Add(prod);
            }

            return set;
        }

        /// <summary>�ɰ�ȫͼ�հ������ܼ��󣩣��������Ҫʱʹ�á�</summary>
        public static HashSet<int> BuildClosure(int center)
        {
            var set = new HashSet<int> { center };
            if (Main.recipe == null)
                return set;

            bool changed = true;
            int guard = 0;
            int n = Recipe.numRecipes;
            while (changed && guard++ < 64)
            {
                changed = false;
                for (int i = 0; i < n; i++)
                {
                    Recipe r = Main.recipe[i];
                    if (r?.createItem == null || r.createItem.IsAir)
                        continue;
                    int prod = r.createItem.type;
                    if (!set.Contains(prod))
                        continue;
                    for (int j = 0; j < r.requiredItem.Count; j++)
                    {
                        Item req = r.requiredItem[j];
                        if (req == null || req.IsAir || req.type <= ItemID.None)
                            continue;
                        if (set.Add(req.type))
                            changed = true;
                    }
                }

                for (int i = 0; i < n; i++)
                {
                    Recipe r = Main.recipe[i];
                    if (r?.createItem == null || r.createItem.IsAir)
                        continue;
                    int prod = r.createItem.type;
                    bool uses = false;
                    for (int j = 0; j < r.requiredItem.Count; j++)
                    {
                        Item req = r.requiredItem[j];
                        if (req != null && !req.IsAir && set.Contains(req.type))
                        {
                            uses = true;
                            break;
                        }
                    }
                    if (uses && set.Add(prod))
                        changed = true;
                }
            }

            return set;
        }
    }
}
