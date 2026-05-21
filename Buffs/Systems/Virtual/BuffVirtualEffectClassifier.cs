using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Virtual
{
    /// <summary>
    /// Ϊÿ�� Buff �ж�����ʩ�ӽ׶Σ����� + �������ǣ�������/���� Mod ʱ�ؽ���δ֪ Buff Ĭ�� <see cref="BuffVirtualEffectPhase.Stat"/>�������ԣ���
    /// </summary>
    public static class BuffVirtualEffectClassifier
    {
        private static BuffVirtualEffectPhase[] _phaseByBuffId = Array.Empty<BuffVirtualEffectPhase>();

        private static readonly Dictionary<int, BuffVirtualEffectPhase> ManualOverrides = new Dictionary<int, BuffVirtualEffectPhase>();

        private static readonly string[] VisualNameTokens =
        {
            "Inferno", "Hellfire", "HolyFlame", "CursedFlame", "Flame", "Fire", "Burn", "Ember",
            "Frost", "Frozen", "Lightning", "Thunder", "Spark", "Shine", "Glow", "Aura", "Rainbow",
            "Coating", "Flask", "Vial", "Splash", "Trail", "Ring", "Pulse", "Beam", "Slash"
        };

        private static readonly string[] StatNameTokens =
        {
            "Damage", "Strength", "Might", "Power", "Fury", "Rage", "Wrath", "Endurance", "Force",
            "Defense", "Ward", "Shell", "Regen", "Life", "Health", "Mana", "Magic", "Crit",
            "Speed", "Haste", "Brisk", "Soar", "Flight", "Luck", "Cell", "Banner", "WellFed"
        };

        public static void Rebuild()
        {
            int count = Math.Max(BuffLoader.BuffCount, 1);
            if (_phaseByBuffId.Length != count)
                _phaseByBuffId = new BuffVirtualEffectPhase[count];
            else
                Array.Fill(_phaseByBuffId, BuffVirtualEffectPhase.Stat);

            for (int buffId = 1; buffId < BuffLoader.BuffCount; buffId++)
            {
                if (ManualOverrides.TryGetValue(buffId, out BuffVirtualEffectPhase manual))
                {
                    _phaseByBuffId[buffId] = manual;
                    continue;
                }

                if (BuffModSupportLoader.TryGetOverride(buffId, out BuffVirtualEffectPhase fromJson))
                {
                    _phaseByBuffId[buffId] = fromJson;
                    continue;
                }

                _phaseByBuffId[buffId] = ClassifyHeuristic(buffId);
            }
        }

        public static BuffVirtualEffectPhase GetPhase(int buffId)
        {
            if (buffId <= 0)
                return BuffVirtualEffectPhase.Stat;

            if (buffId < _phaseByBuffId.Length)
                return _phaseByBuffId[buffId];

            return ClassifyHeuristic(buffId);
        }

        private static BuffVirtualEffectPhase ClassifyHeuristic(int buffId)
        {
            if (BuffEntityIndexSystem.IsEntitySpawningBuff(buffId))
                return BuffVirtualEffectPhase.CombatVisual;

            ModBuff modBuff = BuffLoader.GetBuff(buffId);
            string name = modBuff?.Name ?? string.Empty;

            if (BuffEntityIndexSystem.ModBuffNamespaceIndicatesEntitySpawn(modBuff))
                return BuffVirtualEffectPhase.CombatVisual;

            bool visual = ContainsAnyToken(name, VisualNameTokens);
            bool stat = ContainsAnyToken(name, StatNameTokens);

            if (visual && !stat)
                return BuffVirtualEffectPhase.CombatVisual;

            string category = BuffPage.GetBuffCategoryStatic(buffId);
            if (BuffCategories.IsVirtualizablePositiveSubCategory(category))
            {
                if (visual && stat)
                    return BuffVirtualEffectPhase.Stat;

                if (!visual)
                    return BuffVirtualEffectPhase.Stat;
            }

            if (category == BuffCategories.Positive && !visual)
                return BuffVirtualEffectPhase.Stat;

            if (visual)
                return BuffVirtualEffectPhase.CombatVisual;

            return BuffVirtualEffectPhase.Stat;
        }

        private static bool ContainsAnyToken(string name, string[] tokens)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            foreach (string token in tokens)
            {
                if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
