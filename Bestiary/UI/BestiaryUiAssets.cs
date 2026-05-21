using System;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>原版图鉴 UI 贴图（Slot_Back 等），与 ImproveGame / 原版一致。</summary>
    internal static class BestiaryUiAssets
    {
        private static bool _tried;
        private static Texture2D _slotBack;
        private static Texture2D _slotFront;
        private static Texture2D _iconTagsShadow;

        public static Texture2D SlotBack
        {
            get
            {
                EnsureLoaded();
                return _slotBack;
            }
        }

        public static Texture2D SlotFront
        {
            get
            {
                EnsureLoaded();
                return _slotFront;
            }
        }

        public static Texture2D IconTagsShadow
        {
            get
            {
                EnsureLoaded();
                return _iconTagsShadow;
            }
        }

        public static void EnsureLoaded()
        {
            if (_tried)
                return;

            _tried = true;
            _slotBack = TryLoad("Images/UI/Bestiary/Slot_Back");
            _slotFront = TryLoad("Images/UI/Bestiary/Slot_Front");
            _iconTagsShadow = TryLoad("Images/UI/Bestiary/Icon_Tags_Shadow");
        }

        private static Texture2D TryLoad(string path)
        {
            try
            {
                Asset<Texture2D> asset = Main.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
                return asset?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
