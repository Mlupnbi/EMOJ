namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    /// <summary>脸 × 视图可见性；卡片网格走原版 UnlockState，不再用 BlackWithQuestion 覆盖。</summary>
    public static class BestiaryVisibilityPolicy
    {
        public enum ListAppearance
        {
            Hidden,
            FullPortraitAndName,
            SilhouetteAndName,
            /// <summary>仅筛选项 chip 等 UI 换肤；网格卡片不使用。</summary>
            BlackWithName,
            BlackWithQuestion
        }

        public static bool IsVisibleInList(BestiaryFaceMode face, bool wasFound, bool fullyUnlocked)
        {
            return face switch
            {
                BestiaryFaceMode.AllVisible => true,
                BestiaryFaceMode.ProgressivePlus => true,
                BestiaryFaceMode.ProgressiveMinus => wasFound,
                BestiaryFaceMode.UnlockedOnly => !fullyUnlocked,
                _ => true
            };
        }

        public static ListAppearance GetGroupPhotoAppearance(BestiaryFaceMode face, bool wasFound, bool fullyUnlocked)
        {
            if (!IsVisibleInList(face, wasFound, fullyUnlocked))
                return ListAppearance.Hidden;

            return face switch
            {
                BestiaryFaceMode.AllVisible => ListAppearance.FullPortraitAndName,
                BestiaryFaceMode.ProgressivePlus when !wasFound => ListAppearance.SilhouetteAndName,
                BestiaryFaceMode.ProgressivePlus => ListAppearance.FullPortraitAndName,
                BestiaryFaceMode.ProgressiveMinus => ListAppearance.FullPortraitAndName,
                BestiaryFaceMode.UnlockedOnly => ListAppearance.FullPortraitAndName,
                _ => ListAppearance.FullPortraitAndName
            };
        }

        /// <summary>网格卡片：始终 FullPortrait 或 Hidden，具体外观由原版 UnlockState 决定。</summary>
        public static ListAppearance GetCardAppearance(BestiaryFaceMode face, bool wasFound, bool fullyUnlocked)
        {
            if (!IsVisibleInList(face, wasFound, fullyUnlocked))
                return ListAppearance.Hidden;

            return ListAppearance.FullPortraitAndName;
        }

        public static ListAppearance GetFilterChipAppearance(BestiaryFaceMode face) =>
            face switch
            {
                BestiaryFaceMode.AllVisible => ListAppearance.FullPortraitAndName,
                BestiaryFaceMode.ProgressiveMinus => ListAppearance.BlackWithQuestion,
                _ => ListAppearance.BlackWithName
            };
    }
}
