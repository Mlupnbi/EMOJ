using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Shell.UI.Assets
{
    public sealed class EojUiTextureCacheSystem : ModSystem
    {
        public override void Load()
        {
            EojUiTextureCache.Initialize(Mod);
            LogModAssetPackStatus();
        }

        public override void Unload()
        {
            EojUiTextureCache.Unload();
        }

        private static void LogModAssetPackStatus()
        {
            if (Main.dedServ)
                return;

            Mod mod = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            if (mod == null)
                return;

            string[] probes =
            {
                "Assets/UI/Common/Cursor_2",
                "Assets/UI/Common/SearchCancel",
                "Assets/UI/Bestiary/Slot_Back",
                "Assets/UI/Shell/TabResearch",
            };

            int hit = 0;
            foreach (string path in probes)
            {
                if (mod.HasAsset(path))
                    hit++;
            }

            mod.Logger.Info(
                $"[EMOJ UI] 模组贴图包探测 {hit}/{probes.Length}。若改 PNG 后仍显示原版，请先退出游戏并重新编译生成 .tmod，再重载模组。");
        }
    }
}
