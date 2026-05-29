using System.Text;
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
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Integration.Session
{
    internal static class VanillaBuffCatalogExporter
    {
        public static bool TryExportToClipboard()
        {
            if (Main.dedServ)
            {
                Main.NewText("\u4ec5\u5ba2\u6237\u7aef\u53ef\u7528\u3002", Color.OrangeRed);
                return false;
            }

            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine("# EMOJ Vanilla Buff Catalog");
            sb.AppendLine($"# Total: {VanillaBuffCatalogSystem.CatalogCount}");
            sb.AppendLine("BuffId\tKey\tName\tMode\tDebuff");

            for (int id = 1; id < BuffID.Count; id++)
            {
                if (!VanillaBuffCatalogSystem.TryGetMode(id, out VanillaBuffSupportMode mode))
                    continue;

                string name = BuffID.Search.GetName(id) ?? "";
                sb.Append(id);
                sb.Append('\t');
                sb.Append(BuffStableKey.ToKey(id));
                sb.Append('\t');
                sb.Append(name);
                sb.Append('\t');
                sb.Append(mode);
                sb.Append('\t');
                sb.Append(id < Main.debuff.Length && Main.debuff[id] ? "Y" : "N");
                sb.AppendLine();
            }

            if (!GameClipboard.TrySetText(sb.ToString()))
            {
                Main.NewText("\u65e0\u6cd5\u5199\u5165\u526a\u8d34\u677f\u3002", Color.OrangeRed);
                return false;
            }

            Main.NewText($"\u5df2\u590d\u5236 {VanillaBuffCatalogSystem.CatalogCount} \u6761\u539f\u7248 Buff \u76ee\u5f55\u5230\u526a\u8d34\u677f\u3002", Color.LightGreen);
            return true;
        }
    }
}
