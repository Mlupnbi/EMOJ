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
        public static void EnsureUiSpriteBatch(SpriteBatch spriteBatch)
        {
            try
            {
                spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    Main.DefaultSamplerState,
                    DepthStencilState.None,
                    Main.Rasterizer,
                    null,
                    Main.UIScaleMatrix);
            }
            catch (System.InvalidOperationException)
            {
                // 批仍在活动中，无需重复 Begin。
            }
        }
    }
}
