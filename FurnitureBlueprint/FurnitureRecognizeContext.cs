using System.Collections.Generic;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>单次 Recognize 会话缓存：避免占位阶段重复分类/扫配方。</summary>
    public sealed class FurnitureRecognizeContext
    {
        private readonly Dictionary<int, FurnitureSlotKind> _classified = new Dictionary<int, FurnitureSlotKind>();
        private readonly Dictionary<long, int> _recipeScores = new Dictionary<long, int>();
        private readonly Dictionary<long, int> _candidateScores = new Dictionary<long, int>();
        private readonly HashSet<FurnitureSlotKind> _placeholderAttempted = new HashSet<FurnitureSlotKind>();
        private IReadOnlyList<string> _commonWords = System.Array.Empty<string>();

        public bool PlaceholderCommonWordBoost { get; set; }

        public FurnitureSetConfidenceTier ConfidenceTier { get; private set; } = FurnitureSetConfidenceTier.Medium;

        public int StyleAlignmentPercent { get; private set; }

        public bool StrictSlotGate => ConfidenceTier == FurnitureSetConfidenceTier.High;

        public void SetConfidence(FurnitureSetConfidenceReport report)
        {
            ConfidenceTier = report.Tier;
            StyleAlignmentPercent = report.StyleAlignmentPercent;
            _candidateScores.Clear();
        }

        /// <summary>每个空槽占位仅允许计分一次；重复调用返回 false。</summary>
        public bool TryBeginPlaceholderAttempt(FurnitureSlotKind slot) =>
            _placeholderAttempted.Add(slot);

        public void SetCommonWords(IReadOnlyList<string> words)
        {
            _commonWords = words ?? System.Array.Empty<string>();
            _candidateScores.Clear();
        }

        public void SetPlaceholderCommonWordBoost(bool enabled)
        {
            if (PlaceholderCommonWordBoost == enabled)
                return;

            PlaceholderCommonWordBoost = enabled;
            _candidateScores.Clear();
        }

        public IReadOnlyList<string> CommonWords => _commonWords;

        public void RecordClassification(int itemType, FurnitureSlotKind slot)
        {
            if (itemType > Terraria.ID.ItemID.None)
                _classified[itemType] = slot;
        }

        public bool TryGetClassification(int itemType, out FurnitureSlotKind slot) =>
            _classified.TryGetValue(itemType, out slot);

        public int GetRecipeScore(int itemType, FurnitureSlotKind slot, int materialBlock)
        {
            long key = Pack3(itemType, (int)slot, materialBlock);
            if (_recipeScores.TryGetValue(key, out int cached))
                return cached;

            int score = FurnitureRecipeSlotSignals.ComputeRecipeScore(itemType, slot, materialBlock);
            _recipeScores[key] = score;
            return score;
        }

        public int GetCandidateScore(
            int itemType,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations)
        {
            long key = Pack4(itemType, (int)slot, seedType, materialBlock);
            if (_candidateScores.TryGetValue(key, out int cached))
                return cached;

            int score = FurnitureSlotScoring.ComputeCandidateScore(
                itemType, slot, seedType, materialBlock, blockSig, stations, this);
            _candidateScores[key] = score;
            return score;
        }

        private static long Pack3(int a, int b, int c) =>
            ((long)(uint)a << 42) | ((long)(uint)b << 21) | (uint)c;

        private static long Pack4(int a, int b, int c, int d) =>
            ((long)(uint)a << 48) | ((long)(uint)b << 32) | ((long)(uint)c << 16) | (uint)d;
    }
}
