using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>在更新阶段刷新预览贴图缓存，避免 UI/世界绘制时在 SpriteBatch 内改 RenderTarget。</summary>
    public sealed class BlueprintLayoutPreviewCacheSystem : ModSystem
    {
        public override void PostUpdateWorld()
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || Main.gameMenu)
                return;

            if (!player.TryGetModPlayer(out FurnitureBlueprintPlayer fb))
                return;

            if (!BlueprintSubsystemGuard.CanRebuildPreviewCache)
                return;

            BlueprintLayout layout = BuiltinBlueprintTemplates.ResolveActiveLayout(fb);
            if (layout == null || fb.ActiveScheme == null)
            {
                BlueprintLayoutPreviewCache.Clear();
                return;
            }

            BlueprintLayoutPreviewCache.EnsureBuilt(layout, fb.ActiveScheme);
        }

        public override void OnWorldUnload() => BlueprintLayoutPreviewCache.DisposeTargets();

        public override void Unload() => BlueprintLayoutPreviewCache.DisposeTargets();
    }
}
