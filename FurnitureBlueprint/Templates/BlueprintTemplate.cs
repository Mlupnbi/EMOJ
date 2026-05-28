using System;
using System.Collections.Generic;
using Terraria.ModLoader.IO;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates
{
    /// <summary>
    /// 样板房模板：structure + replace 双数组，宽×高与 legacy <see cref="BlueprintLayout"/> 对齐。
    /// Phase 3.2 磁盘读写见 <see cref="BlueprintTemplateIO"/>（.eopjbp / 目录三文件）。
    /// </summary>
    public sealed class BlueprintTemplate
    {
        public const int FormatVersion = 1;

        public string Id { get; init; }
        public string DisplayNameKey { get; init; }
        public int Width { get; }
        public int Height { get; }
        public StructureCell[] Structure { get; }
        public ReplaceRule[] ReplaceRules { get; }

        public BlueprintTemplate(
            string id,
            string displayNameKey,
            int width,
            int height,
            StructureCell[] structure,
            ReplaceRule[] replaceRules)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayNameKey = displayNameKey ?? "";
            Width = width;
            Height = height;
            Structure = structure ?? throw new ArgumentNullException(nameof(structure));
            ReplaceRules = replaceRules ?? throw new ArgumentNullException(nameof(replaceRules));
            int count = width * height;
            if (structure.Length != count || replaceRules.Length != count)
                throw new ArgumentException("Structure and replace arrays must equal width * height.");
        }

        public ref StructureCell StructureAt(int x, int y) => ref Structure[x + y * Width];

        public ref ReplaceRule ReplaceAt(int x, int y) => ref ReplaceRules[x + y * Width];

        public static BlueprintTemplate FromLegacyLayout(BlueprintLayout layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            int count = layout.Width * layout.Height;
            var structure = new StructureCell[count];
            var rules = new ReplaceRule[count];
            byte nextGroupId = 1;
            var kindToGroup = new Dictionary<FurnitureSlotKind, byte>();

            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    BlueprintCell legacy = layout[x, y];
                    int idx = x + y * layout.Width;
                    structure[idx] = StructureCell.FromLegacyCell(legacy);
                    rules[idx] = ReplaceRuleFromLegacyCell(legacy, kindToGroup, ref nextGroupId);
                }
            }

            return new BlueprintTemplate(
                layout.Id,
                layout.DisplayNameKey,
                layout.Width,
                layout.Height,
                structure,
                rules);
        }

        public BlueprintLayout ToLegacyLayout()
        {
            int count = Width * Height;
            var cells = new BlueprintCell[count];
            for (int i = 0; i < count; i++)
                cells[i] = Structure[i].ToLegacyCell();
            return new BlueprintLayout(Id, DisplayNameKey, Width, Height, cells);
        }

        /// <summary>按替换规则统计套组所需槽位（与 <see cref="BlueprintLayout.CountKinds"/> 对齐）。</summary>
        public IReadOnlyDictionary<FurnitureSlotKind, int> CountRequiredSlots()
        {
            var dict = new Dictionary<FurnitureSlotKind, int>();
            var furnitureKinds = new HashSet<FurnitureSlotKind>();
            var slotGroupLeaders = new HashSet<(FurnitureSlotKind kind, byte group)>();

            for (int i = 0; i < ReplaceRules.Length; i++)
            {
                ReplaceRule rule = ReplaceRules[i];
                StructureCell cell = Structure[i];

                if (cell.HasWall)
                {
                    dict.TryGetValue(FurnitureSlotKind.Wall, out int wallCount);
                    dict[FurnitureSlotKind.Wall] = wallCount + 1;
                }

                if (!rule.RequiresSchemeMaterial)
                    continue;

                FurnitureSlotKind kind = rule.MaterialSlotKind;
                if (kind == FurnitureSlotKind.None)
                    continue;

                if (rule.Mode == ReplaceMode.SlotGroup)
                {
                    var key = (kind, rule.GroupId);
                    if (!slotGroupLeaders.Add(key))
                        continue;
                }

                if (BlueprintLayout.CountsMaterialPerCell(kind))
                {
                    dict.TryGetValue(kind, out int perCell);
                    dict[kind] = perCell + 1;
                    continue;
                }

                if (furnitureKinds.Add(kind))
                    dict[kind] = 1;
            }

            return dict;
        }

        public TagCompound ToTag()
        {
            int count = Width * Height;
            var structureTags = new List<TagCompound>(count);
            var replaceTags = new List<TagCompound>(count);
            for (int i = 0; i < count; i++)
            {
                StructureCell s = Structure[i];
                ReplaceRule r = ReplaceRules[i];
                structureTags.Add(new TagCompound
                {
                    ["c"] = (byte)s.Content,
                    ["k"] = (byte)s.Kind,
                    ["w"] = s.HasWall,
                    ["f"] = s.Flip
                });
                replaceTags.Add(new TagCompound
                {
                    ["m"] = (byte)r.Mode,
                    ["k"] = (byte)r.SlotKind,
                    ["g"] = r.GroupId,
                    ["i"] = r.FixedItemType
                });
            }

            return new TagCompound
            {
                ["ver"] = FormatVersion,
                ["id"] = Id,
                ["nameKey"] = DisplayNameKey ?? "",
                ["w"] = Width,
                ["h"] = Height,
                ["structure"] = structureTags,
                ["replace"] = replaceTags
            };
        }

        public static BlueprintTemplate FromTag(TagCompound tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            int width = tag.GetInt("w");
            int height = tag.GetInt("h");
            int count = width * height;
            var structure = new StructureCell[count];
            var rules = new ReplaceRule[count];

            IList<TagCompound> structureTags = tag.GetList<TagCompound>("structure");
            IList<TagCompound> replaceTags = tag.GetList<TagCompound>("replace");
            if (structureTags.Count != count || replaceTags.Count != count)
                throw new ArgumentException("Tag structure/replace length mismatch.");

            for (int i = 0; i < count; i++)
            {
                TagCompound s = structureTags[i];
                TagCompound r = replaceTags[i];
                structure[i] = new StructureCell
                {
                    Content = (StructureCellContent)s.GetByte("c"),
                    Kind = (FurnitureSlotKind)s.GetByte("k"),
                    HasWall = s.GetBool("w"),
                    Flip = s.GetBool("f")
                };
                rules[i] = new ReplaceRule
                {
                    Mode = (ReplaceMode)r.GetByte("m"),
                    SlotKind = (FurnitureSlotKind)r.GetByte("k"),
                    GroupId = r.GetByte("g"),
                    FixedItemType = r.GetInt("i")
                };
            }

            return new BlueprintTemplate(
                tag.GetString("id"),
                tag.GetString("nameKey"),
                width,
                height,
                structure,
                rules);
        }

        private static ReplaceRule ReplaceRuleFromLegacyCell(
            BlueprintCell cell,
            Dictionary<FurnitureSlotKind, byte> kindToGroup,
            ref byte nextGroupId)
        {
            if (cell.Kind == FurnitureSlotKind.None && !cell.HasWall)
                return ReplaceRule.Fixed();

            if (cell.HasWall && cell.Kind == FurnitureSlotKind.None)
                return ReplaceRule.ForSlot(FurnitureSlotKind.Wall);

            if (cell.Kind is FurnitureSlotKind.Block or FurnitureSlotKind.Platform)
                return ReplaceRule.ForSlot(cell.Kind);

            if (cell.Kind == FurnitureSlotKind.None)
                return ReplaceRule.Fixed();

            if (BlueprintLayout.CountsMaterialPerCell(cell.Kind))
                return ReplaceRule.ForSlot(cell.Kind);

            if (!kindToGroup.TryGetValue(cell.Kind, out byte groupId))
            {
                groupId = nextGroupId++;
                kindToGroup[cell.Kind] = groupId;
            }

            return ReplaceRule.ForSlotGroup(cell.Kind, groupId);
        }
    }
}
