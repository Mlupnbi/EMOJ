using Microsoft.Xna.Framework;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public struct BlueprintCell
    {
        public FurnitureSlotKind Kind;
        public bool HasWall;
        public bool Flip;

        /// <summary>
        /// ImproveGame CreateWand datamap：R/G 编码 <see cref="ImproveGameTileSortIndex"/>（0=None … 23=Campfire）。
        /// B&gt;127=墙，A&lt;128=翻转。与 IG <c>TileInfo.FromColor</c> 公式一致。
        /// 22 槽 wiki 套组无火把/篝火，索引 22/23 解码为 None（墙位仍走 HasWall）。
        /// </summary>
        private static readonly FurnitureSlotKind[] ImproveGameTileSortIndexToKind =
        {
            FurnitureSlotKind.None,       // 0 None
            FurnitureSlotKind.Block,      // 1
            FurnitureSlotKind.Platform,   // 2
            FurnitureSlotKind.Workbench,  // 3
            FurnitureSlotKind.Table,      // 4
            FurnitureSlotKind.Chair,      // 5
            FurnitureSlotKind.Door,       // 6
            FurnitureSlotKind.Chest,      // 7
            FurnitureSlotKind.Bed,        // 8
            FurnitureSlotKind.Bookcase,   // 9
            FurnitureSlotKind.Bathtub,    // 10
            FurnitureSlotKind.Candelabra, // 11
            FurnitureSlotKind.Candle,     // 12
            FurnitureSlotKind.Chandelier, // 13
            FurnitureSlotKind.Clock,      // 14
            FurnitureSlotKind.Dresser,    // 15
            FurnitureSlotKind.Lamp,       // 16
            FurnitureSlotKind.Lantern,    // 17
            FurnitureSlotKind.Piano,      // 18
            FurnitureSlotKind.Sink,       // 19
            FurnitureSlotKind.Sofa,       // 20
            FurnitureSlotKind.Toilet,     // 21
            FurnitureSlotKind.None,       // 22 Torch（22 槽不含）
            FurnitureSlotKind.None        // 23 Campfire
        };

        public static BlueprintCell FromColor(Color color) =>
            FromRgb(color.R, color.G, color.B, color.A);

        public static BlueprintCell FromArgb(int argb) =>
            FromRgb(
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF),
                (byte)((argb >> 24) & 0xFF));

        private static BlueprintCell FromRgb(byte r, byte g, byte b, byte a)
        {
            int pixelIndex = DecodeImproveGameTileSortIndex(r, g);
            FurnitureSlotKind kind = FurnitureSlotKind.None;
            if (pixelIndex >= 0 && pixelIndex < ImproveGameTileSortIndexToKind.Length)
                kind = ImproveGameTileSortIndexToKind[pixelIndex];

            return new BlueprintCell
            {
                Kind = kind,
                HasWall = b > 127,
                Flip = a < 128
            };
        }

        /// <summary>与 IG <c>TileInfo.FromColor</c> 相同：index = (R+1)/64*5 + (G+1)/64。</summary>
        public static int DecodeImproveGameTileSortIndex(byte r, byte g)
        {
            int index = (r + 1) / 64 * 5 + (g + 1) / 64;
            if (index is > 23 or < 0)
                index = 0;
            return index;
        }

        public readonly Color ToColor()
        {
            int index = EncodeImproveGameTileSortIndex(Kind);
            return new Color(
                index < 5 ? 0 : index / 5 * 64 - 1,
                index == 0 ? 0 : index % 5 * 64 - 1,
                HasWall ? (byte)255 : (byte)0,
                Flip ? (byte)127 : (byte)255);
        }

        private static int EncodeImproveGameTileSortIndex(FurnitureSlotKind kind)
        {
            if (kind == FurnitureSlotKind.None)
                return 0;

            for (int i = 1; i < ImproveGameTileSortIndexToKind.Length; i++)
            {
                if (ImproveGameTileSortIndexToKind[i] == kind)
                    return i;
            }

            return 0;
        }
    }
}
