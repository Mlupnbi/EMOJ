using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.ItemHub.Filters
{
    internal static class ItemHubTileRules
    {
        /// <summary>ʵ�Ŀ飺solid���ǻ�����Ҫ����ƽ̨�������� Creative Blocks �Ӽ�����</summary>
        internal static bool IsSolidBlockTile(int ct)
        {
            if (ct < 0 || ct >= TileLoader.TileCount)
                return false;
            if (Main.tileFrameImportant[ct])
                return false;
            if (!Main.tileSolid[ct])
                return false;
            if (Main.tileSolidTop[ct])
                return false;
            return true;
        }

        internal static bool IsContainerTile(int ct)
        {
            if (ct < 0 || ct >= TileLoader.TileCount)
                return false;
            return Main.tileContainer[ct];
        }

        internal static bool IsPlacedLightTile(int ct)
        {
            if (ct < 0 || ct >= TileLoader.TileCount)
                return false;
            if (ct == TileID.Campfire)
                return true;
            if (ct < TileID.Sets.RoomNeeds.CountsAsTorch.Length && TileID.Sets.RoomNeeds.CountsAsTorch[ct] != 0)
                return true;
            if (ct < TileID.Sets.Torch.Length && TileID.Sets.Torch[ct])
                return true;
            return Main.tileLighted[ct];
        }

        internal static bool IsExcludedWall(int wallId)
        {
            if (wallId <= 0 || wallId >= WallLoader.WallCount)
                return true;
            return false;
        }
    }
}
