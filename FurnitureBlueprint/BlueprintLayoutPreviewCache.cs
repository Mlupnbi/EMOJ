using System;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 建筑方案 + 套组材料的内容预览贴图。仅在 layoutId 或套组槽位变化时重绘一次，
    /// 供建筑方案 UI 与部署器世界幽灵预览共用。
    /// </summary>
    public static class BlueprintLayoutPreviewCache
    {
        /// <summary>与原版图格贴图 16×16 对齐，避免缩放后出现缝隙。</summary>
        public const int PixelsPerCell = 16;
        private const float ContentPad = 6f;

        private static RenderTarget2D _target;
        private static string _layoutId;
        private static int _layoutWidth;
        private static int _layoutHeight;
        private static int _schemeSignature = int.MinValue;
        private static bool _hasContent;
        private static bool _rebuildPending;

        public static bool HasContent => _hasContent && _target != null && !_target.IsDisposed;

        public static bool RebuildPending => _rebuildPending;

        public static int ComputeSchemeSignature(FurnitureScheme scheme)
        {
            if (scheme == null)
                return 0;

            unchecked
            {
                int hash = 17;
                int[] slots = scheme.SlotItemTypes;
                for (int i = 0; i < slots.Length; i++)
                    hash = hash * 31 + slots[i];
                return hash;
            }
        }

        /// <summary>标记预览待重建；实际重绘仅由 <see cref="BlueprintLayoutPreviewCacheSystem"/> 在空闲帧执行。</summary>
        public static void RequestRebuild(BlueprintLayout layout, FurnitureScheme scheme)
        {
            if (layout == null || scheme == null || Main.gameMenu || Main.dedServ)
            {
                Clear();
                return;
            }

            _schemeSignature = int.MinValue;
            _rebuildPending = true;
        }

        /// <summary>在无活跃 SpriteBatch 且子系统空闲时调用（如 PostUpdateWorld）。</summary>
        public static void EnsureBuilt(BlueprintLayout layout, FurnitureScheme scheme)
        {
            if (layout == null || scheme == null || Main.gameMenu || Main.dedServ)
            {
                Clear();
                return;
            }

            if (!BlueprintSubsystemGuard.CanRebuildPreviewCache)
                return;

            int signature = ComputeSchemeSignature(scheme);
            if (!_rebuildPending
                && _hasContent
                && _layoutId == layout.Id
                && _layoutWidth == layout.Width
                && _layoutHeight == layout.Height
                && _schemeSignature == signature
                && _target != null
                && !_target.IsDisposed)
                return;

            BlueprintTemplate template = null;
            BuiltinBlueprintTemplates.TryGetTemplate(layout.Id, out template);
            Rebuild(layout, scheme, template, signature);
            _rebuildPending = false;
        }

        public static void Draw(SpriteBatch spriteBatch, Rectangle dest, Color color)
        {
            if (!HasContent || dest.Width < 2 || dest.Height < 2)
                return;

            Rectangle fit = GetLetterboxRect(dest, _target.Width, _target.Height);
            spriteBatch.Draw(_target, fit, null, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        }

        /// <summary>保持户型宽高比，居中 fit 到 UI 区域（不拉伸压扁）。</summary>
        public static Rectangle GetLetterboxRect(Rectangle dest, int contentWidth, int contentHeight)
        {
            if (contentWidth < 1 || contentHeight < 1)
                return dest;

            float scale = Math.Min(dest.Width / (float)contentWidth, dest.Height / (float)contentHeight);
            int w = Math.Max(1, (int)(contentWidth * scale));
            int h = Math.Max(1, (int)(contentHeight * scale));
            return new Rectangle(dest.X + (dest.Width - w) / 2, dest.Y + (dest.Height - h) / 2, w, h);
        }

        public static void Clear()
        {
            _hasContent = false;
            _layoutId = null;
            _schemeSignature = int.MinValue;
            _rebuildPending = false;
        }

        public static void DisposeTargets()
        {
            Clear();
            if (_target == null)
                return;

            _target.Dispose();
            _target = null;
        }

        private static void Rebuild(BlueprintLayout layout, FurnitureScheme scheme, BlueprintTemplate template, int signature)
        {
            GraphicsDevice device = Main.graphics.GraphicsDevice;
            if (device == null)
                return;

            int texW = layout.Width * PixelsPerCell + (int)(ContentPad * 2f);
            int texH = layout.Height * PixelsPerCell + (int)(ContentPad * 2f);
            texW = (int)MathHelper.Clamp(texW, 32, 2048);
            texH = (int)MathHelper.Clamp(texH, 32, 2048);

            if (_target == null || _target.IsDisposed || _target.Width != texW || _target.Height != texH)
            {
                _target?.Dispose();
                _target = new RenderTarget2D(device, texW, texH, false, SurfaceFormat.Color, DepthFormat.None);
            }

            RenderTarget2D previous = device.GetRenderTargets().Length > 0
                ? device.GetRenderTargets()[0].RenderTarget as RenderTarget2D
                : null;

            try
            {
                device.SetRenderTarget(_target);
                device.Clear(Color.Transparent);

                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise);

                var dest = new Rectangle(0, 0, texW, texH);
                FurnitureBlueprintTilePreviewDraw.DrawLayout(
                    Main.spriteBatch,
                    dest,
                    layout,
                    scheme,
                    template,
                    PixelsPerCell);

                Main.spriteBatch.End();
            }
            finally
            {
                device.SetRenderTarget(previous);
            }

            _layoutId = layout.Id;
            _layoutWidth = layout.Width;
            _layoutHeight = layout.Height;
            _schemeSignature = signature;
            _hasContent = true;
        }
    }
}
