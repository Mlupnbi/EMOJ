using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI.Components
{
    /// <summary>全部显示脸：抬高 UnlockState；肖像区与原版 UIBestiaryEntryIcon 相同（父级裁剪框）。</summary>
    public sealed class EMOJBestiaryAllVisibleEntryIcon : UIBestiaryEntryIcon
    {
        private readonly BestiaryEntry _entryRef;

        public EMOJBestiaryAllVisibleEntryIcon(BestiaryEntry entry, bool isPortrait)
            : base(entry, isPortrait)
        {
            _entryRef = entry;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            BestiaryUICollectionInfo info = BuildAllVisibleInfo();
            Rectangle iconBox = Parent?.GetDimensions().ToRectangle() ?? GetDimensions().ToRectangle();
            bool hovered = IsMouseHovering || ForceHover;
            var settings = new EntryIconDrawSettings
            {
                iconbox = iconBox,
                IsPortrait = true,
                IsHovered = hovered
            };

            if (hovered)
                _entryRef.Icon.Update(info, iconBox, settings);

            _entryRef.Icon.Draw(info, spriteBatch, settings);
        }

        private BestiaryUICollectionInfo BuildAllVisibleInfo()
        {
            BestiaryUICollectionInfo info = _entryRef.UIInfoProvider.GetEntryUICollectionInfo();
            info.OwnerEntry = _entryRef;
            if (info.UnlockState < BestiaryEntryUnlockState.CanShowDropsWithoutDropRates_3)
                info.UnlockState = BestiaryEntryUnlockState.CanShowDropsWithoutDropRates_3;
            return info;
        }
    }
}
