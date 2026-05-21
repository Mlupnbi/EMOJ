using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.Shell.UI.Components
{
    /// <summary>从原版 <c>Images/UI/NPCHappiness</c> 横向裁剪四张脸，互斥选中。</summary>
    public class UIFaceModeSelector : UIElement
    {
        private Texture2D _sheet;
        private int _sliceW;
        private int _sliceH;
        private bool _fallback;

        public ResearchFaceMode ActiveFace { get; set; }

        /// <summary>若返回 false，点击无效（仍绘制暗淡）。</summary>
        public Func<ResearchFaceMode, bool> CanInteract = _ => true;

        public Action<ResearchFaceMode> OnFaceSelected;

        public UIFaceModeSelector(float totalHeight)
        {
            float side = totalHeight;
            Width.Set(side * 4f + 6f, 0f);
            Height.Set(totalHeight, 0f);
            TryLoadSheet();
        }

        private void TryLoadSheet()
        {
            try
            {
                Asset<Texture2D> asset = Main.Assets.Request<Texture2D>("Images/UI/NPCHappiness", AssetRequestMode.ImmediateLoad);
                _sheet = asset?.Value;
                if (_sheet != null && _sheet.Width >= 4)
                {
                    _sliceW = _sheet.Width / 4;
                    _sliceH = _sheet.Height;
                    _fallback = false;
                    return;
                }
            }
            catch
            {
                // ignored
            }
            _sheet = TextureAssets.MagicPixel.Value;
            _sliceW = 1;
            _sliceH = 1;
            _fallback = true;
        }

        public ResearchFaceMode GetFaceUnderMouse()
        {
            if (!IsMouseHovering) return ActiveFace;
            return HitTest(Main.MouseScreen);
        }

        private ResearchFaceMode HitTest(Vector2 mouse)
        {
            CalculatedStyle dims = GetDimensions();
            Rectangle r = dims.ToRectangle();
            if (!r.Contains(mouse.ToPoint())) return ActiveFace;
            float cell = (dims.Width - 6f) / 4f;
            float pad = 2f;
            float x = mouse.X - dims.X;
            int idx = (int)(x / (cell + pad));
            idx = Math.Clamp(idx, 0, 3);
            return (ResearchFaceMode)idx;
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            ResearchFaceMode face = HitTest(evt.MousePosition);
            if (!CanInteract(face)) return;
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
                var mode = (ResearchFaceMode)i;
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
                    Color[] cols = { Color.Goldenrod, Color.LimeGreen, Color.DeepSkyBlue, Color.MediumPurple };
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
