using System.Collections.Generic;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>已合并至 <see cref="FurnitureWikiSlotPlaceholder"/>，保留类型以兼容旧调用。</summary>
    public static class FurnitureWikiSlotDiscoverer
    {
        public static void FillMissingWikiSlotsFromProducts(
            FurnitureScheme scheme,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature filterSig,
            HashSet<int> materialProducts,
            FurnitureCraftStationProfile stations,
            Dictionary<FurnitureSlotKind, List<int>> perSlot,
            HashSet<int> occupied,
            FurnitureRecognizeContext ctx) =>
            FurnitureWikiSlotPlaceholder.FillEmptySlotsOnce(
                scheme, seedType, materialBlock, filterSig, materialProducts, perSlot, stations, ctx);
    }
}
