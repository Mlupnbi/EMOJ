using System.Collections.Generic;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>已占槽 / 材料块 / 种子：不参与后续空槽占位竞争。</summary>
    public static class FurnitureSchemeOccupancy
    {
        public static HashSet<int> CollectOccupied(FurnitureScheme scheme, int materialBlock, int seedType)
        {
            var occupied = new HashSet<int>();
            if (scheme == null)
                return occupied;

            for (int i = 0; i < scheme.SlotItemTypes.Length; i++)
            {
                int t = scheme.SlotItemTypes[i];
                if (t > ItemID.None)
                    occupied.Add(t);
            }

            if (materialBlock > ItemID.None)
                occupied.Add(materialBlock);

            if (seedType > ItemID.None && seedType != materialBlock)
                occupied.Add(seedType);

            return occupied;
        }

        public static void MarkUsed(HashSet<int> occupied, int itemType)
        {
            if (occupied != null && itemType > ItemID.None)
                occupied.Add(itemType);
        }
    }
}
