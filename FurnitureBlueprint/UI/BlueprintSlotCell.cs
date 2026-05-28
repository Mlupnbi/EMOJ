using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Localization;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    public sealed class BlueprintSlotCell : UIElement
    {
        public BlueprintSchemeSlot Slot { get; }
        public UIText Label { get; }

        public BlueprintSlotCell(FurnitureSlotKind kind, BlueprintSchemeSlot slot)
        {
            Slot = slot;
            Width.Set(BlueprintSlotGridPanel.CellSize, 0);
            Height.Set(BlueprintSlotGridPanel.CellSize + BlueprintSlotGridPanel.LabelHeight, 0);

            slot.Width.Set(BlueprintSlotGridPanel.CellSize, 0);
            slot.Height.Set(BlueprintSlotGridPanel.CellSize, 0);
            Append(slot);

            Label = new UIText(EOPJText.SlotLabel(kind), 0.5f)
            {
                Top = { Pixels = BlueprintSlotGridPanel.CellSize + 2f },
                Left = { Pixels = 0f },
                Width = { Pixels = BlueprintSlotGridPanel.CellSize },
                HAlign = 0.5f,
                IsWrapped = false,
                TextColor = Color.Gray,
                IgnoresMouseInteraction = true
            };
            Append(Label);
        }
    }
}
