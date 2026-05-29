using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 仅从种子/材料物品名解析 style 前缀，供血统判定与打分门控使用。
    /// 不向候选池做 mod 全物品扫描——套组成员必须经配方/材料块管道进入候选集。
    /// </summary>
    internal static class FurnitureStylePrefixCatalog
    {
        private const int MinPrefixLength = 4;

        public static string ResolveStylePrefix(int seedType)
        {
            if (seedType <= ItemID.None)
                return string.Empty;

            ModItem mi = ItemLoader.GetItem(seedType);
            string internalName = mi?.Name ?? string.Empty;
            if (!string.IsNullOrEmpty(internalName))
            {
                string stripped = StripFurnitureSuffix(internalName);
                if (stripped.Length >= MinPrefixLength)
                    return stripped;
            }

            string styleKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            if (!string.IsNullOrWhiteSpace(styleKey) && styleKey.Length >= MinPrefixLength)
                return styleKey;

            return string.Empty;
        }

        /// <summary>对已在候选池内的产物做风格血缘判定（不扩大候选池）。</summary>
        public static bool ProductMatchesSeedStyle(int productType, int seedType, int materialBlock)
        {
            if (productType <= ItemID.None || seedType <= ItemID.None)
                return false;

            if (productType == seedType || productType == materialBlock)
                return true;

            ModItem seedMod = ItemLoader.GetItem(seedType);
            ModItem prodMod = ItemLoader.GetItem(productType);
            if (seedMod == null || prodMod == null)
                return false;

            if (seedMod.Mod.Name == "Terraria")
            {
                string seedStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
                string pickStyle = FurnitureSetRecognizer.ExtractStyleKeyPublic(productType);
                return FurnitureStyleSignature.StyleKeyFuzzyMatch(seedStyle, pickStyle)
                    || FurnitureMaterialKeyNormalizer.SameMaterialFamily(seedStyle, pickStyle);
            }

            if (!string.Equals(seedMod.Mod.Name, prodMod.Mod.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            string prefix = ResolveStylePrefix(seedType);
            if (!string.IsNullOrWhiteSpace(prefix) && InternalMatchesPrefix(prodMod.Name, prefix))
                return true;

            if (materialBlock > ItemID.None)
            {
                string matPrefix = ResolveStylePrefix(materialBlock);
                if (!string.IsNullOrWhiteSpace(matPrefix) && InternalMatchesPrefix(prodMod.Name, matPrefix))
                    return true;
            }

            return FurnitureSetMaterialRules.MaterialAlignsWithSeedStyle(seedType, productType);
        }

        public static bool RequiresStyleGate(int seedType, int materialBlock)
        {
            if (seedType <= ItemID.None)
                return false;

            ModItem seedMod = ItemLoader.GetItem(seedType);
            if (seedMod == null || seedMod.Mod.Name == "Terraria")
                return false;

            if (!string.IsNullOrWhiteSpace(ResolveStylePrefix(seedType)))
                return true;

            return FurnitureSetMaterialRules.IsMisalignedGenericMaterialForModSeed(seedType, materialBlock);
        }

        private static string StripFurnitureSuffix(string internalName)
        {
            if (string.IsNullOrEmpty(internalName))
                return string.Empty;

            int cut = internalName.Length;
            foreach (string suffix in FurnitureStyleSuffixes.FurnitureSuffixes)
            {
                if (internalName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                    && internalName.Length - suffix.Length >= MinPrefixLength)
                {
                    cut = Math.Min(cut, internalName.Length - suffix.Length);
                }
            }

            return cut < internalName.Length ? internalName.Substring(0, cut) : string.Empty;
        }

        private static readonly HashSet<string> AmbiguousStylePrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Salt", "Wood", "Fire", "Gold", "Iron", "Ice", "Rock", "Sand", "Star", "Moon", "Dirt", "Bone", "Ash"
        };

        private static bool InternalMatchesPrefix(string internalName, string prefix)
        {
            if (string.IsNullOrEmpty(internalName) || string.IsNullOrWhiteSpace(prefix))
                return false;

            if (!internalName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            if (prefix.Length <= 4 && AmbiguousStylePrefixes.Contains(prefix))
                return InternalNameLooksLikeFurnitureSetMember(internalName, prefix);

            return true;
        }

        private static bool InternalNameLooksLikeFurnitureSetMember(string internalName, string prefix)
        {
            if (internalName.Length <= prefix.Length)
                return false;

            foreach (string suffix in FurnitureStyleSuffixes.FurnitureSuffixes)
            {
                if (internalName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            string rest = internalName.Substring(prefix.Length);
            foreach (string suffix in FurnitureStyleSuffixes.FurnitureSuffixes)
            {
                if (rest.StartsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    internal static class FurnitureStyleSuffixes
    {
        internal static readonly string[] FurnitureSuffixes =
        {
            "Workbench", "WorkBench", "Table", "Chair", "Door", "Chest", "Bed", "Bookcase",
            "Bathtub", "Candelabra", "Candle", "Chandelier", "Clock", "Dresser", "Lamp", "Lantern",
            "Piano", "Platform", "Sink", "Sofa", "Toilet", "Wall", "Block", "Brick", "Planks",
            "Bookshelf", "Organ", "Basin", "Column", "Screen", "Pod", "Jukebox"
        };
    }
}
