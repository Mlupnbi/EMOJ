using System;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Registry
{
    /// <summary>
    /// Phase 2 放置/缺件校验：22 槽 <c>CheckersForItem</c>（对齐 EMOJ wiki 槽序，参考 IG CreateWandHelper）。
    /// <para>隔离原则：仅供建筑/模板/魔杖路径使用；<b>不得</b>写入识别缓存或调用赋分/识别管线
    /// （不引用 <see cref="FurnitureSetRecognizer"/>、<see cref="FurnitureSlotScoring"/> 等）。</para>
    /// Mod 兜底：只读 <see cref="FurnitureTileSlotRegistry.TryGetSlotExact"/>，与识别用
    /// <see cref="FurnitureSlotClassifier"/> 分离。
    /// </summary>
    public static class FurnitureSetMaterialCheckers
    {
        private static Func<Item, bool>[] _checkersForItem;
        private static bool _built;

        /// <summary>索引 = <see cref="FurnitureSlotKinds.ToIndex"/>，长度 22。</summary>
        public static ReadOnlySpan<Func<Item, bool>> CheckersForItem
        {
            get
            {
                EnsureBuilt();
                return _checkersForItem;
            }
        }

        public static void Build()
        {
            _checkersForItem = new Func<Item, bool>[FurnitureSlotKinds.Count];
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                FurnitureSlotKind kind = FurnitureSlotKinds.FromIndex(i);
                _checkersForItem[i] = item => ItemMatchesSlot(item, kind);
            }

            _built = true;
        }

        public static bool ItemMatchesSlotIndex(Item item, int slotIndex)
        {
            if (item == null || slotIndex < 0 || slotIndex >= FurnitureSlotKinds.Count)
                return false;

            EnsureBuilt();
            return _checkersForItem[slotIndex](item);
        }

        public static bool ItemMatchesSlot(Item item, FurnitureSlotKind slot)
        {
            if (item == null || item.IsAir || slot == FurnitureSlotKind.None)
                return false;

            if (TryVanillaSlotCheck(item, slot))
                return true;

            return TryRegistrySlotFallback(item, slot);
        }

        /// <summary>返回第一个匹配的 wiki 槽（仅 Registry 判定，供放置预览/背包校验）。</summary>
        public static bool TryClassifyForPlacement(Item item, out FurnitureSlotKind kind)
        {
            kind = FurnitureSlotKind.None;
            if (item == null || item.IsAir)
                return false;

            EnsureBuilt();
            for (int i = 0; i < _checkersForItem.Length; i++)
            {
                if (!_checkersForItem[i](item))
                    continue;

                kind = FurnitureSlotKinds.FromIndex(i);
                return true;
            }

            return false;
        }

        private static void EnsureBuilt()
        {
            if (!_built)
                Build();
        }

        private static bool TryRegistrySlotFallback(Item item, FurnitureSlotKind expected)
        {
            if (expected == FurnitureSlotKind.Wall)
                return item.createWall > WallID.None;

            if (item.createTile < TileID.Dirt)
                return false;

            return FurnitureTileSlotRegistry.TryGetSlotExact(item.createTile, item.placeStyle, out FurnitureSlotKind registryKind)
                && registryKind == expected;
        }

        private static bool TryVanillaSlotCheck(Item item, FurnitureSlotKind slot) => slot switch
        {
            FurnitureSlotKind.Block => IsBlockItem(item),
            FurnitureSlotKind.Wall => IsWallItem(item),
            FurnitureSlotKind.Bathtub => IsTileBathtub(item.createTile),
            FurnitureSlotKind.Bed => IsTileBed(item.createTile),
            FurnitureSlotKind.Bookcase => IsTileBookcase(item.createTile),
            FurnitureSlotKind.Candelabra => item.createTile == TileID.Candelabras,
            FurnitureSlotKind.Candle => item.createTile == TileID.Candles,
            FurnitureSlotKind.Chandelier => item.createTile == TileID.Chandeliers,
            FurnitureSlotKind.Chair => IsTileChair(item.createTile, item.placeStyle),
            FurnitureSlotKind.Chest => IsTileChest(item.createTile),
            FurnitureSlotKind.Clock => item.createTile == TileID.GrandfatherClocks,
            FurnitureSlotKind.Door => item.createTile == TileID.ClosedDoor,
            FurnitureSlotKind.Dresser => item.createTile == TileID.Dressers,
            FurnitureSlotKind.Lamp => item.createTile == TileID.Lamps,
            FurnitureSlotKind.Lantern => item.createTile == TileID.HangingLanterns,
            FurnitureSlotKind.Piano => item.createTile == TileID.Pianos,
            FurnitureSlotKind.Platform => IsTilePlatform(item.createTile),
            FurnitureSlotKind.Sink => item.createTile == TileID.Sinks,
            FurnitureSlotKind.Sofa => IsSofaItem(item),
            FurnitureSlotKind.Table => IsTileTable(item.createTile),
            FurnitureSlotKind.Toilet => IsTileToilet(item.createTile, item.placeStyle),
            FurnitureSlotKind.Workbench => item.createTile == TileID.WorkBenches,
            _ => false
        };

        private static bool IsBlockItem(Item item)
        {
            if (item.createWall > WallID.None && item.createTile < TileID.Dirt)
                return false;

            if (ItemID.Sets.GrassSeeds[item.type]
                || item.type is ItemID.StaffofRegrowth or ItemID.AcornAxe)
                return false;

            return IsTileBlock(item.createTile);
        }

        private static bool IsWallItem(Item item) =>
            item.createWall > WallID.None && item.createTile < TileID.Dirt;

        private static bool IsSofaItem(Item item)
        {
            if (item.createTile == TileID.Benches)
                return true;

            string name = (item.Name ?? "").ToLowerInvariant();
            if (name.Contains("sofa") || name.Contains("沙发") || name.Contains("长椅"))
            {
                return item.createTile == TileID.Chairs
                    && item.placeStyle is not 1 and not 20;
            }

            return false;
        }

        private static bool IsTilePlatform(int tileType) =>
            FurnitureTileSafety.IsPlatformTile(tileType);

        private static bool IsTileBlock(int tileType) =>
            FurnitureTileSafety.IsPlainSolidBlock(tileType);

        private static bool IsTileChair(int tileType, int placeStyle) =>
            tileType == TileID.Chairs && placeStyle is not 1 and not 20;

        private static bool IsTileTable(int tileType) =>
            tileType is TileID.Tables or TileID.Tables2;

        private static bool IsTileBed(int tileType) => tileType == TileID.Beds;

        private static bool IsTileChest(int tileType) =>
            tileType is TileID.Containers or TileID.Containers2;

        private static bool IsTileBookcase(int tileType) => tileType == TileID.Bookcases;

        private static bool IsTileBathtub(int tileType) => tileType == TileID.Bathtubs;

        private static bool IsTileToilet(int tileType, int placeStyle) =>
            tileType == TileID.Toilets || (tileType == TileID.Chairs && placeStyle is 1 or 20);
    }
}
