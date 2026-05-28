namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    /// <summary>样板房物理结构单元（与 replace 规则分离；未来 .eopjbp structure.tag）。</summary>
    public struct StructureCell
    {
        public StructureCellContent Content;
        public FurnitureSlotKind Kind;
        public bool HasWall;
        public bool Flip;

        public static StructureCell FromLegacyCell(BlueprintCell cell) => new()
        {
            Content = InferContent(cell),
            Kind = cell.Kind,
            HasWall = cell.HasWall,
            Flip = cell.Flip
        };

        public readonly BlueprintCell ToLegacyCell() => new()
        {
            Kind = Content switch
            {
                StructureCellContent.Air => FurnitureSlotKind.None,
                StructureCellContent.WallOnly => FurnitureSlotKind.None,
                _ => Kind
            },
            HasWall = HasWall,
            Flip = Flip
        };

        private static StructureCellContent InferContent(BlueprintCell cell)
        {
            if (cell.Kind is FurnitureSlotKind.Block or FurnitureSlotKind.Platform)
                return StructureCellContent.Tile;

            if (cell.Kind != FurnitureSlotKind.None)
                return StructureCellContent.FurnitureAnchor;

            return cell.HasWall ? StructureCellContent.WallOnly : StructureCellContent.Air;
        }
    }

    public enum StructureCellContent : byte
    {
        Air = 0,
        Tile = 1,
        WallOnly = 2,
        FurnitureAnchor = 3
    }
}
