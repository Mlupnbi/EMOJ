using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport
{
    /// <summary>
    /// ԭ�泣���������ֱֵд������ ModBuff.Update�������� EMOJ �й��б��е� Terraria Buff��
    /// ��ɨ���������� overrides.json��ģ����ࣩ������
    /// </summary>
    public static class VanillaBuffStatRegistry
    {
        private static readonly Dictionary<int, Action<Player>> ApplyByBuffId = new();

        public static int RegisteredCount => ApplyByBuffId.Count;

        static VanillaBuffStatRegistry() => RegisterKnownVanillaBuffs();

        public static bool IsSyntheticStatBuff(int buffId) =>
            buffId > 0 && buffId < BuffID.Count && ApplyByBuffId.ContainsKey(buffId);

        public static void ApplyActiveSyntheticStats(Player player, BuffResearchPlayer mp)
        {
            if (player == null || mp == null || !OPJourneyConfig.UseVanillaSyntheticStats())
                return;

            foreach (int buffId in mp.ActiveBuffs)
            {
                if (mp.DisabledBuffs.Contains(buffId))
                    continue;

                if (ApplyByBuffId.TryGetValue(buffId, out Action<Player> apply))
                    apply(player);
            }
        }

        private static void Register(int buffId, Action<Player> apply)
        {
            if (buffId > 0)
                ApplyByBuffId[buffId] = apply;
        }

        private static void RegisterKnownVanillaBuffs()
        {
            Register(BuffID.Ironskin, p => p.statDefense += 8);
            Register(BuffID.Regeneration, p => p.lifeRegen += 2);
            Register(BuffID.Swiftness, p => p.moveSpeed += 0.25f);
            Register(BuffID.Archery, p =>
            {
                p.GetDamage(DamageClass.Ranged) += 0.2f;
                p.GetCritChance(DamageClass.Ranged) += 0.1f;
            });
            Register(BuffID.WellFed, p =>
            {
                p.GetDamage(DamageClass.Generic) += 0.05f;
                p.GetCritChance(DamageClass.Generic) += 0.02f;
                p.statDefense += 2;
            });
            Register(BuffID.WellFed2, p =>
            {
                p.GetDamage(DamageClass.Generic) += 0.1f;
                p.GetCritChance(DamageClass.Generic) += 0.04f;
                p.statDefense += 4;
            });
            Register(BuffID.WellFed3, p =>
            {
                p.GetDamage(DamageClass.Generic) += 0.15f;
                p.GetCritChance(DamageClass.Generic) += 0.06f;
                p.statDefense += 6;
                p.statLifeMax2 += 10;
                p.statManaMax2 += 10;
            });
            Register(BuffID.Lifeforce, p => p.statLifeMax2 += 100);
            Register(BuffID.Endurance, p => p.endurance = 1f - (1f - p.endurance) * 0.9f);
            Register(BuffID.Rage, p => p.GetCritChance(DamageClass.Generic) += 0.1f);
            Register(BuffID.Wrath, p => p.GetDamage(DamageClass.Generic) += 0.1f);
            Register(BuffID.Thorns, p => p.thorns = 1f);
            Register(BuffID.MagicPower, p => p.GetDamage(DamageClass.Magic) += 0.2f);
            Register(BuffID.ManaRegeneration, p => p.manaRegen += 2);
            Register(BuffID.Summoning, p => p.maxMinions += 1);
            Register(BuffID.Lucky, p => p.luck += 0.1f);
            Register(BuffID.Builder, p =>
            {
                p.tileSpeed += 0.25f;
                p.wallSpeed += 0.25f;
            });
            Register(BuffID.Mining, p => p.pickSpeed -= 0.25f);
            Register(BuffID.ObsidianSkin, p =>
            {
                p.lavaImmune = true;
                p.fireWalk = true;
            });
            // ����/ҩˮ�Ӿ��������ʳ�����ƵȲ�ע�ᣨ������ʵ��λ��ʵ���߼���
        }
    }
}
