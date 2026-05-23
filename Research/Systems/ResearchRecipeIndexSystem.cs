using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.Research.Systems
{
    /// <summary>配方索引在 PostSetupContent 预热，避免 Mouse5 查询时在 UI 线程首次构建导致卡死。</summary>
    public sealed class ResearchRecipeIndexSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            if (Main.dedServ)
                return;
            Warm("PostSetupContent");
        }

        public override void OnWorldLoad()
        {
            if (Main.dedServ)
                return;
            Warm("OnWorldLoad");
        }

        private static void Warm(string phase)
        {
            long ms = RecipeAnalyzer.WarmIndices();
            EmojLog.Info(EmojLogChannel.Research, $"Recipe indices warmed ({phase}) recipes={Recipe.numRecipes} ms={ms}");
        }
    }
}
