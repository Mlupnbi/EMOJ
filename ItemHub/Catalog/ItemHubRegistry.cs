using System.Collections.Generic;
using Terraria;

namespace EvenMoreOverpoweredJourney.ItemHub.Catalog
{
    /// <summary>
    /// �������棺Э�� <see cref="ItemHubCatalog"/>��Ŀ¼���� <see cref="ItemHubClassificationIndex"/>�����ࣩ��
    /// �´�����ֱ��ʹ�ø�ר�����ͣ��б����� <see cref="ItemHubDisplayQuery"/>��
    /// </summary>
    public static class HubRegistry
    {
        public struct Meta
        {
            public string ModKey;
            public string NameLower;
            public string InternalLower;
            public int Value;
            public int Rare;
            public bool Accessory;
            public bool Consumable;
            public int CreateTile;
            public bool Melee, Ranged, Magic, Summon;
            public bool Pick, Axe, Hammer;
            public bool FishingPole;
            public int Damage;
            public int Defense;
        }

        public static bool Ready => HubCatalog.Ready && HubClassificationIndex.Ready;

        public static List<int> AllTypes { get; } = new List<int>();

        public static List<string> ModKeys => HubClassificationIndex.ModKeys;

        public static Meta[] ByType => HubClassificationIndex.ByType;

        public static HubExtData[] ExtByType => HubClassificationIndex.ExtByType;

        public static bool IsDebugItem(int type) => HubClassificationIndex.IsDebugItem(type);

        public static bool HasDisplayItem(int type) =>
            HubCatalog.Contains(type) && (HubCatalog.GetDisplayItemReference(type)?.type ?? 0) > 0;

        public static void Reset()
        {
            HubCatalog.Reset();
            HubClassificationIndex.Reset();
            AllTypes.Clear();
        }

        public static void EnsureBuilt()
        {
            HubCatalog.EnsureBuilt();
            HubClassificationIndex.EnsureBuilt();
            AllTypes.Clear();
            foreach (int t in HubCatalog.AllTypes)
                AllTypes.Add(t);
        }

        public static Item GetDisplayItem(int type) => HubCatalog.GetDisplayItem(type);

        public static Item GetDisplayItemReference(int type) => HubCatalog.GetDisplayItemReference(type);
    }
}
