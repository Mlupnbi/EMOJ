using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.ItemHub.Data
{
    /// <summary>在物品中枢悬停未解锁物品时，在原生物品 Tooltip 末尾追加灰色说明。</summary>
    public class HubTooltipGlobal : GlobalItem
    {
        public static bool AppendNotOwnedLine;
        public static int AppendForItemType;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!AppendNotOwnedLine || item.type != AppendForItemType)
                return;
            string suffix = EOPJText.UI("ItemHubNotOwnedSuffix");
            var line = new TooltipLine(Mod, "ItemHubNotOwned", $"[{suffix}]")
            {
                OverrideColor = new Color(136, 136, 136)
            };
            tooltips.Add(line);
        }
    }
}
