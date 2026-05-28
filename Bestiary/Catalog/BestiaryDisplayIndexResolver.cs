using System.Reflection;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    /// <summary>
    /// 角标序号（BestiaryDisplayIndex）与内部排序 id（BestiarySortIndex）分开解析。
    /// 超级图鉴网格列表按角标序号升序；<see cref="BestiaryVanillaEntrySort"/> 供需要原版排序 id 的场景。
    /// </summary>
    internal static class BestiaryDisplayIndexResolver
    {
        public readonly struct Result
        {
            public readonly int SortIndex;
            public readonly int LabelIndex;
            public readonly bool HasLabel;

            public Result(int sortIndex, int labelIndex, bool hasLabel)
            {
                SortIndex = sortIndex;
                LabelIndex = labelIndex;
                HasLabel = hasLabel;
            }
        }

        private static FieldInfo _npcIdToDisplayIndexField;

        public static Result Resolve(BestiaryEntry entry, int catalogIndex, int netId)
        {
            if (netId <= 0 && entry != null)
                BestiaryEntryResolver.TryGetNpcNetId(entry, out netId);

            int sortIndex = ResolveSortingId(netId, entry, catalogIndex);
            if (TryResolveDisplayLabel(entry, netId, out int label))
                return new Result(sortIndex, label, true);

            // 勿用 catalogIndex+1 作角标：与 UIBestiaryEntryButton 左上角序号不一致，会导致列表看似未按序号排。
            return new Result(sortIndex, 0, false);
        }

        /// <summary>列表排序用：与卡片左上角角标同源。</summary>
        public static bool TryGetLabelSortKey(BestiaryEntry entry, int netId, out int label) =>
            TryResolveDisplayLabel(entry, netId, out label);

        private static int ResolveSortingId(int netId, BestiaryEntry entry, int catalogIndex)
        {
            if (netId > 0 && ContentSamples.NpcBestiarySortingId.TryGetValue(netId, out int sortId))
                return sortId;

            if (TryResolveDisplayLabel(entry, netId, out int label))
                return label;

            return catalogIndex >= 0 ? catalogIndex : int.MaxValue;
        }

        private static bool TryResolveDisplayLabel(BestiaryEntry entry, int netId, out int index)
        {
            index = 0;

            if (entry?.Info != null)
            {
                for (int i = 0; i < entry.Info.Count; i++)
                {
                    if (entry.Info[i] is not NPCNetIdBestiaryInfoElement npcElement)
                        continue;

                    try
                    {
                        index = npcElement.BestiaryDisplayIndex;
                        if (index >= 0)
                            return true;
                    }
                    catch
                    {
                        // 未注册 netId
                    }
                }

                for (int i = 0; i < entry.Info.Count; i++)
                {
                    if (entry.Info[i] is NPCNetIdBestiaryInfoElement)
                        continue;

                    if (entry.Info[i] is not IBestiaryEntryDisplayIndex displayIndex)
                        continue;

                    try
                    {
                        index = displayIndex.BestiaryDisplayIndex;
                        if (index >= 0)
                            return true;
                    }
                    catch
                    {
                        // 模组自定义 display 元素
                    }
                }
            }

            return netId > 0 && TryLookupDisplayIndex(netId, out index) && index >= 0;
        }

        private static bool TryLookupDisplayIndex(int netId, out int index)
        {
            index = 0;
            if (netId <= 0)
                return false;

            FieldInfo mapField = GetNpcIdToDisplayIndexField();
            if (mapField?.GetValue(null) is System.Collections.Generic.Dictionary<int, int> map &&
                map.TryGetValue(netId, out index))
            {
                return true;
            }

            return false;
        }

        private static FieldInfo GetNpcIdToDisplayIndexField()
        {
            if (_npcIdToDisplayIndexField != null)
                return _npcIdToDisplayIndexField;

            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            _npcIdToDisplayIndexField =
                typeof(NPCNetIdBestiaryInfoElement).GetField("NPCIdToBestiaryDisplayIndex", flags) ??
                typeof(NPCNetIdBestiaryInfoElement).GetField("_npcNetIdToBestiaryDisplayIndex", flags) ??
                typeof(NPCNetIdBestiaryInfoElement).GetField("NpcNetIdToBestiaryDisplayIndex", flags);

            return _npcIdToDisplayIndexField;
        }
    }
}
