using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>从种子只解析「套组材料方块」，供玩家确认后再正推 22 槽。</summary>
    public static class FurnitureMaterialBlockResolver
    {
        public static int ResolveMaterialBlockFromSeed(int seedType)
        {
            if (seedType <= ItemID.None)
                return ItemID.None;

            FurnitureReverseSeedProbe probe = FurnitureReverseSeedProbeCache.Ensure(seedType);
            return ResolvePlaceableBlockFromProbe(seedType, probe.BestAnchorIngredient, probe.SeedSignature);
        }

        /// <summary>在已有倒推探测结果上解析可放置材料方块（不再扫第二遍原料表）。</summary>
        public static int ResolvePlaceableBlockFromProbe(
            int seedType,
            int anchorIngredient,
            FurnitureStyleSignature seedSignature)
        {
            if (seedType <= ItemID.None)
                return ItemID.None;

            Item seed = new Item();
            seed.SetDefaults(seedType);
            if (FurnitureMaterialAnchor.IsValidAnchorBlock(seed))
                return seedType;

            if (anchorIngredient <= ItemID.None)
                return ItemID.None;

            FurnitureStyleSignature sig = seedSignature;
            if (string.IsNullOrWhiteSpace(sig.StyleKey))
                sig = FurnitureStyleSignature.FromItemType(seedType);
            FurnitureStyleSignature anchorSig = FurnitureStyleSignature.FromItemTypeForRecipes(anchorIngredient);
            int block = FurnitureMaterialAnchor.ResolvePlaceableBlock(anchorIngredient, anchorSig);
            if (block <= ItemID.None)
                block = FurnitureMaterialAnchor.ResolvePlaceableBlock(anchorIngredient, sig);

            if (block <= ItemID.None)
                block = FurniturePlacementLineMaterialResolver.TryResolveBlockFromFurnitureSeed(seedType);

            if (block > ItemID.None)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"material block seed={seedType} anchor={anchorIngredient} block={block}");
            }

            return block;
        }

        public static bool SeedIsMaterialBlock(int seedType)
        {
            if (seedType <= ItemID.None)
                return false;
            Item seed = new Item();
            seed.SetDefaults(seedType);
            return FurnitureMaterialAnchor.IsValidAnchorBlock(seed);
        }
    }
}
