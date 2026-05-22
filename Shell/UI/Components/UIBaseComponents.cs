using System;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace EvenMoreOverpoweredJourney.Shell.UI.Components
{
    public class UIDraggablePanel : UIPanel
    {
        public override bool ContainsPoint(Vector2 point)
        {
            CalculatedStyle dims = GetDimensions();
            float grip = OPJourneyShellMetrics.ResizeHandleSize + 10f;
            if (point.X >= dims.X + dims.Width - grip &&
                point.Y >= dims.Y + dims.Height - grip)
                return false;

            Rectangle rect = new Rectangle((int)dims.X - 50, (int)dims.Y, (int)dims.Width + 50, (int)dims.Height);
            return rect.Contains(point.ToPoint());
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (IsMouseHovering) Main.LocalPlayer.mouseInterface = true;
        }
    }

    public class UIDragHandle : UIElement
    {
        private bool dragging;
        private Vector2 dragOffset;
        public UIDraggablePanel ParentPanel;

        public UIDragHandle() { Width.Set(0, 1f); Height.Set(22, 0); }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            if (evt.Target == this)
            {
                dragging = true;
                dragOffset = Main.MouseScreen - (ParentPanel?.GetDimensions().Position() ?? Vector2.Zero);
            }
            base.LeftMouseDown(evt);
        }

        public override void LeftMouseUp(UIMouseEvent evt) { base.LeftMouseUp(evt); dragging = false; }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (dragging && ParentPanel != null)
            {
                Vector2 newPos = Main.MouseScreen - dragOffset;
                newPos.X = MathHelper.Clamp(newPos.X, 0, Main.screenWidth - ParentPanel.GetDimensions().Width);
                newPos.Y = MathHelper.Clamp(newPos.Y, 0, Main.screenHeight - ParentPanel.GetDimensions().Height);
                ParentPanel.Left.Set(newPos.X, 0); ParentPanel.Top.Set(newPos.Y, 0);
                ParentPanel.Recalculate();
            }
        }
    }

    public class UIFlatButton : UIElement
    {
        public string Text;
        public Color BgColor;
        public Color TextColor;
        public float TextScale;
        /// <summary>?? true ??????????????????????????</summary>
        public bool GrayedOut;

        public UIFlatButton(string text, Color bgColor, Color textColor, float textScale = 0.6f)
        {
            Text = text; BgColor = bgColor; TextColor = textColor; TextScale = textScale;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle dims = GetDimensions();
            Rectangle rect = dims.ToRectangle();

            Color baseBg = GrayedOut ? OPJourneyUiColors.ButtonGrayedBackground : BgColor;
            Color drawColor = GrayedOut
                ? baseBg
                : (IsMouseHovering ? OPJourneyUiColors.ButtonBackgroundHover : BgColor);
            drawColor.A = 255;

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, drawColor);
            Color edge = OPJourneyUiPalette.BorderSubtle * 0.7f;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, 1), edge);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), edge);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 1, rect.Height), edge);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), edge);

            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(Text) * TextScale;
            Vector2 textPos = new Vector2(dims.X + dims.Width / 2 - textSize.X / 2, dims.Y + dims.Height / 2 - textSize.Y / 2 + (TextScale > 0.7f ? 4f : 2f));
            Utils.DrawBorderString(spriteBatch, Text, textPos, TextColor, TextScale);
        }
    }

    public class UICloseButton : UIElement
    {
        public event Action OnClose;
        private readonly float _sizeScale;
        private Texture2D _cancelTex;

        public UICloseButton(float sizeScale = 1f)
        {
            _sizeScale = Math.Max(0.25f, sizeScale);
            ApplySizeFromTexture();
        }

        private void ApplySizeFromTexture()
        {
            Texture2D tex = GetCancelTexture();
            if (tex != null)
            {
                float s = _sizeScale;
                Width.Set(tex.Width * s, 0f);
                Height.Set(tex.Height * s, 0f);
                return;
            }

            float px = 22f * _sizeScale;
            Width.Set(px, 0);
            Height.Set(px, 0);
        }

        private Texture2D GetCancelTexture()
        {
            return _cancelTex ??= global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Common.SearchCancel;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Rectangle rect = GetDimensions().ToRectangle();
            if (rect.Width < 2 || rect.Height < 2)
                return;

            Texture2D tex = GetCancelTexture();
            if (tex == null)
                return;

            float scale = Math.Min(rect.Width / (float)tex.Width, rect.Height / (float)tex.Height);
            Color tint = Color.White * (IsMouseHovering ? 1f : 0.82f);
            Vector2 center = new Vector2(rect.Center.X, rect.Center.Y);
            Vector2 origin = new Vector2(tex.Width, tex.Height) * 0.5f;
            spriteBatch.Draw(tex, center, null, tint, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            SoundEngine.PlaySound(SoundID.MenuClose);
            OnClose?.Invoke();
        }
    }

    public class UITab : UIPanel
    {
        public int ID;
        public bool Active;
        private readonly string _textKey;
        private readonly string _hoverKey;
        public UITab(int id, string textKey, string hoverKey)
        {
            ID = id;
            _textKey = textKey;
            _hoverKey = string.IsNullOrEmpty(hoverKey) ? textKey : hoverKey;
            Width.Set(40, 0);
            Height.Set(40, 0);
            BorderColor = OPJourneyUiColors.TabInactiveBorder;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            BackgroundColor = Active ? OPJourneyUiColors.TabActiveBackground : OPJourneyUiColors.TabInactiveBackground;
            BorderColor = Active ? OPJourneyUiColors.TabActiveBorder : OPJourneyUiColors.TabInactiveBorder;
            base.DrawSelf(spriteBatch);
            CalculatedStyle dims = GetDimensions();
            Vector2 center = dims.Center();
            string label = EOPJText.UI(_textKey);

            Texture2D tex = TryGetTabIcon(ID);
            if (tex != null)
            {
                int iw = tex.Width;
                int ih = tex.Height;
                int sx = 0, sy = 0;
                if (tex.Width > 3 && tex.Height > 3)
                {
                    sx = 1;
                    sy = 1;
                    iw = tex.Width - 2;
                    ih = tex.Height - 2;
                }
                Rectangle? src = new Rectangle(sx, sy, iw, ih);
                float scale = Math.Min(dims.Width / iw, dims.Height / ih) * 0.78f;
                Color tint = Active ? OPJourneyUiColors.TextPrimary : OPJourneyUiColors.TabIconInactiveTint;
                Vector2 originTex = new Vector2(sx + iw * 0.5f, sy + ih * 0.5f);
                spriteBatch.Draw(tex, center, src, tint, 0f, originTex, scale, SpriteEffects.None, 0f);
            }
            else
            {
                Vector2 size = FontAssets.MouseText.Value.MeasureString(label);
                Utils.DrawBorderString(spriteBatch, label, center - size * 0.5f, Color.White, 0.85f);
            }

            if (Active)
            {
                Rectangle r = dims.ToRectangle();
                r.Inflate(1, 1);
                BorderDrawUtil.DrawRectOutline(spriteBatch, r, OPJourneyUiColors.AccentGoldOutline, 2);
            }

            if (IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(EOPJText.UI(_hoverKey));
            }
        }

        private static Texture2D TryGetTabIcon(int tabId)
        {
            EojUiTextureCache.WarmTab(EojUiTab.Shell);
            return tabId switch
            {
                0 => EojUiTextures.Shell.TabResearch,
                1 => EojUiTextures.Shell.TabBuff,
                2 => EojUiTextures.Shell.TabStorage,
                3 => EojUiTextures.Shell.TabBestiary,
                _ => null
            };
        }
    }

    internal class UIResizeHandle : UIElement
    {
        private readonly UIElement _targetPanel;
        private Texture2D _cursorTex;
        private bool _dragging;
        private bool _gripHot;
        private float _startMouseX, _startMouseY, _startWidth, _startHeight;
        private float _lastNotifiedWidth = -1f, _lastNotifiedHeight = -1f;

        public float MinWindowWidth = OPJourneyShellMetrics.MinMainWidth;
        public float MinWindowHeight = OPJourneyShellMetrics.MinMainHeight;
        public float MaxWindowWidth = 1600f;
        public float MaxWindowHeight = 1200f;
        public Action OnResized;

        internal bool IsGripHighlighted => _gripHot || _dragging;

        public static Rectangle GetGripScreenRect(UIElement panel)
        {
            CalculatedStyle d = panel.GetOuterDimensions();
            float grip = OPJourneyShellMetrics.ResizeHandleSize;
            return new Rectangle(
                (int)(d.X + d.Width - grip),
                (int)(d.Y + d.Height - grip),
                (int)grip,
                (int)grip);
        }

        internal Texture2D GetCursorTexture()
        {
            if (_cursorTex == null)
            {
                Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
                _cursorTex = ShellUiTextureHelper.TryLoadHandle(mod);
            }

            return _cursorTex;
        }

        public UIResizeHandle(UIElement targetPanel)
        {
            _targetPanel = targetPanel;
            float grip = OPJourneyShellMetrics.ResizeHandleSize;
            Width.Set(grip, 0f);
            Height.Set(grip, 0f);
            HAlign = 1f;
            VAlign = 1f;
            Left.Set(-grip, 1f);
            Top.Set(-grip, 1f);
            IgnoresMouseInteraction = true;
        }

        /// <summary>? OPJourneyUI ???????????????? UI ???????????</summary>
        public void UpdateGripInteraction(Rectangle gripScreenRect)
        {
            _gripHot = gripScreenRect.Contains(Main.MouseScreen.ToPoint());

            if (_gripHot || _dragging)
                Main.LocalPlayer.mouseInterface = true;

            if (Main.mouseLeft)
            {
                if (!_dragging && _gripHot)
                {
                    _dragging = true;
                    _startMouseX = Main.MouseScreen.X;
                    _startMouseY = Main.MouseScreen.Y;
                    _startWidth = _targetPanel.GetOuterDimensions().Width;
                    _startHeight = _targetPanel.GetOuterDimensions().Height;
                }

                if (_dragging)
                {
                    float newWidth = MathHelper.Clamp(
                        _startWidth + (Main.MouseScreen.X - _startMouseX) / Main.UIScale,
                        MinWindowWidth,
                        MaxWindowWidth);
                    float newHeight = MathHelper.Clamp(
                        _startHeight + (Main.MouseScreen.Y - _startMouseY) / Main.UIScale,
                        MinWindowHeight,
                        MaxWindowHeight);
                    _targetPanel.Width.Set(newWidth, 0f);
                    _targetPanel.Height.Set(newHeight, 0f);
                    ApplyLayoutAfterResize(newWidth, newHeight);
                }
            }
            else
            {
                _dragging = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        private void ApplyLayoutAfterResize(float newWidth, float newHeight)
        {
            _targetPanel.Recalculate();
            UIElement node = _targetPanel.Parent;
            while (node != null)
            {
                node.Recalculate();
                node = node.Parent;
            }

            NotifyIfSizeChanged(newWidth, newHeight);
        }

        private void NotifyIfSizeChanged(float width, float height)
        {
            if (Math.Abs(width - _lastNotifiedWidth) < 0.5f && Math.Abs(height - _lastNotifiedHeight) < 0.5f)
                return;
            _lastNotifiedWidth = width;
            _lastNotifiedHeight = height;
            OnResized?.Invoke();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            // ? OPJourneyUI.Draw ???????????????????????????
        }

        internal static void DrawGripAt(SpriteBatch spriteBatch, Rectangle handleRect, Texture2D cursorTex, bool highlighted)
        {
            if (handleRect.Width <= 0 || handleRect.Height <= 0)
                return;

            if (cursorTex != null)
            {
                Color tint = highlighted ? Color.White : Color.White * 0.92f;
                float scale = Math.Max(1f, handleRect.Width / OPJourneyShellMetrics.HandleTexturePx);
                Vector2 origin = new Vector2(cursorTex.Width * 0.5f, cursorTex.Height * 0.5f);
                Vector2 center = handleRect.Center.ToVector2();
                spriteBatch.Draw(
                    cursorTex,
                    center,
                    null,
                    tint,
                    0f,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f);
                return;
            }

            DrawFallbackGrip(spriteBatch, handleRect);
        }

        private static void DrawFallbackGrip(SpriteBatch spriteBatch, Rectangle handleRect)
        {
            const float GripThicknessScale = 1.5f;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int arm = Math.Max(6, (int)(handleRect.Width * 0.65f * GripThicknessScale));
            int thick = Math.Max(3, (int)(handleRect.Width / 6f * GripThicknessScale));
            Color gold = OPJourneyUiColors.DragHandleGold;
            spriteBatch.Draw(pixel, new Rectangle(handleRect.Right - arm, handleRect.Bottom - thick, arm, thick), gold);
            spriteBatch.Draw(pixel, new Rectangle(handleRect.Right - thick, handleRect.Bottom - arm, thick, arm), Color.White);
        }
    }
}