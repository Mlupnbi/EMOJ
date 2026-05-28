using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Research.Players;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
    /// <summary>绿脸查询诊断：写入 simple.log（简化模式）与 09_research.log（完整模式）。</summary>
    internal static class GreenFaceDiagnostics
    {
        private const int MaxRejectSamples = 12;
        private const int MaxResultLines = 24;
        /// <summary>候选过多时跳过逐条 DiagnoseProduct（完整模式也会卡数秒）。</summary>
        private const int SkipHeavyDiagnosticCandidateThreshold = 128;

        internal static void LogQueryComplete(RecipeBrowserNestedCraft.GreenFaceQuerySession session)
        {
            // 完整诊断极耗 CPU（对数百候选重复 DiagnoseProduct）；简化模式只保留 complete 摘要行
            if (!EmojLog.IsFullMode || session == null)
                return;

            if (session.Candidates != null && session.Candidates.Count > SkipHeavyDiagnosticCandidateThreshold)
            {
                EmojLog.Info(EmojLogChannel.Research,
                    $"GreenFace diagnostic skipped (candidates={session.Candidates.Count}>{SkipHeavyDiagnosticCandidateThreshold}); see GreenFace complete line");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("======== GreenFace diagnostic ========");
            sb.AppendLine($"seed={ItemLabel(session.SeedType)} researched={RecipeAnalyzer.IsFullyResearched(session.SeedType)}");
            sb.AppendLine($"list={session.Results.Count} listMs={session.ListReadyMs} journeyGreen={session.ImmediateCraftProducts.Count}");
            sb.AppendLine(LogEnvironmentSnapshot());

            if (session.Results.Count > 0)
            {
                sb.AppendLine("-- results (why qualified) --");
                int n = 0;
                foreach (int product in session.Results)
                {
                    if (n++ >= MaxResultLines)
                    {
                        sb.AppendLine($"... +{session.Results.Count - MaxResultLines} more");
                        break;
                    }

                    sb.AppendLine(DiagnoseQualifyingRecipe(product, session.SeedType));
                }
            }

            sb.AppendLine("-- probes --");
            sb.AppendLine(DiagnoseProduct(ItemID.Wood, session.SeedType));
            sb.AppendLine(DiagnoseProduct(ItemID.WoodWall, session.SeedType));
            sb.AppendLine(DiagnoseProduct(ItemID.WorkBench, session.SeedType));
            if (TileID.Sawmill > 0)
                sb.AppendLine(DiagnoseStationTile(TileID.Sawmill));

            foreach (int product in PickTombstoneProbes(session.Results))
                sb.AppendLine(DiagnoseProduct(product, session.SeedType));

            sb.AppendLine("======== end GreenFace diagnostic ========");

            string report = sb.ToString();
            foreach (string line in report.Split('\n'))
            {
                string trimmed = line.TrimEnd('\r');
                if (trimmed.Length > 0)
                    EmojLog.Info(EmojLogChannel.Research, trimmed);
            }

            EmojLog.InfoFull(EmojLogChannel.Research, report);
        }

        private static IEnumerable<int> PickTombstoneProbes(List<int> results)
        {
            foreach (int id in results)
            {
                if (Lang.GetItemName(id).Value.Contains("Tomb") || Lang.GetItemName(id).Value.Contains("墓"))
                    yield return id;
            }
        }

        internal static string LogEnvironmentSnapshot()
        {
            Player p = Main.LocalPlayer;
            var sb = new StringBuilder();
            sb.Append("-- env snapshot: ");
            sb.Append($"seenEnv={ResearchCraftingPlayer.SeenEnvironment} researchEnv={ResearchCraftingPlayer.ResearchedEnvironment} ");
            sb.Append($"seenTiles={ResearchCraftingPlayer.CountSeenTiles()} researchTiles={ResearchCraftingPlayer.CountResearchedTiles()} ");

            if (p != null && p.active)
            {
                sb.Append($"ZoneGraveyard={p.ZoneGraveyard} ZoneSnow={p.ZoneSnow} ");
                sb.Append($"adjWater={p.adjWater} adjLava={p.adjLava} ");
                sb.Append(FormatAdjTile(TileID.WorkBenches, "WorkBenches"));
                sb.Append(FormatAdjTile(TileID.Sawmill, "Sawmill"));
                sb.Append(FormatAdjTile(TileID.Anvils, "Anvils"));
                sb.Append($"carryWB={ResearchCraftingPlayer.DebugCarriesTile(p, TileID.WorkBenches)} ");
                sb.Append($"carrySaw={ResearchCraftingPlayer.DebugCarriesTile(p, TileID.Sawmill)} ");
                sb.Append($"unlockWB={ResearchCraftingPlayer.IsCraftingEnvironmentUnlocked(TileID.WorkBenches)} ");
                sb.Append($"unlockSaw={ResearchCraftingPlayer.IsCraftingEnvironmentUnlocked(TileID.Sawmill)} ");
                sb.Append($"unlockGrave={ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Graveyard)} ");
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatAdjTile(int tileId, string label)
        {
            Player p = Main.LocalPlayer;
            if (p?.adjTile == null || tileId < 0 || tileId >= p.adjTile.Length)
                return $"{label}=? ";
            return $"{label}={p.adjTile[tileId]} ";
        }

        private static string DiagnoseStationTile(int tileId) =>
            $"station probe tile={tileId} seen={ResearchCraftingPlayer.IsCraftingStationSeen(tileId)} " +
            $"persistResearch={ResearchCraftingPlayer.IsResearchedTilePublic(tileId)} " +
            $"journeySacrifice={ResearchCraftingPlayer.IsCraftingStationUnlockedForJourneyGreen(tileId)} " +
            $"unlock={ResearchCraftingPlayer.IsCraftingEnvironmentUnlocked(tileId)} " +
            JourneyStationSacrifice.DescribeTileSacrificeState(tileId);

        internal static string DiagnoseProduct(int productType, int seedType)
        {
            if (productType <= ItemID.None)
                return "product=?";

            bool isProduct = RecipeBrowserNestedCraft.IsGreenFaceProduct(productType);
            var recipes = RecipeAnalyzer.GetRecipesForItem(productType).ToList();
            var sb = new StringBuilder();
            sb.Append($"probe {ItemLabel(productType)} recipes={recipes.Count} greenProduct={isProduct}");

            if (!isProduct)
            {
                sb.Append(" -> notProduct");
                return sb.ToString();
            }

            List<Recipe> qualifying = RecipeBrowserNestedCraft.GetQualifyingRecipesForGreenFace(productType, seedType);
            if (qualifying.Count == 0)
            {
                sb.Append(" -> notOnList");
                return sb.ToString();
            }

            string firstGreenFail = null;
            foreach (Recipe recipe in qualifying)
            {
                if (ResearchCraftEnvironment.IsRecipeJourneyGreenEnvironmentUnlocked(recipe))
                {
                    sb.Append(" | LIST+GREEN via ").Append(SummarizeRecipe(recipe));
                    return sb.ToString();
                }

                if (firstGreenFail == null)
                    firstGreenFail = SummarizeRecipe(recipe) + " => env:" + ResearchCraftEnvironment.DescribeJourneyGreenEnvironmentFailure(recipe);
            }

            sb.Append(" -> yellow ").Append(firstGreenFail ?? "");
            return sb.ToString();
        }

        private static string DiagnoseQualifyingRecipe(int productType, int seedType)
        {
            foreach (Recipe recipe in RecipeBrowserNestedCraft.GetQualifyingRecipesForGreenFace(productType, seedType))
            {
                bool green = ResearchCraftEnvironment.IsRecipeJourneyGreenEnvironmentUnlocked(recipe);
                return $"OK {ItemLabel(productType)} <= {SummarizeRecipe(recipe)} tint={(green ? "green" : "yellow")}";
            }

            return $"OK {ItemLabel(productType)} (recipe detail missing)";
        }

        internal static string DiagnoseRecipeFailure(Recipe recipe, int seedType, out bool valid, out bool ingredients, out bool seed)
        {
            valid = false;
            ingredients = false;
            seed = false;

            if (recipe?.createItem == null || recipe.createItem.IsAir)
                return "noCreateItem";

            if (recipe.Disabled)
                return "disabled";

            if (RecipeBrowserNestedCraft.RecipeRequiresNearShimmer(recipe))
                return "shimmer:near";
            if (RecipeBrowserNestedCraft.DebugIsShimmerBlocked(recipe))
                return "shimmer:transform-only";

            if (!ResearchCraftEnvironment.IsRecipeEnvironmentUnlocked(recipe))
                return "env:" + ResearchCraftEnvironment.DescribeEnvironmentFailure(recipe);

            valid = true;

            var researchMemo = new Dictionary<int, bool>();
            var researchVisiting = new HashSet<int>();
            if (!RecipeBrowserNestedCraft.DebugIngredientsComplete(recipe, researchMemo, researchVisiting))
                return "ingredients";

            ingredients = true;

            var seedMemo = new Dictionary<int, bool>();
            var seedVisiting = new HashSet<int>();
            if (!RecipeBrowserNestedCraft.DebugDependsOnSeed(recipe, seedType, seedMemo, seedVisiting))
                return "noSeed";

            seed = true;
            return "ok";
        }

        private static string SummarizeRecipe(Recipe recipe)
        {
            if (recipe?.createItem == null)
                return "recipe?";

            var parts = new List<string>();
            if (recipe.requiredTile != null)
            {
                foreach (int t in recipe.requiredTile)
                {
                    if (t < 0)
                        continue;
                    parts.Add($"tile[{t}]");
                }
            }

            if (RecipeEnvironmentHelper.RecipeNeedsGraveyardIncludingConditions(recipe))
                parts.Add("needGraveyard");
            if (RecipeEnvironmentHelper.RecipeNeedsWater(recipe))
                parts.Add("needWater");
            if (RecipeEnvironmentHelper.RecipeNeedsSnowBiome(recipe))
                parts.Add("needSnow");
            if (recipe.Conditions != null && recipe.Conditions.Count > 0)
                parts.Add($"conditions={recipe.Conditions.Count}");

            string mats = recipe.requiredItem == null
                ? ""
                : string.Join(",", recipe.requiredItem
                    .Where(i => i != null && !i.IsAir)
                    .Select(i => ItemLabel(i.type)));

            return $"#{recipe.RecipeIndex} mats=[{mats}] stations=[{string.Join(",", parts)}] mod={recipe.Mod?.Name ?? "Terraria"}";
        }

        private static string ItemLabel(int type)
        {
            if (type <= ItemID.None)
                return "none";
            string name = Lang.GetItemName(type).Value;
            if (string.IsNullOrWhiteSpace(name))
                name = "?";
            return $"{name}(id={type})";
        }
    }
}
