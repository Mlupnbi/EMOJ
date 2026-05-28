using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>详情窗「从识别覆盖」操作结果（Phase 2.4）。</summary>
    public enum FurnitureRecognitionOverlayResult
    {
        Success,
        Busy,
        MissingSeed,
        MissingMaterial,
        Empty
    }

    /// <summary>
    /// 将最近一次识别结果写回当前编辑区（不修改识别赋分/Registry 层）。
    /// 供详情窗与后续放置预览使用。
    /// </summary>
    public static class FurnitureRecognitionOverlay
    {
        public static FurnitureRecognitionOverlayResult TryApply(
            FurnitureBlueprintPlayer player,
            int seedType = ItemID.None,
            int materialBlock = ItemID.None)
        {
            if (player == null)
                return FurnitureRecognitionOverlayResult.Empty;

            if (player.RecognitionBusy)
                return FurnitureRecognitionOverlayResult.Busy;

            if (player.TryApplyStoredRecognitionOverlay())
                return FurnitureRecognitionOverlayResult.Success;

            int seed = seedType > ItemID.None ? seedType : player.PendingSeedType;
            int block = materialBlock > ItemID.None ? materialBlock : player.PendingMaterialBlock;

            if (seed <= ItemID.None)
                return FurnitureRecognitionOverlayResult.MissingSeed;

            if (block <= ItemID.None)
                return FurnitureRecognitionOverlayResult.MissingMaterial;

            if (FurnitureSetCacheSystem.TryGetCached(seed, block, out FurnitureScheme cached)
                && CountFilled(cached) > 0)
            {
                FurnitureScheme hit = cached.Clone();
                hit.SeedType = seed;
                player.ApplyRecognitionToActive(hit, rememberAsOverlaySource: true);
                player.NeedsBlueprintUiRefresh = true;
                return FurnitureRecognitionOverlayResult.Success;
            }

            if (TryRunSynchronizedRecognition(player, seed, block))
                return FurnitureRecognitionOverlayResult.Success;

            return FurnitureRecognitionOverlayResult.Empty;
        }

        private static bool TryRunSynchronizedRecognition(FurnitureBlueprintPlayer player, int seed, int block)
        {
            try
            {
                FurnitureRecognitionJob job = FurnitureSetRecognizer.BeginRecognition(seed, block);
                if (job.IsComplete)
                {
                    if (job.Scheme == null || CountFilled(job.Scheme) <= 0)
                        return false;

                    FurnitureScheme scheme = job.Scheme.Clone();
                    scheme.SeedType = seed;
                    player.ApplyRecognitionToActive(scheme, rememberAsOverlaySource: true);
                    player.NeedsBlueprintUiRefresh = true;
                    return true;
                }

                const int budgetMs = FurnitureRecognitionRunner.FrameBudgetMs;
                int guard = 0;
                while (!job.IsComplete && guard++ < 8_000)
                {
                    if (FurnitureRecognitionRunner.Tick(job, budgetMs))
                        break;
                }

                if (job.Scheme == null || CountFilled(job.Scheme) <= 0)
                    return false;

                FurnitureScheme result = job.Scheme.Clone();
                result.SeedType = seed;
                player.ApplyRecognitionToActive(result, rememberAsOverlaySource: true);
                player.NeedsBlueprintUiRefresh = true;
                return true;
            }
            catch (System.Exception ex)
            {
                FurnitureBlueprintLog.Warn($"recognition overlay sync failed seed={seed} block={block}: {ex.Message}");
                return false;
            }
        }

        private static int CountFilled(FurnitureScheme scheme)
        {
            if (scheme?.SlotItemTypes == null)
                return 0;

            int n = 0;
            for (int i = 0; i < scheme.SlotItemTypes.Length; i++)
            {
                if (scheme.SlotItemTypes[i] > ItemID.None)
                    n++;
            }

            return n;
        }
    }
}
