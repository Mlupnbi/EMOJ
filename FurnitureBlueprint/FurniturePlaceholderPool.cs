using System.Collections.Generic;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>空槽占位只评少量候选，避免对全产物集反复计分。</summary>
    internal static class FurniturePlaceholderPool
    {
        public static List<int> Build(
            FurnitureSlotKind slot,
            Dictionary<FurnitureSlotKind, List<int>> perSlot,
            HashSet<int> expanded,
            HashSet<int> occupied,
            int seedType = ItemID.None,
            int materialBlock = ItemID.None,
            FurnitureRecognizeContext ctx = null)
        {
            var pool = new List<int>();
            var seen = new HashSet<int>();

            if (perSlot != null && perSlot.TryGetValue(slot, out List<int> bucket) && bucket != null)
            {
                foreach (int type in bucket)
                {
                    if (type <= ItemID.None || occupied != null && occupied.Contains(type))
                        continue;
                    if (seen.Add(type))
                        pool.Add(type);
                }
            }

            if (pool.Count >= FurnitureSlotScoring.MaxPlaceholderCandidates)
                return pool;

            if (expanded == null)
                return pool;

            foreach (int type in expanded)
            {
                if (pool.Count >= FurnitureSlotScoring.MaxPlaceholderCandidates)
                    break;
                if (type <= ItemID.None || !seen.Add(type))
                    continue;
                if (occupied != null && occupied.Contains(type))
                    continue;
                if (FurnitureStylePrefixCatalog.RequiresStyleGate(seedType, materialBlock)
                    && !FurnitureStylePrefixCatalog.ProductMatchesSeedStyle(type, seedType, materialBlock))
                    continue;
                if (!FurnitureSlotScoring.IsEligiblePlaceholderCandidate(type, slot, materialBlock, seedType, ctx))
                    continue;
                pool.Add(type);
            }

            return pool;
        }
    }
}
