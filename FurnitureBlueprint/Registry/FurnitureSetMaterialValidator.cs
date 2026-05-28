using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Registry
{
    /// <summary>
    /// 基于 <see cref="FurnitureSetMaterialCheckers"/> 的背包材料校验（Phase 2 放置路径专用，不接入识别）。
    /// </summary>
    public static class FurnitureSetMaterialValidator
    {
        public static int CountOwnedSlots(Player player, FurnitureScheme scheme)
        {
            if (player == null || scheme?.SlotItemTypes == null)
                return 0;

            int owned = 0;
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                int required = scheme.SlotItemTypes[i];
                if (required <= ItemID.None)
                    continue;

                if (PlayerHasItemForSlot(player, i, required))
                    owned++;
            }

            return owned;
        }

        public static bool PlayerHasItemForSlot(Player player, int slotIndex, int requiredItemType)
        {
            if (player == null || slotIndex < 0 || slotIndex >= FurnitureSlotKinds.Count)
                return false;

            if (requiredItemType <= ItemID.None)
                return true;

            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item it = player.inventory[i];
                if (it == null || it.IsAir || it.type != requiredItemType)
                    continue;

                if (FurnitureSetMaterialCheckers.ItemMatchesSlotIndex(it, slotIndex))
                    return true;
            }

            return false;
        }
    }
}
