using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace EvenMoreOverpoweredJourney.Shell.UI
{
    /// <summary>
    /// 肖像绘制可能 End 掉 UI 批；仅在批已结束时重新 Begin。
    /// 禁止盲目 End()——批未开启时会抛 InvalidOperationException（client.log 已证实）。
    /// </summary>
    internal static class ShellUiDrawUtil
    {
        private const float ScaleUnityEpsilon = 0.005f;

        public static void EnsureUiSpriteBatch(SpriteBatch spriteBatch)
        {
            try
            {
                BeginUiBatch(spriteBatch, Main.UIScaleMatrix);
            }
            catch (InvalidOperationException)
            {
                // 批仍在活动中，无需重复 Begin。
            }
        }

        /// <summary>
        /// 以 <paramref name="origin"/> 为锚点等比缩放绘制（子树仍按 72px 逻辑布局，仅变换输出）。
        /// </summary>
        public static void DrawScaledAbout(SpriteBatch spriteBatch, Vector2 origin, float scale, Action drawContent)
        {
            if (drawContent == null || scale <= 0f)
                return;

            if (Math.Abs(scale - 1f) < ScaleUnityEpsilon)
            {
                drawContent();
                return;
            }

            bool ended = false;
            try
            {
                spriteBatch.End();
                ended = true;

                Matrix aboutOrigin =
                    Matrix.CreateTranslation(origin.X, origin.Y, 0f) *
                    Matrix.CreateScale(scale, scale, 1f) *
                    Matrix.CreateTranslation(-origin.X, -origin.Y, 0f);

                BeginUiBatch(spriteBatch, aboutOrigin * Main.UIScaleMatrix);
                drawContent();
                spriteBatch.End();
            }
            finally
            {
                if (ended)
                    EnsureUiSpriteBatch(spriteBatch);
            }
        }

        private static void BeginUiBatch(SpriteBatch spriteBatch, Matrix transform) =>
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                transform);
    }
}
