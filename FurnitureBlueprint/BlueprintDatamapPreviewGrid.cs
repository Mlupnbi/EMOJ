using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// ImproveGame datamap 预览：多格家具展开 + 贴图 framing（参考 CreateWand.PreviewFrame）。
    /// 仅用于 UI/世界预览，不改变模板材料计数（计数仍用原始 anchor 格）。
    /// </summary>
    internal static class BlueprintDatamapPreviewGrid
    {
        public static BlueprintCell[,] BuildVisualGrid(BlueprintLayout layout)
        {
            int width = layout.Width;
            int height = layout.Height;
            var grid = new BlueprintCell[width, height];
            var overridden = new HashSet<Point>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    BlueprintCell source = layout[x, y];
                    var point = new Point(x, y);
                    if (overridden.Contains(point))
                    {
                        grid[x, y].HasWall = source.HasWall;
                        continue;
                    }

                    grid[x, y] = source;
                    ExpandAnchor(grid, width, height, x, y, source, overridden);
                }
            }

            return grid;
        }

        public static void GetTileFrame(
            BlueprintCell[,] grid,
            int width,
            int height,
            int x,
            int y,
            out int frameX,
            out int frameY)
        {
            frameX = frameY = 0;
            BlueprintCell current = grid[x, y];
            FurnitureSlotKind kind = current.Kind;
            if (kind == FurnitureSlotKind.None)
                return;

            switch (kind)
            {
                case FurnitureSlotKind.Block:
                    GetBlockFrame(grid, width, height, x, y, out frameX, out frameY);
                    frameX *= 18;
                    frameY *= 18;
                    return;

                case FurnitureSlotKind.Platform:
                    GetPlatformFrame(grid, width, height, x, y, out frameX, out frameY);
                    frameX *= 18;
                    return;

                case FurnitureSlotKind.Chair:
                case FurnitureSlotKind.Toilet:
                    frameX = current.Flip ? 18 : 0;
                    frameY = y > 0 && grid[x, y - 1].Kind == kind ? 18 : 0;
                    if (kind == FurnitureSlotKind.Toilet && frameY > 0)
                        frameY = 58;
                    else if (kind == FurnitureSlotKind.Toilet)
                        frameY = 40;
                    return;

                case FurnitureSlotKind.Lantern:
                    frameX = 0;
                    frameY = y > 0 && grid[x, y - 1].Kind == kind ? 18 : 0;
                    return;

                case FurnitureSlotKind.Table:
                case FurnitureSlotKind.Dresser:
                case FurnitureSlotKind.Piano:
                case FurnitureSlotKind.Sofa:
                    MultiTileFrame(grid, kind, x, y, width, height, 3, 2, out frameX, out frameY);
                    return;

                case FurnitureSlotKind.Chest:
                case FurnitureSlotKind.Candelabra:
                case FurnitureSlotKind.Sink:
                    MultiTileFrame(grid, kind, x, y, width, height, 2, 2, out frameX, out frameY);
                    return;

                case FurnitureSlotKind.Bookcase:
                    MultiTileFrame(grid, kind, x, y, width, height, 3, 4, out frameX, out frameY);
                    return;

                case FurnitureSlotKind.Bed:
                case FurnitureSlotKind.Bathtub:
                    MultiTileFrame(grid, kind, x, y, width, height, 4, 2, out frameX, out frameY);
                    if (current.Flip)
                        frameX += 72;
                    return;

                case FurnitureSlotKind.Lamp:
                case FurnitureSlotKind.Door:
                    MultiTileFrame(grid, kind, x, y, width, height, 1, 3, out frameX, out frameY);
                    if (kind == FurnitureSlotKind.Door)
                        frameX = StablePick(x, y, 3) * 18;
                    return;

                case FurnitureSlotKind.Workbench:
                    MultiTileFrame(grid, kind, x, y, width, height, 2, 1, out frameX, out frameY);
                    return;

                case FurnitureSlotKind.Chandelier:
                    MultiTileFrame(grid, kind, x, y, width, height, 3, 3, out frameX, out frameY);
                    return;

                case FurnitureSlotKind.Clock:
                    MultiTileFrame(grid, kind, x, y, width, height, 2, 5, out frameX, out frameY);
                    return;

                default:
                    return;
            }
        }

        public static void GetWallFrame(
            BlueprintCell[,] grid,
            int width,
            int height,
            int x,
            int y,
            out int frameX,
            out int frameY)
        {
            bool left = x > 0 && grid[x - 1, y].HasWall;
            bool right = x < width - 1 && grid[x + 1, y].HasWall;
            bool up = y > 0 && grid[x, y - 1].HasWall;
            bool down = y < height - 1 && grid[x, y + 1].HasWall;
            int index = 0;
            if (left) index |= 1;
            if (right) index |= 2;
            if (up) index |= 4;
            if (down) index |= 8;

            int pick = StablePick(x, y, 3);
            (frameX, frameY) = index switch
            {
                0 => (9 + pick, 3),
                1 => (12, pick),
                2 => (9, pick),
                3 => (6 + pick, 4),
                4 => (6 + pick, 3),
                5 => (1 + pick * 2, 4),
                6 => (pick * 2, 4),
                7 => (1 + pick, 2),
                8 => (6 + pick, 0),
                9 => (1 + pick * 2, 3),
                10 => (pick * 2, 3),
                11 => (1 + pick, 0),
                12 => (5, pick),
                13 => (4, pick),
                14 => (0, pick),
                _ => (6 + pick, 1)
            };
            frameX *= 18;
            frameY *= 18;
        }

        private static void ExpandAnchor(
            BlueprintCell[,] grid,
            int width,
            int height,
            int x,
            int y,
            BlueprintCell anchor,
            HashSet<Point> overridden)
        {
            FurnitureSlotKind kind = anchor.Kind;
            switch (kind)
            {
                case FurnitureSlotKind.Table:
                case FurnitureSlotKind.Dresser:
                case FurnitureSlotKind.Piano:
                case FurnitureSlotKind.Sofa:
                    StampRect(grid, width, height, x - 1, y, 3, 2, kind, anchor.Flip, overridden);
                    break;

                case FurnitureSlotKind.Chest:
                    StampRect(grid, width, height, x, y, 2, 2, kind, anchor.Flip, overridden);
                    break;

                case FurnitureSlotKind.Sink:
                case FurnitureSlotKind.Candelabra:
                    StampRect(grid, width, height, x, y, 2, 2, kind, anchor.Flip, overridden, mirrorX: true);
                    break;

                case FurnitureSlotKind.Bookcase:
                    StampRect(grid, width, height, x - 1, y, 3, 4, kind, anchor.Flip, overridden);
                    break;

                case FurnitureSlotKind.Bed:
                case FurnitureSlotKind.Bathtub:
                    StampRect(grid, width, height, x - 1, y, 4, 2, kind, anchor.Flip, overridden);
                    break;

                case FurnitureSlotKind.Chair:
                case FurnitureSlotKind.Toilet:
                    StampCell(grid, width, height, x, y - 1, kind, anchor.Flip, overridden);
                    break;

                case FurnitureSlotKind.Workbench:
                    StampCell(grid, width, height, x + 1, y, kind, anchor.Flip, overridden);
                    break;

                case FurnitureSlotKind.Lamp:
                case FurnitureSlotKind.Door:
                    StampCell(grid, width, height, x, y - 1, kind, anchor.Flip, overridden);
                    StampCell(grid, width, height, x, y - 2, kind, anchor.Flip, overridden);
                    break;

                case FurnitureSlotKind.Chandelier:
                    StampRect(grid, width, height, x - 1, y, 3, 3, kind, anchor.Flip, overridden, growDown: true);
                    break;

                case FurnitureSlotKind.Clock:
                    StampRect(grid, width, height, x, y, 2, 5, kind, anchor.Flip, overridden);
                    break;

                case FurnitureSlotKind.Lantern:
                    StampCell(grid, width, height, x, y + 1, kind, anchor.Flip, overridden);
                    break;
            }
        }

        private static void StampRect(
            BlueprintCell[,] grid,
            int width,
            int height,
            int originX,
            int originY,
            int sizeX,
            int sizeY,
            FurnitureSlotKind kind,
            bool flip,
            HashSet<Point> overridden,
            bool mirrorX = false,
            bool growDown = false)
        {
            for (int u = 0; u < sizeX; u++)
            {
                for (int v = 0; v < sizeY; v++)
                {
                    int tx = mirrorX ? originX - u : originX + u;
                    int ty = growDown ? originY + v : originY - v;
                    StampCell(grid, width, height, tx, ty, kind, flip, overridden);
                }
            }
        }

        private static void StampCell(
            BlueprintCell[,] grid,
            int width,
            int height,
            int x,
            int y,
            FurnitureSlotKind kind,
            bool flip,
            HashSet<Point> overridden)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;

            var point = new Point(x, y);
            if (overridden.Contains(point))
                return;

            grid[x, y] = new BlueprintCell
            {
                Kind = kind,
                HasWall = grid[x, y].HasWall,
                Flip = flip
            };
            overridden.Add(point);
        }

        private static void MultiTileFrame(
            BlueprintCell[,] grid,
            FurnitureSlotKind kind,
            int x,
            int y,
            int width,
            int height,
            int tileWidth,
            int tileHeight,
            out int frameX,
            out int frameY)
        {
            int offX = 0;
            while (x >= offX && SameKind(grid, x - offX, y, kind))
                offX++;
            offX--;

            int offY = 0;
            while (y >= offY && SameKind(grid, x, y - offY, kind))
                offY++;
            offY--;

            if (tileWidth > 0)
                offX %= tileWidth;
            if (offY >= tileHeight)
                offY = tileHeight - 1;

            frameX = offX * 18;
            frameY = offY * 18;
        }

        private static bool SameKind(BlueprintCell[,] grid, int x, int y, FurnitureSlotKind kind) =>
            x >= 0 && y >= 0 && x < grid.GetLength(0) && y < grid.GetLength(1) && grid[x, y].Kind == kind;

        private static void GetBlockFrame(
            BlueprintCell[,] grid,
            int width,
            int height,
            int x,
            int y,
            out int frameX,
            out int frameY)
        {
            bool left = x > 0 && grid[x - 1, y].Kind == FurnitureSlotKind.Block;
            bool right = x < width - 1 && grid[x + 1, y].Kind == FurnitureSlotKind.Block;
            bool up = y > 0 && grid[x, y - 1].Kind == FurnitureSlotKind.Block;
            bool down = y < height - 1 && grid[x, y + 1].Kind == FurnitureSlotKind.Block;
            int index = 0;
            if (left) index |= 1;
            if (right) index |= 2;
            if (up) index |= 4;
            if (down) index |= 8;

            int pick = StablePick(x, y, 3);
            (frameX, frameY) = index switch
            {
                0 => (9 + pick, 3),
                1 => (12, pick),
                2 => (9, pick),
                3 => (6 + pick, 4),
                4 => (6 + pick, 3),
                5 => (1 + pick * 2, 4),
                6 => (pick * 2, 4),
                7 => (1 + pick, 2),
                8 => (6 + pick, 0),
                9 => (1 + pick * 2, 3),
                10 => (pick * 2, 3),
                11 => (1 + pick, 0),
                12 => (5, pick),
                13 => (4, pick),
                14 => (0, pick),
                _ => (6 + pick, 1)
            };
        }

        private static void GetPlatformFrame(
            BlueprintCell[,] grid,
            int width,
            int height,
            int x,
            int y,
            out int frameX,
            out int frameY)
        {
            frameY = 0;
            int index = 0;
            if (x > 0)
            {
                FurnitureSlotKind left = grid[x - 1, y].Kind;
                if (left == FurnitureSlotKind.Block)
                    index += 1;
                if (left == FurnitureSlotKind.Platform)
                    index += 2;
            }

            if (x < width - 1)
            {
                FurnitureSlotKind right = grid[x + 1, y].Kind;
                if (right == FurnitureSlotKind.Block)
                    index += 3;
                if (right == FurnitureSlotKind.Platform)
                    index += 6;
            }

            frameX = index switch
            {
                1 => 6,
                2 => 1,
                3 => 7,
                5 => 4,
                6 => 2,
                7 => 3,
                8 => 0,
                _ => 5
            };
        }

        private static int StablePick(int x, int y, int count) =>
            count <= 1 ? 0 : Math.Abs(x * 17 + y * 31) % count;
    }
}
