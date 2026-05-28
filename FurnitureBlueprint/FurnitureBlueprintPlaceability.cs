using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>放置入口：材料槽校验走 IG CheckersForItem，放置走 BongBongPlace 逻辑。</summary>
    public static class FurnitureBlueprintPlaceability
    {
        public static bool CanUseForBlueprint(Item item, FurnitureSlotKind slotKind) =>
            ImproveGameMaterialCheckers.ItemMatchesSlot(item, slotKind);

        /// <summary>参考 IG BongBongPlace：先清格再 PlaceTile/PlaceObject（带 player.whoAmI）。</summary>
        public static bool TryBongBongPlace(int x, int y, Item item, Player player)
        {
            if (item == null || item.IsAir || item.createTile < TileID.Dirt || player == null)
                return false;

            if (!WorldGen.InWorld(x, y, 1))
                return false;

            if (Main.tile[x, y].HasTile)
                FurniturePlacementRules.PrepareCell(x, y);

            bool success;
            if (item.createTile == TileID.Toilets)
            {
                WorldGen.Place1x2(x, y, TileID.Toilets, item.placeStyle);
                success = Main.tile[x, y].HasTile;
            }
            else if (TileID.Sets.BasicChest[item.createTile])
            {
                success = WorldGen.PlaceChest(
                    x, y, (ushort)item.createTile, notNearOtherChests: false, style: item.placeStyle) >= 0;
            }
            else if (TileID.Sets.BasicDresser[item.createTile])
            {
                success = WorldGen.PlaceObject(x, y, item.createTile, style: item.placeStyle);
            }
            else
            {
                success = WorldGen.PlaceTile(
                    x, y, item.createTile, mute: true, forced: true, player.whoAmI, item.placeStyle);
            }

            if (!success)
                success = WorldGen.PlaceObject(x, y, item.createTile, style: item.placeStyle);

            return success && Main.tile[x, y].HasTile;
        }
    }
}
