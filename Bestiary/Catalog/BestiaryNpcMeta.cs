using Terraria.GameContent.Bestiary;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    public sealed class BestiaryNpcMeta
    {
        /// <summary>在 <see cref="Main.BestiaryDB.Entries"/> 中的序号；-1 表示未收录兜底。</summary>
        public int CatalogIndex = -1;

        /// <summary>????????????</summary>
        public int BestiarySortIndex = int.MaxValue;

        /// <summary>????????<see cref="HasBestiaryDisplayLabel"/> ? false ?????</summary>
        public int BestiaryDisplayIndex = int.MaxValue;

        public bool HasBestiaryDisplayLabel;

        public int NetId;
        public string StableKey = "";
        public string ModKey = "";
        public string DisplayName = "";
        public BestiaryNpcBand Band;
        public bool IsEventEnemy;
        public bool HasBestiaryEntry;
        public bool HiddenByNpcDrawModifier;
        public BestiaryEntry Entry;
        public int PortraitWidth;
        public int PortraitHeight;
    }
}
