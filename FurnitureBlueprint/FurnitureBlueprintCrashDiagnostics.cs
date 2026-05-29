using Terraria;
using Terraria.ID;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Registry;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>Native 闪退定位：在危险调用前写 10_blueprint.log，崩溃时看最后一行。</summary>
    internal static class FurnitureBlueprintCrashDiagnostics
    {
        private static int _seed = ItemID.None;

        /// <summary>多种子 batch 时关闭逐槽明细，只保留 phase / material 关键行。</summary>
        public static bool Verbose { get; set; } = true;

        public static void BeginSeed(int seed) => _seed = seed;

        public static void EndSeed() => _seed = ItemID.None;

        public static void Phase(string phase, string detail = null)
        {
            if (_seed <= ItemID.None)
                return;

            string tail = string.IsNullOrEmpty(detail) ? string.Empty : $" {detail}";
            FurnitureBlueprintLog.InfoFull($"crash-diag seed={_seed} phase={phase}{tail}");
        }

        public static void SlotStep(FurnitureSlotKind slot, string step, int itemType = ItemID.None)
        {
            if (!Verbose || _seed <= ItemID.None)
                return;

            if (itemType <= ItemID.None)
            {
                FurnitureBlueprintLog.InfoFull($"crash-diag seed={_seed} slot={slot} step={step}");
                return;
            }

            LogItem(slot, step, itemType);
        }

        public static void Item(FurnitureSlotKind slot, int itemType, string step)
        {
            if (!Verbose)
                return;

            LogItem(slot, step, itemType);
        }

        public static void Check(FurnitureSlotKind slot, int itemType, string check)
        {
            if (!Verbose || _seed <= ItemID.None || itemType <= ItemID.None)
                return;

            LogItem(slot, $"check={check}", itemType);
        }

        private static void LogItem(FurnitureSlotKind slot, string step, int itemType)
        {
            int tile = -1;
            int style = 0;
            int wall = WallID.None;
            string known = "?";

            if (FurnitureRecognitionCaches.TryGetProbe(itemType, out Item probe))
            {
                tile = probe.createTile;
                style = probe.placeStyle;
                wall = probe.createWall;
                if (tile >= TileID.Dirt)
                    known = FurnitureTileItemRegistry.IsKnownPlacementStyle(tile, style) ? "Y" : "N";
                else if (tile < TileID.Dirt && wall > WallID.None)
                    known = "wall";
                else
                    known = "no-tile";
            }

            FurnitureBlueprintLog.InfoFull(
                $"crash-diag seed={_seed} slot={slot} step={step} type={itemType} tile={tile} style={style} wall={wall} known={known}");
        }
    }
}
