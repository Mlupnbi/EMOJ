using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Integration.Session
{
    /// <summary>超级指令 DEBUG_COMPAREDRAGONLENS：对比 DragonLens 物品浏览器与物品中心注册表。</summary>
    internal static class SessionBrowserCatalogCompare
    {
        public static bool TryCompareToClipboard()
        {
            if (Main.dedServ)
            {
                Main.NewText("\u5bf9\u6bd4\u4ec5\u5728\u5ba2\u6237\u7aef\u53ef\u7528\u3002", Microsoft.Xna.Framework.Color.OrangeRed);
                return false;
            }

            HubCatalog.EnsureBuilt();
            HubClassificationIndex.EnsureBuilt();
            if (!HubCatalog.Ready)
            {
                Main.NewText("\u7269\u54c1\u4e2d\u5fc3\u76ee\u5f55\u5c1a\u672a\u5c31\u7eea\u3002", Microsoft.Xna.Framework.Color.OrangeRed);
                return false;
            }

            HashSet<int> hub = new HashSet<int>(HubCatalog.AllTypes);
            HashSet<int> dlFull = ExternalBrowserCatalog.BuildFullGridTypes();

            bool hasVisible = ExternalBrowserReflection.TryGetVisibleItemTypes(
                out HashSet<int> dlVisible, out string visibleNote);

            string text = BuildReport(hub, dlFull, hasVisible ? dlVisible : null, visibleNote);
            if (!GameClipboard.TrySetText(text))
            {
                Main.NewText("\u65e0\u6cd5\u5199\u5165\u526a\u8d34\u677f\u3002", Microsoft.Xna.Framework.Color.OrangeRed);
                return false;
            }

            int fullMissing = CountSectionLines(text, "DL_FULL__HUB_MISSING");
            int visMissing = hasVisible ? CountSectionLines(text, "DL_VISIBLE__HUB_MISSING") : 0;
            bool regAligned = fullMissing == 0 && hub.Count == dlFull.Count;
            Main.NewText(
                regAligned
                    ? (hasVisible && visMissing == 0
                        ? "\u767b\u8bb0\u8868\u4e0e DL \u5168\u91cf\u4e00\u81f4\uff1b\u5f53\u524d\u53ef\u89c1\u96c6\u4e5f\u4e00\u81f4\u3002\u82e5 UI \u4ecd\u7f3a\u9879\uff0c\u67e5\u7b5b\u9009/\u6807\u7b7e\u3002"
                        : $"\u767b\u8bb0\u8868\u4e0e DL \u5168\u91cf\u4e00\u81f4\uff08{hub.Count} \u9879\uff09\uff1b\u53ef\u89c1 UI \u4ecd\u6709\u7f3a\u9879\uff08\u89c1\u526a\u8d34\u677f\uff09\uff1a{visibleNote}")
                    : (hasVisible
                        ? $"\u5df2\u590d\u5236\uff1a\u5168\u91cf\u7f3a {fullMissing}\uff0c\u53ef\u89c1\u7f3a {visMissing}"
                        : $"\u5df2\u590d\u5236\uff1a\u5168\u91cf\u7f3a {fullMissing}\uff0c{visibleNote}"),
                regAligned && visMissing == 0
                    ? Microsoft.Xna.Framework.Color.LightGreen
                    : Microsoft.Xna.Framework.Color.Orange);
            return true;
        }

        private static int CountSectionLines(string text, string marker)
        {
            int idx = text.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0)
                return 0;
            int n = 0;
            int start = idx;
            while (start < text.Length)
            {
                int lineEnd = text.IndexOf('\n', start);
                if (lineEnd < 0)
                    lineEnd = text.Length;
                string line = text.Substring(start, lineEnd - start).TrimEnd('\r');
                start = lineEnd + 1;
                if (line.Length == 0)
                    break;
                if (line.StartsWith("##", StringComparison.Ordinal) || line.StartsWith("# ===", StringComparison.Ordinal))
                    break;
                if (line[0] == '#' || line.StartsWith("TypeID", StringComparison.Ordinal))
                    continue;
                n++;
            }

            return n;
        }

        private static string BuildReport(HashSet<int> hub, HashSet<int> dlFull, HashSet<int> dlVisible, string visibleNote)
        {
            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine("# EvenMoreOverpoweredJourney DEBUG_COMPAREDRAGONLENS");
            sb.AppendLine($"# Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# DragonLens full grid types: {dlFull.Count}");
            sb.AppendLine($"# ItemHub registered types: {hub.Count}");
            sb.AppendLine("# Algorithm: DL full = ItemBrowser.PopulateGrid (type 1..ItemCount-1, Deprecated cleared)");
            sb.AppendLine("# Hub registry uses the same loop; differences past this report are UI/filter/tag layers.");
            sb.AppendLine();

            if (dlVisible != null)
            {
                sb.AppendLine("# === DL_VISIBLE__HUB_MISSING (DragonLens visible but not registered in Hub) ===");
                sb.AppendLine($"# {visibleNote}");
                AppendDiffSection(sb, dlVisible, hub, "DL_VISIBLE__HUB_MISSING");
                sb.AppendLine();

                HashSet<int> hubOnlyInView = new HashSet<int>(hub);
                hubOnlyInView.IntersectWith(dlVisible);
                int extraInHub = hubOnlyInView.Count;
                int missingInHub = 0;
                foreach (int t in dlVisible)
                {
                    if (!hub.Contains(t))
                        missingInHub++;
                }

                sb.AppendLine($"# Visible summary: DL={dlVisible.Count} HubInDL={extraInHub} missing_in_hub={missingInHub}");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"# DragonLens window: skipped ({visibleNote})");
                sb.AppendLine();
            }

            sb.AppendLine("# === DL_FULL__HUB_MISSING (DL full grid but not registered in Hub) ===");
            AppendDiffSection(sb, dlFull, hub, "DL_FULL__HUB_MISSING");
            sb.AppendLine();

            sb.AppendLine("# === HUB_ONLY (Hub has type not in DL full 1..ItemCount-1; usually should not happen) ===");
            AppendHubOnlySection(sb, hub, dlFull);
            sb.AppendLine();

            sb.AppendLine("# === Per-mod summary (full grid) ===");
            AppendModSummary(sb, dlFull, hub);
            sb.AppendLine();

            if (hub.SetEquals(dlFull))
                AppendFilterLayerDiagnosis(sb, hub);

            return sb.ToString();
        }

        /// <summary>登记表与 DL 一致时，提示应查筛选/分类而非注册层。</summary>
        private static void AppendFilterLayerDiagnosis(StringBuilder sb, HashSet<int> hub)
        {
            sb.AppendLine("# === REGISTRATION_ALIGNED — check filter/classification if UI still differs ===");
            sb.AppendLine("# DragonLens: grid = all types; visibility = search + FilterPanel (OR per category).");
            sb.AppendLine("# ItemHub: list = AllTypes minus DebugItem (unless misc.debug), minus ActiveTags AND-groups, rare slider, chain.");
            sb.AppendLine("# Compare: https://github.com/ScalarVector1/DragonLens/blob/master/Content/Tools/Spawners/ItemSpawner.cs");
            sb.AppendLine("#         Issues/ITEMLOGIC.md + ItemHubTagPredicates / ItemHubExtDataBuilder");
            sb.AppendLine();

            int debug = 0;
            int residual = 0;
            foreach (int t in hub)
            {
                if (t <= ItemID.None || t >= HubClassificationIndex.ExtByType.Length)
                    continue;
                if (HubClassificationIndex.ExtByType[t].DebugItem)
                    debug++;
                if (HubClassificationIndex.ExtByType[t].HubMiscCatalogResidual)
                    residual++;
            }

            sb.AppendLine("Metric\tCount");
            sb.AppendLine($"Hub_AllTypes\t{hub.Count}");
            sb.AppendLine($"Hub_DebugItem_hidden_without_misc.debug\t{debug}");
            sb.AppendLine($"Hub_HubMiscCatalog_residual_only_misc.other\t{residual}");
            sb.AppendLine();
            sb.AppendLine("# If DL mod filter shows item but hub mod.* does not: tag/classification bug, not registry.");
            sb.AppendLine("# If both show item but hub list empty: ActiveTags AND across mod/tile/wpn groups or rare/chain.");
        }

        private static void AppendDiffSection(StringBuilder sb, HashSet<int> dlSet, HashSet<int> hub, string _)
        {
            sb.AppendLine("TypeID\tMod\tInternalName\tHubRegistered");
            List<int> missing = dlSet.Where(t => !hub.Contains(t)).OrderBy(t => t).ToList();
            Item probe = new Item();
            foreach (int type in missing)
            {
                ResolveIdentity(type, probe, out string modKey, out string internalName);
                sb.Append(type);
                sb.Append('\t');
                sb.Append(modKey);
                sb.Append('\t');
                sb.Append(internalName);
                sb.AppendLine("\tfalse");
            }
        }

        private static void AppendHubOnlySection(StringBuilder sb, HashSet<int> hub, HashSet<int> dlFull)
        {
            sb.AppendLine("TypeID\tMod\tInternalName");
            Item probe = new Item();
            foreach (int type in hub.Where(t => !dlFull.Contains(t)).OrderBy(t => t))
            {
                ResolveIdentity(type, probe, out string modKey, out string internalName);
                sb.Append(type);
                sb.Append('\t');
                sb.Append(modKey);
                sb.Append('\t');
                sb.AppendLine(internalName);
            }
        }

        private static void AppendModSummary(StringBuilder sb, HashSet<int> dlFull, HashSet<int> hub)
        {
            sb.AppendLine("Mod\tDL_Full\tHub\tMissingInHub");
            Item probe = new Item();
            Dictionary<string, (int dl, int hub, int miss)> perMod = new Dictionary<string, (int, int, int)>();

            foreach (int type in dlFull)
            {
                ResolveIdentity(type, probe, out string modKey, out _);
                if (!perMod.TryGetValue(modKey, out var c))
                    c = (0, 0, 0);
                c.dl++;
                if (hub.Contains(type))
                    c.hub++;
                else
                    c.miss++;
                perMod[modKey] = c;
            }

            foreach (string mod in perMod.Keys.OrderBy(m => m))
            {
                (int dl, int h, int miss) = perMod[mod];
                sb.Append(mod);
                sb.Append('\t');
                sb.Append(dl);
                sb.Append('\t');
                sb.Append(h);
                sb.Append('\t');
                sb.AppendLine(miss.ToString());
            }
        }

        private static void ResolveIdentity(int type, Item probe, out string modKey, out string internalName)
        {
            ModItem mi = ItemLoader.GetItem(type);
            if (mi != null)
            {
                modKey = mi.Mod.Name;
                internalName = mi.Name;
                return;
            }

            modKey = "Terraria";
            internalName = ItemID.Search.GetName(type) ?? $"Item_{type}";
        }
    }
}
