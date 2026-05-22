using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Bestiary;
using EvenMoreOverpoweredJourney.Core.Localization;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Bestiary.UI.Components
{
    public class UIBestiaryFaceSelector : UIElement
    {
        private static readonly string[] FaceTipKeys =
        {
            "BestiaryFaceTip_AllVisible",
            "BestiaryFaceTip_ProgressivePlus",
            "BestiaryFaceTip_ProgressiveMinus",
            "BestiaryFaceTip_UnlockedOnly"
        };

        private Texture2D _sheet;
        private int _sliceW;
        private int _sliceH;
        private bool _fallback;

        public BestiaryFaceMode ActiveFace { get; set; }

        public Func<BestiaryFaceMode, bool> CanInteract = _ => true;
        public Action<BestiaryFaceMode> OnFaceSelected;

        public UIBestiaryFaceSelector(float totalHeight)
        {
            float side = totalHeight;
            Width.Set(side * 4f + 6f, 0f);
            Height.Set(totalHeight, 0f);
            TryLoadSheet();
        }

        private void TryLoadSheet()
        {
            EojUiTextureCache.WarmTab(EojUiTab.Bestiary);
            _sheet = EojUiTextures.Bestiary.NpcHappiness;
            if (_sheet != null && _sheet.Width >= 4)
            {
                _sliceW = _sheet.Width / 4;
                _sliceH = _sheet.Height;
                _fallback = false;
                return;
            }

            _sheet = TextureAssets.MagicPixel.Value;
            _sliceW = 1;
            _sliceH = 1;
            _fallback = true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!IsMouseHovering || Main.LocalPlayer == null)
                return;

            BestiaryFaceMode face = HitTest(Main.MouseScreen);
            if (face >= BestiaryFaceMode.AllVisible && (int)face < FaceTipKeys.Length)
                Main.instance.MouseText(EOPJText.UI(FaceTipKeys[(int)face]));
        }

        private BestiaryFaceMode HitTest(Vector2 mouse)
        {
            CalculatedStyle dims = GetDimensions();
            Rectangle r = dims.ToRectangle();
            if (!r.Contains(mouse.ToPoint()))
                return ActiveFace;

            float cell = (dims.Width - 6f) / 4f;
            float pad = 2f;
            float x = mouse.X - dims.X;
            int idx = (int)(x / (cell + pad));
            idx = Math.Clamp(idx, 0, 3);
            return (BestiaryFaceMode)idx;
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            BestiaryFaceMode face = HitTest(evt.MousePosition);
            if (!CanInteract(face))
                return;

            ActiveFace = face;
            OnFaceSelected?.Invoke(face);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle dims = GetDimensions();
            float cell = (dims.Width - 6f) / 4f;
            float pad = 2f;

            for (int i = 0; i < 4; i++)
            {
                var mode = (BestiaryFaceMode)i;
                Rectangle dest = new Rectangle(
                    (int)(dims.X + i * (cell + pad)),
                    (int)dims.Y,
                    (int)cell,
                    (int)dims.Height);

                bool lit = ActiveFace == mode;
                bool can = CanInteract(mode);
                float dim = lit && can ? 1f : 0.42f;

                if (_fallback)
                {
                    Color[] cols =
                    {
                        OPJourneyUiColors.FaceFallback0,
                        OPJourneyUiColors.FaceFallback1,
                        OPJourneyUiColors.FaceFallback2,
                        OPJourneyUiColors.FaceFallback3
                    };
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, dest, cols[i] * dim);
                }
                else
                {
                    Rectangle src = new Rectangle(i * _sliceW, 0, _sliceW, _sliceH);
                    spriteBatch.Draw(_sheet, dest, src, Color.White * dim);
                }
            }
        }
    }
}
