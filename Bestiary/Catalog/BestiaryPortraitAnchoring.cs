using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    /// <summary>图鉴肖像锚点风格：原版脚底锚定，飞行/悬停需单独处理。</summary>
    internal enum BestiaryPortraitAnchorStyle
    {
        Ground,
        /// <summary>无重力、空中单位（恶魔眼等）；脚底锚点会整体偏高。</summary>
        Aerial,
    }

    internal static class BestiaryPortraitAnchoring
    {
        public static BestiaryPortraitAnchorStyle GetStyle(int netId)
        {
            if (netId <= 0 || !BestiaryPortraitMetrics.TryProbeNpc(netId, out NPC npc))
                return BestiaryPortraitAnchorStyle.Ground;

            if (npc.townNPC || npc.friendly)
                return BestiaryPortraitAnchorStyle.Ground;

            if (npc.noGravity)
                return BestiaryPortraitAnchorStyle.Aerial;

            switch (npc.aiStyle)
            {
                case NPCAIStyleID.Flying:
                case NPCAIStyleID.Bat:
                case NPCAIStyleID.HoveringFighter:
                case NPCAIStyleID.DungeonSpirit:
                case NPCAIStyleID.ElfCopter:
                case NPCAIStyleID.Firefly:
                    return BestiaryPortraitAnchorStyle.Aerial;
            }

            return BestiaryPortraitAnchorStyle.Ground;
        }

        /// <summary>原版 NPCBestiaryDrawOffset.Position 等（像素，已按格子高度缩放）。</summary>
        public static int GetModifierShiftY(int netId, float hostHeightPx)
        {
            if (netId <= 0 ||
                !NPCID.Sets.NPCBestiaryDrawOffset.TryGetValue(netId, out NPCID.Sets.NPCBestiaryDrawModifiers mods))
            {
                return 0;
            }

            float refH = 52f;
            float scale = hostHeightPx > 0f ? hostHeightPx / refH : 1f;
            float y = mods.Position.Y;
            if (mods.PortraitPositionYOverride is float oy)
                y += oy;

            return (int)Math.Round(y * scale);
        }
    }
}
