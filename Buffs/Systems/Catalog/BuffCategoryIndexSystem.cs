using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Catalog
{
    /// <summary>
    /// ����ʱ������ҳ��������Ԥ���� Buff���� UI �б��� misc ��Ʒ�� / ʵ�� Buff ʵ�ֹ��á�
    /// </summary>
    public sealed class BuffCategoryIndexSystem : ModSystem
    {
        public static HashSet<int> PetBuffIds { get; } = new HashSet<int>();
        public static HashSet<int> LightPetBuffIds { get; } = new HashSet<int>();
        public static HashSet<int> MinionBuffIds { get; } = new HashSet<int>();
        public static HashSet<int> SentryBuffIds { get; } = new HashSet<int>();

        private static string[] categoryByBuffId = Array.Empty<string>();

        public static bool IsMiscExclusiveCategory(string category) =>
            category == BuffCategories.Pet || category == BuffCategories.LightPet ||
            category == BuffCategories.Mount || category == BuffCategories.Minecart;

        public static bool IsCombatSummonCategory(string category) =>
            category == BuffCategories.Minion || category == BuffCategories.Sentry;

        public static string GetCategory(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return BuffCategories.Positive;

            if (categoryByBuffId != null && buffId < categoryByBuffId.Length)
            {
                string cached = categoryByBuffId[buffId];
                if (cached != null)
                    return cached;
            }

            return ResolveCategory(buffId);
        }

        /// <summary>������ҳ <see cref="BuffPage.GetBuffCategoryStatic"/> ��ͬ���ȼ�������ʱȫ��ɨ��д�뻺�档</summary>
        public static string ResolveCategory(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return BuffCategories.Positive;

            if (BuffMountCategorySystem.TryResolveMountCategory(buffId, out bool isMinecart))
                return isMinecart ? BuffCategories.Minecart : BuffCategories.Mount;

            if (IsVanityOrModPetBuff(buffId))
                return BuffCategories.Pet;

            if (buffId < Main.lightPet.Length && Main.lightPet[buffId])
                return BuffCategories.LightPet;

            if (BuffCombatSummonClassifier.IsSentryBuff(buffId))
                return BuffCategories.Sentry;

            if (BuffCombatSummonClassifier.IsMinionBuff(buffId))
                return BuffCategories.Minion;

            if (BuffBeneficialDebuffFlagSystem.IsBeneficialDespiteDebuffFlag(buffId))
                return BuffCategories.Positive;

            if (buffId < Main.debuff.Length && Main.debuff[buffId])
                return BuffCategories.Negative;

            return BuffCategories.Positive;
        }

        private static bool IsVanityOrModPetBuff(int buffId) =>
            buffId < Main.vanityPet.Length && Main.vanityPet[buffId] ||
            BuffEntityIndexSystem.IsModPetNamespaceBuff(buffId);

        public static void Rebuild()
        {
            PetBuffIds.Clear();
            LightPetBuffIds.Clear();
            MinionBuffIds.Clear();
            SentryBuffIds.Clear();

            int count = BuffLoader.BuffCount;
            categoryByBuffId = new string[count];

            for (int buffId = 1; buffId < count; buffId++)
            {
                string category = ResolveCategory(buffId);
                categoryByBuffId[buffId] = category;

                switch (category)
                {
                    case BuffCategories.Pet:
                        PetBuffIds.Add(buffId);
                        break;
                    case BuffCategories.LightPet:
                        LightPetBuffIds.Add(buffId);
                        break;
                    case BuffCategories.Minion:
                        MinionBuffIds.Add(buffId);
                        break;
                    case BuffCategories.Sentry:
                        SentryBuffIds.Add(buffId);
                        break;
                }
            }

            SyncMountCategoriesToCache();
        }

        private static void SyncMountCategoriesToCache()
        {
            foreach (int buffId in BuffMountCategorySystem.MountBuffIds)
            {
                if (buffId <= 0 || buffId >= categoryByBuffId.Length)
                    continue;

                categoryByBuffId[buffId] = BuffCategories.Mount;
            }

            foreach (int buffId in BuffMountCategorySystem.MinecartBuffIds)
            {
                if (buffId <= 0 || buffId >= categoryByBuffId.Length)
                    continue;

                categoryByBuffId[buffId] = BuffCategories.Minecart;
            }
        }

        public static IEnumerable<int> EnumerateMiscExclusiveBuffIds()
        {
            foreach (int id in PetBuffIds)
                yield return id;

            foreach (int id in LightPetBuffIds)
                yield return id;

            foreach (int id in BuffMountCategorySystem.MountBuffIds)
                yield return id;

            foreach (int id in BuffMountCategorySystem.MinecartBuffIds)
                yield return id;
        }
    }
}
