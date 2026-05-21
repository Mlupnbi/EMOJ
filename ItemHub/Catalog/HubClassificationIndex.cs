using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.ItemHub.Catalog
{
    /// <summary>
    /// ��Ʒ������Ԫ���ݣ���ǩν�ʡ�ģ����������ֶΣ������� <see cref="ItemHubCatalog"/>�����б���ʾ/�������
    /// </summary>
    public static class HubClassificationIndex
    {
        private const int SchemaVersion = 13;
        private static int _builtSchema;

        public static bool Ready { get; private set; }
        public static HubRegistry.Meta[] ByType { get; private set; }
        public static HubExtData[] ExtByType { get; private set; }
        public static List<string> ModKeys { get; private set; } = new List<string>();

        public static void Reset()
        {
            Ready = false;
            _builtSchema = 0;
            ByType = null;
            ExtByType = null;
            ModKeys.Clear();
            HubStationTileIndex.Reset();
            HubExtDataBuilder.ResetCaches();
        }

        public static void EnsureBuilt()
        {
            HubCatalog.EnsureBuilt();
            if (Ready && _builtSchema == SchemaVersion)
                return;

            if (Ready)
                Reset();

            int max = ItemLoader.ItemCount;
            ByType = new HubRegistry.Meta[max];
            ExtByType = new HubExtData[max];

            HubStationTileIndex.BuildStationTileIndexFromRecipes();
            HubMaterialResearchBridge.Rebuild();
            ItemHubAmmoTypeReferencedByWeaponsCache.EnsureBuilt(max);

            bool[] deprecatedSnapshot = HubCatalog.DeprecatedSnapshot;
            HashSet<int> validRarities = new HashSet<int>();
            foreach (int type in HubCatalog.AllTypes)
            {
                Item probe = HubCatalog.GetDisplayItemReference(type) ?? new Item();
                ModItem mi = ItemLoader.GetItem(type);
                string modKey = mi == null ? "Terraria" : mi.Mod.Name;
                string intName = mi?.Name ?? ItemID.Search.GetName(type) ?? "";
                string loc = probe.type > ItemID.None ? (probe.Name ?? "") : (mi?.DisplayName.Value ?? intName);

                ByType[type] = new HubRegistry.Meta
                {
                    ModKey = modKey,
                    NameLower = loc.ToLowerInvariant(),
                    InternalLower = intName.ToLowerInvariant(),
                    Value = probe.value,
                    Rare = probe.rare,
                    Accessory = probe.accessory,
                    Consumable = probe.consumable,
                    CreateTile = probe.createTile,
                    Melee = probe.CountsAsClass(DamageClass.Melee) && probe.damage > 0 &&
                        !(probe.pick > 0 || probe.axe > 0 || probe.hammer > 0),
                    Ranged = probe.CountsAsClass(DamageClass.Ranged) && probe.damage > 0,
                    Magic = probe.CountsAsClass(DamageClass.Magic) && probe.damage > 0,
                    Summon = probe.CountsAsClass(DamageClass.Summon) && probe.damage > 0,
                    Pick = probe.pick > 0,
                    Axe = probe.axe > 0,
                    Hammer = probe.hammer > 0,
                    FishingPole = probe.fishingPole > 0,
                    Damage = probe.damage,
                    Defense = probe.defense
                };
                ExtByType[type] = HubExtDataBuilder.Build(type, probe, ref ByType[type], deprecatedSnapshot);
                validRarities.Add(ByType[type].Rare);
            }

            ItemHubRareRangeStrip.ConfigureValidRarities(validRarities);
            ModKeys = HubModFilters.BuildFilterModKeys();

            Ready = true;
            _builtSchema = SchemaVersion;
        }

        public static bool IsDebugItem(int type) =>
            Ready && type > 0 && type < ExtByType.Length && ExtByType[type].DebugItem;

        public static ref HubRegistry.Meta GetMeta(int type) => ref ByType[type];

        public static ref HubExtData GetExt(int type) => ref ExtByType[type];
    }
}
