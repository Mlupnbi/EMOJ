using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Virtual
{
    /// <summary>������Чʱ�ų����ƻ��ƶ�/�����ˢ��������� NaN �� Buff��</summary>
    public static class BuffVirtualEffectSafety
    {
        private static readonly HashSet<int> DangerousBuffIds = new HashSet<int>();
        private static readonly HashSet<int> EmoteSpamBuffIds = new HashSet<int>();
        private static readonly HashSet<int> ManualOnlyBulkEnableIds = new HashSet<int>();
        private static readonly string[] MovementBlockNameTokens =
        {
            "Webbed", "Stoned", "Petrif", "Root", "Immobil", "Anchored", "Snare", "Gobbed"
        };

        private static readonly string[] EmoteSpamNameTokens = { "Panic", "Emote", "Emoji" };

        /// <summary>ÿ֡ Update ��ˢ����/�⻷������ scratch ʩ�ӻ�һ��һ��������ռ��ʵ��λ��</summary>
        private static readonly HashSet<int> ContinuousPhysicalBarBuffIds = new HashSet<int>();

        private static readonly string[] ContinuousVisualNameTokens =
        {
            "Inferno", "Hellfire", "Flame", "Fire", "Holy", "Cursed", "Frost", "Lightning",
            "Poison", "Venom", "Ichor", "Midnight", "Shine", "Sparkle", "Rainbow", "Coating",
            "Flask", "Aura", "Glow", "Burn", "Ember"
        };

        public static void Rebuild()
        {
            DangerousBuffIds.Clear();
            EmoteSpamBuffIds.Clear();
            ManualOnlyBulkEnableIds.Clear();
            ContinuousPhysicalBarBuffIds.Clear();

            AddVanillaContinuousPhysicalBar();
            AddVanillaDangerous();
            AddManualOnlyBulkEnable();

            for (int buffId = BuffID.Count; buffId < BuffLoader.BuffCount; buffId++)
            {
                ModBuff buff = BuffLoader.GetBuff(buffId);
                if (buff == null)
                    continue;

                string name = buff.Name ?? "";
                if (ContainsAnyToken(name, MovementBlockNameTokens))
                    DangerousBuffIds.Add(buffId);

                if (ContainsAnyToken(name, EmoteSpamNameTokens))
                    EmoteSpamBuffIds.Add(buffId);

                if (ContainsAnyToken(name, ContinuousVisualNameTokens))
                    ContinuousPhysicalBarBuffIds.Add(buffId);

                if (name.Equals("GoldenStasisBuff", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("BetsyDashBuff", StringComparison.OrdinalIgnoreCase))
                {
                    DangerousBuffIds.Add(buffId);
                    ManualOnlyBulkEnableIds.Add(buffId);
                }
            }
        }

        private static void AddManualOnlyBulkEnable()
        {
            if (BuffID.Gills > 0)
                ManualOnlyBulkEnableIds.Add(BuffID.Gills);

            for (int buffId = 1; buffId < BuffLoader.BuffCount; buffId++)
            {
                ModBuff buff = BuffLoader.GetBuff(buffId);
                if (buff == null)
                    continue;

                string name = buff.Name ?? "";
                if (name.Equals("Trippy", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("GoldenStasisBuff", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("BetsyDashBuff", StringComparison.OrdinalIgnoreCase))
                    ManualOnlyBulkEnableIds.Add(buffId);
            }
        }

        /// <summary>��ȫ�����������������ڸ������ֶ������</summary>
        public static bool IsManualOnlyBulkEnable(int buffId) => ManualOnlyBulkEnableIds.Contains(buffId);

        private static void AddVanillaDangerous()
        {
            int[] ids =
            {
                BuffID.Webbed,
                BuffID.Stoned,
                BuffID.Frozen,
                BuffID.Confused,
                BuffID.ChaosState,
                BuffID.VortexDebuff,
                BuffID.Suffocation,
                BuffID.Electrified,
                BuffID.Cursed,
                BuffID.NoBuilding,
                BuffID.Tipsy,
                BuffID.Obstructed,
                BuffID.PaladinsShield,
                BuffID.Honey
            };

            foreach (int id in ids)
            {
                if (id > 0)
                    DangerousBuffIds.Add(id);
            }
        }

        /// <summary>��Ҫ��������״̬�������������ÿ֡��ժ���ٹҡ������������Ч��˸��</summary>
        public static bool PrefersContinuousPhysicalBar(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            return ContinuousPhysicalBarBuffIds.Contains(buffId);
        }

        private static void TryAddContinuousPhysical(int buffId)
        {
            if (buffId > 0)
                ContinuousPhysicalBarBuffIds.Add(buffId);
        }

        private static void AddVanillaContinuousPhysicalBar()
        {
            TryAddContinuousPhysical(BuffID.Inferno);
            TryAddContinuousPhysical(BuffID.CursedInferno);
            TryAddContinuousPhysical(BuffID.Poisoned);
            TryAddContinuousPhysical(BuffID.Venom);
            TryAddContinuousPhysical(BuffID.Ichor);
            TryAddContinuousPhysical(BuffID.OnFire);
            TryAddContinuousPhysical(BuffID.Rabies);
            TryAddContinuousPhysical(BuffID.Midas);
            TryAddContinuousPhysical(BuffID.ManaSickness);

            if (BuffID.Search.TryGetId("HolyFlames", out int holyFlames))
                ContinuousPhysicalBarBuffIds.Add(holyFlames);
        }

        public static bool IsSafeForVirtualApply(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (!BuffPlayerApplicability.IsMeantForPlayer(buffId))
                return false;

            if (PrefersContinuousPhysicalBar(buffId))
                return false;

            if (DangerousBuffIds.Contains(buffId) || EmoteSpamBuffIds.Contains(buffId))
                return false;

            if (Main.debuff[buffId] && IsMovementOrControlDebuff(buffId))
                return false;

            return true;
        }

        public static bool BlocksPlayerMovement(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (DangerousBuffIds.Contains(buffId))
                return true;

            if (Main.debuff[buffId] && IsMovementOrControlDebuff(buffId))
                return true;

            return false;
        }

        private static bool IsMovementOrControlDebuff(int buffId)
        {
            return buffId == BuffID.Webbed ||
                   buffId == BuffID.Stoned ||
                   buffId == BuffID.Frozen ||
                   buffId == BuffID.Confused;
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
