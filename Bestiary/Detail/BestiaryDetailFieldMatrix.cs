using EvenMoreOverpoweredJourney.Bestiary.Catalog;

namespace EvenMoreOverpoweredJourney.Bestiary.Detail
{
    /// <summary>脸 × 详情字段可见性（与 <see cref="BestiaryVisibilityPolicy"/> 列表策略分离）。</summary>
    internal static class BestiaryDetailFieldMatrix
    {
        public enum FieldVisibility
        {
            Hidden,
            Visible,
            /// <summary>显示区块但不展示数值（如全部可见脸的掉落率）。</summary>
            VisibleWithoutNumbers
        }

        public static FieldVisibility GetVisibility(
            BestiaryFaceMode face,
            BestiaryDetailFieldId field,
            bool wasFound,
            bool fullyUnlocked)
        {
            if (face == BestiaryFaceMode.ProgressiveMinus && !wasFound)
                return FieldVisibility.Hidden;

            return (face, field) switch
            {
                (BestiaryFaceMode.AllVisible, BestiaryDetailFieldId.Drops) => FieldVisibility.VisibleWithoutNumbers,
                (BestiaryFaceMode.AllVisible, BestiaryDetailFieldId.Stats) => FieldVisibility.VisibleWithoutNumbers,
                (BestiaryFaceMode.ProgressivePlus, BestiaryDetailFieldId.Drops) when !wasFound => FieldVisibility.Hidden,
                (BestiaryFaceMode.ProgressivePlus, BestiaryDetailFieldId.Stats) when !wasFound => FieldVisibility.Hidden,
                (BestiaryFaceMode.UnlockedOnly, BestiaryDetailFieldId.Drops) when fullyUnlocked => FieldVisibility.Hidden,
                (BestiaryFaceMode.UnlockedOnly, BestiaryDetailFieldId.Stats) when fullyUnlocked => FieldVisibility.Hidden,
                (BestiaryFaceMode.UnlockedOnly, _) when fullyUnlocked => FieldVisibility.Visible,
                _ => FieldVisibility.Visible
            };
        }

        public static bool ShouldShow(BestiaryFaceMode face, BestiaryDetailFieldId field, bool wasFound, bool fullyUnlocked) =>
            GetVisibility(face, field, wasFound, fullyUnlocked) != FieldVisibility.Hidden;
    }
}
