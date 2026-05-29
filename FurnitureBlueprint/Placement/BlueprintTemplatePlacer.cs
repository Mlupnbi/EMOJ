using System.Collections.Generic;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;
using EvenMoreOverpoweredJourney.SuperAdmin;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ObjectData;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Placement
{
    /// <summary>
    /// 放置流程：① 整区清空 → ② 框架（墙 → 实心块/平台）并校验 → ③ 家具。
    /// </summary>
    public static class BlueprintTemplatePlacer
    {
        public enum PlaceRejectReason : byte
        {
            None = 0,
            Busy = 1,
            Strict = 2,
            Nothing = 3,
            Framework = 4
        }

        public static PlaceRejectReason LastRejectReason { get; private set; }

        internal enum FurniturePhase : byte
        {
            MultiTile = 0,
            SingleTile = 1
        }

        internal enum PlacementOpKind : byte
        {
            Wall = 0,
            Tile = 1,
            Furniture = 2
        }

        internal readonly struct PlacementOp(PlacementOpKind kind, int cellIndex, FurniturePhase furniturePhase = FurniturePhase.SingleTile)
        {
            public readonly PlacementOpKind Kind = kind;
            public readonly int CellIndex = cellIndex;
            public readonly FurniturePhase FurniturePhase = furniturePhase;
        }

        internal readonly struct PlacementPlan(List<PlacementOp> frameworkOps, List<PlacementOp> furnitureOps)
        {
            public readonly List<PlacementOp> FrameworkOps = frameworkOps;
            public readonly List<PlacementOp> FurnitureOps = furnitureOps;

            public int TotalOps => (FrameworkOps?.Count ?? 0) + (FurnitureOps?.Count ?? 0);
        }

        private readonly struct FurnitureWork(int cellIndex, FurniturePhase phase)
        {
            public readonly int CellIndex = cellIndex;
            public readonly FurniturePhase Phase = phase;
        }

        private const int SyncTileChunk = 16;
        private static readonly List<Point> FootprintScratch = new();
        private static readonly HashSet<Point> FootprintDedupe = new();

        public static bool TryPlace(
            Player player,
            BlueprintTemplate template,
            FurnitureScheme scheme,
            bool consumeMaterials,
            BlueprintPlacementMode mode = BlueprintPlacementMode.Strict)
        {
            LastRejectReason = PlaceRejectReason.None;

            if (player == null || template == null || scheme == null || Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            if (BlueprintTemplatePlacementRunner.IsBusy)
            {
                LastRejectReason = PlaceRejectReason.Busy;
                return false;
            }

            if (player.TryGetModPlayer(out FurnitureBlueprintPlayer fb) && fb.RecognitionBusy)
            {
                LastRejectReason = PlaceRejectReason.Busy;
                return false;
            }

            Point origin = Main.MouseWorld.ToTileCoordinates() - new Point(template.Width / 2, template.Height / 2);
            bool consume = consumeMaterials && !SuperAdminSession.DebugFillTheBlueprint;

            if (!TryValidateBeforePlace(player, template, scheme, consume, mode, out PlacementPlan plan))
                return false;

            if (template.Width * template.Height >= BlueprintTemplatePlacementRunner.AsyncCellThreshold)
                return BlueprintTemplatePlacementRunner.TryEnqueue(player, template, scheme, origin, consume, mode, plan);

            ClearPlacementArea(template, scheme, origin);

            foreach (PlacementOp op in plan.FrameworkOps)
                ExecuteOp(player, template, scheme, origin, op, consume);

            if (!VerifyFramework(template, scheme, origin, plan.FrameworkOps, mode))
            {
                LastRejectReason = PlaceRejectReason.Framework;
                FurnitureBlueprintLog.Warn($"template framework verify failed mode={mode}");
                return mode != BlueprintPlacementMode.Strict;
            }

            bool anyFurniture = ExecuteFurniturePhase(player, template, scheme, origin, plan.FurnitureOps, consume);
            if (!anyFurniture && plan.FurnitureOps.Count > 0 && mode == BlueprintPlacementMode.Strict)
            {
                LastRejectReason = PlaceRejectReason.Nothing;
                FurnitureBlueprintLog.Warn("template place furniture phase nothing strict");
                return false;
            }

            SyncTileSquares(origin, template.Width, template.Height);
            SoundEngine.PlaySound(SoundID.Item14, Main.MouseWorld);
            FurnitureBlueprintLog.Info($"template place ok at {origin.X},{origin.Y} mode={mode}");
            return true;
        }

        internal static bool ExecuteOp(
            Player player,
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            PlacementOp op,
            bool consume)
        {
            int idx = op.CellIndex;
            int x = idx % template.Width;
            int y = idx / template.Width;
            int wx = origin.X + x;
            int wy = origin.Y + y;
            StructureCell structure = template.Structure[idx];
            ReplaceRule rule = template.ReplaceRules[idx];

            return op.Kind switch
            {
                PlacementOpKind.Wall => TryPlaceWall(player, scheme, rule, wx, wy, consume),
                PlacementOpKind.Tile => TryPlaceTile(player, scheme, rule, structure.Kind, wx, wy, consume),
                PlacementOpKind.Furniture => TryPlaceFurniture(player, template, scheme, origin, rule, structure, wx, wy, consume),
                _ => false
            };
        }

        internal static void SyncTileSquares(Point origin, int width, int height)
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            for (int y = 0; y < height; y += SyncTileChunk)
            {
                int h = y + SyncTileChunk > height ? height - y : SyncTileChunk;
                for (int x = 0; x < width; x += SyncTileChunk)
                {
                    int w = x + SyncTileChunk > width ? width - x : SyncTileChunk;
                    NetMessage.SendTileSquare(-1, origin.X + x, origin.Y + y, w, h);
                }
            }
        }

        /// <summary>模板矩形内全部 KillTile + KillWall，并清理多格家具 footprint 外扩格。</summary>
        internal static void ClearPlacementArea(BlueprintTemplate template, FurnitureScheme scheme, Point origin)
        {
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    ClearWorldCell(origin.X + x, origin.Y + y);
                }
            }

            FootprintDedupe.Clear();
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    int idx = x + y * template.Width;
                    StructureCell structure = template.Structure[idx];
                    if (structure.Content != StructureCellContent.FurnitureAnchor || !IsSlotGroupLeader(template, idx))
                        continue;

                    ReplaceRule rule = template.ReplaceRules[idx];
                    if (!TryResolveItemType(rule, scheme, out int itemType))
                        continue;

                    Item item = new Item();
                    item.SetDefaults(itemType);
                    FurniturePlacementRules.CollectFootprintWorldTiles(item, origin.X + x, origin.Y + y, FootprintScratch);
                    foreach (Point wp in FootprintScratch)
                    {
                        if (!FootprintDedupe.Add(wp))
                            continue;
                        ClearWorldCell(wp.X, wp.Y);
                    }
                }
            }
        }

        private static void ClearWorldCell(int wx, int wy)
        {
            if (!WorldGen.InWorld(wx, wy, 1))
                return;

            WorldGen.KillTile(wx, wy, fail: false, effectOnly: false, noItem: true);
            WorldGen.KillWall(wx, wy, false);
        }

        internal static bool ExecuteFurniturePhase(
            Player player,
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            List<PlacementOp> furnitureOps,
            bool consume)
        {
            if (furnitureOps == null || furnitureOps.Count == 0)
                return false;

            bool any = false;
            foreach (PlacementOp op in furnitureOps)
                any |= ExecuteOp(player, template, scheme, origin, op, consume);
            return any;
        }

        /// <summary>框架阶段结束后检查：严格模式要求计划内的墙/块/平台全部到位。</summary>
        internal static bool VerifyFramework(
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            List<PlacementOp> frameworkOps,
            BlueprintPlacementMode mode)
        {
            if (frameworkOps == null || frameworkOps.Count == 0)
                return true;

            int failures = 0;
            foreach (PlacementOp op in frameworkOps)
            {
                if (!IsFrameworkOpSatisfied(template, scheme, origin, op))
                    failures++;
            }

            if (mode == BlueprintPlacementMode.Strict)
                return failures == 0;

            return failures < frameworkOps.Count;
        }

        internal static void AbortWithReason(PlaceRejectReason reason) => LastRejectReason = reason;

        private static bool IsFrameworkOpSatisfied(
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            PlacementOp op)
        {
            int idx = op.CellIndex;
            int lx = idx % template.Width;
            int ly = idx / template.Width;
            int wx = origin.X + lx;
            int wy = origin.Y + ly;
            if (!WorldGen.InWorld(wx, wy, 1))
                return false;

            StructureCell structure = template.Structure[idx];
            ReplaceRule rule = template.ReplaceRules[idx];

            if (op.Kind == PlacementOpKind.Wall)
            {
                if (!structure.HasWall)
                    return true;

                if (!TryResolveWallItemType(rule, scheme, out int wallItemType))
                    return false;

                Item item = new Item();
                item.SetDefaults(wallItemType);
                if (item.createWall <= WallID.None)
                    return false;

                return Main.tile[wx, wy].WallType == item.createWall;
            }

            if (op.Kind == PlacementOpKind.Tile)
            {
                if (structure.Content != StructureCellContent.Tile)
                    return true;

                FurnitureSlotKind kind = ResolveMaterialKind(rule, structure);
                if (kind is not (FurnitureSlotKind.Block or FurnitureSlotKind.Platform))
                    return false;

                if (!TryResolveItemType(rule, scheme, out int tileItemType))
                    return false;

                Item item = new Item();
                item.SetDefaults(tileItemType);
                if (item.createTile < TileID.Dirt)
                    return false;

                Tile tile = Main.tile[wx, wy];
                return tile.HasTile && tile.TileType == item.createTile;
            }

            return true;
        }

        private static bool TryValidateBeforePlace(
            Player player,
            BlueprintTemplate template,
            FurnitureScheme scheme,
            bool consume,
            BlueprintPlacementMode mode,
            out PlacementPlan plan)
        {
            plan = default;
            LastRejectReason = PlaceRejectReason.None;

            if (mode == BlueprintPlacementMode.Strict)
            {
                if (!HasSchemeCoverage(template, scheme))
                {
                    LastRejectReason = PlaceRejectReason.Strict;
                    FurnitureBlueprintLog.Warn("template place aborted strict missing scheme slots");
                    return false;
                }

                if (consume && !HasEnoughMaterials(player, template, scheme))
                {
                    LastRejectReason = PlaceRejectReason.Strict;
                    FurnitureBlueprintLog.Warn("template place aborted strict insufficient materials");
                    return false;
                }
            }

            if (!TryBuildPlacementPlan(template, scheme, out List<PlacementOp> frameworkOps, out List<PlacementOp> furnitureOps)
                || frameworkOps.Count + furnitureOps.Count == 0)
            {
                LastRejectReason = PlaceRejectReason.Nothing;
                return false;
            }

            plan = new PlacementPlan(frameworkOps, furnitureOps);
            return true;
        }

        private static bool TryBuildPlacementPlan(
            BlueprintTemplate template,
            FurnitureScheme scheme,
            out List<PlacementOp> frameworkOps,
            out List<PlacementOp> furnitureOps)
        {
            frameworkOps = new List<PlacementOp>();
            furnitureOps = new List<PlacementOp>();
            var furnitureWork = new List<FurnitureWork>();

            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    int idx = x + y * template.Width;
                    if (template.Structure[idx].HasWall)
                        frameworkOps.Add(new PlacementOp(PlacementOpKind.Wall, idx));
                }
            }

            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    int idx = x + y * template.Width;
                    StructureCell structure = template.Structure[idx];
                    if (structure.Content == StructureCellContent.Tile)
                        frameworkOps.Add(new PlacementOp(PlacementOpKind.Tile, idx));
                }
            }

            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    int idx = x + y * template.Width;
                    StructureCell structure = template.Structure[idx];
                    ReplaceRule rule = template.ReplaceRules[idx];

                    if (structure.Content != StructureCellContent.FurnitureAnchor)
                        continue;

                    if (!IsSlotGroupLeader(template, idx))
                        continue;

                    if (!TryResolveItemType(rule, scheme, out int itemType))
                        continue;

                    Item probe = new Item();
                    probe.SetDefaults(itemType);
                    FurnitureSlotKind materialKind = ResolveMaterialKind(rule, structure);
                    if (!ImproveGameMaterialCheckers.ItemMatchesSlot(probe, materialKind))
                        continue;

                    furnitureWork.Add(new FurnitureWork(
                        idx,
                        IsMultiTilePlacement(probe) ? FurniturePhase.MultiTile : FurniturePhase.SingleTile));
                }
            }

            furnitureWork.Sort(static (a, b) => a.Phase.CompareTo(b.Phase));
            foreach (FurnitureWork work in furnitureWork)
                furnitureOps.Add(new PlacementOp(PlacementOpKind.Furniture, work.CellIndex, work.Phase));

            return frameworkOps.Count + furnitureOps.Count > 0;
        }

        private static bool HasSchemeCoverage(BlueprintTemplate template, FurnitureScheme scheme)
        {
            foreach (var pair in template.CountRequiredSlots())
            {
                if (scheme.GetSlot(pair.Key) <= ItemID.None)
                    return false;
            }

            return true;
        }

        private static bool HasEnoughMaterials(Player player, BlueprintTemplate template, FurnitureScheme scheme)
        {
            foreach (var pair in template.CountRequiredSlots())
            {
                int type = scheme.GetSlot(pair.Key);
                if (type <= ItemID.None)
                    return false;
                if (CountInInventory(player, type) < pair.Value)
                    return false;
            }

            return true;
        }

        private static bool IsSlotGroupLeader(BlueprintTemplate template, int index)
        {
            ReplaceRule rule = template.ReplaceRules[index];
            if (rule.Mode != ReplaceMode.SlotGroup)
                return true;

            FurnitureSlotKind kind = rule.SlotKind;
            byte groupId = rule.GroupId;
            for (int i = index + 1; i < template.ReplaceRules.Length; i++)
            {
                ReplaceRule later = template.ReplaceRules[i];
                if (later.Mode == ReplaceMode.SlotGroup
                    && later.SlotKind == kind
                    && later.GroupId == groupId)
                    return false;
            }

            return true;
        }

        private static bool TryResolveItemType(ReplaceRule rule, FurnitureScheme scheme, out int itemType)
        {
            itemType = ItemID.None;
            if (scheme == null)
                return false;

            switch (rule.Mode)
            {
                case ReplaceMode.Fixed:
                    itemType = rule.FixedItemType;
                    break;
                case ReplaceMode.Slot:
                case ReplaceMode.SlotGroup:
                    itemType = scheme.GetSlot(rule.SlotKind);
                    break;
            }

            return itemType > ItemID.None;
        }

        private static bool TryResolveWallItemType(ReplaceRule rule, FurnitureScheme scheme, out int itemType)
        {
            if (rule.Mode == ReplaceMode.Fixed && rule.FixedItemType > ItemID.None)
            {
                itemType = rule.FixedItemType;
                return true;
            }

            itemType = scheme.GetSlot(FurnitureSlotKind.Wall);
            return itemType > ItemID.None;
        }

        private static FurnitureSlotKind ResolveMaterialKind(ReplaceRule rule, StructureCell structure)
        {
            if (rule.RequiresSchemeMaterial)
                return rule.MaterialSlotKind;

            return structure.Kind;
        }

        private static bool CanAfford(Player player, int type, int amount, bool consume) =>
            !consume || CountInInventory(player, type) >= amount;

        private static int CountInInventory(Player player, int type)
        {
            int total = 0;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item it = player.inventory[i];
                if (it != null && !it.IsAir && it.type == type)
                    total += it.stack;
            }

            return total;
        }

        private static void Consume(Player player, int type, int amount)
        {
            if (SuperAdminSession.DebugFillTheBlueprint)
                return;

            int left = amount;
            for (int i = 0; i < player.inventory.Length && left > 0; i++)
            {
                Item it = player.inventory[i];
                if (it == null || it.IsAir || it.type != type)
                    continue;
                int take = left < it.stack ? left : it.stack;
                it.stack -= take;
                if (it.stack <= 0)
                    it.TurnToAir();
                left -= take;
            }
        }

        private static bool TryPlaceWall(
            Player player,
            FurnitureScheme scheme,
            ReplaceRule rule,
            int x,
            int y,
            bool consume)
        {
            if (!TryResolveWallItemType(rule, scheme, out int type))
                return false;

            if (!CanAfford(player, type, 1, consume))
                return false;

            Item item = new Item();
            item.SetDefaults(type);
            if (item.createWall <= WallID.None)
                return false;

            if (!WorldGen.InWorld(x, y, 1))
                return false;

            Tile tile = Main.tile[x, y];
            if (tile.WallType == item.createWall)
                return true;

            WorldGen.KillWall(x, y, false);
            WorldGen.PlaceWall(x, y, (ushort)item.createWall, true);
            if (consume)
                Consume(player, type, 1);
            return true;
        }

        private static bool TryPlaceTile(
            Player player,
            FurnitureScheme scheme,
            ReplaceRule rule,
            FurnitureSlotKind structureKind,
            int x,
            int y,
            bool consume)
        {
            FurnitureSlotKind kind = ResolveMaterialKind(rule, new StructureCell { Kind = structureKind });
            if (kind is not (FurnitureSlotKind.Block or FurnitureSlotKind.Platform))
                return false;

            if (!TryResolveItemType(rule, scheme, out int type))
                return false;

            if (!CanAfford(player, type, 1, consume))
                return false;

            Item item = new Item();
            item.SetDefaults(type);
            if (!ImproveGameMaterialCheckers.ItemMatchesSlot(item, kind))
                return false;

            if (Main.tile[x, y].HasTile)
                FurniturePlacementRules.PrepareCell(x, y);

            if (!FurnitureBlueprintPlaceability.TryBongBongPlace(x, y, item, player))
                return false;
            if (consume)
                Consume(player, type, 1);
            return true;
        }

        private static bool TryPlaceFurniture(
            Player player,
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            ReplaceRule rule,
            StructureCell structure,
            int x,
            int y,
            bool consume)
        {
            FurnitureSlotKind kind = ResolveMaterialKind(rule, structure);
            if (kind == FurnitureSlotKind.None)
                return false;

            if (!TryResolveItemType(rule, scheme, out int type))
                return false;

            if (!CanAfford(player, type, 1, consume))
                return false;

            Item item = new Item();
            item.SetDefaults(type);
            if (!ImproveGameMaterialCheckers.ItemMatchesSlot(item, kind))
                return false;

            FurniturePlacementRules.PrepareFootprint(item, x, y);

            if (!FurnitureBlueprintPlaceability.TryBongBongPlace(x, y, item, player))
            {
                RestoreTemplateWallsAfterFurnitureFailure(player, template, scheme, origin, x, y, item);
                FurnitureBlueprintLog.Warn($"template furniture failed kind={kind} type={type} at {x},{y}");
                return false;
            }

            ApplyFlip(kind, x, y, structure.Flip);

            if (consume)
                Consume(player, type, 1);
            return true;
        }

        private static void RestoreTemplateWallsAfterFurnitureFailure(
            Player player,
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            int anchorWorldX,
            int anchorWorldY,
            Item item)
        {
            FurniturePlacementRules.CollectFootprintWorldTiles(item, anchorWorldX, anchorWorldY, FootprintScratch);
            foreach (Point wp in FootprintScratch)
            {
                int lx = wp.X - origin.X;
                int ly = wp.Y - origin.Y;
                if (lx < 0 || ly < 0 || lx >= template.Width || ly >= template.Height)
                    continue;

                int idx = lx + ly * template.Width;
                if (!template.Structure[idx].HasWall)
                    continue;

                TryPlaceWall(player, scheme, template.ReplaceRules[idx], wp.X, wp.Y, consume: false);
            }
        }

        private static bool IsMultiTilePlacement(Item item)
        {
            if (item == null || item.IsAir || item.createTile < TileID.Dirt)
                return false;

            TileObjectData data = TileObjectData.GetTileData(item.createTile, item.placeStyle);
            if (data == null)
                return false;

            return data.Width > 1 || data.Height > 1;
        }

        private static void ApplyFlip(FurnitureSlotKind kind, int x, int y, bool flip)
        {
            if (!flip)
                return;

            switch (kind)
            {
                case FurnitureSlotKind.Chair:
                case FurnitureSlotKind.Toilet:
                    if (Main.tile[x, y].HasTile)
                        Main.tile[x, y].TileFrameX += 18;
                    if (y > 0 && Main.tile[x, y - 1].HasTile)
                        Main.tile[x, y - 1].TileFrameX += 18;
                    break;
                case FurnitureSlotKind.Bed:
                case FurnitureSlotKind.Bathtub:
                    for (int u = -1; u < 3; u++)
                    {
                        for (int v = -1; v < 1; v++)
                        {
                            if (!WorldGen.InWorld(x + u, y + v))
                                continue;
                            if (Main.tile[x + u, y + v].HasTile)
                                Main.tile[x + u, y + v].TileFrameX -= 72;
                        }
                    }
                    break;
            }
        }
    }
}
