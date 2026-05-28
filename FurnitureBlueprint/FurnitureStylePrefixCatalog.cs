using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>同 mod 内按 internal 名/styleKey 前缀扫描产物，补全配方链未覆盖的套组件。</summary>
    internal static class FurnitureStylePrefixCatalog
    {
        public const int MaxProductsPerPrefix = 128;
        private const int MinPrefixLength = 4;
        private const int MaxScanItems = 14_000;
        private const int BatchMaxScanItems = 2_500;

        private static int EffectiveMaxScanItems =>
            FurnitureBlueprintBatchTest.IsRunning ? BatchMaxScanItems : MaxScanItems;

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

        public static void AddStylePrefixProducts(
            int seedType,
            string modKey,
            string stylePrefix,
            HashSet<int> dest,
            int maxAdd = MaxProductsPerPrefix)
        {
            if (dest == null || seedType <= ItemID.None || string.IsNullOrWhiteSpace(stylePrefix))
                return;

            modKey ??= GetModKey(seedType);
            if (string.IsNullOrWhiteSpace(modKey) || modKey == "Terraria")
                return;

            int added = 0;
            int scanned = 0;
            for (int type = ItemID.None + 1; type < ItemLoader.ItemCount && added < maxAdd && scanned < EffectiveMaxScanItems; type++)
            {
                scanned++;
                ModItem mi = ItemLoader.GetItem(type);
                if (mi == null || !string.Equals(mi.Mod.Name, modKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!InternalMatchesPrefix(mi.Name, stylePrefix))
                    continue;

                if (!FurnitureRecognitionCaches.IsPlaceableFurniture(type))
                    continue;

                if (FurnitureNameSignals.IsDecorativeMark(type))
                    continue;

                if (dest.Add(type))
                    added++;
            }

            if (added > 0)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"style-prefix expand seed={seedType} prefix={stylePrefix} added={added} total={dest.Count}");
            }
        }

        public static void ExpandForSeed(int seedType, int materialBlock, FurnitureStyleSignature blockSig, HashSet<int> dest)
        {
            if (dest == null || seedType <= ItemID.None)
                return;

            string modKey = blockSig.ModKey ?? GetModKey(seedType);
            string prefix = ResolveStylePrefix(seedType);
            if (string.IsNullOrWhiteSpace(prefix))
                prefix = blockSig.StyleKey?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(prefix))
                AddStylePrefixProducts(seedType, modKey, prefix, dest);

            if (materialBlock > ItemID.None && materialBlock != seedType)
            {
                string matPrefix = ResolveStylePrefix(materialBlock);
                if (!string.IsNullOrWhiteSpace(matPrefix)
                    && !string.Equals(matPrefix, prefix, StringComparison.OrdinalIgnoreCase))
                {
                    AddStylePrefixProducts(seedType, modKey, matPrefix, dest, maxAdd: MaxProductsPerPrefix / 2);
                }
            }
        }

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

        private static string GetModKey(int itemType)
        {
            ModItem mi = ItemLoader.GetItem(itemType);
            return mi == null ? "Terraria" : mi.Mod.Name;
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
