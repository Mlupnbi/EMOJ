using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>产物管道之后：仅补充同 placeStyle 线 / 同风格配方产物，禁止无过滤扫全 ModTile。</summary>
    public static class FurnitureCandidateExpander
    {
        private const int RelaxedProductCap = 128;
        private const int BatchRelaxedProductCap = 40;

        private static int EffectiveRelaxedProductCap =>
            FurnitureBlueprintBatchTest.IsRunning ? BatchRelaxedProductCap : RelaxedProductCap;

        public static void Expand(
            int seedType,
            FurnitureStyleSignature filterSig,
            int anchorMaterial,
            HashSet<int> dest)
        {
            if (dest == null || seedType <= ItemID.None)
                return;

            int before = dest.Count;
            AddPlacementLineSiblings(seedType, filterSig, dest);
            if (anchorMaterial > ItemID.None && anchorMaterial != seedType)
                AddPlacementLineSiblings(anchorMaterial, filterSig, dest);

            if (dest.Count < FurnitureWikiSlots.TotalCount)
                AddRelaxedRecipeProducts(seedType, filterSig, anchorMaterial, dest);

            FurnitureStylePrefixCatalog.ExpandForSeed(seedType, anchorMaterial, filterSig, dest);

            if (anchorMaterial > ItemID.None)
                EnsureMaterialRoleProducts(anchorMaterial, filterSig, dest);

            if (dest.Count > before)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"candidate expand seed={seedType} material={anchorMaterial} added={dest.Count - before} total={dest.Count}");
            }
        }

        private static void AddPlacementLineSiblings(int itemType, FurnitureStyleSignature sig, HashSet<int> dest)
        {
            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, itemType))
                return;
            int tile = item.createTile;
            int style = item.placeStyle;

            if (tile < TileID.Dirt && sig.UsesPlacementStyleLine && sig.PlacementTile >= TileID.Dirt)
            {
                tile = sig.PlacementTile;
                style = sig.PlacementStyle;
            }

            if (tile < TileID.Dirt)
                return;

            FurnitureTileSlotRegistry.AddPlacementLineSiblings(tile, style, sig.ModKey, sig.StyleKey, dest, maxItems: 128);

            if (tile >= TileID.Count && !string.IsNullOrWhiteSpace(sig.StyleKey))
            {
                FurnitureTileSlotRegistry.AddAllItemsOnPlacementTile(
                    tile, sig.ModKey, sig.StyleKey, dest, maxItems: 128, requireStyleMatch: true);
            }
        }

        private static void AddRelaxedRecipeProducts(
            int seedType,
            FurnitureStyleSignature filterSig,
            int anchorMaterial,
            HashSet<int> dest)
        {
            string modKey = filterSig.ModKey;
            int material = anchorMaterial > ItemID.None ? anchorMaterial : seedType;
            int added = 0;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(material))
            {
                if (added >= EffectiveRelaxedProductCap)
                    break;

                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                int product = recipe.createItem.type;
                if (dest.Contains(product))
                    continue;

                if (GetModKey(product) != modKey)
                    continue;

                if (!PassesRelaxedStyle(filterSig, product, seedType, material))
                    continue;

                dest.Add(product);
                added++;
            }

            if (added > 0)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"relaxed expand seed={seedType} material={material} added={added} total={dest.Count}");
            }

            EnsureMaterialRoleProducts(material, filterSig, dest);
        }

        /// <summary>大理石/花岗岩等：保证工作台/水槽/床等关键槽位产物在占位扩池内（避免被先占槽后只剩柱/蜡烛）。</summary>
        internal static void EnsureMaterialRoleProducts(
            int materialBlock,
            FurnitureStyleSignature filterSig,
            HashSet<int> dest)
        {
            if (dest == null || materialBlock <= ItemID.None)
                return;

            int before = dest.Count;
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesConsumingMaterial(materialBlock))
            {
                if (recipe?.createItem == null || recipe.createItem.IsAir)
                    continue;

                int product = recipe.createItem.type;
                if (dest.Contains(product))
                    continue;

                if (!PassesRelaxedStyle(filterSig, product, ItemID.None, materialBlock))
                    continue;

                Item probe = new Item();
                if (!FurnitureItemDefaults.TrySetDefaults(probe, product))
                    continue;
                if (probe.createTile < TileID.Dirt)
                    continue;

                if (probe.createTile != TileID.WorkBenches
                    && probe.createTile != TileID.Sinks
                    && probe.createTile != TileID.Beds
                    && probe.createTile != TileID.Bathtubs)
                    continue;

                if (!FurnitureCandidateFilter.IsPlaceableFurnitureItem(probe))
                    continue;

                dest.Add(product);
            }

            if (dest.Count > before)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"role expand material={materialBlock} style={filterSig.StyleKey} added={dest.Count - before} total={dest.Count}");
            }
        }

        private static bool PassesRelaxedStyle(
            FurnitureStyleSignature filterSig,
            int productType,
            int seedType,
            int materialBlock)
        {
            if (string.IsNullOrWhiteSpace(filterSig.StyleKey) || FurnitureTileSlotRegistry.IsWeakStyleKey(filterSig.StyleKey))
                return true;

            string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(productType);
            if (FurnitureStyleSignature.StyleKeyFuzzyMatch(filterSig.StyleKey, productKey))
                return true;

            if (FurnitureMaterialKeyNormalizer.SameMaterialFamily(filterSig.StyleKey, productKey))
                return true;

            if (materialBlock > ItemID.None && FurnitureRecipeSetLinker.ProductUsesMaterial(productType, materialBlock))
                return true;

            if (FurnitureRecipeSetLinker.ProductUsesMaterial(productType, seedType))
                return true;

            return false;
        }

        private static string GetModKey(int itemType)
        {
            ModItem mi = ItemLoader.GetItem(itemType);
            return mi == null ? "Terraria" : mi.Mod.Name;
        }
    }
}
