using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Catalog
{
    /// <summary>
    /// 原版将部分环境增益标为 <see cref="Main.debuff"/>（如无计时显示的旗帜、快乐！等），
    /// EMOJ 按实际效果归入增益，而非减益页签。
    /// </summary>
    public static class BuffBeneficialDebuffFlagSystem
    {
        private static readonly string[] DebuffNameTokens =
        {
            "Debuff", "NPCDebuff", "EnemyDebuff", "WhipNPC", "WhipEnemy"
        };

        private static readonly string[] AmbientPositiveNameTokens =
        {
            "Campfire", "HeartLamp", "StarInBottle", "PeaceCandle", "ShadowCandle",
            "WaterCandle", "DryadsWard", "Honey", "Sunflower", "MonsterBanner", "Banner"
        };

        /// <summary>该 Buff 被标为 debuff，但实际为玩家增益（环境/旗帜等）。</summary>
        public static bool IsBeneficialDespiteDebuffFlag(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (buffId >= Main.debuff.Length || !Main.debuff[buffId])
                return false;

            return MatchesKnownBeneficial(buffId);
        }

        public static bool MatchesKnownBeneficial(int buffId)
        {
            switch (buffId)
            {
                case BuffID.MonsterBanner:
                case BuffID.Sunflower:
                case BuffID.Campfire:
                case BuffID.HeartLamp:
                case BuffID.StarInBottle:
                case BuffID.PeaceCandle:
                case BuffID.ShadowCandle:
                case BuffID.WaterCandle:
                case BuffID.Honey:
                case BuffID.DryadsWard:
                    return true;
            }

            string name = buffId < BuffID.Count
                ? BuffID.Search.GetName(buffId) ?? ""
                : BuffLoader.GetBuff(buffId)?.Name ?? "";

            if (string.IsNullOrEmpty(name))
                return false;

            if (ContainsAnyToken(name, DebuffNameTokens))
                return false;

            if (name.Contains("Banner", StringComparison.OrdinalIgnoreCase))
                return true;

            return ContainsAnyToken(name, AmbientPositiveNameTokens);
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
