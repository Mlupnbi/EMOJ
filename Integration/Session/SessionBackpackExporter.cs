using System;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Integration.Session
{
    /// <summary>超级指令 DEBUG_OUTPUTBACKPACK：导出玩家背包物品到剪贴板。</summary>
    internal static class SessionBackpackExporter
    {
        /// <summary>热键栏 0–9 + 背包栏 10–49（不含钱币/弹药栏）。</summary>
        private const int ExportSlotEndExclusive = 50;

        public static bool TryExportToClipboard()
        {
            if (Main.dedServ)
            {
                Main.NewText("\u5bfc\u51fa\u80cc\u5305\u4ec5\u5728\u5ba2\u6237\u7aef\u53ef\u7528\u3002", Microsoft.Xna.Framework.Color.OrangeRed);
                return false;
            }

            Player plr = Main.LocalPlayer;
            if (plr == null)
                return false;

            string text = BuildExportText(plr);
            if (!GameClipboard.TrySetText(text))
            {
                Main.NewText("\u65e0\u6cd5\u5199\u5165\u526a\u8d34\u677f\uff08\u5f53\u524d\u5e73\u53f0\u53ef\u80fd\u4e0d\u652f\u6301\uff09\u3002", Microsoft.Xna.Framework.Color.OrangeRed);
                return false;
            }

            int count = CountExportedSlots(plr);
            Main.NewText($"\u5df2\u590d\u5236 {count} \u683c\u7269\u54c1\u5230\u526a\u8d34\u677f\uff08TSV\uff0c\u53ef\u7c98\u8d34\u5230 Excel\uff09", Microsoft.Xna.Framework.Color.LightGreen);
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
