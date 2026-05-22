using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI.Components
{
    /// <summary>全部可见脸：抬高 UnlockState；Update/Draw 与原版 UIBestiaryEntryIcon 一致。</summary>
    public sealed class EMOJBestiaryAllVisibleEntryIcon : UIBestiaryEntryIcon
    {
        private readonly BestiaryEntry _entryRef;
        private readonly Texture2D _notUnlockedTexture;
        private readonly bool _isPortrait;

        public EMOJBestiaryAllVisibleEntryIcon(BestiaryEntry entry, bool isPortrait)
            : base(entry, isPortrait)
        {
            _entryRef = entry;
            _isPortrait = isPortrait;
            EojUiTextureCache.WarmTab(EojUiTab.Bestiary);
            _notUnlockedTexture = EojUiTextures.Bestiary.IconLocked;
        }

        public bool UsesPortraitMode => _isPortrait;

        public override void Update(GameTime gameTime)
        {
            bool hovered = IsMouseHovering || ForceHover;
            if (hovered)
            {
                int interval = BestiaryCardVisuals.GridIconAnimationInterval;
                if (interval > 1 && Main.GameUpdateCount % interval != 0)
                    return;
            }

            base.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            BestiaryUICollectionInfo info = BuildAllVisibleInfo();
            CalculatedStyle dimensions = GetDimensions();
            Rectangle iconBox = dimensions.ToRectangle();
            bool hovered = IsMouseHovering || ForceHover;

            if (_entryRef.Icon.GetUnlockState(info))
            {
                _entryRef.Icon.Draw(info, spriteBatch, new EntryIconDrawSettings
                {
                    iconbox = iconBox,
                    IsPortrait = _isPortrait,
                    IsHovered = hovered
                });
            }
            else if (_notUnlockedTexture != null)
            {
                Texture2D locked = _notUnlockedTexture;
                spriteBatch.Draw(
                    locked,
                    dimensions.Center(),
                    null,
                    Color.White * 0.15f,
                    0f,
                    locked.Size() * 0.5f,
                    1f,
                    SpriteEffects.None,
                    0f);
            }
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
