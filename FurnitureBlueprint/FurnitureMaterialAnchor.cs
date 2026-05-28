using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>从家具种子反推「可放置的套组方块」锚点，避免水槽/玻璃料当方块、紫晶玻璃抢槽。</summary>
    public static class FurnitureMaterialAnchor
    {
        public static bool IsValidAnchorBlock(Item item)
        {
            if (item == null || item.IsAir)
                return false;

            if (!FurnitureTileSafety.IsPhysicallySolidTile(item.createTile))
                return false;

            if (FurnitureBlueprintRecursionGuard.IsDepthExceeded
                || FurnitureBlueprintRecursionGuard.IsInsideAnchorOrClassify)
                return IsValidAnchorBlockWithoutClassification(item);

            using var scope = FurnitureBlueprintRecursionGuard.EnterAnchorOrClassify();
            if (!scope.Entered)
                return IsValidAnchorBlockWithoutClassification(item);

            if (FurnitureRecognitionCaches.TryGetCachedClassification(item.type, out FurnitureSlotKind cached)
                && cached is not FurnitureSlotKind.Block and not FurnitureSlotKind.None)
                return false;

            if (FurnitureSlotClassifier.TryGetSlot(item, out FurnitureSlotKind kind)
                && kind is not FurnitureSlotKind.Block and not FurnitureSlotKind.None)
                return false;

            return true;
        }

        /// <summary>递归保护路径：仅物理实心 + 非家具图格身份，不调用 TryGetSlot。</summary>
        private static bool IsValidAnchorBlockWithoutClassification(Item item)
        {
            if (FurnitureBuildingBlockRules.IsPlainMaterialBrick(item))
                return true;

            if (FurnitureRecognitionCaches.TryGetCachedClassification(item.type, out FurnitureSlotKind cached))
                return cached is FurnitureSlotKind.Block or FurnitureSlotKind.None;

            return false;
        }

        /// <summary>玻璃料、锭等 → 对应可放置方块（如玻璃块）。</summary>
        public static int ResolvePlaceableBlock(int anchorOrMaterial, FurnitureStyleSignature styleSig)
        {
            if (anchorOrMaterial <= ItemID.None)
                return ItemID.None;

            Item probe = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(probe, anchorOrMaterial))
                return ItemID.None;
            if (IsValidAnchorBlock(probe))
                return anchorOrMaterial;

            int fromProducts = FindSolidBlockAmongProducts(anchorOrMaterial, styleSig);
            if (fromProducts > ItemID.None)
                return fromProducts;

            return ItemID.None;
        }

        private static int FindSolidBlockAmongProducts(int materialType, FurnitureStyleSignature styleSig)
        {
            int best = ItemID.None;
            int bestScore = int.MinValue;
            int checkedExact = 0;

            foreach (int product in RecipeAnalyzer.GetProductTypesUsingExactMaterial(materialType, maxProducts: 96))
            {
                if (++checkedExact > 96)
                    break;

                if (!FurnitureRecognitionCaches.TryGetProbe(product, out Item item))
                    continue;
                if (!IsValidAnchorBlock(item))
                    continue;

                int score = ScoreBlockCandidate(product, styleSig);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = product;
                }
            }

            var exact = new HashSet<int>(RecipeAnalyzer.GetProductTypesUsingExactMaterial(materialType, maxProducts: 96));
            int checkedGroup = 0;
            foreach (int product in FurnitureRecipeProductEnumerator.EnumerateProducts(materialType, maxGroupProducts: 48))
            {
                if (exact.Contains(product))
                    continue;

                if (!FurnitureRecognitionCaches.TryGetProbe(product, out Item item))
                    continue;
                if (!IsValidAnchorBlock(item))
                    continue;

                string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(product);
                if (!FurnitureStyleSignature.StyleKeySameMaterialFamily(styleSig.StyleKey, productKey))
                    continue;

                int score = ScoreBlockCandidate(product, styleSig);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = product;
                }

                if (++checkedGroup > 64)
                    break;
            }

            return best;
        }

        private static int ScoreBlockCandidate(int blockType, FurnitureStyleSignature styleSig)
        {
            string blockKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(blockType);
            if (!FurnitureStyleSignature.StyleKeySameMaterialFamily(styleSig.StyleKey, blockKey))
                return int.MinValue;

            int score = 100;
            if (string.Equals(blockKey, styleSig.StyleKey?.Trim(), System.StringComparison.OrdinalIgnoreCase))
                score += 500;

            return score;
        }
    }
}
