using Terraria.GameContent.UI.Elements;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>ÒÆ³ý EntryButton ÄÚÔ­°æ Slot_Back UIImage£¨¼û <see cref="BestiaryVanillaEntryButtonLayers"/>£©¡£</summary>
    internal static class BestiaryEntryButtonVisuals
    {
        public static void StripVanillaBackgroundLayers(UIBestiaryEntryButton button) =>
            BestiaryVanillaEntryButtonLayers.StripOpaqueSlotBack(button);
    }
}
