using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.ItemHub.Catalog
{
    /// <summary>
    /// 物品目录层：仅登记 collectible-checklist 模组可收集（findable）的 type。
    /// 参考：https://github.com/JavidPack/ItemChecklist ItemChecklistPlayer.Initialize
    /// </summary>
    public static class HubCatalog
    {
        private const int CatalogSchemaVersion = 4;
        private static int _builtSchema;

        /// <summary>构建期 <see cref="ItemID.Sets.Deprecated"/> 快照，供分类层 debug 标记使用。</summary>
        public static bool[] DeprecatedSnapshot { get; private set; }

        public static bool Ready { get; private set; }

        /// <summary>全部已登记 type（顺序与构建循环一致）。</summary>
        public static IReadOnlyList<int> AllTypes => _allTypes;

        private static readonly List<int> _allTypes = new List<int>();
        private static Item[] _itemsByType;

        public static void Reset()
        {
            Ready = false;
            _builtSchema = 0;
            DeprecatedSnapshot = null;
            _allTypes.Clear();
            _itemsByType = null;
            HubCollectibleRules.Reset();
        }

        public static void EnsureBuilt()
        {
            if (Ready && _builtSchema == CatalogSchemaVersion)
                return;

            Reset();

            HubCollectibleRules.EnsureCreativeSortOrderBuilt();

            int max = ItemLoader.ItemCount;
            _itemsByType = new Item[max];
            DeprecatedSnapshot = ItemID.Sets.Deprecated;

            for (int k = 1; k < max; k++)
            {
                if (!HubCollectibleRules.IsFindable(k))
                    continue;

                if (!ContentSamples.ItemsByType.TryGetValue(k, out Item item) || item == null)
                    continue;

                _itemsByType[k] = item;
                _allTypes.Add(k);
            }

            Ready = true;
            _builtSchema = CatalogSchemaVersion;
        }

        /// <summary>兼容旧调用：返回只读样本引用，不 Clone。</summary>
        public static Item GetDisplayItem(int type) =>
            GetDisplayItemReference(type) ?? new Item();

        /// <summary>只读展示/筛选引用，避免 Clone/SetDefaults 破坏劣质模组物品。</summary>
        public static Item GetDisplayItemReference(int type)
        {
            if (!Ready || type <= ItemID.None || _itemsByType == null || type >= _itemsByType.Length)
                return null;

            return _itemsByType[type];
        }

        public static bool Contains(int type) =>
            Ready && type > ItemID.None && _itemsByType != null && type < _itemsByType.Length && _itemsByType[type] != null;
    }
}
