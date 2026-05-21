using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Spawning
{
    /// <summary>������������ʩ��/������ EMOJ ���ڵ� Buff���������ٻ��ʹӵȣ���</summary>
    public sealed class BuffEntityIndexSystem : ModSystem
    {
        public static HashSet<int> NonMaintainableBuffIds { get; } = new HashSet<int>();

        private static readonly string[] SpawningBuffNameSuffixes =
        {
            "MinionBuff", "SummonBuff", "MinionDebuff", "SummonDebuff", "SentryBuff",
            "PetBuff", "VanityPetBuff", "LightPetBuff", "MountBuff"
        };

        private static readonly string[] SpawningBuffNamespaceSegments =
        {
            "Minions", "Minion", "Pets", "Pet", "Mounts", "Mount"
        };

        public static void Rebuild()
        {
            NonMaintainableBuffIds.Clear();

            RegisterNonMaintainableBuffs(BuffCategoryIndexSystem.EnumerateMiscExclusiveBuffIds());

            for (int buffId = 1; buffId < BuffLoader.BuffCount; buffId++)
            {
                if (IsVanillaEntityPetOrMount(buffId))
                    NonMaintainableBuffIds.Add(buffId);

                if (IsModSpawningEntityBuff(buffId))
                    NonMaintainableBuffIds.Add(buffId);
            }

            foreach (var pair in ContentSamples.ItemsByType)
            {
                Item item = pair.Value;
                if (item == null || item.IsAir || item.buffType <= 0)
                    continue;

                if (item.mountType != -1 || BuffSummonProjectileHelper.ItemShootIsSentry(item))
                    NonMaintainableBuffIds.Add(item.buffType);
                else if (item.shoot != ProjectileID.None && item.DamageType == DamageClass.Summon)
                    NonMaintainableBuffIds.Add(item.buffType);
                else if (ModBuffNamespaceIndicatesEntitySpawn(BuffLoader.GetBuff(item.buffType)))
                    NonMaintainableBuffIds.Add(item.buffType);
            }

            RegisterBuffsFromEntityProjectiles();
        }

        private static void RegisterBuffsFromEntityProjectiles()
        {
            foreach (var pair in ContentSamples.ProjectilesByType)
            {
                int projType = pair.Key;
                Projectile sample = pair.Value;
                if (sample == null || projType <= 0)
                    continue;

                if (!Main.projPet[projType] && !sample.minion && !sample.sentry)
                    continue;

                foreach (var itemPair in ContentSamples.ItemsByType)
                {
                    Item item = itemPair.Value;
                    if (item == null || item.IsAir || item.buffType <= 0 || item.shoot != projType)
                        continue;

                    NonMaintainableBuffIds.Add(item.buffType);
                }
            }
        }

        public static void RegisterNonMaintainableBuffs(IEnumerable<int> buffIds)
        {
            foreach (int buffId in buffIds)
            {
                if (buffId > 0 && buffId < BuffLoader.BuffCount)
                    NonMaintainableBuffIds.Add(buffId);
            }
        }

        public static bool ModBuffNamespaceIndicatesEntitySpawn(ModBuff buff)
        {
            if (buff == null)
                return false;

            return NamespaceIndicatesEntitySpawn(buff.GetType().Namespace ?? "");
        }

        internal static bool NamespaceIndicatesEntitySpawn(string ns)
        {
            if (string.IsNullOrEmpty(ns))
                return false;

            foreach (string segment in SpawningBuffNamespaceSegments)
            {
                if (NamespaceContainsSegment(ns, segment))
                    return true;
            }

            return false;
        }

        public static bool NamespaceContainsSegment(string ns, string segment)
        {
            return ns.IndexOf("." + segment + ".", StringComparison.OrdinalIgnoreCase) >= 0
                || ns.EndsWith("." + segment, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>ģ�齫���� Buff ���� *.Pets �����ռ䵫δ���� vanityPet ʱ���� UI/��Ʒ�۷��ࡣ</summary>
        public static bool IsModPetNamespaceBuff(int buffId)
        {
            if (buffId < BuffID.Count)
                return false;

            ModBuff buff = BuffLoader.GetBuff(buffId);
            return buff != null && NamespaceContainsSegment(buff.GetType().Namespace ?? "", "Pets");
        }

        public static bool IsEntitySpawningBuff(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            return NonMaintainableBuffIds.Contains(buffId);
        }

        /// <summary>ս���ٻ��ʹӣ����ڱ����� Buff����������ʩ�ӻ�һ��ȫ����</summary>
        public static bool IsCombatSummonBuff(int buffId) =>
            IsSummonMinionBuff(buffId) || IsSentryBuff(buffId);

        public static bool IsSummonMinionBuff(int buffId)
        {
            if (!IsEntitySpawningBuff(buffId))
                return false;

            if (IsSentryBuff(buffId))
                return false;

            if (BuffMountCategorySystem.IsMountBuff(buffId) || BuffMountCategorySystem.IsMinecartBuff(buffId))
                return false;

            if (BuffMountCategorySystem.TryResolveMountCategory(buffId, out _))
                return false;

            string category = BuffCategoryIndexSystem.GetCategory(buffId);
            if (BuffCategoryIndexSystem.IsMiscExclusiveCategory(category))
                return false;

            return true;
        }

        public static bool IsSentryBuff(int buffId) => BuffCombatSummonClassifier.IsSentryBuff(buffId);

        public static bool RequiresManualEntityManagement(int buffId)
        {
            if (!IsEntitySpawningBuff(buffId))
                return false;

            if (BuffCategoryIndexSystem.IsCombatSummonCategory(BuffCategoryIndexSystem.GetCategory(buffId)))
                return false;

            if (BuffMountCategorySystem.IsMountBuff(buffId) || BuffMountCategorySystem.IsMinecartBuff(buffId))
                return false;

            if (BuffMountCategorySystem.TryResolveMountCategory(buffId, out _))
                return false;

            return BuffMiscEquipIndexSystem.GetMiscEquipSlotIndex(BuffCategoryIndexSystem.GetCategory(buffId)) < 0;
        }

        private static bool IsVanillaEntityPetOrMount(int buffId)
        {
            if (buffId < BuffID.Count)
            {
                if (Main.vanityPet[buffId] || Main.lightPet[buffId] || BuffID.Sets.BasicMountData[buffId] != null)
                    return true;

                if (Main.projPet[buffId])
                    return true;
            }
            else
            {
                if (buffId < Main.vanityPet.Length && Main.vanityPet[buffId])
                    return true;

                if (buffId < Main.lightPet.Length && Main.lightPet[buffId])
                    return true;

                if (buffId < Main.projPet.Length && Main.projPet[buffId])
                    return true;

                if (BuffID.Sets.BasicMountData[buffId] != null)
                    return true;
            }

            return false;
        }

        private static bool IsModSpawningEntityBuff(int buffId)
        {
            if (buffId < BuffID.Count)
                return false;

            ModBuff buff = BuffLoader.GetBuff(buffId);
            if (buff == null)
                return false;

            string name = buff.Name ?? "";
            foreach (string suffix in SpawningBuffNameSuffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return NamespaceIndicatesEntitySpawn(buff.GetType().Namespace ?? "");
        }
    }
}
