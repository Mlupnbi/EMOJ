using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Catalog
{
    /// <summary>�ж� Buff �Ƿ�ɶԱ��������Ч���ų�����/NPC ר�õ������λ����?</summary>
    public static class BuffPlayerApplicability
    {
        private static readonly string[] NotForPlayerNameTokens =
        {
            "WhipNPC", "WhipEnemy", "EnemyDebuff", "NPCDebuff", "MinionBleed",
            "BallistaPanic", "DryadsWardDebuff", "TagBuff", "PerditusTag"
        };

        public static bool IsMeantForPlayer(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (IsNpcOnlyBuffByName(buffId))
                return false;

            return true;
        }

        public static bool IsNpcOnlyBuffByName(int buffId)
        {
            string name = buffId < BuffID.Count ? BuffID.Search.GetName(buffId) : BuffLoader.GetBuff(buffId)?.Name;
            if (string.IsNullOrEmpty(name))
                return false;

            return ContainsAnyToken(name, NotForPlayerNameTokens);
        }

        /// <summary>��Ӧд�� ActiveBuffs ��ˢ�µ�״̬����</summary>
        public static bool ShouldBlockManagedApplication(int buffId)
        {
            if (!IsMeantForPlayer(buffId))
                return true;

            if (BuffBeneficialDebuffFlagSystem.IsBeneficialDespiteDebuffFlag(buffId))
                return false;

            if (buffId > 0 && buffId < Main.debuff.Length && Main.debuff[buffId])
                return true;

            return false;
        }

        private static bool ContainsAnyToken(string name, string[] tokens)
        {
            foreach (string token in tokens)
            {
                if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
