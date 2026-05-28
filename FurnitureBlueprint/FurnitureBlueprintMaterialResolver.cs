using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public static class FurnitureBlueprintMaterialResolver
    {
        public static int ResolveItemType(FurnitureScheme scheme, FurnitureSlotKind kind, out bool isMissing)
        {
            isMissing = false;
            if (scheme == null || kind == FurnitureSlotKind.None)
                return ItemID.None;

            int type = scheme.GetSlot(kind);
            if (type > ItemID.None)
                return type;

            isMissing = true;
            return FurnitureVanillaPlaceholders.Get(kind);
        }

        /// <summary>与放置器相同：按 replace 规则 + structure 解析材料（避免预览仍显示木质占位）。</summary>
        public static int ResolveTemplateCell(
            BlueprintTemplate template,
            int cellIndex,
            FurnitureScheme scheme,
            out bool isMissing)
        {
            isMissing = false;
            if (template == null || scheme == null || cellIndex < 0 || cellIndex >= template.ReplaceRules.Length)
                return ItemID.None;

            ReplaceRule rule = template.ReplaceRules[cellIndex];
            StructureCell structure = template.Structure[cellIndex];
            FurnitureSlotKind materialKind = ResolveMaterialKind(rule, structure);

            int type = ItemID.None;
            switch (rule.Mode)
            {
                case ReplaceMode.Fixed:
                    type = rule.FixedItemType;
                    break;
                case ReplaceMode.Slot:
                case ReplaceMode.SlotGroup:
                    type = scheme.GetSlot(rule.SlotKind);
                    break;
            }

            if (type > ItemID.None)
                return type;

            if (materialKind != FurnitureSlotKind.None && scheme.GetSlot(materialKind) > ItemID.None)
                type = scheme.GetSlot(materialKind);

            if (type > ItemID.None)
                return type;

            isMissing = true;
            return FurnitureVanillaPlaceholders.Get(
                materialKind != FurnitureSlotKind.None ? materialKind : structure.Kind);
        }

        private static FurnitureSlotKind ResolveMaterialKind(ReplaceRule rule, StructureCell structure)
        {
            if (rule.RequiresSchemeMaterial)
                return rule.MaterialSlotKind;

            return structure.Kind;
        }
    }
}
