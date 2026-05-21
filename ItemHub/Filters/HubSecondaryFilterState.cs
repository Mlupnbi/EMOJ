using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.ItemHub.Filters
{
    /// <summary>��������ɸѡ����ǩ/ϡ�ж�/��Ʒ���������� <see cref="HasActiveConstraints"/> ʱԼ�����б��</summary>
    public sealed class HubSecondaryFilterState
    {
        public readonly HashSet<string> ActiveTags = new HashSet<string>(StringComparer.Ordinal);

        public bool UpstreamChainActive;
        public int ChainSlotItemType;
        public HashSet<int> UpstreamClosure;

        public int MajorTabIndex;

        public int RareFilterMin;
        public int RareFilterMax;
        public bool RareFilterCustomized;

        public HubSecondaryFilterState()
        {
            RareFilterMin = ItemHubRareRangeStrip.SliderMin;
            RareFilterMax = ItemHubRareRangeStrip.SliderMax;
        }

        /// <summary>�Ƿ�����û��������õĶ���Լ�����������б���ʾĿ¼ȫ������</summary>
        public bool HasActiveConstraints
        {
            get
            {
                NormalizeRareFilterBounds();
                return ActiveTags.Count > 0 ||
                    IsRareFilterActive ||
                    (UpstreamChainActive && ChainSlotItemType > ItemID.None);
            }
        }

        public bool IsRareFilterActive
        {
            get
            {
                NormalizeRareFilterBounds();
                return RareFilterCustomized &&
                    (RareFilterMin > ItemHubRareRangeStrip.SliderMin ||
                    RareFilterMax < ItemHubRareRangeStrip.SliderMax);
            }
        }

        public void ResetForNewSession()
        {
            ActiveTags.Clear();
            UpstreamChainActive = false;
            ChainSlotItemType = ItemID.None;
            UpstreamClosure = null;
            MajorTabIndex = 0;
            RareFilterMin = ItemHubRareRangeStrip.SliderMin;
            RareFilterMax = ItemHubRareRangeStrip.SliderMax;
            RareFilterCustomized = false;
            InvalidateUpstream();
        }

        public void ResetFilters()
        {
            ActiveTags.Clear();
            UpstreamChainActive = false;
            ChainSlotItemType = ItemID.None;
            UpstreamClosure = null;
            RareFilterMin = ItemHubRareRangeStrip.SliderMin;
            RareFilterMax = ItemHubRareRangeStrip.SliderMax;
            RareFilterCustomized = false;
            InvalidateUpstream();
        }

        public void ResetRareFilterToDefault()
        {
            RareFilterMin = ItemHubRareRangeStrip.SliderMin;
            RareFilterMax = ItemHubRareRangeStrip.SliderMax;
            RareFilterCustomized = false;
        }

        public void NormalizeRareFilterBounds()
        {
            if (!RareFilterCustomized)
            {
                RareFilterMin = ItemHubRareRangeStrip.SliderMin;
                RareFilterMax = ItemHubRareRangeStrip.SliderMax;
                return;
            }

            RareFilterMin = ItemHubRareRangeStrip.NormalizeRarityValue(RareFilterMin);
            RareFilterMax = ItemHubRareRangeStrip.NormalizeRarityValue(RareFilterMax);
            if (RareFilterMin > RareFilterMax)
                (RareFilterMin, RareFilterMax) = (RareFilterMax, RareFilterMin);

            if (RareFilterMin == ItemHubRareRangeStrip.SliderMin &&
                RareFilterMax == ItemHubRareRangeStrip.SliderMax)
                RareFilterCustomized = false;
        }

        public void ToggleTag(string tag)
        {
            if (ActiveTags.Contains(tag))
                ActiveTags.Remove(tag);
            else
                ActiveTags.Add(tag);
            InvalidateUpstream();
        }

        public void SetUpstreamSlotType(int type)
        {
            ChainSlotItemType = type;
            InvalidateUpstream();
        }

        private int _upstreamClosureBuiltFor = int.MinValue;

        public void InvalidateUpstream()
        {
            UpstreamClosure = null;
            _upstreamClosureBuiltFor = int.MinValue;
        }

        public void EnsureUpstreamBuilt()
        {
            if (!UpstreamChainActive || ChainSlotItemType <= ItemID.None)
            {
                UpstreamClosure = null;
                _upstreamClosureBuiltFor = int.MinValue;
                return;
            }

            if (UpstreamClosure != null && _upstreamClosureBuiltFor == ChainSlotItemType)
                return;

            _upstreamClosureBuiltFor = ChainSlotItemType;
            UpstreamClosure = HubRecipeClosure.BuildRecipeNeighborhood(ChainSlotItemType);
        }

        public int ComputeHash()
        {
            EnsureUpstreamBuilt();
            NormalizeRareFilterBounds();
            unchecked
            {
                int h = 17;
                foreach (string t in ActiveTags.OrderBy(x => x, StringComparer.Ordinal))
                    h = h * 397 ^ t.GetHashCode(StringComparison.Ordinal);
                h = h * 397 ^ (UpstreamChainActive ? 1 : 0);
                h = h * 397 ^ ChainSlotItemType;
                h = h * 397 ^ MajorTabIndex;
                h = h * 397 ^ (RareFilterCustomized ? 1 : 0);
                h = h * 397 ^ RareFilterMin;
                h = h * 397 ^ RareFilterMax;
                h = h * 397 ^ (UpstreamClosure?.Count ?? -1);
                return h;
            }
        }

        /// <summary>����ɸѡ���У�������δ������������Ӱ����ȡ�����֣���</summary>
        public bool PassesClassification(int type)
        {
            if (!HubClassificationIndex.Ready || type <= ItemID.None || type >= HubClassificationIndex.ExtByType.Length)
                return true;

            ref HubExtData ext = ref HubClassificationIndex.ExtByType[type];
            ref HubRegistry.Meta meta = ref HubClassificationIndex.ByType[type];

            NormalizeRareFilterBounds();
            if (RareFilterCustomized)
            {
                if (meta.Rare < RareFilterMin)
                    return false;
                if (RareFilterMax < ItemHubRareRangeStrip.SliderMax && meta.Rare > RareFilterMax)
                    return false;
            }

            if (ActiveTags.Count > 0)
            {
                IEnumerable<IGrouping<string, string>> groups = ActiveTags.GroupBy(TagGroupKey, StringComparer.Ordinal);

                foreach (IGrouping<string, string> g in groups)
                {
                    bool any = false;
                    foreach (string tag in g)
                    {
                        if (HubTagPredicates.Matches(type, tag, ref ext))
                        {
                            any = true;
                            break;
                        }
                    }

                    if (!any)
                        return false;
                }
            }

            if (UpstreamChainActive && ChainSlotItemType > ItemID.None)
            {
                EnsureUpstreamBuilt();
                if (UpstreamClosure == null || !UpstreamClosure.Contains(type))
                    return false;
            }

            return true;
        }

        /// <summary>���ݾɵ��á�</summary>
        public bool PassesItem(int type) => PassesClassification(type);

        /// <summary>��� AND��ÿ�� IC �������һ�飻<c>mod.*</c> ���� mod �顣</summary>
        private static string TagGroupKey(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return tag;
            if (tag.StartsWith("ic.", StringComparison.Ordinal))
                return tag;
            if (tag.StartsWith("mod.", StringComparison.Ordinal))
                return "mod";
            int dot = tag.IndexOf('.');
            return dot < 0 ? tag : tag.Substring(0, dot);
        }
    }
}
