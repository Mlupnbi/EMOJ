using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>驱动 TEST_BLUEPRINT 后台批量识别（不打开蓝图 UI）。</summary>
    public sealed class FurnitureBlueprintBatchTestSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gameMenu)
                return;

            FurnitureBlueprintBatchTest.Tick();
        }

        public override void OnWorldUnload()
        {
            if (FurnitureBlueprintBatchTest.IsRunning)
                FurnitureBlueprintBatchTest.Cancel();
        }

        public override void Unload()
        {
            if (FurnitureBlueprintBatchTest.IsRunning)
                FurnitureBlueprintBatchTest.Cancel();
        }
    }
}
