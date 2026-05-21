using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Catalog
{
    /// <summary>���� Buff ��ʾ������ Lang ���� BuffName.* ռλʱ���˵� tML �����������ġ�</summary>
    public static class BuffDisplayNameHelper
    {
        private static readonly Dictionary<string, string> VanillaZhFallback = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BlandWhipEnemyDebuff"] = "ƽƽ����",
            ["SwordWhipNPCDebuff"] = "����֮��",
            ["ScytheWhipEnemyDebuff"] = "�ո�֮��",
            ["ThornWhipNPCDebuff"] = "����֮��",
            ["RainbowWhipNPCDebuff"] = "�ʺ�֮��",
            ["CoolWhipNPCDebuff"] = "����֮��",
            ["TentacleSpike"] = "���",
            ["MaceWhipNPCDebuff"] = "���Ǵ�֮��",
            ["FlameWhipEnemyDebuff"] = "����֮��"
        };

        public static string GetDisplayName(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return $"#{buffId}";

            string raw = Lang.GetBuffName(buffId);
            if (IsResolvedName(raw))
                return raw;

            string internalName = GetInternalName(buffId);
            if (!string.IsNullOrEmpty(internalName))
            {
                string tml = buffId < BuffID.Count
                    ? Language.GetTextValue($"Mods.Terraria.Buffs.{internalName}.DisplayName")
                    : GetModBuffDisplayKey(buffId, internalName);

                if (IsResolvedName(tml))
                    return tml;

                if (VanillaZhFallback.TryGetValue(internalName, out string zh))
                    return zh;
            }

            if (!string.IsNullOrEmpty(raw) && raw.StartsWith("BuffName.", StringComparison.Ordinal))
                return raw.Substring("BuffName.".Length);

            return string.IsNullOrEmpty(internalName) ? $"#{buffId}" : internalName;
        }

        public static string GetDescription(int buffId)
        {
            string raw = Lang.GetBuffDescription(buffId);
            if (IsResolvedName(raw))
                return raw;

            string internalName = GetInternalName(buffId);
            if (string.IsNullOrEmpty(internalName))
                return raw ?? "";

            if (buffId < BuffID.Count)
            {
                string tml = Language.GetTextValue($"Mods.Terraria.Buffs.{internalName}.Description");
                if (IsResolvedName(tml))
                    return tml;
            }
            else
            {
                ModBuff mb = BuffLoader.GetBuff(buffId);
                if (mb != null)
                {
                    string tml = Language.GetTextValue($"Mods.{mb.Mod.Name}.Buffs.{mb.Name}.Description");
                    if (IsResolvedName(tml))
                        return tml;
                }
            }

            return raw ?? "";
        }

        private static string GetModBuffDisplayKey(int buffId, string internalName)
        {
            ModBuff mb = BuffLoader.GetBuff(buffId);
            return mb == null
                ? ""
                : Language.GetTextValue($"Mods.{mb.Mod.Name}.Buffs.{internalName}.DisplayName");
        }

        private static string GetInternalName(int buffId)
        {
            if (buffId < BuffID.Count)
                return BuffID.Search.GetName(buffId);

            return ModContent.GetModBuff(buffId)?.Name ?? "";
        }

        private static bool IsResolvedName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (text.StartsWith("BuffName.", StringComparison.Ordinal))
                return false;

            if (text.StartsWith("Mods.", StringComparison.Ordinal) && text.Contains(".DisplayName"))
                return false;

            return true;
        }
    }
}
