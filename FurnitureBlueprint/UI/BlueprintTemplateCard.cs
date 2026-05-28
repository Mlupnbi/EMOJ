using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    internal sealed class BlueprintTemplateCard : UIElement
    {
        private const float CardPadding = 8f;
        private const float TextWidthInset = -(CardPadding * 2f);

        public BlueprintTemplateCard(
            BlueprintLayout layout,
            bool selected,
            int configuredKinds,
            int requiredKinds,
            Action<BlueprintLayout> onPick)
        {
            Width.Set(0, 1f);
            Height.Set(FurnitureBlueprintPageLayout.TemplateCardHeight, 0);

            var panel = new UIPanel
            {
                Width = { Percent = 1f },
                Height = { Percent = 1f },
                BackgroundColor = selected
                    ? OPJourneyUiColors.TabActiveBackground
                    : new Color(30, 36, 48) * 0.92f,
                BorderColor = selected ? OPJourneyUiColors.AccentGoldOutline : OPJourneyUiColors.PanelBorder
            };
            panel.SetPadding(CardPadding);
            panel.OnLeftClick += (_, _) => onPick(layout);
            Append(panel);

            string name = BlueprintTemplateSecondaryPanel.ResolveTemplateName(layout.DisplayNameKey);
            panel.Append(new UIText(name, 0.72f)
            {
                Left = { Pixels = 0f },
                Top = { Pixels = 0f },
                Width = { Pixels = TextWidthInset, Percent = 1f },
                TextColor = Color.White
            });

            string sizeLine = EOPJText.UIOr("Blueprint.TemplateCardSizeFormat", "{0}×{1}")
                .Replace("{0}", layout.Width.ToString())
                .Replace("{1}", layout.Height.ToString());
            panel.Append(new UIText(sizeLine, 0.58f)
            {
                Left = { Pixels = 0f },
                Top = { Pixels = 20f + BlueprintUiFlatButton.DefaultTextNudgeY },
                Width = { Pixels = TextWidthInset, Percent = 1f },
                TextColor = Color.LightGray
            });

            bool ready = requiredKinds > 0 && configuredKinds >= requiredKinds;
            string partsFormat = EOPJText.BlueprintOr("TemplateCardPartsFormat", "\u6750\u6599 {0}/{1}");
            string partsLine = partsFormat
                .Replace("{0}", configuredKinds.ToString())
                .Replace("{1}", requiredKinds.ToString());
            panel.Append(new UIText(partsLine, 0.58f)
            {
                Left = { Pixels = 0f },
                Top = { Pixels = 34f + BlueprintUiFlatButton.DefaultTextNudgeY },
                Width = { Pixels = TextWidthInset, Percent = 1f },
                TextColor = ready ? new Color(140, 210, 150) : new Color(230, 170, 120)
            });
        }
    }

    /// <summary>??????? <see cref="BlueprintTemplateCard"/> ????</summary>
    internal sealed class TemplatePickerRow : UIElement
    {
        public TemplatePickerRow(BlueprintLayout layout, bool selected, Action<BlueprintLayout> onPick)
        {
            Width.Set(0, 1f);
            Height.Set(28, 0);

            var panel = new UIPanel
            {
                Width = { Percent = 1f },
                Height = { Percent = 1f },
                BackgroundColor = selected
                    ? OPJourneyUiColors.TabActiveBackground
                    : new Color(32, 38, 52) * 0.92f,
                BorderColor = selected ? OPJourneyUiColors.AccentGoldOutline : OPJourneyUiColors.PanelBorder
            };
            var label = new UIText(BlueprintTemplateSecondaryPanel.ResolveTemplateName(layout.DisplayNameKey), 0.7f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f
            };
            label.Top.Set(BlueprintUiFlatButton.DefaultTextNudgeY, 0f);
            panel.Append(label);
            panel.OnLeftClick += (_, _) => onPick(layout);
            Append(panel);
        }
    }
}
