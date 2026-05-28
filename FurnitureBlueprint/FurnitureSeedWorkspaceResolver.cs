using System.Collections.Generic;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>种子 → 材料块/候选列表；供 PostUpdate 分帧调用，避免 UI/输入线程同步 Ensure(probe)。</summary>
    public readonly struct FurnitureSeedWorkspaceResolution
    {
        public int SeedType { get; init; }
        public int MaterialBlock { get; init; }
        public List<int> MaterialCandidates { get; init; }
        public bool OpenMaterialPicker { get; init; }
        public bool AppliedFromCache { get; init; }
        public FurnitureScheme CachedScheme { get; init; }
        public int CachedMaterialBlock { get; init; }
    }

    public static class FurnitureSeedWorkspaceResolver
    {
        public static FurnitureSeedWorkspaceResolution Resolve(int seedType)
        {
            if (seedType <= ItemID.None)
                return default;

            FurnitureBlueprintLog.Info(
                $"seed probe begin seed={seedType} name={FurnitureItemDefaults.SafeItemName(seedType)}");

            if (FurnitureMaterialBlockResolver.SeedIsMaterialBlock(seedType))
            {
                FurnitureBlueprintLog.Info($"seed probe done seed={seedType} block={seedType} candidates=1 (self-block)");
                return new FurnitureSeedWorkspaceResolution
                {
                    SeedType = seedType,
                    MaterialBlock = seedType,
                    MaterialCandidates = new List<int> { seedType },
                    OpenMaterialPicker = false
                };
            }

            FurnitureReverseSeedProbe probe = FurnitureReverseSeedProbeCache.Ensure(seedType);
            List<int> candidates = probe.PickerCandidates ?? new List<int>();
            int block = FurnitureReverseRecipeIngredients.PickDefaultPlaceableBlock(seedType, candidates);
            if (block <= ItemID.None)
                block = FurnitureMaterialBlockResolver.ResolvePlaceableBlockFromProbe(
                    seedType, probe.BestAnchorIngredient, probe.SeedSignature);

            block = FurnitureVanillaLivingWoodBridge.RedirectReverseAnchor(seedType, block);
            block = FurnitureSetMaterialRules.ResolveModMaterialBlock(seedType, block);
            FurnitureSetMaterialRules.ApplyLivingWoodRecipeMaterial(seedType, ref block);

            FurnitureBlueprintLog.Info(
                $"seed probe done seed={seedType} block={block} candidates={candidates.Count}");

            if (block > ItemID.None
                && FurnitureSetCacheSystem.TryGetCachedSchemeForItem(
                    seedType, block, out FurnitureScheme cached, out int cachedMat))
            {
                return new FurnitureSeedWorkspaceResolution
                {
                    SeedType = seedType,
                    MaterialBlock = block,
                    MaterialCandidates = candidates,
                    AppliedFromCache = true,
                    CachedScheme = cached,
                    CachedMaterialBlock = cachedMat > ItemID.None ? cachedMat : block
                };
            }

            return new FurnitureSeedWorkspaceResolution
            {
                SeedType = seedType,
                MaterialBlock = block,
                MaterialCandidates = candidates,
                OpenMaterialPicker = block <= ItemID.None && candidates.Count > 0
            };
        }
    }
}
