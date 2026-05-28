using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>倒推无配方原料时，按 StyleKey 在同模组可放置方块中回退锚点。</summary>
    public static class FurnitureAnchorFallbackResolver
    {
        private const int MaxScan = 8000;

        public static int TryResolveBlockAnchor(int seedType, FurnitureStyleSignature seedSig)
        {
            if (seedType <= ItemID.None)
                return ItemID.None;

            string targetKey = seedSig.StyleKey?.Trim() ?? FurnitureSetRecognizer.ExtractStyleKeyPublic(seedType);
            if (string.IsNullOrWhiteSpace(targetKey))
                return ItemID.None;

            int best = ItemID.None;
            int bestScore = int.MinValue;
            int scanned = 0;

            for (int type = ItemID.None + 1; type < ItemLoader.ItemCount && scanned < MaxScan; type++)
            {
                scanned++;
                ModItem mi = ItemLoader.GetItem(type);
                if (mi == null || mi.Mod.Name != seedSig.ModKey)
                    continue;

                Item probe = new Item();
                probe.SetDefaults(type);
                if (!FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
                    continue;

                string blockKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(type);
                int score = ScoreBlockKey(targetKey, blockKey, probe.Name);
                if (score <= 0 || score <= bestScore)
                    continue;

                bestScore = score;
                best = type;
            }

            if (best > ItemID.None)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"anchor fallback seed={seedType} block={best} score={bestScore} style={targetKey}");
            }

            return best;
        }

        private static int ScoreBlockKey(string targetKey, string blockKey, string displayName)
        {
            if (string.Equals(blockKey, targetKey, System.StringComparison.OrdinalIgnoreCase))
                return 10_000;

            if (FurnitureMaterialKeyNormalizer.SameMaterialFamily(targetKey, blockKey))
                return 7_000;

            if (FurnitureStyleSignature.StyleKeyFuzzyMatch(targetKey, blockKey))
                return 4_000;

            if (!string.IsNullOrEmpty(displayName)
                && displayName.Contains(targetKey, System.StringComparison.OrdinalIgnoreCase))
                return 2_500;

            return 0;
        }
    }
}
