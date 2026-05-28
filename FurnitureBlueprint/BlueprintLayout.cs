using System;
using System.Collections.Generic;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public sealed class BlueprintLayout
    {
        public string Id { get; init; }
        public string DisplayNameKey { get; init; }
        public int Width { get; }
        public int Height { get; }
        public BlueprintCell[] Cells { get; }

        public BlueprintLayout(string id, string displayNameKey, int width, int height, BlueprintCell[] cells)
        {
            Id = id;
            DisplayNameKey = displayNameKey;
            Width = width;
            Height = height;
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            if (cells.Length != width * height)
                throw new ArgumentException("Cell count must equal width * height.");
        }

        public ref BlueprintCell this[int x, int y] => ref Cells[x + y * Width];

        /// <summary>方块/平台按格计材料；墙按 HasWall 计；其余家具槽位整图只计 1（对应一次 PlaceObject）。</summary>
        public IReadOnlyDictionary<FurnitureSlotKind, int> CountKinds()
        {
            var dict = new Dictionary<FurnitureSlotKind, int>();
            var furnitureKinds = new HashSet<FurnitureSlotKind>();

            foreach (BlueprintCell cell in Cells)
            {
                if (cell.HasWall)
                {
                    dict.TryGetValue(FurnitureSlotKind.Wall, out int w);
                    dict[FurnitureSlotKind.Wall] = w + 1;
                }

                if (cell.Kind == FurnitureSlotKind.None)
                    continue;

                if (CountsMaterialPerCell(cell.Kind))
                {
                    dict.TryGetValue(cell.Kind, out int n);
                    dict[cell.Kind] = n + 1;
                    continue;
                }

                if (furnitureKinds.Add(cell.Kind))
                    dict[cell.Kind] = 1;
            }

            return dict;
        }

        public static bool CountsMaterialPerCell(FurnitureSlotKind kind) =>
            kind is FurnitureSlotKind.Block or FurnitureSlotKind.Platform;
    }
}
