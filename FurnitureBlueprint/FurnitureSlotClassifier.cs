using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Registry;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 22 �۷��ࣺGemini ���� TileObjectData ���� �� RoomNeeds/ԭ��ͼ�� �� ���� �� ���ƶ��ס�
    /// </summary>
    public static class FurnitureSlotClassifier
    {
        public struct ClassificationTrace
        {
            public bool RoomNeedsMatched;
            public bool GeometryMatched;
            public bool RegistryExactMatched;
            public bool NameMatched;
            public bool RecipeSignalMatched;
            public bool TileDataPresent;
            public bool UsedDeepTier;
        }

        public static bool TryGetSlotFromType(int itemType, out FurnitureSlotKind kind)
        {
            if (!FurnitureRecognitionCaches.TryGetProbe(itemType, out Item item))
            {
                kind = FurnitureSlotKind.None;
                return false;
            }

            return TryGetSlot(item, out kind);
        }

        public static bool TryGetSlot(Item item, out FurnitureSlotKind kind)
        {
            return TryGetSlot(item, out kind, out _);
        }

        internal static bool TryGetSlot(Item item, out FurnitureSlotKind kind, out ClassificationTrace trace)
        {
            kind = FurnitureSlotKind.None;
            trace = default;
            if (item == null || item.IsAir)
                return false;

            if (FurnitureBlueprintRecursionGuard.IsDepthExceeded)
                return false;

            using var scope = FurnitureBlueprintRecursionGuard.EnterAnchorOrClassify();
            if (!scope.Entered)
                return false;

            try
            {
                return TryGetSlotCore(item, out kind, out trace);
            }
            catch (System.Exception ex)
            {
                FurnitureBlueprintLog.Warn($"TryGetSlot failed type={item.type}: {ex.Message}");
                return false;
            }
        }

        private static bool TryGetSlotCore(Item item, out FurnitureSlotKind kind, out ClassificationTrace trace)
        {
            kind = FurnitureSlotKind.None;
            trace = default;

            string nameEarly = (item.Name ?? "").ToLowerInvariant();
            if (FurnitureNameSignals.IsDecorativeMark(nameEarly))
                return false;

            if (FurnitureRecognitionCaches.TryGetCachedClassification(item.type, out FurnitureSlotKind cachedKind))
            {
                kind = cachedKind;
                return true;
            }

            int tile = item.createTile;
            int style = item.placeStyle;

            if (tile < TileID.Dirt)
            {
                if (item.createWall > WallID.None)
                {
                    kind = FurnitureSlotKind.Wall;
                    return true;
                }
                return false;
            }

            if (!FurnitureTileSafety.IsValidTileId(tile))
                return false;

            if (FurnitureTileSafety.IsPlatformTile(tile))
            {
                kind = FurnitureSlotKind.Platform;
                return true;
            }

            trace.TileDataPresent = FurnitureTileSafety.HasTileData(tile, style)
                || (style != 0 && FurnitureTileSafety.HasTileData(tile, 0));

            // ԭ��ͼ�� ID �����ڼ��Σ�����Ĭ�� 3��2��Table��4��2��Bed������������ bath/piano ʱ������
            trace.NameMatched = TryClassifyByItemName(item, out kind);
            if (trace.NameMatched)
            {
                FurnitureRecognitionCaches.CacheClassification(item.type, kind, true);
                return true;
            }

            if (TryClassifyByRecipeSignals(item.type, out kind, out _))
            {
                trace.RecipeSignalMatched = true;
                return true;
            }

            if (tile >= TileID.Count && TryClassifyModTile(tile, style, out kind))
            {
                trace.NameMatched = true;
                return true;
            }

            trace.RoomNeedsMatched = TryClassifyByRoomNeeds(tile, style, item, out kind);
            if (trace.RoomNeedsMatched)
                return AcceptClassifiedKind(item, kind);

            string hint = GetNameHint(tile, item);
            trace.GeometryMatched = TryClassifyByGeminiGeometry(tile, style, hint, out kind);
            if (trace.GeometryMatched && AcceptClassifiedKind(item, kind))
                return true;

            if (style != 0)
            {
                trace.GeometryMatched = TryClassifyByGeminiGeometry(tile, 0, hint, out kind);
                if (trace.GeometryMatched && AcceptClassifiedKind(item, kind))
                    return true;
            }

            trace.UsedDeepTier = true;

            if (FurnitureTileSlotRegistry.TryGetSlotExact(tile, style, out kind))
            {
                trace.RegistryExactMatched = true;
                return AcceptClassifiedKind(item, kind);
            }

            if (tile >= TileID.Count && TryClassifyModTileByAlternateStyles(tile, style, hint, out kind))
                return AcceptClassifiedKind(item, kind);

            if (IsPlainBuildingBlock(tile, style))
            {
                kind = FurnitureSlotKind.Block;
                return true;
            }

            if (tile >= TileID.Count && TryClassifyModTile(tile, style, out kind) && AcceptClassifiedKind(item, kind))
                return true;

            trace.NameMatched = TryClassifyByItemName(item, out kind);
            if (trace.NameMatched && AcceptClassifiedKind(item, kind))
                return true;

            if (FurnitureSlotScoring.TryInferClassifySlot(item.type, out kind, out _))
            {
                trace.RecipeSignalMatched = true;
                return true;
            }

            return false;
        }

        /// <summary>供全量识别日志：�?/深分档与最终槽位�?</summary>
        public static string FormatClassificationTier(in ClassificationTrace trace, FurnitureSlotKind kind)
        {
            if (kind == FurnitureSlotKind.Wall)
                return "coarse:wall";
            if (kind == FurnitureSlotKind.Platform)
                return "coarse:platform";
            if (!trace.UsedDeepTier)
            {
                if (trace.NameMatched)
                    return "coarse:name";
                if (trace.RecipeSignalMatched)
                    return "coarse:recipe";
                if (trace.RoomNeedsMatched)
                    return "coarse:roomNeeds";
                if (trace.GeometryMatched)
                    return "coarse:geom";
                return "coarse:unknown";
            }

            if (trace.RegistryExactMatched)
                return "deep:registry";
            if (trace.NameMatched)
                return "deep:name";
            return "deep:modOrBlock";
        }

        public static string FormatClassificationHints(in ClassificationTrace trace) =>
            $"rn={trace.RoomNeedsMatched} geom={trace.GeometryMatched} registry={trace.RegistryExactMatched} " +
            $"name={trace.NameMatched} deep={trace.UsedDeepTier} tileData={trace.TileDataPresent}";

        private static bool TryClassifyModTileByAlternateStyles(int tile, int style, string hint, out FurnitureSlotKind kind)
        {
            kind = FurnitureSlotKind.None;
            if (FurnitureBlueprintRecursionGuard.IsDepthExceeded)
                return false;

            if (!FurnitureTileItemRegistry.TryGetKnownStyles(tile, out int[] styles) || styles.Length == 0)
                return false;

            const int maxStyleProbes = 8;
            int probed = 0;
            for (int i = 0; i < styles.Length && probed < maxStyleProbes; i++)
            {
                int s = styles[i];
                if (s == style)
                    continue;
                if (FurnitureTileSafety.TryGetTileData(tile, s) == null)
                    continue;
                probed++;
                if (TryClassifyByGeminiGeometry(tile, s, hint, out kind))
                    return true;
            }

            return false;
        }

        /// <summary>Gemini ά��һ/����TileObjectData ������ê�㡣</summary>
        private static bool TryClassifyByGeminiGeometry(int tile, int style, string hint, out FurnitureSlotKind kind) =>
            FurnitureTileGeometryClassifier.TryClassify(tile, style, hint, out kind);

        private static string GetNameHint(int tile, Item item)
        {
            string itemName = (item.Name ?? "").ToLowerInvariant();
            if (tile >= TileID.Count)
            {
                string tileHint = FurnitureTileGeometryClassifier.GetTileNameHint(tile).ToLowerInvariant();
                return string.IsNullOrEmpty(tileHint) ? itemName : tileHint + " " + itemName;
            }

            return itemName;
        }

        private static bool TryClassifyByItemName(Item item, out FurnitureSlotKind kind)
        {
            kind = FurnitureSlotKind.None;
            if (item == null || item.IsAir)
                return false;

            string name = (item.Name ?? ItemID.Search.GetName(item.type) ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(name))
                return false;

            if (!TryClassifyNameHints(name, item.placeStyle, out kind))
                return false;

            if (kind == FurnitureSlotKind.Candelabra && FurnitureSlotNameRules.PreferLampOverCandelabra(item.type))
                kind = FurnitureSlotKind.Lamp;

            return true;
        }

        private static bool AcceptClassifiedKind(Item item, FurnitureSlotKind kind)
        {
            if (kind == FurnitureSlotKind.None)
                return false;

            if (kind == FurnitureSlotKind.Bed)
                return FurnitureNameSignals.MeetsBedPickEvidence(item.type, ItemID.None, ItemID.None);

            if (kind == FurnitureSlotKind.Chest)
            {
                string name = (item.Name ?? "").ToLowerInvariant();
                if (name.Contains("火盆") || name.Contains("火坛") || name.Contains("brazier") || name.Contains("bowl"))
                    return false;
                return name.Contains("�?") || name.Contains("chest") || name.Contains("宝箱");
            }

            if (kind == FurnitureSlotKind.Candelabra && FurnitureSlotNameRules.PreferLampOverCandelabra(item.type))
                return false;

            if (item != null && FurnitureBuildingBlockRules.MustNotOccupyWikiFurnitureSlot(item, kind))
                return false;

            return true;
        }

        private static bool TryClassifyByRecipeSignals(int productType, out FurnitureSlotKind kind, out int score) =>
            FurnitureRecipeSlotSignals.TryInferSlotFromRecipe(productType, out kind, out score);

        internal static bool TryClassifyByRoomNeedsPublic(int tile, int style, out FurnitureSlotKind kind) =>
            TryClassifyByRoomNeeds(tile, style, null, out kind);

        private static bool TryClassifyByRoomNeeds(int tile, int style, Item item, out FurnitureSlotKind kind)
        {
            kind = FurnitureSlotKind.None;
            string name = (item?.Name ?? "").ToLowerInvariant();

            if (FurnitureTileSafety.RoomNeedsCountsAsChair(tile, style))
            {
                kind = FurnitureSlotKind.Chair;
                return true;
            }

            if (FurnitureTileSafety.RoomNeedsCountsAsTable(tile))
            {
                kind = FurnitureSlotKind.Table;
                return true;
            }

            if (FurnitureTileSafety.RoomNeedsCountsAsDoor(tile))
            {
                kind = FurnitureSlotKind.Door;
                return true;
            }

            if (FurnitureTileSafety.RoomNeedsCountsAsTorch(tile)
                || FurnitureTileSafety.InBoolSet(TileID.Sets.Torch, tile))
            {
                if (item != null && FurnitureBuildingBlockRules.IsPlainMaterialBrick(item))
                    return false;

                kind = FurnitureSlotKind.Candle;
                return item == null || AcceptClassifiedKind(item, kind);
            }

            if (tile == TileID.WorkBenches)
            {
                kind = FurnitureSlotKind.Workbench;
                return true;
            }

            if (tile is TileID.Containers or TileID.Containers2)
            {
                if (name.Contains("火盆") || name.Contains("火坛") || name.Contains("brazier"))
                {
                    kind = FurnitureSlotKind.Candelabra;
                    return true;
                }

                if (name.Contains("�?") || name.Contains("chest") || name.Contains("宝箱"))
                {
                    kind = FurnitureSlotKind.Chest;
                    return true;
                }

                return false;
            }

            if (tile == TileID.Beds)
            {
                kind = FurnitureSlotKind.Bed;
                return true;
            }

            if (tile == TileID.Bookcases)
            {
                kind = FurnitureSlotKind.Bookcase;
                return true;
            }

            if (tile == TileID.Bathtubs)
            {
                kind = FurnitureSlotKind.Bathtub;
                return true;
            }

            if (tile == TileID.Candelabras)
            {
                kind = FurnitureSlotKind.Candelabra;
                return true;
            }

            if (tile == TileID.Candles)
            {
                kind = FurnitureSlotKind.Candle;
                return true;
            }

            if (tile == TileID.Chandeliers)
            {
                kind = FurnitureSlotKind.Chandelier;
                return true;
            }

            if (tile == TileID.GrandfatherClocks)
            {
                kind = FurnitureSlotKind.Clock;
                return true;
            }

            if (tile == TileID.Dressers)
            {
                kind = FurnitureSlotKind.Dresser;
                return true;
            }

            if (tile == TileID.Lamps)
            {
                kind = FurnitureSlotKind.Lamp;
                return true;
            }

            if (tile == TileID.HangingLanterns)
            {
                kind = FurnitureSlotKind.Lantern;
                return true;
            }

            if (tile == TileID.Pianos)
            {
                kind = FurnitureSlotKind.Piano;
                return true;
            }

            if (tile == TileID.Sinks)
            {
                kind = FurnitureSlotKind.Sink;
                return true;
            }

            if (tile == TileID.Benches)
            {
                kind = FurnitureSlotKind.Sofa;
                return true;
            }

            if (tile == TileID.Toilets || (tile == TileID.Chairs && style is 1 or 20))
            {
                kind = FurnitureSlotKind.Toilet;
                return true;
            }

            return false;
        }

        private static bool TryClassifyModTile(int tile, int style, out FurnitureSlotKind kind)
        {
            kind = FurnitureSlotKind.None;
            ModTile mt = TileLoader.GetTile(tile);
            if (mt == null)
                return false;

            string name = (mt.Name + " " + mt.FullName).ToLowerInvariant();
            return TryClassifyNameHints(name, style, out kind);
        }

        private static bool TryClassifyNameHints(string name, int style, out FurnitureSlotKind kind)
        {
            kind = FurnitureSlotKind.None;
            if (FurnitureNameSignals.IsDecorativeMark(name))
                return false;

            if (name.Contains("睡眠�?") || name.Contains("睡舱") || name.Contains("sleep pod") || name.Contains("sleeppod"))
            {
                kind = FurnitureSlotKind.Bed;
                return true;
            }
            if (name.Contains("台灯") || name.Contains("落地�?") || name.Contains("desk lamp") || name.Contains("table lamp"))
            {
                kind = FurnitureSlotKind.Lamp;
                return true;
            }
            if (name.Contains("键盘") || name.Contains("keyboard"))
            {
                kind = FurnitureSlotKind.Piano;
                return true;
            }
            if (name.Contains("长凳") || (name.Contains("bench") && !name.Contains("workbench")))
            {
                kind = FurnitureSlotKind.Sofa;
                return true;
            }
            if (name.Contains("书架") || name.Contains("书柜") || name.Contains("书橱"))
            {
                kind = FurnitureSlotKind.Bookcase;
                return true;
            }
            if (name.Contains("马桶"))
            {
                kind = FurnitureSlotKind.Toilet;
                return true;
            }
            if (name.Contains("火盆") || name.Contains("火坛") || name.Contains("brazier"))
            {
                kind = FurnitureSlotKind.Candelabra;
                return true;
            }
            if (name.Contains("烛台") || name.Contains("candelabra"))
            {
                kind = FurnitureSlotKind.Candelabra;
                return true;
            }
            if (name.Contains("灯笼") && !name.Contains("吊灯"))
            {
                kind = FurnitureSlotKind.Lantern;
                return true;
            }
            if (name.Contains("�?") || name.Contains("酒杯") || name.Contains("马克�?"))
            {
                kind = FurnitureSlotKind.Candle;
                return true;
            }
            if (name.Contains("�?") || name.Contains("火把"))
            {
                kind = FurnitureSlotKind.Candle;
                return true;
            }
            if (name.Contains("吊灯") || name.Contains("枝形吊灯"))
            {
                kind = FurnitureSlotKind.Chandelier;
                return true;
            }
            if (name.Contains("浴缸") || (name.Contains("�?") && !name.Contains("�?")))
            {
                kind = FurnitureSlotKind.Bathtub;
                return true;
            }
            if (name.Contains("�?") && !name.Contains("床头�?"))
            {
                kind = FurnitureSlotKind.Bed;
                return true;
            }
            if (name.Contains("梳妆") || name.Contains("衣柜") || name.Contains("衣橱"))
            {
                kind = FurnitureSlotKind.Dresser;
                return true;
            }
            if (name.Contains("钢琴") || name.Contains("七弦�?")
                || (name.Contains("�?") && !name.Contains("书架")))
            {
                kind = FurnitureSlotKind.Piano;
                return true;
            }
            if (name.Contains("水槽") || name.Contains("洗手�?") || name.Contains("水池"))
            {
                kind = FurnitureSlotKind.Sink;
                return true;
            }
            if (name.Contains("工作�?") || name.Contains("制作�?"))
            {
                kind = FurnitureSlotKind.Workbench;
                return true;
            }
            if (name.Contains("沙发") || name.Contains("长椅") || name.Contains("板凳") || name.Contains("躺椅"))
            {
                kind = FurnitureSlotKind.Sofa;
                return true;
            }
            if (name.Contains("�?") && !name.Contains("�?") && !name.Contains("吊灯") && !name.Contains("灯笼") && !name.Contains("�?"))
            {
                kind = FurnitureSlotKind.Lamp;
                return true;
            }
            if (name.EndsWith("�?") && !name.Contains("轮椅"))
            {
                kind = style is 1 or 20 ? FurnitureSlotKind.Toilet : FurnitureSlotKind.Chair;
                return true;
            }
            if (name.EndsWith("�?") || name.Contains("桌子"))
            {
                kind = FurnitureSlotKind.Table;
                return true;
            }
            if (name.Contains("�?") && !name.Contains("开�?"))
            {
                kind = FurnitureSlotKind.Door;
                return true;
            }
            if (name.Contains("�?") && !name.Contains("信箱"))
            {
                kind = FurnitureSlotKind.Chest;
                return true;
            }
            if (name.Contains("toilet") || name.Contains("toiletseat"))
            {
                kind = FurnitureSlotKind.Toilet;
                return true;
            }
            if (name.Contains("workbench") || name.Contains("work bench")
                || name.Contains("workstation") || name.Contains("work station")
                || (name.Contains("station") && (name.Contains("work") || name.Contains("craft") || name.Contains("forge"))))
            {
                kind = FurnitureSlotKind.Workbench;
                return true;
            }
            if (name.Contains("chair") && style is not 1 and not 20)
            {
                kind = FurnitureSlotKind.Chair;
                return true;
            }
            if (name.Contains("table") || name.Contains("desk"))
            {
                kind = FurnitureSlotKind.Table;
                return true;
            }
            if (name.Contains("door"))
            {
                kind = FurnitureSlotKind.Door;
                return true;
            }
            if (name.Contains("bed"))
            {
                kind = FurnitureSlotKind.Bed;
                return true;
            }
            if (name.Contains("bathtub") || name.Contains("ԡ��")
                || (name.Contains("bath") && !name.Contains("book")) || (name.Contains("ԡ") && !name.Contains("��")))
            {
                kind = FurnitureSlotKind.Bathtub;
                return true;
            }
            if (name.Contains("dresser"))
            {
                kind = FurnitureSlotKind.Dresser;
                return true;
            }
            if (name.Contains("bookcase") || name.Contains("bookshelf"))
            {
                kind = FurnitureSlotKind.Bookcase;
                return true;
            }
            if (name.Contains("candelabra"))
            {
                kind = FurnitureSlotKind.Candelabra;
                return true;
            }
            if (name.Contains("chandelier"))
            {
                kind = FurnitureSlotKind.Chandelier;
                return true;
            }
            if (name.Contains("clock"))
            {
                kind = FurnitureSlotKind.Clock;
                return true;
            }
            if (name.Contains("lantern"))
            {
                kind = FurnitureSlotKind.Lantern;
                return true;
            }
            if (name.Contains("lamppost"))
            {
                kind = FurnitureSlotKind.Lamp;
                return true;
            }
            if (name.Contains("cabinet") || name.Contains("wardrobe") || name.Contains("closet") || name.Contains("dresser")
                || name.Contains("�?") || name.Contains("衣柜") || name.Contains("�?"))
            {
                kind = FurnitureSlotKind.Dresser;
                return true;
            }
            if (name.Contains("bookshelf") || name.Contains("bookcase") || name.Contains("shelf") || name.Contains("书架"))
            {
                kind = FurnitureSlotKind.Bookcase;
                return true;
            }
            if (name.Contains("sofa") || name.Contains("couch") || name.Contains("loveseat") || name.Contains("沙发"))
            {
                kind = FurnitureSlotKind.Sofa;
                return true;
            }
            if (name.Contains("stool") || name.Contains("�?"))
            {
                kind = FurnitureSlotKind.Chair;
                return true;
            }
            if (name.EndsWith("lamp") || name.Contains(" lamp") || name.Contains("lamp "))
            {
                kind = FurnitureSlotKind.Lamp;
                return true;
            }
            if (name.Contains("piano") || name.Contains("����"))
            {
                kind = FurnitureSlotKind.Piano;
                return true;
            }
            if (name.Contains("sink"))
            {
                kind = FurnitureSlotKind.Sink;
                return true;
            }
            if (name.Contains("sofa") || name.Contains("couch") || name.Contains("ɳ��"))
            {
                kind = FurnitureSlotKind.Sofa;
                return true;
            }
            if (name.Contains("candle") || name.Contains("torch"))
            {
                kind = FurnitureSlotKind.Candle;
                return true;
            }
            if (name.Contains("chest"))
            {
                kind = FurnitureSlotKind.Chest;
                return true;
            }
            if (name.Contains("bench") && !name.Contains("work"))
            {
                kind = FurnitureSlotKind.Sofa;
                return true;
            }

            return false;
        }

        /// <summary>������ style ���� TileObjectData ���ǽ������飨���� style0 ���мҾ�Ϊ Block����</summary>
        private static bool IsPlainBuildingBlock(int tileType, int style)
        {
            if (tileType < TileID.Dirt)
                return false;

            if (FurnitureTileSafety.HasTileData(tileType, style))
                return false;

            if (style != 0 && FurnitureTileSafety.HasTileData(tileType, 0))
                return false;

            return FurnitureTileSafety.IsPhysicallySolidTile(tileType);
        }
    }
}
