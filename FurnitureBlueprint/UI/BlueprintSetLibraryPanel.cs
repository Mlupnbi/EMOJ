using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>Legacy secondary library shell; list lives on the main blueprint page.</summary>
    public sealed class BlueprintSetLibraryPanel : BlueprintSecondaryWindowBase
    {
        protected override string TitleLocalizationKey => "Blueprint.SetLibraryTitle";
        protected override string TitleFallback => "Set library";

        public BlueprintSetLibraryPanel(OPJourneyUI shell) : base(shell) { }

        protected override void BuildContent()
        {
            ContentHost.Append(new UIText(
                EOPJText.UIOr("Blueprint.SetLibraryMovedHint", "Saved sets are listed on the main blueprint tab."),
                0.68f)
            {
                Width = { Pixels = 0f, Percent = 1f },
                IsWrapped = true,
                TextColor = Color.LightGray
            });
        }

        public void RefreshSchemeList() => Shell.ActiveBlueprintPage?.RefreshSchemeList();
    }
}
