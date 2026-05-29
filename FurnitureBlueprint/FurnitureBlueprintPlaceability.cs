using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>放置入口：材料槽校验走 IG CheckersForItem，放置走 BongBongPlace 逻辑。</summary>
    public static class FurnitureBlueprintPlaceability
    {
        public static bool CanUseForBlueprint(Item item, FurnitureSlotKind slotKind) =>
            ImproveGameMaterialCheckers.ItemMatchesSlot(item, slotKind);

        /// <summary>参考 IG BongBongPlace：多格优先 PlaceObject，单格 PlaceTile，失败再 fallback。</summary>
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
            else if (ShouldPlaceAsObject(item))
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

            if (!success && !ShouldPlaceAsObject(item))
            {
                success = WorldGen.PlaceTile(
                    x, y, item.createTile, mute: true, forced: true, player.whoAmI, item.placeStyle);
            }

            return success && Main.tile[x, y].HasTile;
        }

        private static bool ShouldPlaceAsObject(Item item)
        {
            if (TileID.Sets.BasicDresser[item.createTile])
                return true;

            TileObjectData data = TileObjectData.GetTileData(item.createTile, item.placeStyle);
            if (data == null)
                return false;

            return data.Width > 1 || data.Height > 1;
        }
    }
}
