using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public readonly struct FurnitureStyleSignature
    {
        public string ModKey { get; init; }
        public string StyleKey { get; init; }
        public int PlacementTile { get; init; }
        public int PlacementStyle { get; init; }
        public bool UsesPlacementStyleLine { get; init; }
        public bool SeedIsMaterialBlock { get; init; }

        public static FurnitureStyleSignature FromItemType(int itemType) =>
            BuildSignature(itemType, inferBlockPlacementLine: true);

        /// <summary>产物扩展/过滤用：不推断 placeStyle 线，避免与 FromItemType 互相递归。</summary>
        public static FurnitureStyleSignature FromItemTypeForRecipes(int itemType) =>
            BuildSignature(itemType, inferBlockPlacementLine: false);

        private static FurnitureStyleSignature BuildSignature(int itemType, bool inferBlockPlacementLine)
        {
            if (itemType <= ItemID.None)
                return default;

            if (!FurnitureRecognitionCaches.TryGetProbe(itemType, out Item item))
                return default;

            ModItem mi = ItemLoader.GetItem(itemType);
            string modKey = mi == null ? "Terraria" : mi.Mod.Name;
            string styleKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
            int tile = item.createTile;
            int style = item.placeStyle;
            bool blockSeed = IsMaterialBlock(item);
            bool furnitureSeed = tile >= TileID.Dirt && !IsPlainSolidBlock(tile);

            int lineTile = tile;
            int lineStyle = style;
            bool useLine = furnitureSeed && UsesPlaceStyleForSet(tile);
            if (blockSeed && inferBlockPlacementLine)
                InferPlacementLineFromProducts(itemType, out lineTile, out lineStyle, out useLine);

            return new FurnitureStyleSignature
            {
                ModKey = modKey,
                StyleKey = styleKey,
                PlacementTile = lineTile,
                PlacementStyle = lineStyle,
                UsesPlacementStyleLine = useLine,
                SeedIsMaterialBlock = blockSeed
            };
        }

        /// <summary>方块种子：从「消耗该方块的产物」里统计最常见的 (tile, placeStyle) 作为套组线（Gemini 第一层）。</summary>
        private static void InferPlacementLineFromProducts(int blockSeed, out int lineTile, out int lineStyle, out bool hasLine)
        {
            lineTile = -1;
            lineStyle = 0;
            hasLine = false;
            var counts = new Dictionary<(int tile, int style), int>();

            string blockStyleKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(blockSeed);
            FurnitureStyleSignature blockSig = FromItemTypeForRecipes(blockSeed);

            int processed = 0;
            foreach (int product in FurnitureRecipeProductEnumerator.EnumerateProducts(blockSeed, maxGroupProducts: 96))
            {
                if (processed >= 128)
                    break;

                if (!FurnitureRecipeSetLinker.ProductBelongsToMaterialStyle(product, blockSeed, blockSig)
                    && !CountsTowardPlacementLine(product, blockStyleKey))
                    continue;

                if (!FurnitureRecognitionCaches.TryGetProbe(product, out Item probe))
                    continue;

                var key = (probe.createTile, probe.placeStyle);
                counts.TryGetValue(key, out int n);
                counts[key] = n + 1;
                processed++;
            }

            if (counts.Count == 0)
                return;

            (int tile, int style) best = counts.OrderByDescending(kv => kv.Value).First().Key;
            lineTile = best.tile;
            lineStyle = best.style;
            hasLine = true;
        }

        private static bool CountsTowardPlacementLine(int productType, string blockStyleKey)
        {
            if (!FurnitureRecognitionCaches.TryGetProbe(productType, out Item probe))
                return false;

            if (probe.createTile < TileID.Dirt || IsPlainSolidBlock(probe.createTile))
                return false;
            if (!UsesPlaceStyleForSet(probe.createTile))
                return false;

            if (string.IsNullOrWhiteSpace(blockStyleKey))
                return true;

            string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(productType);
            return StyleKeyFuzzyMatch(blockStyleKey.Trim(), productKey);
        }

        private static bool IsMaterialBlock(Item item) =>
            item != null && !item.IsAir
            && item.createTile >= TileID.Dirt
            && Main.tileSolid[item.createTile]
            && !Main.tileSolidTop[item.createTile];

        public bool MatchesItem(int itemType)
        {
            if (itemType <= ItemID.None)
                return false;

            FurnitureStyleSignature other = FromItemType(itemType);
            if (other.ModKey != ModKey)
                return false;

            if (UsesPlacementStyleLine && PlacementTile >= TileID.Dirt)
            {
                if (other.PlacementTile == PlacementTile && other.PlacementStyle == PlacementStyle)
                    return true;
            }

            if (!string.IsNullOrEmpty(StyleKey) && !string.IsNullOrEmpty(other.StyleKey)
                && string.Equals(StyleKey, other.StyleKey, StringComparison.OrdinalIgnoreCase))
                return true;

            if (SharesPlacementStyle(PlacementTile, PlacementStyle, other.PlacementTile, other.PlacementStyle))
                return true;

            return false;
        }

        /// <summary>种子与候选是否视为同一套组（用于候选集，比 MatchesItem 宽松）。</summary>
        public bool BelongsToSet(int seedType, int itemType, FurnitureStyleSignature seedSig)
        {
            if (itemType <= ItemID.None)
                return false;

            if (itemType == seedType)
                return true;

            FurnitureStyleSignature other = FromItemType(itemType);
            if (other.ModKey != seedSig.ModKey)
                return false;

            if (FurnitureRecipeSetLinker.ProductUsesMaterial(itemType, seedType))
                return true;

            if (seedSig.UsesPlacementStyleLine && seedSig.PlacementTile >= TileID.Dirt)
            {
                if (other.PlacementTile == seedSig.PlacementTile)
                {
                    if (other.PlacementStyle == seedSig.PlacementStyle)
                        return true;
                    if (UsesPlaceStyleForSet(seedSig.PlacementTile) && seedSig.PlacementTile >= TileID.Count)
                        return true;
                }
            }

            if (seedSig.SeedIsMaterialBlock)
                return false;

            if (StyleKeyFuzzyMatch(seedSig.StyleKey, other.StyleKey))
                return true;

            if (SharesPlacementStyle(seedSig.PlacementTile, seedSig.PlacementStyle, other.PlacementTile, other.PlacementStyle))
                return true;

            return false;
        }

        public static bool StyleKeyFuzzyMatch(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            a = a.Trim();
            b = b.Trim();
            if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
                return true;

            if (FurnitureMaterialKeyNormalizer.StyleKeysMatch(a, b))
                return true;

            if (a.Length >= 3 && b.Length >= 3)
            {
                if (a.StartsWith(b, StringComparison.OrdinalIgnoreCase)
                    || b.StartsWith(a, StringComparison.OrdinalIgnoreCase))
                    return StyleKeySameMaterialFamily(a, b);
            }

            int common = LongestCommonPrefixLength(a, b);
            int minLen = Math.Min(a.Length, b.Length);
            if (minLen < 3 || common < Math.Max(3, minLen * 2 / 3))
                return false;

            return StyleKeySameMaterialFamily(a, b);
        }

        /// <summary>排除紫晶玻璃 Glass、LivingWood 与 Wood 等「包含关系但不同套组」。</summary>
        public static bool StyleKeySameMaterialFamily(string anchorKey, string otherKey)
        {
            if (string.IsNullOrWhiteSpace(anchorKey) || string.IsNullOrWhiteSpace(otherKey))
                return false;

            anchorKey = anchorKey.Trim();
            otherKey = otherKey.Trim();
            if (anchorKey.Equals(otherKey, StringComparison.OrdinalIgnoreCase))
                return true;

            if (FurnitureMaterialKeyNormalizer.StyleKeysMatch(anchorKey, otherKey))
                return true;

            if (FurnitureMaterialKeyNormalizer.SameMaterialFamily(anchorKey, otherKey))
                return true;

            if (otherKey.Length > anchorKey.Length
                && otherKey.IndexOf(anchorKey, StringComparison.OrdinalIgnoreCase) > 0)
                return false;

            if (anchorKey.Length > otherKey.Length
                && anchorKey.IndexOf(otherKey, StringComparison.OrdinalIgnoreCase) > 0)
                return false;

            // 无包含关系（如 Sandstone 与 Glass）→ 不同套组
            return false;
        }

        private static int LongestCommonPrefixLength(string a, string b)
        {
            int n = Math.Min(a.Length, b.Length);
            int i = 0;
            for (; i < n; i++)
            {
                if (char.ToLowerInvariant(a[i]) != char.ToLowerInvariant(b[i]))
                    break;
            }
            return i;
        }

        private static bool IsPlainSolidBlock(int tileType) =>
            tileType >= TileID.Dirt
            && Terraria.ObjectData.TileObjectData.GetTileData(tileType, 0) == null
            && Main.tileSolid[tileType]
            && !Main.tileSolidTop[tileType];

        private static bool SharesPlacementStyle(int tileA, int styleA, int tileB, int styleB)
        {
            if (tileA < TileID.Dirt || tileB < TileID.Dirt)
                return false;
            if (tileA != tileB)
                return false;
            if (!UsesPlaceStyleForSet(tileA))
                return false;
            return styleA == styleB;
        }

        private static bool UsesPlaceStyleForSet(int tileType) =>
            tileType == TileID.Chairs
            || tileType == TileID.Tables
            || tileType == TileID.Tables2
            || tileType == TileID.Beds
            || tileType == TileID.ClosedDoor
            || tileType == TileID.Containers
            || tileType == TileID.Containers2
            || tileType == TileID.Dressers
            || tileType >= TileID.Count
            || TileID.Sets.Platforms[tileType];
    }
}
