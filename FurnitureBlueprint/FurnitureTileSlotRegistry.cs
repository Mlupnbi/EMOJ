using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Registry;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>����ʱ���� (tile, placeStyle) �� ��λ�����ü��� + RoomNeeds�������� TryGetSlot ѭ��������</summary>
    public static class FurnitureTileSlotRegistry
    {
        private static readonly Dictionary<int, FurnitureSlotKind> DefaultByTile = new();
        private static readonly Dictionary<long, FurnitureSlotKind> ByTileStyle = new();
        private static readonly Dictionary<long, List<int>> ItemsByPlacementLine = new();
        private static bool _built;

        public static bool IsBuilt => _built;

        private const int MaxItemsPerPlacementLine = 256;

        public static void Invalidate()
        {
            DefaultByTile.Clear();
            ByTileStyle.Clear();
            ItemsByPlacementLine.Clear();
            _built = false;
        }

        public static void Build(bool force = false)
        {
            if (_built && !force)
                return;

            DefaultByTile.Clear();
            ByTileStyle.Clear();
            ItemsByPlacementLine.Clear();

            if (!FurnitureTileItemRegistry.IsBuilt)
                FurnitureTileItemRegistry.Build();

            int maxTile = TileLoader.TileCount;
            for (int tile = TileID.Dirt; tile < maxTile; tile++)
            {
                if (!FurnitureTileItemRegistry.TryGetKnownStyles(tile, out int[] styles) || styles.Length == 0)
                    continue;

                string hint = FurnitureTileGeometryClassifier.GetTileNameHint(tile);
                for (int i = 0; i < styles.Length; i++)
                {
                    int style = styles[i];
                    if (FurnitureTileSafety.TryGetTileData(tile, style) == null)
                        continue;

                    if (FurnitureSlotClassifier.TryClassifyByRoomNeedsPublic(tile, style, out FurnitureSlotKind vanillaKind))
                        Register(tile, style, vanillaKind);
                    else if (FurnitureTileGeometryClassifier.TryClassify(tile, style, hint, out FurnitureSlotKind geoKind))
                        Register(tile, style, geoKind);
                }
            }

            int maxItem = ItemLoader.ItemCount;
            for (int type = ItemID.None + 1; type < maxItem; type++)
            {
                Item item = new Item();
                if (!FurnitureItemDefaults.TrySetDefaults(item, type))
                    continue;

                int tile = item.createTile;
                if (tile < TileID.Dirt)
                    continue;

                if (FurnitureSlotClassifier.TryClassifyByRoomNeedsPublic(tile, item.placeStyle, out FurnitureSlotKind kind))
                    Register(tile, item.placeStyle, kind);
                else if (FurnitureTileGeometryClassifier.TryClassify(tile, item.placeStyle,
                        FurnitureTileGeometryClassifier.GetTileNameHint(tile), out kind))
                    Register(tile, item.placeStyle, kind);

                RegisterItemPlacement(type, tile, item.placeStyle);
            }

            _built = true;
        }

        public static void AddPlacementLineSiblings(
            int tile,
            int style,
            string modKey,
            string styleKey,
            HashSet<int> dest,
            int maxItems = MaxItemsPerPlacementLine)
        {
            if (dest == null || tile < TileID.Dirt || maxItems <= 0)
                return;

            if (!_built)
                Build();

            if (!ItemsByPlacementLine.TryGetValue(Pack(tile, style), out List<int> list) || list == null)
                return;

            int added = 0;
            for (int i = 0; i < list.Count && added < maxItems; i++)
            {
                int type = list[i];
                if (type <= ItemID.None || dest.Contains(type))
                    continue;

                ModItem mi = ItemLoader.GetItem(type);
                if (mi != null && mi.Mod.Name != modKey)
                    continue;

                if (!string.IsNullOrWhiteSpace(styleKey) && !IsWeakStyleKey(styleKey))
                {
                    string otherKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(type);
                    if (!FurnitureStyleSignature.StyleKeyFuzzyMatch(styleKey, otherKey)
                        && !FurnitureMaterialKeyNormalizer.SameMaterialFamily(styleKey, otherKey))
                        continue;
                }

                dest.Add(type);
                added++;
            }
        }

        /// <summary>ͬһģ��ͼ�������� placeStyle ���ѵǼ���Ʒ���ֶ�һ�׼Ҿ߹���һ�� ModTile����</summary>
        public static void AddAllItemsOnModTile(
            int tile,
            string modKey,
            string styleKey,
            HashSet<int> dest,
            int maxItems = MaxItemsPerPlacementLine) =>
            AddAllItemsOnPlacementTile(tile, modKey, styleKey, dest, maxItems, requireStyleMatch: true);

        public static void AddAllItemsOnPlacementTile(
            int tile,
            string modKey,
            string styleKey,
            HashSet<int> dest,
            int maxItems = MaxItemsPerPlacementLine,
            bool requireStyleMatch = true)
        {
            if (dest == null || tile < TileID.Dirt || maxItems <= 0)
                return;

            if (!_built)
                Build();

            int added = 0;
            foreach (KeyValuePair<long, List<int>> kv in ItemsByPlacementLine)
            {
                int packedTile = (int)(kv.Key >> 32);
                if (packedTile != tile || kv.Value == null)
                    continue;

                foreach (int type in kv.Value)
                {
                    if (type <= ItemID.None || dest.Contains(type))
                        continue;

                    ModItem mi = ItemLoader.GetItem(type);
                    if (mi != null && mi.Mod.Name != modKey)
                        continue;

                    if (requireStyleMatch
                        && !string.IsNullOrWhiteSpace(styleKey)
                        && !IsWeakStyleKey(styleKey))
                    {
                        string otherKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(type);
                        if (!FurnitureStyleSignature.StyleKeyFuzzyMatch(styleKey, otherKey)
                            && !FurnitureMaterialKeyNormalizer.SameMaterialFamily(styleKey, otherKey))
                            continue;
                    }

                    dest.Add(type);
                    if (++added >= maxItems)
                        return;
                }
            }
        }

        /// <summary>�? (tile, placeStyle) 精确命中，不用图格默认槽�?</summary>
        public static bool TryGetSlotExact(int tile, int style, out FurnitureSlotKind kind)
        {
            if (!_built)
                Build();

            return ByTileStyle.TryGetValue(Pack(tile, style), out kind);
        }

        public static bool TryGetSlot(int tile, int style, out FurnitureSlotKind kind)
        {
            if (TryGetSlotExact(tile, style, out kind))
                return true;

            // 禁止 placeStyle�?0 时回退 DefaultByTile（否则同 ModTile 全套会被误判为同一槽，日志�? Sink:21 即此 bug�?
            if (style == 0 && DefaultByTile.TryGetValue(tile, out kind))
                return true;

            return false;
        }

        private static void Register(int tile, int style, FurnitureSlotKind kind)
        {
            ByTileStyle[Pack(tile, style)] = kind;
            if (!DefaultByTile.ContainsKey(tile))
                DefaultByTile[tile] = kind;
        }

        private static void RegisterItemPlacement(int itemType, int tile, int style)
        {
            if (itemType <= ItemID.None || tile < TileID.Dirt)
                return;

            long key = Pack(tile, style);
            if (!ItemsByPlacementLine.TryGetValue(key, out List<int> list))
            {
                list = new List<int>();
                ItemsByPlacementLine[key] = list;
            }

            if (list.Count >= MaxItemsPerPlacementLine)
                return;

            if (!list.Contains(itemType))
                list.Add(itemType);
        }

        private static long Pack(int tile, int style) => ((long)tile << 32) | (uint)style;

        internal static bool IsWeakStyleKey(string styleKey)
        {
            if (string.IsNullOrWhiteSpace(styleKey))
                return true;

            if (styleKey.Length > 32)
                return true;

            return styleKey.StartsWith("Item", StringComparison.OrdinalIgnoreCase);
        }
    }
}
