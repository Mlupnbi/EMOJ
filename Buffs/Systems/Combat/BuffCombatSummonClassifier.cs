using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Combat
{
    /// <summary>���ؽ׶�ʶ���ʹ�/�ڱ� Buff�������� <see cref="BuffEntityIndexSystem"/> ������ɣ���</summary>
    public static class BuffCombatSummonClassifier
    {
        private static readonly string[] MinionBuffNameSuffixes =
        {
            "MinionBuff", "SummonBuff", "MinionDebuff", "SummonDebuff"
        };

        private static readonly string[] SentryBuffNameSuffixes =
        {
            "SentryBuff"
        };

        public static bool IsSentryBuff(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (BuffMountCategorySystem.TryResolveMountCategory(buffId, out _))
                return false;

            if (IsVanityOrLightPet(buffId))
                return false;

            string name = GetBuffName(buffId);
            if (!string.IsNullOrEmpty(name))
            {
                if (name.IndexOf("Sentry", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                foreach (string suffix in SentryBuffNameSuffixes)
                {
                    if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            if (BuffSummonProjectileHelper.BuffHasSentryItem(buffId))
                return true;

            return false;
        }

        public static bool IsMinionBuff(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (IsSentryBuff(buffId))
                return false;

            if (BuffMountCategorySystem.TryResolveMountCategory(buffId, out _))
                return false;

            if (IsVanityOrLightPet(buffId))
                return false;

            if (buffId < Main.debuff.Length && Main.debuff[buffId])
                return false;

            string name = GetBuffName(buffId);
            if (!string.IsNullOrEmpty(name))
            {
                foreach (string suffix in MinionBuffNameSuffixes)
                {
                    if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            ModBuff modBuff = BuffLoader.GetBuff(buffId);
            if (modBuff != null && BuffEntityIndexSystem.NamespaceIndicatesEntitySpawn(modBuff.GetType().Namespace ?? ""))
            {
                string ns = modBuff.GetType().Namespace ?? "";
                if (BuffEntityIndexSystem.NamespaceContainsSegment(ns, "Minions") ||
                    BuffEntityIndexSystem.NamespaceContainsSegment(ns, "Minion"))
                    return true;
            }

            foreach (var pair in ContentSamples.ItemsByType)
            {
                Item item = pair.Value;
                if (item == null || item.IsAir || item.buffType != buffId)
                    continue;

                if (BuffSummonProjectileHelper.ItemShootIsSentry(item))
                    continue;

                if (item.mountType != -1)
                    continue;

                if (item.DamageType.CountsAsClass(DamageClass.Summon) && item.shoot > ProjectileID.None)
                    return true;
            }

            foreach (var pair in ContentSamples.ItemsByType)
            {
                Item item = pair.Value;
                if (item == null || item.IsAir || item.shoot <= ProjectileID.None)
                    continue;

                if (!ContentSamples.ProjectilesByType.TryGetValue(item.shoot, out Projectile sample) || sample == null)
                    continue;

                if (!sample.minion || sample.sentry)
                    continue;

                if (item.buffType == buffId)
                    return true;
            }

            return false;
        }

        private static bool IsVanityOrLightPet(int buffId) =>
            buffId < Main.vanityPet.Length && Main.vanityPet[buffId] ||
            buffId < Main.lightPet.Length && Main.lightPet[buffId] ||
            BuffEntityIndexSystem.IsModPetNamespaceBuff(buffId);

        private static string GetBuffName(int buffId) =>
            buffId < BuffID.Count ? BuffID.Search.GetName(buffId) : BuffLoader.GetBuff(buffId)?.Name;
    }
}
