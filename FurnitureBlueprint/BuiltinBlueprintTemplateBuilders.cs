using System;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>内置样板房 C# 回退布局（供 Register 与资源导出共用）。</summary>
    internal static class BuiltinBlueprintTemplateBuilders
    {
        public static BlueprintLayout BuildSimpleNpcRoom() =>
            BuildBorderRoom(20, 14, "simple_npc_room", "Blueprint.Template.SimpleNpcRoom", stampInterior: true);

        public static BlueprintLayout BuildNpcRoomA() =>
            BuildBorderRoom(24, 16, "npc_room_a", "Blueprint.Template.Preset.RoomA", stampInterior: true);

        public static BlueprintLayout BuildNpcRoomB() =>
            BuildBorderRoom(18, 12, "npc_room_b", "Blueprint.Template.Preset.RoomB", stampInterior: true);

        public static BlueprintLayout BuildNpcRoomC() =>
            BuildBorderRoom(20, 14, "npc_room_c", "Blueprint.Template.Preset.RoomC", stampInterior: true);

        public static BlueprintLayout BuildCompactShelter()
        {
            const int w = 14;
            const int h = 10;
            var cells = BuildBorderCells(w, h);
            int cx = w / 2;
            int cy = h / 2;
            StampMulti(cells, w, cx - 1, cy, FurnitureSlotKind.Candelabra, 2, 2);
            StampMulti(cells, w, cx, cy + 1, FurnitureSlotKind.Chair, 1, 2);
            cells[cx + (cy + 1) * w].Flip = true;
            StampMulti(cells, w, cx, 3, FurnitureSlotKind.Lantern, 1, 2);
            return new BlueprintLayout("compact_shelter", "Blueprint.Template.CompactShelter", w, h, cells);
        }

        internal static BlueprintLayout BuildFallbackById(string id) => id switch
        {
            "simple_npc_room" => BuildSimpleNpcRoom(),
            "npc_room_a" => BuildNpcRoomA(),
            "npc_room_b" => BuildNpcRoomB(),
            "npc_room_c" => BuildNpcRoomC(),
            "compact_shelter" => BuildCompactShelter(),
            _ => null
        };

        private static BlueprintLayout BuildBorderRoom(int w, int h, string id, string nameKey, bool stampInterior)
        {
            var cells = BuildBorderCells(w, h);
            if (!stampInterior)
                return new BlueprintLayout(id, nameKey, w, h, cells);

            int cx = w / 2;
            int cy = h / 2;
            StampMulti(cells, w, cx - 1, cy, FurnitureSlotKind.Table, 3, 2);
            cells[cx + (cy + 1) * w].Kind = FurnitureSlotKind.Chair;
            cells[cx + (cy + 1) * w].Flip = true;
            StampMulti(cells, w, cx - 1, cy - 3, FurnitureSlotKind.Bed, 4, 2);
            cells[cx + 2 + cy * w].Kind = FurnitureSlotKind.Lantern;
            cells[2 + 2 * w].Kind = FurnitureSlotKind.Workbench;
            cells[2 + 4 * w].Kind = FurnitureSlotKind.Chest;
            return new BlueprintLayout(id, nameKey, w, h, cells);
        }

        private static BlueprintCell[] BuildBorderCells(int w, int h)
        {
            var cells = new BlueprintCell[w * h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    ref BlueprintCell c = ref cells[x + y * w];
                    bool border = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    if (border)
                    {
                        c.Kind = FurnitureSlotKind.Block;
                        c.HasWall = true;
                    }
                    else
                    {
                        c.Kind = FurnitureSlotKind.None;
                        c.HasWall = true;
                    }

                    if (x == w / 2 && y == h - 1)
                    {
                        c.Kind = FurnitureSlotKind.Door;
                        c.HasWall = false;
                    }
                }
            }

            return cells;
        }

        private static void StampMulti(
            BlueprintCell[] cells,
            int width,
            int originX,
            int originY,
            FurnitureSlotKind kind,
            int sizeX,
            int sizeY)
        {
            for (int u = 0; u < sizeX; u++)
            {
                for (int v = 0; v < sizeY; v++)
                {
                    int x = originX + u;
                    int y = originY - v;
                    if (x < 0 || y < 0 || x >= width)
                        continue;
                    int idx = x + y * width;
                    if (idx >= 0 && idx < cells.Length)
                        cells[idx].Kind = kind;
                }
            }
        }
    }
}
