using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>聊天指令 TEST_BLUEPRINT：不打开 UI，后台自动跑一轮家具识别并写日志。</summary>
    public static class FurnitureBlueprintBatchTest
    {
        public enum RunMode
        {
            Quick,
            Sets,
            Full
        }

        private const int BatchFrameBudgetMs = FurnitureRecognitionRunner.FrameBudgetMs;
        private const int MaxRecognizeTicksPerSeed = 4_000;
        private const int MaxSkipsPerFrame = 48;
        private const int CacheTrimEverySeeds = 24;
        private const int FullQueueBuildBatch = 384;
        private const int CatalogBuildBatch = 512;
        private const int ChatProgressEvery = 15;
        private const int ChatProgressMinIntervalMs = 3_000;

        private enum BatchPhase
        {
            BuildingCatalog,
            BuildingQueue,
            Running
        }

        private static readonly int[] GoldenRegressionSeeds =
        {
            13905, 13875, 13851, 13870, 13833, 13926, 12131, 16372
        };

        private static readonly int[] QuickRegressionSeeds =
        {
            13905, 13875, 13851, 13870, 13833, 13926, 12131, 16372,
            829, 3154, 3163,
            828, 812, 917, 1794, 1926, 1717, 2248, 1144, 3932, 3897,
            5158, 4576, 4415, 4307, 5179, 5204, 2065, 2813,
            16521, 16543, 16498, 13908, 13832, 13862, 16604, 12741,
            13846, 13843, 13904, 13886, 13852,
            10166, 10004, 10036, 10512, 6832, 6789, 6715, 6708,
            16595, 6875, 6878, 7058, 7078, 7135, 10240, 10229
        };

        public static bool IsRunning { get; private set; }
        public static RunMode ActiveMode { get; private set; }

        private static List<int> _queue;
        private static int _index;
        private static Stopwatch _runClock;
        private static int _full22;
        private static int _ge20;
        private static int _lt12;
        private static int _bedBathPairMiss;
        private static long _wikiSum;
        private static int _skippedNoMaterial;
        private static int _failed;
        private static int _scoredCount;
        private static long _accuracySum;
        private static int _accuracyFilled;
        private static int _slotMismatch;
        private static int _lineageMiss;
        private static int _vanillaLeak;
        private static int _materialMiss;
        private static int _styleSlotMatchSum;
        private static int _styleSlotFilledSum;
        private static int _wikiMatchSum;
        private static int _wikiCheckedSum;
        private static int _wikiAuditedSeeds;
        private static int _goldenMatchSum;
        private static int _goldenCheckedSum;
        private static int _goldenAuditedSeeds;
        private static int _confHigh;
        private static int _confMedium;
        private static int _confLow;
        private static long _accSumHigh;
        private static int _accFilledHigh;
        private static long _accSumMedium;
        private static int _accFilledMedium;
        private static long _accSumLow;
        private static int _accFilledLow;
        private static List<string> _worst;
        private static FurnitureRecognitionJob _activeJob;
        private static int _activeSeed;
        private static int _activeMaterial;
        private static int _activeRecognizeTicks;
        private static BatchPhase _phase;
        private static List<int> _queueBuilder;
        private static int _queueBuildCursor;
        private static long _lastChatProgressMs;

        public static bool TryStart(RunMode mode)
        {
            return TryStartInternal(mode, null);
        }

        public static bool TryStartSingleSeed(int seedType)
        {
            if (seedType <= ItemID.None)
                return false;

            return TryStartInternal(RunMode.Sets, new List<int> { seedType });
        }

        private static bool TryStartInternal(RunMode mode, List<int> fixedQueue)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return false;

            if (IsRunning)
                return false;

            FurnitureTileSlotRegistry.Build();

            FurnitureSetCacheSystem.ClearSchemesOnly();
            FurnitureRecognitionCaches.Clear();
            FurnitureReverseSeedProbeCache.Clear();

            _index = 0;
            _full22 = _ge20 = _lt12 = _bedBathPairMiss = _skippedNoMaterial = _failed = _scoredCount = 0;
            _wikiSum = 0;
            _accuracySum = 0;
            _accuracyFilled = 0;
            _slotMismatch = _lineageMiss = _vanillaLeak = _materialMiss = 0;
            _styleSlotMatchSum = _styleSlotFilledSum = 0;
            _wikiMatchSum = _wikiCheckedSum = _wikiAuditedSeeds = 0;
            _goldenMatchSum = _goldenCheckedSum = _goldenAuditedSeeds = 0;
            _confHigh = _confMedium = _confLow = 0;
            _accSumHigh = _accSumMedium = _accSumLow = 0;
            _accFilledHigh = _accFilledMedium = _accFilledLow = 0;
            _worst = new List<string>(12);
            _activeJob = null;
            _activeSeed = ItemID.None;
            _activeMaterial = ItemID.None;
            _activeRecognizeTicks = 0;
            _queue = null;
            _queueBuilder = null;
            _queueBuildCursor = ItemID.None + 1;
            _lastChatProgressMs = 0;
            _runClock = Stopwatch.StartNew();
            ActiveMode = mode;
            IsRunning = true;

            if (mode == RunMode.Quick || fixedQueue != null)
            {
                _queue = fixedQueue ?? BuildQueue(mode);
                if (_queue.Count == 0)
                {
                    IsRunning = false;
                    return false;
                }

                _phase = BatchPhase.Running;
                LogBatchStart(mode, _queue.Count, fixedQueue != null);
                return true;
            }

            FurnitureSetCatalog.BeginIncrementalBuild();
            _phase = BatchPhase.BuildingCatalog;
            FurnitureBlueprintLog.Info($"batch-test catalog-build start mode={mode} items={ItemLoader.ItemCount}");
            return true;
        }

        private static void LogBatchStart(RunMode mode, int count, bool singleSeed = false)
        {
            string label = singleSeed ? "单种子诊断" : ModeLabel(mode);
            FurnitureBlueprintLog.Info(
                $"batch-test start mode={mode} seeds={count} wiki_cache_sets={FurnitureWikiExpectations.LoadedSetCount} golden_sets={FurnitureGoldenExpectations.LoadedSetCount}");
            Main.NewText(
                $"蓝图批量测试已开始（{label}，共 {count} 项）。CANCEL_TEST_BLUEPRINT 可中止。",
                Microsoft.Xna.Framework.Color.LightGreen);
        }

        public static void Cancel()
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            _queue = null;
            _queueBuilder = null;
            _activeJob = null;
            FurnitureBlueprintLog.Warn($"batch-test cancelled at {_index}/{_queue?.Count ?? 0}");
            Main.NewText("蓝图批量测试已中止。", Microsoft.Xna.Framework.Color.Orange);
        }

        /// <summary>每帧推进当前种子识别（分帧，避免单帧卡死）。</summary>
        public static void Tick()
        {
            if (!IsRunning)
                return;

            if (Main.gameMenu)
            {
                Cancel();
                return;
            }

            if (_phase == BatchPhase.BuildingCatalog)
            {
                AdvanceCatalogBuild();
                return;
            }

            if (_phase == BatchPhase.BuildingQueue)
            {
                AdvanceFullQueueBuild();
                return;
            }

            if (_queue == null)
                return;

            if (_activeJob != null)
            {
                AdvanceActiveJob();
                return;
            }

            if (_index >= _queue.Count)
            {
                Finish();
                return;
            }

            TryStartNextSeed();

            MaybeShowChatProgress();
        }

        private static void MaybeShowChatProgress()
        {
            if (_queue == null || _queue.Count == 0)
                return;

            long now = _runClock?.ElapsedMilliseconds ?? 0;
            bool indexHit = _index > 0 && (_index % ChatProgressEvery == 0 || _index >= _queue.Count);
            if (!indexHit || now - _lastChatProgressMs < ChatProgressMinIntervalMs)
                return;

            _lastChatProgressMs = now;
            double avg = _scoredCount > 0 ? (double)_wikiSum / _scoredCount : 0;
            FurnitureBlueprintLog.Info(
                $"batch-test progress {_index}/{_queue.Count} scored={_scoredCount} avg={avg:F1}/22");
        }

        private static void AdvanceCatalogBuild()
        {
            if (!FurnitureSetCatalog.RegisterNextBatch(CatalogBuildBatch, out bool complete))
            {
                IsRunning = false;
                return;
            }

            if (!complete)
                return;

            if (ActiveMode == RunMode.Full)
            {
                _phase = BatchPhase.BuildingQueue;
                _queueBuilder = new List<int>(2048);
                _queueBuildCursor = ItemID.None + 1;
                FurnitureBlueprintLog.Info($"batch-test queue-build start mode={ActiveMode}");
                return;
            }

            _queue = BuildQueue(ActiveMode);
            if (_queue.Count == 0)
            {
                IsRunning = false;
                FurnitureBlueprintLog.Warn("batch-test abort: empty queue after catalog");
                return;
            }

            _phase = BatchPhase.Running;
            LogBatchStart(ActiveMode, _queue.Count);
        }

        private static void AdvanceFullQueueBuild()
        {
            if (_queueBuilder == null)
                return;

            int max = ItemLoader.ItemCount;
            int end = Math.Min(_queueBuildCursor + FullQueueBuildBatch, max);
            for (int type = _queueBuildCursor; type < end; type++)
            {
                if (IsRecognizableSeed(type))
                    _queueBuilder.Add(type);
            }

            _queueBuildCursor = end;
            if (_queueBuildCursor < max)
                return;

            _queueBuilder.Sort();
            _queue = _queueBuilder;
            _queueBuilder = null;
            FurnitureBlueprintLog.Info($"batch-test queue-build done seeds={_queue.Count}");
            if (_queue.Count == 0)
            {
                IsRunning = false;
                Main.NewText("批量测试：未找到可识别家具种子。", Microsoft.Xna.Framework.Color.Orange);
                return;
            }

            _phase = BatchPhase.Running;
            LogBatchStart(ActiveMode, _queue.Count);
        }

        private static void TryStartNextSeed()
        {
            int skips = 0;
            while (_index < _queue.Count && skips < MaxSkipsPerFrame)
            {
                int seed = _queue[_index++];
                if (TryBeginSeed(seed))
                    return;
                skips++;
            }
        }

        private static bool TryBeginSeed(int seedType)
        {
            int material = FurnitureSeedWorkspaceResolver.Resolve(seedType).MaterialBlock;
            if (material <= ItemID.None)
            {
                _skippedNoMaterial++;
                FurnitureBlueprintLog.InfoFull(
                    $"batch-test skip seed={seedType} name={SafeName(seedType)} reason=no-material");
                return false;
            }

            material = FurnitureVanillaLivingWoodBridge.RedirectReverseAnchor(seedType, material);
            material = FurnitureSetMaterialRules.ResolveModMaterialBlock(seedType, material);
            FurnitureSetMaterialRules.ApplyLivingWoodRecipeMaterial(seedType, ref material);

            try
            {
                _activeSeed = seedType;
                _activeMaterial = material;
                _activeRecognizeTicks = 0;
                _activeJob = FurnitureSetRecognizer.BeginRecognition(
                    seedType, material, forceRefresh: true);

                return true;
            }
            catch (Exception ex)
            {
                _failed++;
                _activeJob = null;
                FurnitureBlueprintLog.Warn(
                    $"batch-test fail seed={seedType} name={SafeName(seedType)}: {ex.Message}");
                return true;
            }
        }

        private static void AdvanceActiveJob()
        {
            if (_activeJob == null)
                return;

            _activeRecognizeTicks++;
            if (_activeRecognizeTicks > MaxRecognizeTicksPerSeed)
            {
                _failed++;
                FurnitureBlueprintLog.Warn(
                    $"batch-test timeout seed={_activeSeed} ticks={_activeRecognizeTicks}");
                CompleteActiveSeed();
                _activeJob = null;
                return;
            }

            try
            {
                FurnitureRecognitionRunner.Tick(_activeJob, BatchFrameBudgetMs);
                if (_activeJob.IsComplete)
                {
                    CompleteActiveSeed();
                    _activeJob = null;
                }
            }
            catch (Exception ex)
            {
                _failed++;
                FurnitureBlueprintLog.Warn(
                    $"batch-test fail seed={_activeSeed} name={SafeName(_activeSeed)}: {ex.Message}");
                _activeJob = null;
            }
        }

        private static void CompleteActiveSeed()
        {
            int seedType = _activeSeed;
            int material = _activeMaterial;
            FurnitureScheme scheme = _activeJob?.Scheme;

            try
            {
                int wiki = CountWikiFilled(scheme);
                FurnitureSchemeAccuracy.Report accuracy =
                    FurnitureSchemeAccuracy.Evaluate(seedType, material, scheme);
                _wikiSum += wiki;
                _accuracySum += accuracy.Accurate;
                _accuracyFilled += accuracy.Filled;
                _slotMismatch += accuracy.SlotMismatch;
                _lineageMiss += accuracy.LineageMiss;
                _vanillaLeak += accuracy.VanillaLeak;
                _materialMiss += accuracy.MaterialMiss;
                _styleSlotMatchSum += accuracy.StyleSlotMatch;
                _styleSlotFilledSum += accuracy.StyleSlotFilled;
                if (accuracy.WikiChecked > 0)
                {
                    _wikiMatchSum += accuracy.WikiMatch;
                    _wikiCheckedSum += accuracy.WikiChecked;
                    _wikiAuditedSeeds++;
                }

                FurnitureGoldenExpectations.GoldenMatchReport golden =
                    FurnitureGoldenExpectations.Evaluate(seedType, material, scheme);
                if (golden.Checked > 0)
                {
                    _goldenMatchSum += golden.Match;
                    _goldenCheckedSum += golden.Checked;
                    _goldenAuditedSeeds++;
                }

                int candidates = _activeJob?.CandidateList?.Count ?? 0;
                TrackConfidenceTier(_activeJob?.Ctx?.ConfidenceTier ?? FurnitureSetConfidenceTier.Low, accuracy);
                _scoredCount++;
                if (wiki >= 22) _full22++;
                if (wiki >= 20) _ge20++;
                if (wiki < 12) _lt12++;

                bool bedEmpty = scheme?.GetSlot(FurnitureSlotKind.Bed) <= ItemID.None;
                bool bathEmpty = scheme?.GetSlot(FurnitureSlotKind.Bathtub) <= ItemID.None;
                if (bedEmpty && bathEmpty)
                    _bedBathPairMiss++;

                string wikiMatchPart = accuracy.WikiChecked > 0
                    ? $"wiki_match={accuracy.WikiMatch}/{accuracy.WikiChecked} wiki_set={accuracy.WikiSetLabel} "
                    : "wiki_match=n/a ";

                string goldenPart = golden.Checked > 0
                    ? $"golden_match={golden.Match}/{golden.Checked} golden_set={golden.Label} "
                    : "golden_match=n/a ";

                string line =
                    $"batch-test seed={seedType} name={SafeName(seedType)} material={material} wiki={wiki}/22 " +
                    $"accuracy={accuracy.Accurate}/{accuracy.Filled} mismatch={accuracy.SlotMismatch} lineage_miss={accuracy.LineageMiss} vanilla_leak={accuracy.VanillaLeak} material_miss={accuracy.MaterialMiss} " +
                    wikiMatchPart + goldenPart +
                    $"style_slot_match={accuracy.StyleSlotMatch}/{accuracy.StyleSlotFilled} " +
                    $"set_conf={_activeJob?.Ctx?.ConfidenceTier} style_align={_activeJob?.Ctx?.StyleAlignmentPercent ?? 0}% " +
                    $"candidates={candidates}";
                if (wiki < 22)
                    line += " incomplete";
                FurnitureBlueprintLog.Info(line);

                if (ActiveMode == RunMode.Quick)
                    LogBatchSchemeSlots(seedType, scheme);

                TrackWorst(seedType, wiki, accuracy, material, candidates);
                MaybeTrimRecognitionCaches();
            }
            catch (Exception ex)
            {
                _failed++;
                FurnitureBlueprintLog.Warn(
                    $"batch-test score fail seed={seedType} name={SafeName(seedType)}: {ex.Message}");
            }
        }

        private static void Finish()
        {
            IsRunning = false;
            _runClock.Stop();
            _activeJob = null;
            int done = _index;
            double avgScored = _scoredCount > 0 ? (double)_wikiSum / _scoredCount : 0;
            double avgAll = done > 0 ? (double)_wikiSum / done : 0;
            double avgAccuracy = _accuracyFilled > 0 ? (double)_accuracySum / _accuracyFilled : 0;
            int incomplete = done - _full22;

            double avgStyleSlot = _styleSlotFilledSum > 0
                ? (double)_styleSlotMatchSum / _styleSlotFilledSum
                : 0;
            double avgWikiMatch = _wikiCheckedSum > 0 ? (double)_wikiMatchSum / _wikiCheckedSum : 0;
            double avgGoldenMatch = _goldenCheckedSum > 0 ? (double)_goldenMatchSum / _goldenCheckedSum : 0;
            double accHigh = _accFilledHigh > 0 ? (double)_accSumHigh / _accFilledHigh : 0;
            double accMedium = _accFilledMedium > 0 ? (double)_accSumMedium / _accFilledMedium : 0;
            double accLow = _accFilledLow > 0 ? (double)_accSumLow / _accFilledLow : 0;

            string summary =
                $"batch-test done mode={ActiveMode} ran={done} scored={_scoredCount} avg={avgScored:F2}/22 avg_all={avgAll:F2}/22 " +
                $"accuracy={avgAccuracy:F2} wiki_match={avgWikiMatch:F2} wiki_audited={_wikiAuditedSeeds} golden_match={avgGoldenMatch:F2} golden_audited={_goldenAuditedSeeds} style_slot_match={avgStyleSlot:F2} slot_mismatch={_slotMismatch} lineage_miss={_lineageMiss} vanilla_leak={_vanillaLeak} material_miss={_materialMiss} " +
                $"conf_high={_confHigh} acc_high={accHigh:F2} conf_med={_confMedium} acc_med={accMedium:F2} conf_low={_confLow} acc_low={accLow:F2} " +
                $"full22={_full22} ge20={_ge20} lt12={_lt12} " +
                $"incomplete={incomplete} bed+bath-empty={_bedBathPairMiss} skip_no_mat={_skippedNoMaterial} fail={_failed} " +
                $"ms={_runClock.ElapsedMilliseconds}";
            FurnitureBlueprintLog.Info(summary);
            EmojLog.Info(EmojLogChannel.Blueprint, summary);

            Main.NewText(
                $"批量测试完成：识别 {_scoredCount} 项，均分 {avgScored:F1}/22，code准确度 {avgAccuracy:P0}，金标准 {avgGoldenMatch:P0}（{_goldenAuditedSeeds} 项），满格 {_full22}，耗时 {_runClock.Elapsed.TotalSeconds:F0}s",
                Microsoft.Xna.Framework.Color.LightGreen);

            if (_worst.Count > 0)
            {
                FurnitureBlueprintLog.Info("batch-test worst:");
                for (int i = 0; i < _worst.Count; i++)
                    FurnitureBlueprintLog.Info(_worst[i]);
            }

            _queue = null;
            _queueBuilder = null;
        }

        private static void MaybeTrimRecognitionCaches()
        {
            if (_scoredCount <= 0 || _scoredCount % CacheTrimEverySeeds != 0)
                return;

            FurnitureRecognitionCaches.Clear();
            FurnitureReverseSeedProbeCache.Clear();
            FurnitureSetCacheSystem.ClearSchemesOnly();
            FurnitureBlueprintLog.InfoFull($"batch-test cache-trim scored={_scoredCount}");
        }

        private static void TrackConfidenceTier(
            FurnitureSetConfidenceTier tier,
            FurnitureSchemeAccuracy.Report accuracy)
        {
            switch (tier)
            {
                case FurnitureSetConfidenceTier.High:
                    _confHigh++;
                    _accSumHigh += accuracy.Accurate;
                    _accFilledHigh += accuracy.Filled;
                    break;
                case FurnitureSetConfidenceTier.Medium:
                    _confMedium++;
                    _accSumMedium += accuracy.Accurate;
                    _accFilledMedium += accuracy.Filled;
                    break;
                default:
                    _confLow++;
                    _accSumLow += accuracy.Accurate;
                    _accFilledLow += accuracy.Filled;
                    break;
            }
        }

        private static void TrackWorst(int seed, int wiki, FurnitureSchemeAccuracy.Report accuracy, int material, int candidates)
        {
            string entry =
                $"  seed={seed} {SafeName(seed)} wiki={wiki}/22 accuracy={accuracy.Accurate}/{accuracy.Filled} " +
                $"wiki_match={accuracy.WikiMatch}/{accuracy.WikiChecked} mismatch={accuracy.SlotMismatch} leak={accuracy.VanillaLeak} mat={material} cand={candidates}";
            _worst.Add(entry);
            _worst.Sort((a, b) =>
            {
                int aa = ParseAccuracyFromWorst(a);
                int ab = ParseAccuracyFromWorst(b);
                if (aa != ab)
                    return aa.CompareTo(ab);
                return ParseWikiFromWorst(a).CompareTo(ParseWikiFromWorst(b));
            });
            if (_worst.Count > 10)
                _worst.RemoveAt(_worst.Count - 1);
        }

        private static int ParseAccuracyFromWorst(string line)
        {
            int i = line.IndexOf("accuracy=");
            if (i < 0) return 0;
            int slash = line.IndexOf('/', i);
            if (slash < 0) return 0;
            if (int.TryParse(line.Substring(i + 9, slash - i - 9), out int accurate))
                return accurate;
            return 0;
        }

        private static int ParseWikiFromWorst(string line)
        {
            int i = line.IndexOf("wiki=");
            if (i < 0) return 22;
            int j = line.IndexOf('/', i);
            if (j < 0) return 22;
            if (int.TryParse(line.Substring(i + 5, j - i - 5), out int w))
                return w;
            return 22;
        }

        private static List<int> BuildQueue(RunMode mode)
        {
            if (mode == RunMode.Quick)
                return new List<int>(QuickRegressionSeeds);

            if (mode == RunMode.Full)
                return new List<int>();

            var list = new List<int>(512);
            FurnitureSetCatalog.CollectBatchRepresentativeSeeds(list);
            foreach (int seed in GoldenRegressionSeeds)
            {
                if (!list.Contains(seed))
                    list.Add(seed);
            }

            list.Sort();
            return list;
        }

        private static bool IsRecognizableSeed(int type)
        {
            if (type <= ItemID.None)
                return false;

            if (!FurnitureRecognitionCaches.IsPlaceableFurniture(type))
                return false;

            if (!FurnitureSlotClassifier.TryGetSlotFromType(type, out FurnitureSlotKind kind))
                return false;

            kind = FurnitureWikiSlots.NormalizeClassified(kind);
            if (kind == FurnitureSlotKind.None
                || kind is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                return false;

            if (FurnitureNameSignals.IsDecorativeMark(type))
                return false;

            return true;
        }

        /// <summary>与家具页 ProposeMaterialBlockFromSeed 相同逻辑，不依赖 UI。</summary>
        public static int ResolveAutoMaterialBlock(int seedType)
        {
            if (seedType <= ItemID.None)
                return ItemID.None;

            if (FurnitureMaterialBlockResolver.SeedIsMaterialBlock(seedType))
                return seedType;

            FurnitureReverseSeedProbe probe = FurnitureReverseSeedProbeCache.Ensure(seedType);
            int block = FurnitureReverseRecipeIngredients.PickDefaultPlaceableBlock(seedType, probe.PickerCandidates);
            if (block <= ItemID.None)
            {
                block = FurnitureMaterialBlockResolver.ResolvePlaceableBlockFromProbe(
                    seedType, probe.BestAnchorIngredient, probe.SeedSignature);
            }

            block = FurnitureVanillaLivingWoodBridge.RedirectReverseAnchor(seedType, block);
            block = FurnitureSetMaterialRules.ResolveModMaterialBlock(seedType, block);
            FurnitureSetMaterialRules.ApplyLivingWoodRecipeMaterial(seedType, ref block);
            return block;
        }

        private static void LogBatchSchemeSlots(int seedType, FurnitureScheme scheme)
        {
            if (scheme == null)
                return;

            FurnitureBlueprintLog.InfoFull(
                FurnitureSchemeSlotFormatter.FormatMultiline(seedType, scheme));
        }

        private static int CountWikiFilled(FurnitureScheme scheme)
        {
            if (scheme == null)
                return 0;

            int n = 0;
            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                if (scheme.GetSlot(kind) > ItemID.None)
                    n++;
            }

            return n;
        }

        private static string SafeName(int type)
        {
            Item item = new Item();
            return FurnitureItemDefaults.TrySetDefaults(item, type) ? item.Name ?? type.ToString() : type.ToString();
        }

        private static string ModeLabel(RunMode mode) => mode switch
        {
            RunMode.Quick => "快速回归",
            RunMode.Full => "全量家具",
            _ => "按套组代表"
        };
    }
}
