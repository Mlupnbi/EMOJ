using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    public sealed class FurnitureBlueprintSchemeRow : UIElement
    {
        private const float RowPadding = 6f;
        private const float ActionButtonWidth = 44f;
        private const float ActionButtonHeight = 28f;
        private const float ActionGap = 4f;
        private const float ActionsBlockWidth = ActionButtonWidth * 3f + ActionGap * 2f;

        public readonly SchemeLibraryEntry Entry;

        private const string FallbackDelete = "\u5220\u9664";
        private const string FallbackEdit = "\u7f16\u8f91";
        private const string FallbackRename = "\u6539\u540d";

        public FurnitureBlueprintSchemeRow(
            SchemeLibraryEntry entry,
            Func<string> getSelectedId,
            Action<SchemeLibraryEntry> onSelect,
            Action<SchemeLibraryEntry> onRename,
            Action<SchemeLibraryEntry> onEdit,
            Action<SchemeLibraryEntry> onDelete)
        {
            Entry = entry;
            Width.Set(0, 1f);
            Height.Set(FurnitureBlueprintPageLayout.SchemeRowHeight, 0);

            bool selected = getSelectedId() == entry.Id;
            var panel = new UIPanel
            {
                Width = { Percent = 1f },
                Height = { Percent = 1f },
                BackgroundColor = selected
                    ? OPJourneyUiColors.TabActiveBackground
                    : new Color(30, 36, 48) * 0.9f,
                BorderColor = selected ? OPJourneyUiColors.AccentGoldOutline : OPJourneyUiColors.PanelBorder
            };
            panel.SetPadding(0);
            Append(panel);

            int filled = CountFilled(entry.Scheme);
            int coverType = entry.Scheme?.ResolveCoverItemType() ?? ItemID.None;
            float coverSize = FurnitureBlueprintPageLayout.SchemeRowCoverSize;
            float textLeft = RowPadding + coverSize + 8f;

            var cover = new BlueprintSeedSlot
            {
                ReturnPhysicalOnPlace = false,
                ReturnPhysicalOnClear = false,
                Left = { Pixels = RowPadding },
                VAlign = 0.5f,
                Width = { Pixels = coverSize },
                Height = { Pixels = coverSize }
            };
            cover.item = new Item();
            if (coverType > ItemID.None)
                cover.item.SetDefaults(coverType);
            cover.IgnoresMouseInteraction = true;
            panel.Append(cover);

            string label = $"{entry.DisplayName}  ({filled}/{FurnitureWikiSlots.TotalCount})";
            var nameText = new UIText(label, 0.68f)
            {
                Left = { Pixels = textLeft },
                VAlign = 0.5f,
                Width = { Pixels = -(ActionsBlockWidth + RowPadding * 2 + (textLeft - RowPadding)), Percent = 1f }
            };
            nameText.Top.Set(BlueprintUiFlatButton.DefaultTextNudgeY, 0f);
            panel.Append(nameText);

            float offset = 0f;
            AppendActionButton(panel, ref offset, EOPJText.BlueprintOr("BtnDeleteSet", FallbackDelete), onDelete);
            AppendActionButton(panel, ref offset, EOPJText.BlueprintOr("BtnEditSet", FallbackEdit), onEdit);
            AppendActionButton(panel, ref offset, EOPJText.BlueprintOr("BtnRenameSet", FallbackRename), onRename);

            panel.OnLeftClick += (_, _) =>
            {
                onSelect(entry);
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
        }

        private void AppendActionButton(
            UIPanel panel, ref float offsetFromRight, string label, Action<SchemeLibraryEntry> action)
        {
            float marginRight = RowPadding + offsetFromRight;
            offsetFromRight += ActionButtonWidth + ActionGap;

            var btn = BlueprintUiFlatButton.Create(label, ActionButtonWidth, ActionButtonHeight, () => action(Entry), 0.58f);
            btn.HAlign = 1f;
            btn.VAlign = 0.5f;
            btn.MarginRight = marginRight;
            panel.Append(btn);
        }

        private static int CountFilled(FurnitureScheme scheme)
        {
            if (scheme?.SlotItemTypes == null)
                return 0;
            int n = 0;
            for (int i = 0; i < scheme.SlotItemTypes.Length; i++)
            {
                if (scheme.SlotItemTypes[i] > ItemID.None)
                    n++;
            }
            return n;
        }
    }
}
