using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.ItemHub.Rules
{
    /// <summary>ɸѡ��ť�������Ԥ����Ʒ type��</summary>
    internal static class HubTagPreviewIds
    {
        internal static int ForTag(string tag)
        {
            if (tag.StartsWith("ic.", System.StringComparison.Ordinal) &&
                HubCategoryDefinitions.TryGetPreviewItemId(tag, out int icId) &&
                icId > ItemID.None)
                return icId;

            if (tag.StartsWith("mod.", System.StringComparison.Ordinal))
            {
                string mk = tag.Substring(4);
                if (mk == "Terraria")
                    return ItemID.Gel;
                if (ModLoader.TryGetMod(mk, out Mod mod))
                {
                    foreach (ModItem mi in mod.GetContent<ModItem>())
                    {
                        if (mi?.Item != null && mi.Type > ItemID.None)
                            return mi.Type;
                    }
                }
            }

            return ItemID.CopperWatch;
        }
    }
}
