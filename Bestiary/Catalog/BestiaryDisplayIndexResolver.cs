using System.Reflection;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    /// <summary>
    /// 角标序号（BestiaryDisplayIndex）与列表排序键（BestiarySortingId）分开解析。
    /// 排序必须用 NpcBestiarySortingId / ByBestiarySortingId，不能只用角标数字。
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

            if (catalogIndex >= 0)
            {
                int fallbackLabel = catalogIndex + 1;
                if (sortIndex == int.MaxValue)
                    sortIndex = fallbackLabel;
                return new Result(sortIndex, fallbackLabel, true);
            }

            return new Result(sortIndex, 0, false);
        }

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
            if (netId > 0 && TryLookupDisplayIndex(netId, out index))
                return index >= 0;

            if (entry?.Info == null)
                return false;

            for (int i = 0; i < entry.Info.Count; i++)
            {
                if (entry.Info[i] is NPCNetIdBestiaryInfoElement)
                    continue;

                if (entry.Info[i] is not IBestiaryEntryDisplayIndex displayIndex)
                    continue;

                try
                {
                    index = displayIndex.BestiaryDisplayIndex;
                    return index >= 0;
                }
                catch
                {
                    // 未注册 netId
                }
            }

            return false;
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
