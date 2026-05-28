using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>?????/?????UTF-8?????????? FurnitureBuildingBlockRules?</summary>
    internal static class FurnitureSlotNameRules
    {
        public static int ScoreSlotConflict(int itemType, FurnitureSlotKind slot)
        {
            if (itemType <= ItemID.None)
                return 0;

            if (!FurnitureRecognitionCaches.TryGetProbe(itemType, out Item item))
                return 0;

            string name = (item.Name ?? "").ToLowerInvariant();
            int penalty = 0;
            int bonus = 0;

            if (slot == FurnitureSlotKind.Candle)
            {
                string internalName = (ItemLoader.GetItem(itemType)?.Name ?? "").ToLowerInvariant();
                if (internalName.Contains("column") || name.Contains("Öù"))
                    penalty -= 10_000;
            }

            if (slot == FurnitureSlotKind.Dresser)
            {
                string internalName = (ItemLoader.GetItem(itemType)?.Name ?? "").ToLowerInvariant();
                if (internalName.Contains("sofa") || name.Contains("É³·¢") || name.Contains("sofa"))
                    penalty -= 10_000;
            }

            if (slot == FurnitureSlotKind.Sofa)
            {
                string internalName = (ItemLoader.GetItem(itemType)?.Name ?? "").ToLowerInvariant();
                if (internalName.Contains("dresser") || name.Contains("Êá×±") || name.Contains("dresser"))
                    penalty -= 10_000;
            }

            if (FurnitureBuildingBlockRules.MustNotOccupyWikiFurnitureSlot(item, slot))
                penalty -= 12_000;

            if (slot == FurnitureSlotKind.Chest)
            {
                if (name.Contains("??") || name.Contains("??") || name.Contains("brazier") || name.Contains("bowl"))
                    penalty -= 6_000;
            }

            if (slot == FurnitureSlotKind.Candelabra && PreferLampOverCandelabra(itemType))
                penalty -= 5_500;

            if (slot == FurnitureSlotKind.Lamp)
            {
                if (name.Contains("??") && (item.createTile == TileID.Lamps || PreferLampOverCandelabra(itemType)))
                    bonus += 3_500;
            }

            if (slot == FurnitureSlotKind.Bathtub)
            {
                if (name.Contains("??") || name.Contains("bathtub"))
                    bonus += 3_200;
            }

            if (slot == FurnitureSlotKind.Candelabra
                && (name.Contains("??") || name.Contains("??") || name.Contains("brazier")))
                bonus += 3_200;

            if (item.createTile >= TileID.Dirt)
            {
                if (slot == FurnitureSlotKind.Workbench)
                {
                    if (item.createTile == TileID.WorkBenches)
                        bonus += 4_800;
                    else if (item.createTile == TileID.Sinks)
                        penalty -= 12_000;
                    else if (FurnitureRecipeSlotSignals.ScoreNameBonus(itemType, slot) <= 0)
                        penalty -= 8_000;
                }

                if (slot == FurnitureSlotKind.Sink)
                {
                    if (item.createTile == TileID.Sinks)
                        bonus += 4_800;
                    else if (item.createTile == TileID.WorkBenches)
                        penalty -= 12_000;
                    else if (FurnitureRecipeSlotSignals.ScoreNameBonus(itemType, slot) <= 0)
                        penalty -= 8_000;
                }
            }

            return penalty + bonus;
        }

        public static bool PreferLampOverCandelabra(int itemType)
        {
            if (!FurnitureRecognitionCaches.TryGetProbe(itemType, out Item item))
                return false;

            string name = (item.Name ?? "").ToLowerInvariant();
            if (name.Contains("??") || name.Contains("???") || name.Contains("desk lamp"))
                return true;
            if (name.Contains("??") && item.createTile == TileID.Lamps)
                return true;
            return item.createTile == TileID.Lamps;
        }
    }
}
