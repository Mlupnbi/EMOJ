using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Catalog
{
    /// <summary>??????????????/??????/???????????</summary>
    public sealed class BuffSourceIndexSystem : ModSystem
    {
        private static readonly HashSet<int> PotionFoodBuffIds = new HashSet<int>();
        private static readonly HashSet<int> EquipmentBuffIds = new HashSet<int>();
        private static readonly HashSet<int> EnvironmentBuffIds = new HashSet<int>();

        private static readonly Dictionary<int, SourceFlags> SourceFlagsByBuff = new Dictionary<int, SourceFlags>();

        [System.Flags]
        private enum SourceFlags
        {
            None = 0,
            Consumable = 1,
            Equipment = 2,
            Environment = 4,
            Other = 8
        }

        public override void PostSetupContent() => Rebuild();

        public static void Rebuild()
        {
            PotionFoodBuffIds.Clear();
            EquipmentBuffIds.Clear();
            EnvironmentBuffIds.Clear();
            SourceFlagsByBuff.Clear();

            for (int type = 1; type < ItemLoader.ItemCount; type++)
            {
                Item item = new Item();
                item.SetDefaults(type);
                int buffId = item.buffType;
                if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                    continue;

                if (!SourceFlagsByBuff.TryGetValue(buffId, out SourceFlags flags))
                    flags = SourceFlags.None;

                if (IsConsumableBuffSource(item, type))
                    flags |= SourceFlags.Consumable;
                else if (IsEnvironmentBuffSource(item, type))
                    flags |= SourceFlags.Environment;
                else if (IsEquipmentBuffSource(item))
                    flags |= SourceFlags.Equipment;
                else
                    flags |= SourceFlags.Other;

                SourceFlagsByBuff[buffId] = flags;
            }

            for (int buffId = 1; buffId < BuffLoader.BuffCount; buffId++)
            {
                if (!BuffListCatalog.IsListable(buffId))
                    continue;

                if (IsKnownSetBonusBuff(buffId))
                    SourceFlagsByBuff[buffId] = GetFlags(buffId) | SourceFlags.Equipment;

                if (IsKnownEnvironmentBuff(buffId))
                    SourceFlagsByBuff[buffId] = GetFlags(buffId) | SourceFlags.Environment;
            }

            foreach (var pair in SourceFlagsByBuff)
            {
                int buffId = pair.Key;
                SourceFlags flags = pair.Value;

                if ((flags & SourceFlags.Consumable) != 0)
                    PotionFoodBuffIds.Add(buffId);
                else if ((flags & SourceFlags.Equipment) != 0)
                    EquipmentBuffIds.Add(buffId);
                else
                    EnvironmentBuffIds.Add(buffId);
            }

            for (int buffId = 1; buffId < BuffLoader.BuffCount; buffId++)
            {
                if (!BuffListCatalog.IsListable(buffId))
                    continue;

                if (PotionFoodBuffIds.Contains(buffId) ||
                    EquipmentBuffIds.Contains(buffId) ||
                    EnvironmentBuffIds.Contains(buffId))
                    continue;

                EnvironmentBuffIds.Add(buffId);
            }
        }

        private static SourceFlags GetFlags(int buffId) =>
            SourceFlagsByBuff.TryGetValue(buffId, out SourceFlags flags) ? flags : SourceFlags.None;

        private static bool IsConsumableBuffSource(Item item, int type)
        {
            if (!item.consumable && item.useStyle == ItemUseStyleID.None)
                return false;

            if (item.consumable || item.potion || item.healLife > 0 || item.healMana > 0)
                return true;

            if (ItemID.Sets.IsFood[type])
                return true;

            return item.useStyle != ItemUseStyleID.None && item.buffTime > 0;
        }

        private static bool IsEquipmentBuffSource(Item item) =>
            item.accessory || item.headSlot > 0 || item.bodySlot > 0 || item.legSlot > 0 ||
            item.mountType != -1 || item.shoot > ProjectileID.None;

        private static bool IsEnvironmentBuffSource(Item item, int type)
        {
            if (item.createTile >= TileID.Dirt)
                return true;

            if (item.accessory)
                return false;

            return item.tileWand != -1 || ItemID.Sets.Torches[type];
        }

        public static bool IsKnownSetBonusBuff(int buffId)
        {
            if (SetBonusArmorResolver.HasDefinition(buffId))
                return true;

            if (SetBonusHookSystem.IsVanillaHardcodedSetBonusBuff(buffId))
                return true;

            if (buffId >= BuffID.Count)
            {
                ModBuff buff = BuffLoader.GetBuff(buffId);
                string name = buff?.Name ?? "";
                return name.Contains("SetBonus", System.StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("ArmorSet", System.StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("ArmorBonus", System.StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("SetEffect", System.StringComparison.OrdinalIgnoreCase) ||
                       name.EndsWith("Set", System.StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static bool IsKnownEnvironmentBuff(int buffId)
        {
            switch (buffId)
            {
                case BuffID.WellFed:
                case BuffID.WellFed2:
                case BuffID.WellFed3:
                case BuffID.Campfire:
                case BuffID.HeartLamp:
                case BuffID.StarInBottle:
                case BuffID.Sunflower:
                case BuffID.PeaceCandle:
                case BuffID.ShadowCandle:
                case BuffID.WaterCandle:
                case BuffID.Honey:
                case BuffID.MonsterBanner:
                case BuffID.DryadsWard:
                    return true;
            }

            if (BuffBeneficialDebuffFlagSystem.MatchesKnownBeneficial(buffId))
                return true;

            return false;
        }

        public static bool IsPotionFoodBuff(int buffId) => buffId > 0 && PotionFoodBuffIds.Contains(buffId);

        public static string GetPositiveSubCategory(int buffId)
        {
            if (PotionFoodBuffIds.Contains(buffId))
                return BuffCategories.PositivePotionFood;

            if (IsKnownSetBonusBuff(buffId))
                return BuffCategories.PositiveSetBonus;

            if (EquipmentBuffIds.Contains(buffId))
                return BuffCategories.PositiveEquipment;

            return BuffCategories.PositiveEnvironment;
        }
    }
}
