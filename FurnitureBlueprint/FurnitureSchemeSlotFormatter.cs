using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>wiki 22 槽识别结果：type + 内部英文名 + 当前语言显示名。</summary>
    public static class FurnitureSchemeSlotFormatter
    {
        public static string FormatCompact(FurnitureScheme scheme)
        {
            if (scheme == null)
                return "(null)";

            var parts = new List<string>(FurnitureWikiSlots.TotalCount);
            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                int type = scheme.GetSlot(kind);
                parts.Add($"{kind}={FormatItemTriple(type)}");
            }

            return string.Join(";", parts);
        }

        public static string FormatMultiline(int seedType, FurnitureScheme scheme)
        {
            var sb = new StringBuilder();
            sb.Append($"batch-test slots seed={seedType}");
            if (scheme == null)
            {
                sb.Append(" (null scheme)");
                return sb.ToString();
            }

            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                int type = scheme.GetSlot(kind);
                sb.AppendLine();
                sb.Append("  ");
                sb.Append(kind);
                sb.Append('=');
                sb.Append(FormatItemTriple(type));
            }

            return sb.ToString();
        }

        public static string FormatItemTriple(int type)
        {
            if (type <= ItemID.None)
                return "0||";

            ResolveIdentity(type, out string internalName, out string displayName);
            return $"{type}|{Sanitize(internalName)}|{Sanitize(displayName)}";
        }

        public static string GetInternalEnglishName(int type)
        {
            if (type <= ItemID.None)
                return string.Empty;

            ModItem modItem = ItemLoader.GetItem(type);
            if (modItem != null)
                return modItem.Name ?? string.Empty;

            return ItemID.Search.GetName(type) ?? $"Item_{type}";
        }

        public static void ResolveIdentity(int type, out string internalName, out string displayName)
        {
            internalName = "";
            displayName = "";

            if (type <= ItemID.None)
                return;

            internalName = GetInternalEnglishName(type);

            Item item = new Item();
            if (FurnitureItemDefaults.TrySetDefaults(item, type))
                displayName = item.Name ?? "";
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value
                .Replace("|", "/")
                .Replace(";", ",")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }
    }
}
