using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.ItemHub.Rules;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus
{
    /// <summary>
    /// ����������� buff ͼ�����װ�������� ͷ/��/��ӳ�䣻���������ڻ��������ȫ��ɨ�衣
    /// </summary>
    public static class SetBonusArmorResolver
    {
        public sealed class ArmorSetBuffDefinition
        {
            public readonly int HeadItemType;
            public readonly int BodyItemType;
            public readonly int LegItemType;
            public readonly int HeadSlot;
            public readonly int BodySlot;
            public readonly int LegSlot;

            public ArmorSetBuffDefinition(int headItemType, int bodyItemType, int legItemType)
            {
                HeadItemType = headItemType;
                BodyItemType = bodyItemType;
                LegItemType = legItemType;
                HeadSlot = GetEquipSlot(headItemType, item => item.headSlot);
                BodySlot = GetEquipSlot(bodyItemType, item => item.bodySlot);
                LegSlot = GetEquipSlot(legItemType, item => item.legSlot);
            }
        }

        private static readonly Dictionary<int, ArmorSetBuffDefinition> DefinitionsByBuff = new Dictionary<int, ArmorSetBuffDefinition>();
        private static readonly HashSet<int> UnresolvedBuffIds = new HashSet<int>();

        private const int MaxProbesPerBuff = 512;
        private const int MaxFamiliesPerResolve = 12;
        private const int MaxCandidatesPerSlot = 4;

        public static void ClearCache()
        {
            DefinitionsByBuff.Clear();
            UnresolvedBuffIds.Clear();
        }

        public static bool HasDefinition(int buffId) =>
            buffId > 0 && DefinitionsByBuff.ContainsKey(buffId);

        public static bool TryGetDefinition(int buffId, out ArmorSetBuffDefinition definition)
        {
            if (buffId > 0 && DefinitionsByBuff.TryGetValue(buffId, out definition))
                return true;

            definition = null;
            return false;
        }

        /// <summary>���������װ buff ʱ���ã��ɹ��򻺴�ӳ�乩 <see cref="SetBonusHookSystem"/> ʹ�á�</summary>
        public static bool TryResolve(int buffId, bool forceRetry = false)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (DefinitionsByBuff.ContainsKey(buffId))
                return true;

            if (SetBonusHookSystem.IsVanillaHardcodedSetBonusBuff(buffId))
                return true;

            if (!forceRetry && UnresolvedBuffIds.Contains(buffId))
                return false;

            if (forceRetry)
                UnresolvedBuffIds.Remove(buffId);

            ModBuff modBuff = BuffLoader.GetBuff(buffId);
            if (modBuff == null)
            {
                UnresolvedBuffIds.Add(buffId);
                return false;
            }

            string modName = modBuff.Mod.Name;
            string familyHint = InferFamilyKeyFromBuffName(modBuff.Name);

            List<Item> heads = BuildArmorList(modName, HubCollectibleRules.IsHeadArmor);
            List<Item> bodies = BuildArmorList(modName, HubCollectibleRules.IsBodyArmor);
            List<Item> legs = BuildArmorList(modName, HubCollectibleRules.IsLegArmor);

            IEnumerable<string> families = heads.Concat(bodies).Concat(legs)
                .Select(ArmorFamilyKey)
                .Where(key => !string.IsNullOrEmpty(key))
                .Distinct()
                .OrderBy(key => key);

            if (!string.IsNullOrEmpty(familyHint))
                families = families.Where(key => key.Contains(familyHint, StringComparison.OrdinalIgnoreCase))
                    .Concat(families.Where(key => !key.Contains(familyHint, StringComparison.OrdinalIgnoreCase)));

            int probes = 0;
            int familiesTried = 0;
            foreach (string familyKey in families)
            {
                if (familiesTried++ >= MaxFamiliesPerResolve || probes >= MaxProbesPerBuff)
                    break;

                List<Item> headCandidates = heads.Where(h => ArmorFamilyKey(h) == familyKey).Take(MaxCandidatesPerSlot).ToList();
                List<Item> bodyCandidates = bodies.Where(b => ArmorFamilyKey(b) == familyKey).Take(MaxCandidatesPerSlot).ToList();
                List<Item> legCandidates = legs.Where(l => ArmorFamilyKey(l) == familyKey).Take(MaxCandidatesPerSlot).ToList();

                if (headCandidates.Count == 0)
                    headCandidates.Add(new Item());
                if (bodyCandidates.Count == 0)
                    bodyCandidates.Add(new Item());
                if (legCandidates.Count == 0)
                    legCandidates.Add(new Item());

                foreach (Item head in headCandidates)
                {
                    foreach (Item body in bodyCandidates)
                    {
                        foreach (Item leg in legCandidates)
                        {
                            if (probes++ >= MaxProbesPerBuff)
                                goto Done;

                            if (!TryProbeSet(head, body, leg, out HashSet<int> buffIds) || !buffIds.Contains(buffId))
                                continue;

                            Register(buffId, head, body, leg);
                            EmojLog.InfoOnce(EmojLogChannel.SetBonus, $"setbonus:ok:{buffId}",
                                $"TryResolve ok buffId={buffId} head={ValidType(head)} body={ValidType(body)} leg={ValidType(leg)}",
                                EmojLogDetail.Full);
                            return true;
                        }
                    }
                }
            }

            Done:
            UnresolvedBuffIds.Add(buffId);
            EmojLog.InfoOnce(EmojLogChannel.SetBonus, $"setbonus:fail:{buffId}",
                $"TryResolve failed buffId={buffId} probes={probes}", EmojLogDetail.Full);
            return false;
        }

        public static IEnumerable<ArmorSetBuffDefinition> GetDefinitionsForActiveBuffs(HashSet<int> activeBuffs)
        {
            if (activeBuffs == null || activeBuffs.Count == 0)
                yield break;

            var yielded = new HashSet<ArmorSetBuffDefinition>();
            foreach (int buffId in activeBuffs)
            {
                if (!BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId))
                    continue;

                if (!DefinitionsByBuff.TryGetValue(buffId, out ArmorSetBuffDefinition definition))
                {
                    TryResolve(buffId);
                    if (!DefinitionsByBuff.TryGetValue(buffId, out definition))
                        continue;
                }

                if (yielded.Add(definition))
                    yield return definition;
            }
        }

        private static void Register(int buffId, Item head, Item body, Item leg)
        {
            var definition = new ArmorSetBuffDefinition(ValidType(head), ValidType(body), ValidType(leg));
            DefinitionsByBuff[buffId] = definition;
            UnresolvedBuffIds.Remove(buffId);
        }

        private static List<Item> BuildArmorList(string modName, Func<Item, bool> predicate)
        {
            var result = new List<Item>();
            foreach (Item sample in ContentSamples.ItemsByType.Values)
            {
                if (sample == null || sample.IsAir || sample.type <= ItemID.None || sample.vanity)
                    continue;

                if (sample.ModItem?.Mod?.Name != modName)
                    continue;

                if (!predicate(sample))
                    continue;

                result.Add(CloneOrAir(sample));
            }

            result.Sort((a, b) => a.type.CompareTo(b.type));
            return result;
        }

        private static string ArmorFamilyKey(Item item)
        {
            if (item == null || item.IsAir || item.type <= ItemID.None)
                return "";

            string modKey = item.ModItem?.Mod?.Name ?? "Terraria";
            if (modKey == "Terraria")
                return "";

            string name = item.ModItem?.Name ?? ItemID.Search.GetName(item.type) ?? item.Name ?? "";
            string normalized = NormalizeArmorName(name);
            return string.IsNullOrEmpty(normalized) ? "" : modKey + ":" + normalized;
        }

        private static string InferFamilyKeyFromBuffName(string buffName)
        {
            if (string.IsNullOrEmpty(buffName))
                return "";

            string normalized = buffName;
            string[] suffixes =
            {
                "SetBonus", "ArmorSet", "ArmorBonus", "SetEffect", "SetBuff", "Buff", "Set"
            };

            foreach (string suffix in suffixes)
            {
                if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                    normalized.Length > suffix.Length)
                {
                    normalized = normalized.Substring(0, normalized.Length - suffix.Length);
                    break;
                }
            }

            return NormalizeArmorName(normalized);
        }

        private static string NormalizeArmorName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            string normalized = name;
            string[] suffixes =
            {
                "Helmet", "Headgear", "Headpiece", "Head", "Mask", "Hood", "Hat", "Cap", "Crown", "Helm",
                "Breastplate", "Chestplate", "Chest", "Body", "PlateMail", "Platemail", "Mail", "Robe", "Shirt", "Coat", "Tunic", "Vest",
                "Leggings", "Greaves", "Legs", "Pants", "Boots", "Cuisses", "Sabatons"
            };

            bool changed;
            do
            {
                changed = false;
                foreach (string suffix in suffixes)
                {
                    if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && normalized.Length > suffix.Length)
                    {
                        normalized = normalized.Substring(0, normalized.Length - suffix.Length);
                        changed = true;
                        break;
                    }
                }
            }
            while (changed);

            return normalized.Trim('_', '-', ' ');
        }

        private static bool TryProbeSet(Item head, Item body, Item leg, out HashSet<int> buffIds)
        {
            Player testPlayer = new Player();
            testPlayer.whoAmI = 255;
            testPlayer.statDefense = Player.DefenseStat.Default;
            testPlayer.setBonus = "";
            testPlayer.head = head?.headSlot ?? -1;
            testPlayer.body = body?.bodySlot ?? -1;
            testPlayer.legs = leg?.legSlot ?? -1;
            testPlayer.armor[0] = CloneOrAir(head);
            testPlayer.armor[1] = CloneOrAir(body);
            testPlayer.armor[2] = CloneOrAir(leg);
            ClearBuffs(testPlayer);

            try
            {
                testPlayer.UpdateArmorSets(255);
            }
            catch
            {
                buffIds = new HashSet<int>();
                return false;
            }

            buffIds = CaptureBuffs(testPlayer);
            return buffIds.Count > 0 || !string.IsNullOrWhiteSpace(testPlayer.setBonus);
        }

        private static int ValidType(Item item) =>
            item != null && !item.IsAir && item.type > ItemID.None ? item.type : ItemID.None;

        private static Item CloneOrAir(Item item)
        {
            if (item == null || item.IsAir || item.type <= ItemID.None)
                return new Item();

            return item.Clone();
        }

        private static void ClearBuffs(Player player)
        {
            for (int i = 0; i < player.buffType.Length; i++)
            {
                player.buffType[i] = 0;
                player.buffTime[i] = 0;
            }
        }

        private static HashSet<int> CaptureBuffs(Player player)
        {
            var result = new HashSet<int>();
            for (int i = 0; i < player.buffType.Length; i++)
            {
                int id = player.buffType[i];
                if (id > 0 && id < BuffLoader.BuffCount && player.buffTime[i] > 0 && BuffListCatalog.IsListable(id))
                    result.Add(id);
            }

            return result;
        }

        private static int GetEquipSlot(int itemType, Func<Item, int> selector)
        {
            if (itemType <= ItemID.None || !ContentSamples.ItemsByType.TryGetValue(itemType, out Item item) || item == null)
                return -1;

            return selector(item);
        }
    }
}
