using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    /// <summary>????????????????????</summary>
    internal static class BestiaryPortraitMetrics
    {
        public static void GetPortraitDimensions(int netId, out int width, out int height)
        {
            width = 40;
            height = 56;
            if (netId <= 0)
                return;

            float scale = 1f;
            if (NPCID.Sets.NPCBestiaryDrawOffset.TryGetValue(netId, out NPCID.Sets.NPCBestiaryDrawModifiers modifiers) &&
                modifiers.PortraitScale is float portraitScale && portraitScale > 0f)
            {
                scale = portraitScale;
            }

            if (TryProbeNpc(netId, out NPC probe))
            {
                if (probe.height > 0)
                    height = probe.height;
                if (probe.width > 0)
                    width = probe.width;
            }

            width = Math.Max(8, (int)(width * scale));
            height = Math.Max(8, (int)(height * scale));
        }

        public static bool TryProbeNpc(int netId, out NPC npc)
        {
            npc = null;
            if (netId <= 0 || netId >= NPCLoader.NPCCount || NPCLoader.GetNPC(netId) == null)
                return false;

            try
            {
                npc = new NPC();
                npc.SetDefaults(netId);
                return true;
            }
            catch
            {
                npc = null;
                return false;
            }
        }
    }
}
