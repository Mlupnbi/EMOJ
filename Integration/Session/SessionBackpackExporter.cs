using System;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Integration.Session
{
    /// <summary>����ָ�� DEBUG_OUTPUTBACKPACK��������ұ�������Ʒ�������塣</summary>
    internal static class SessionBackpackExporter
    {
        /// <summary>���� 0�C9 + ������ 10�C49������Ǯ��/��ҩ������</summary>
        private const int ExportSlotEndExclusive = 50;

        public static bool TryExportToClipboard()
        {
            if (Main.dedServ)
            {
                Main.NewText("�����������ڿͻ��˿��á�", Microsoft.Xna.Framework.Color.OrangeRed);
                return false;
            }

            Player plr = Main.LocalPlayer;
            if (plr == null)
                return false;

            string text = BuildExportText(plr);
            if (!GameClipboard.TrySetText(text))
            {
                Main.NewText("�޷�д������壨��ǰƽ̨���ܲ�֧�֣���", Microsoft.Xna.Framework.Color.OrangeRed);
                return false;
            }

            int count = CountExportedSlots(plr);
            Main.NewText($"�Ѹ��� {count} ����Ʒ�񵽼����壨TSV������ճ���� Excel��", Microsoft.Xna.Framework.Color.LightGreen);
            return true;
        }

        private static int CountExportedSlots(Player plr)
        {
            int n = 0;
            Item[] inv = plr.inventory;
            int end = Math.Min(ExportSlotEndExclusive, inv.Length);
            for (int i = 0; i < end; i++)
            {
                if (!inv[i].IsAir)
                    n++;
            }

            return n;
        }

        private static string BuildExportText(Player plr)
        {
            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine("# EvenMoreOverpoweredJourney DEBUG_OUTPUTBACKPACK");
            sb.AppendLine($"# Player: {plr.name}");
            sb.AppendLine($"# Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("# Columns: Slot\\tRegion\\tTypeID\\tMod\\tInternalName\\tDisplayName\\tStack");
            sb.AppendLine("Slot\tRegion\tTypeID\tMod\tInternalName\tDisplayName\tStack");

            Item[] inv = plr.inventory;
            int end = Math.Min(ExportSlotEndExclusive, inv.Length);
            for (int i = 0; i < end; i++)
            {
                Item it = inv[i];
                if (it == null || it.IsAir)
                    continue;

                int type = it.type;
                ResolveItemIdentity(type, it, out string modKey, out string internalName, out string displayName);
                string region = i < 10 ? "Hotbar" : "Inventory";
                sb.Append(i);
                sb.Append('\t');
                sb.Append(region);
                sb.Append('\t');
                sb.Append(type);
                sb.Append('\t');
                sb.Append(modKey);
                sb.Append('\t');
                sb.Append(internalName);
                sb.Append('\t');
                sb.Append(displayName);
                sb.Append('\t');
                sb.AppendLine(it.stack.ToString());
            }

            return sb.ToString();
        }

        internal static void ResolveItemIdentity(int type, Item it, out string modKey, out string internalName, out string displayName)
        {
            ModItem mi = ItemLoader.GetItem(type);
            if (mi != null)
            {
                modKey = mi.Mod.Name;
                internalName = mi.Name;
                displayName = it.HoverName;
                return;
            }

            modKey = "Terraria";
            internalName = ItemID.Search.GetName(type) ?? $"Item_{type}";
            displayName = it.HoverName;
        }
    }
}
