using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>识别候选的多重筛选：可放置、家具槽位、光源、图格占地。</summary>
    public static class FurnitureCandidateFilter
    {
        private readonly struct FootprintRange
        {
            public readonly int MinW;
            public readonly int MaxW;
            public readonly int MinH;
            public readonly int MaxH;

            public FootprintRange(int minW, int maxW, int minH, int maxH)
            {
                MinW = minW;
                MaxW = maxW;
                MinH = minH;
                MaxH = maxH;
            }
        }

        public static bool IsPlaceableFurnitureItem(Item item)
        {
            if (item == null || item.IsAir)
                return false;

            if (item.damage > 0 || item.accessory || item.mountType >= 0)
                return false;

            if (item.createWall > WallID.None)
                return true;

            if (item.createTile < TileID.Dirt)
                return false;

            return true;
        }

        public static bool PassesSlotRules(Item item, FurnitureSlotKind slot)
        {
            if (!FurnitureSlotClassifier.TryGetSlot(item, out FurnitureSlotKind classified))
                return false;

            if (classified != slot)
                return false;

            // 光源槽：图格未标发光也保留（模组家具常漏 tileLighted），仅打分降权
            if (slot == FurnitureSlotKind.Platform && !FurnitureTileSafety.IsPlatformTile(item.createTile))
                return false;

            if (slot == FurnitureSlotKind.Block)
            {
                if (FurnitureTileSafety.IsPlatformTile(item.createTile))
                    return false;
                if (!FurnitureTileSafety.IsPhysicallySolidTile(item.createTile))
                    return false;
            }

            // 模组家具占地常超出原版区间：只用于评分，不硬拒绝（此前导致候选全灭）
            return true;
        }

        /// <summary>图格占地与槽位期望一致时高分（纯加分，不合规为 0）。</summary>
        public static int ScoreFootprintBonus(int itemType, FurnitureSlotKind slot)
        {
            if (slot == FurnitureSlotKind.Wall)
                return 800;

            if (!TryGetTileFootprint(itemType, out int w, out int h))
                return slot is FurnitureSlotKind.Block or FurnitureSlotKind.Platform ? 200 : 0;

            FootprintRange range = GetFootprintRange(slot);
            if (w >= range.MinW && w <= range.MaxW && h >= range.MinH && h <= range.MaxH)
                return FurnitureSlotScoring.FootprintPerfect;

            if (w <= range.MaxW + 1 && h <= range.MaxH + 2)
                return FurnitureSlotScoring.FootprintClose;

            return 0;
        }

        [System.Obsolete("Use ScoreFootprintBonus (additive only).")]
        public static int ScoreFootprint(int itemType, FurnitureSlotKind slot) =>
            ScoreFootprintBonus(itemType, slot);

        public static bool ProvidesLight(Item item)
        {
            if (item == null || item.IsAir)
                return false;

            int tile = item.createTile;
            if (!FurnitureTileSafety.IsValidTileId(tile) || tile < TileID.Dirt)
                return false;

            if (FurnitureTileSafety.InBoolSet(TileID.Sets.Torch, tile))
                return true;

            if (FurnitureTileSafety.RoomNeedsCountsAsTorch(tile))
                return true;

            if (FurnitureTileSafety.IsTileLighted(tile))
                return true;

            return tile is TileID.Candles or TileID.Lamps or TileID.Chandeliers or TileID.Candelabras
                or TileID.Campfire or TileID.HangingLanterns;
        }

        public static bool IsLightSlot(FurnitureSlotKind slot) =>
            slot is FurnitureSlotKind.Candle or FurnitureSlotKind.Lamp
                or FurnitureSlotKind.Lantern or FurnitureSlotKind.Chandelier or FurnitureSlotKind.Candelabra;

        public static bool TryGetTileFootprint(int itemType, out int width, out int height)
        {
            width = 1;
            height = 1;
            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, itemType))
                return false;
            if (!FurnitureTileSafety.HasPlaceableTile(item))
                return false;

            TileObjectData data = FurnitureTileSafety.TryGetTileData(item.createTile, item.placeStyle);
            if (data == null)
                return true;

            width = data.Width;
            height = data.Height;
            return width > 0 && height > 0;
        }

        private static FootprintRange GetFootprintRange(FurnitureSlotKind slot) => slot switch
        {
            FurnitureSlotKind.Block => new FootprintRange(1, 1, 1, 1),
            FurnitureSlotKind.Platform => new FootprintRange(1, 1, 1, 1),
            FurnitureSlotKind.Chair => new FootprintRange(1, 2, 2, 3),
            FurnitureSlotKind.Table => new FootprintRange(2, 5, 1, 3),
            FurnitureSlotKind.Workbench => new FootprintRange(2, 5, 1, 3),
            FurnitureSlotKind.Door => new FootprintRange(1, 2, 3, 4),
            FurnitureSlotKind.Chest => new FootprintRange(2, 3, 2, 3),
            FurnitureSlotKind.Bed => new FootprintRange(2, 5, 2, 4),
            FurnitureSlotKind.Bookcase => new FootprintRange(2, 4, 3, 5),
            FurnitureSlotKind.Bathtub => new FootprintRange(3, 5, 2, 3),
            FurnitureSlotKind.Candelabra => new FootprintRange(1, 2, 2, 3),
            FurnitureSlotKind.Candle => new FootprintRange(1, 1, 1, 2),
            FurnitureSlotKind.Chandelier => new FootprintRange(2, 4, 2, 4),
            FurnitureSlotKind.Clock => new FootprintRange(2, 3, 2, 4),
            FurnitureSlotKind.Dresser => new FootprintRange(2, 4, 2, 3),
            FurnitureSlotKind.Lamp => new FootprintRange(1, 2, 2, 4),
            FurnitureSlotKind.Lantern => new FootprintRange(1, 2, 2, 3),
            FurnitureSlotKind.Piano => new FootprintRange(3, 5, 2, 3),
            FurnitureSlotKind.Sink => new FootprintRange(2, 4, 2, 3),
            FurnitureSlotKind.Sofa => new FootprintRange(2, 4, 1, 2),
            FurnitureSlotKind.Toilet => new FootprintRange(1, 2, 2, 3),
            _ => new FootprintRange(1, 4, 1, 4)
        };
    }
}
