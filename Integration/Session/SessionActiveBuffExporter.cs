using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Utilities;
using EvenMoreOverpoweredJourney.Buffs.Systems.Catalog;
using EvenMoreOverpoweredJourney.Buffs.Systems.Virtual;
using EvenMoreOverpoweredJourney.Buffs.Systems.Managed;
using EvenMoreOverpoweredJourney.Buffs.Systems.Combat;
using EvenMoreOverpoweredJourney.Buffs.Systems.Spawning;
using EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus;
using EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport;
using EvenMoreOverpoweredJourney.Buffs.Systems.FedState;
using EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics;
using EvenMoreOverpoweredJourney.Buffs.Systems.Display;

namespace EvenMoreOverpoweredJourney.Integration.Session
{
    /// <summary>����ָ�� DEBUG_OUTPUTBUFFS��������ǰ��Ч Buff �������塣</summary>
    internal static class SessionActiveBuffExporter
    {
        public static bool TryExportToClipboard()
        {
            if (Main.dedServ)
            {
                Main.NewText("Buff �������ڿͻ��˿��á�", Color.OrangeRed);
                return false;
            }

            Player plr = Main.LocalPlayer;
            if (plr == null)
                return false;

            string text = BuildExportText(plr);
            if (!GameClipboard.TrySetText(text))
            {
                Main.NewText("�޷�д�������?", Color.OrangeRed);
                return false;
            }

            int count = CountRows(text);
            Main.NewText($"�Ѹ��� {count} ����Ч Buff �������壨TSV����", Color.LightGreen);
            return true;
        }

        private static int CountRows(string text)
        {
            int n = 0;
            foreach (string line in text.Split('\n'))
            {
                if (line.Length > 0 && !line.StartsWith("#") && !line.StartsWith("BuffId", StringComparison.Ordinal))
                    n++;
            }

            return n;
        }

        private static string BuildExportText(Player plr)
        {
            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine("# EvenMoreOverpoweredJourney DEBUG_OUTPUTBUFFS");
            sb.AppendLine($"# Player: {plr.name}");
            sb.AppendLine($"# Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("# Columns: BuffId\\tMod\\tInternalName\\tDisplayName\\tSource");
            sb.AppendLine("BuffId\tMod\tInternalName\tDisplayName\tSource");

            var seen = new HashSet<int>();
            var mp = plr.GetModPlayer<BuffResearchPlayer>();

            for (int i = 0; i < plr.buffType.Length; i++)
            {
                int type = plr.buffType[i];
                if (type <= 0 || plr.buffTime[i] <= 0 || !seen.Add(type))
                    continue;

                AppendRow(sb, type, "PlayerBar");
            }

            foreach (int buffId in mp.ActiveBuffs)
            {
                if (buffId <= 0 || mp.DisabledBuffs.Contains(buffId) || !seen.Add(buffId))
                    continue;

                AppendRow(sb, buffId, "EMOJ_ActiveBuffs");
            }

            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, int buffId, string source)
        {
            ResolveBuffIdentity(buffId, out string modKey, out string internalName, out string displayName);
            sb.Append(buffId);
            sb.Append('\t');
            sb.Append(modKey);
            sb.Append('\t');
            sb.Append(internalName);
            sb.Append('\t');
            sb.Append(displayName);
            sb.Append('\t');
            sb.AppendLine(source);
        }

        private static void ResolveBuffIdentity(int buffId, out string modKey, out string internalName, out string displayName)
        {
            ModBuff mb = BuffLoader.GetBuff(buffId);
            if (mb != null)
            {
                modKey = mb.Mod.Name;
                internalName = mb.Name;
                displayName = BuffDisplayNameHelper.GetDisplayName(buffId);
                return;
            }

            modKey = "Terraria";
            internalName = buffId < BuffID.Count ? BuffID.Search.GetName(buffId) ?? $"Buff_{buffId}" : $"Buff_{buffId}";
            displayName = BuffDisplayNameHelper.GetDisplayName(buffId);
        }
    }
}
