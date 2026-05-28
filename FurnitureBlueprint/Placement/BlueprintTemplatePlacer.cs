using System.Collections.Generic;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;
using EvenMoreOverpoweredJourney.SuperAdmin;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Placement
{
    /// <summary>
    /// 基于 <see cref="BlueprintTemplate"/> structure + replace 的放置器。
    /// 顺序：墙 → 块/平台 → 多格家具 → 单格家具。
    /// </summary>
    public static class BlueprintTemplatePlacer
    {
        public enum PlaceRejectReason : byte
        {
            None = 0,
            Busy = 1,
            Strict = 2,
            Nothing = 3,
            HeavyOverlap = 4
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

        private readonly struct FurnitureWork(int cellIndex, FurniturePhase phase)
        {
            public readonly int CellIndex = cellIndex;
            public readonly FurniturePhase Phase = phase;
        }

        private const int SyncTileChunk = 16;

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

            if (!TryValidateBeforePlace(player, template, scheme, origin, consume, mode, out List<PlacementOp> ops))
                return false;

            if (template.Width * template.Height >= BlueprintTemplatePlacementRunner.AsyncCellThreshold)
                return BlueprintTemplatePlacementRunner.TryEnqueue(player, template, scheme, origin, consume, mode, ops);

            ClearPlacementArea(template, scheme, origin);

            bool anyPlaced = false;
            foreach (PlacementOp op in ops)
                anyPlaced |= ExecuteOp(player, template, scheme, origin, op, consume);

            if (!anyPlaced)
            {
                LastRejectReason = PlaceRejectReason.Nothing;
                FurnitureBlueprintLog.Warn($"template place nothing mode={mode}");
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
                PlacementOpKind.Furniture => TryPlaceFurniture(player, scheme, rule, structure, wx, wy, consume),
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

        private static bool TryValidateBeforePlace(
            Player player,
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            bool consume,
            BlueprintPlacementMode mode,
            out List<PlacementOp> ops)
        {
            ops = null;
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

            if (!TryBuildPlacementPlan(template, scheme, origin, out ops) || ops.Count == 0)
            {
                LastRejectReason = PlaceRejectReason.Nothing;
                return false;
            }

            return true;
        }

        /// <summary>放置前清理模板占用区域，避免旧家具/方块残留。</summary>
        internal static void ClearPlacementArea(BlueprintTemplate template, FurnitureScheme scheme, Point origin)
        {
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    int idx = x + y * template.Width;
                    StructureCell structure = template.Structure[idx];
                    ReplaceRule rule = template.ReplaceRules[idx];
                    int wx = origin.X + x;
                    int wy = origin.Y + y;
                    if (!WorldGen.InWorld(wx, wy, 1))
                        continue;

                    bool clear =
                        structure.HasWall
                        || structure.Content == StructureCellContent.Tile
                        || (structure.Content == StructureCellContent.FurnitureAnchor
                            && IsSlotGroupLeader(template, idx));

                    if (!clear)
                        continue;

                    WorldGen.KillTile(wx, wy, fail: false, effectOnly: false, noItem: true);
                    WorldGen.KillWall(wx, wy, false);
                }
            }

            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    int idx = x + y * template.Width;
                    if (template.Structure[idx].Content != StructureCellContent.FurnitureAnchor
                        || !IsSlotGroupLeader(template, idx))
                        continue;

                    ReplaceRule rule = template.ReplaceRules[idx];
                    if (!TryResolveItemType(rule, scheme, out int itemType))
                        continue;

                    Item item = new Item();
                    item.SetDefaults(itemType);
                    FurniturePlacementRules.PrepareFootprint(
                        item,
                        origin.X + x,
                        origin.Y + y);
                }
            }
        }

        private static bool TryBuildPlacementPlan(
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            out List<PlacementOp> ops)
        {
            ops = new List<PlacementOp>();
            var furnitureWork = new List<FurnitureWork>();

            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    int idx = x + y * template.Width;
                    StructureCell structure = template.Structure[idx];
                    ReplaceRule rule = template.ReplaceRules[idx];
                    int wx = origin.X + x;
                    int wy = origin.Y + y;

                    if (structure.HasWall)
                        ops.Add(new PlacementOp(PlacementOpKind.Wall, idx));

                    if (structure.Content == StructureCellContent.Tile)
                    {
                        ops.Add(new PlacementOp(PlacementOpKind.Tile, idx));
                        continue;
                    }

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
                ops.Add(new PlacementOp(PlacementOpKind.Furniture, work.CellIndex, work.Phase));

            return ops.Count > 0;
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
            int type;
            if (rule.Mode == ReplaceMode.Fixed && rule.FixedItemType > ItemID.None)
                type = rule.FixedItemType;
            else
            {
                type = scheme.GetSlot(FurnitureSlotKind.Wall);
                if (type <= ItemID.None)
                    return false;
            }

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

            FurniturePlacementRules.PrepareCell(x, y);
            if (!FurnitureBlueprintPlaceability.TryBongBongPlace(x, y, item, player))
                return false;
            if (consume)
                Consume(player, type, 1);
            return true;
        }

        private static bool TryPlaceFurniture(
            Player player,
            FurnitureScheme scheme,
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
                return false;

            ApplyFlip(kind, x, y, structure.Flip);

            if (consume)
                Consume(player, type, 1);
            return true;
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
