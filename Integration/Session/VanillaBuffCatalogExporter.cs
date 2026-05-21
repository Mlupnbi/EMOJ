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
                Main.NewText("���ͻ��˿��á�", Color.OrangeRed);
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
                Main.NewText("�޷�д�������?", Color.OrangeRed);
                return false;
            }

            Main.NewText($"�Ѹ��� {VanillaBuffCatalogSystem.CatalogCount} ��ԭ�� Buff Ŀ¼�������塣", Color.LightGreen);
            return true;
        }
    }
}
