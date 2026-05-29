using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.ItemHub.Rules;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// ���ƣ��ϳ����ӵ��䷽ԭ�� �� ���������������ê����ϡ�
    /// ���ƣ�����ê����䷽����? �� 22 �۷�����ѡ�š�
    /// </summary>
    public static class FurnitureSetRecognizer
    {
        private static readonly string[] NameSuffixes =
        {
            "Platform", "Workbench", "WorkBench", "Work Bench", "Table", "Chair", "Door", "Chest",
            "Bed", "Bookcase", "Bathtub", "Candelabra", "Candle", "Chandelier", "Clock", "Dresser",
            "Lamp", "Lantern", "Piano", "Sink", "Sofa", "Bench", "Toilet",
            "Wall", "Block", "Brick", "Bricks", "Plank", "Planks", "Slab", "Slabs", "Bar", "Bars",
            "平台", "工作�?", "桌子", "�?", "�?", "�?", "�?", "书架", "浴缸", "烛台", "蜡烛", "吊灯",
            "�?", "梳妆�?", "�?", "钢琴", "水槽", "沙发", "马桶", "�?", "�?", "�?", "�?", "�?"
        };

        public static FurnitureScheme Recognize(int seedType, bool forceRefresh = false, int anchorBlockOverride = ItemID.None)
        {
            if (seedType <= ItemID.None)
                return new FurnitureScheme();

            if (!forceRefresh
                && FurnitureSetCacheSystem.TryGetCachedSchemeForItem(
                    seedType, anchorBlockOverride, out FurnitureScheme byItem, out int cachedMaterial))
            {
                int material = anchorBlockOverride > ItemID.None ? anchorBlockOverride : cachedMaterial;
                FurnitureScheme result = byItem.Clone();
                result.SeedType = seedType;
                FurnitureBlueprintLog.Info(
                    $"recognize cache redirect query={seedType} material={material} primarySeed={byItem.SeedType} filled={CountFilled(byItem)}/{FurnitureWikiSlots.TotalCount}");
                return result;
            }

            if (!forceRefresh && anchorBlockOverride > ItemID.None
                && FurnitureSetCacheSystem.TryGetCached(seedType, anchorBlockOverride, out FurnitureScheme cached))
            {
                FurnitureBlueprintLog.InfoFull(
                    $"recognize cache hit seed={seedType} anchor={anchorBlockOverride}");
                return cached.Clone();
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var scheme = BuildScheme(seedType, anchorBlockOverride);
            sw.Stop();
            if (sw.ElapsedMilliseconds > 200)
                FurnitureBlueprintLog.Warn($"recognize slow seed={seedType} ms={sw.ElapsedMilliseconds}");
            int cacheMaterial = scheme.AnchorMaterialType > ItemID.None ? scheme.AnchorMaterialType : anchorBlockOverride;
            if (cacheMaterial > ItemID.None
                && FurnitureGenericWoodLineageRules.IsSchemeCacheable(scheme, seedType, cacheMaterial))
                FurnitureSetCacheSystem.RegisterScheme(scheme, seedType, cacheMaterial);
            FurnitureBlueprintLog.InfoFull(
                $"recognize seed={seedType} anchor={scheme.AnchorMaterialType} blockOverride={anchorBlockOverride} name={scheme.DisplayName} filled={CountFilled(scheme)}");
            return scheme.Clone();
        }

        /// <summary>分帧识别入口；缓存命中时立即返回已完�? Job�?</summary>
        public static FurnitureRecognitionJob BeginRecognition(
            int seedType,
            int anchorBlockOverride = ItemID.None,
            bool forceRefresh = false)
        {
            if (seedType <= ItemID.None)
                return FurnitureRecognitionJob.CreateCompleted(new FurnitureScheme(), seedType, anchorBlockOverride);

            if (!forceRefresh && anchorBlockOverride > ItemID.None
                && FurnitureSetCacheSystem.TryGetCached(seedType, anchorBlockOverride, out FurnitureScheme cached))
                return FurnitureRecognitionJob.CreateCompleted(cached.Clone(), seedType, anchorBlockOverride);

            if (!forceRefresh
                && FurnitureSetCacheSystem.TryGetCachedSchemeForItem(
                    seedType, anchorBlockOverride, out FurnitureScheme byItem, out _))
            {
                FurnitureScheme hit = byItem.Clone();
                hit.SeedType = seedType;
                return FurnitureRecognitionJob.CreateCompleted(hit, seedType, anchorBlockOverride);
            }

            try
            {
                return FurnitureRecognitionJob.CreatePending(seedType, anchorBlockOverride);
            }
            catch (System.Exception ex)
            {
                FurnitureBlueprintDiagnostics.LogRecognizeFailure(seedType, anchorBlockOverride, ex, "prepare");
                var empty = new FurnitureScheme { SeedType = seedType };
                return FurnitureRecognitionJob.CreateCompleted(empty, seedType, anchorBlockOverride);
            }
        }

        private static FurnitureScheme BuildScheme(int seedType, int anchorBlockOverride = ItemID.None)
        {
            FurnitureRecognitionJob job = PrepareRecognitionJob(seedType, anchorBlockOverride);
            int guard = 0;
            while (!job.IsComplete && guard++ < MaxRecognizeTicksPerSeed)
            {
                if (FurnitureRecognitionRunner.Tick(job, FurnitureRecognitionRunner.FrameBudgetMs))
                    break;
            }

            return job.Scheme;
        }

        private const int MaxRecognizeTicksPerSeed = 8_000;

        internal static FurnitureRecognitionJob PrepareRecognitionJob(int seedType, int anchorBlockOverride = ItemID.None)
        {
            var scheme = new FurnitureScheme
            {
                SeedType = seedType,
                IsAutoGenerated = true,
                DisplayName = GetDefaultDisplayName(seedType)
            };

            FurnitureStyleSignature signature = FurnitureStyleSignature.FromItemType(seedType);
            FurnitureCraftStationProfile stations = FurnitureCraftStationProfile.FromSeed(seedType);
            if (stations.IsConstrained)
            {
                bool enhanced = FurnitureCraftStationRules.UsesEnhancedWorkbenchSubstitution(seedType, stations);
                FurnitureBlueprintLog.InfoFull(
                    $"craft stations seed={seedType} tiles={stations.StationTiles.Count} living={stations.ImpliesLivingWoodStation} sawmill={stations.ImpliesSawmillStation} enhancedWorkbench={enhanced}");
            }

            int anchor;
            if (anchorBlockOverride > ItemID.None)
            {
                anchor = anchorBlockOverride;
                FurnitureBlueprintLog.InfoFull(
                    $"recognize use confirmed anchor seed={seedType} block={anchorBlockOverride}");
            }
            else
            {
                anchor = ResolveAnchorMaterial(seedType, signature);
            }

            anchor = FurnitureVanillaLivingWoodBridge.RedirectReverseAnchor(seedType, anchor);
            anchor = FurnitureSetMaterialRules.ResolveModMaterialBlock(seedType, anchor);
            scheme.AnchorMaterialType = anchor;

            Item seedItem = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(seedItem, seedType))
                seedItem = new Item();

            FurnitureStyleSignature pickSignature = BuildPickSignature(seedType, signature, anchor, seedItem);

            if (signature.UsesPlacementStyleLine)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"style line seed={seedType} tile={signature.PlacementTile} style={signature.PlacementStyle} blockSeed={signature.SeedIsMaterialBlock}");
            }

            FurnitureStyleSignature blockSig = pickSignature;
            int materialBlock = ItemID.None;
            if (anchor > ItemID.None)
            {
                Item confirmedBlock = new Item();
                if (FurnitureItemDefaults.TrySetDefaults(confirmedBlock, anchor)
                    && FurnitureMaterialAnchor.IsValidAnchorBlock(confirmedBlock))
                    materialBlock = anchor;
            }

            if (materialBlock <= ItemID.None)
                materialBlock = ResolveMaterialBlock(seedType, anchor, seedItem, blockSig);

            materialBlock = FurnitureSetMaterialRules.ResolveModMaterialBlock(seedType, materialBlock);
            FurnitureSetMaterialRules.ApplyLivingWoodRecipeMaterial(seedType, ref materialBlock);
            if (anchorBlockOverride > ItemID.None
                && !FurnitureVanillaLivingWoodBridge.TryGetRecipeWoodMaterial(seedType, out _)
                && !FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
                materialBlock = anchorBlockOverride;
            if (materialBlock > ItemID.None)
                scheme.AnchorMaterialType = materialBlock;

            if (materialBlock > ItemID.None)
                blockSig = FurnitureMaterialBlockSignature.ForProducts(materialBlock, signature, seedType);

            scheme.SetSlot(FurnitureSlotKind.Block, materialBlock > ItemID.None ? materialBlock : ItemID.None);

            HashSet<int> candidates = CollectCandidates(
                seedType,
                signature,
                materialBlock,
                blockSig,
                FurnitureSetConfidence.EvaluatePreview(seedType, signature, materialBlock, blockSig));
            FurnitureBlueprintLog.Info(
                $"recognize begin seed={seedType} name={FurnitureItemDefaults.SafeItemName(seedType)} material={materialBlock} matName={(materialBlock > ItemID.None ? FurnitureItemDefaults.SafeItemName(materialBlock) : "-")} candidates={candidates.Count}");

            var candidateList = new List<int>(candidates.Count);
            foreach (int type in candidates)
                candidateList.Add(type);

            return new FurnitureRecognitionJob(
                seedType, anchorBlockOverride, scheme, candidateList, materialBlock, blockSig, stations);
        }

        /// <summary>分帧 prepare：每帧推进一步，避免单帧 CollectCandidates + probe 卡死�?</summary>
        internal static bool TickPrepareRecognitionJob(
            FurnitureRecognitionJob job,
            int budgetMs,
            out FurnitureRecognitionJob prepared)
        {
            prepared = null;
            if (job == null)
                return true;

            _ = budgetMs;

            int seedType = job.SeedType;
            int anchorBlockOverride = job.AnchorBlock;

            switch (job.PrepareStep)
            {
                case 0:
                    job.PrepareScheme = new FurnitureScheme
                    {
                        SeedType = seedType,
                        IsAutoGenerated = true,
                        DisplayName = GetDefaultDisplayName(seedType)
                    };
                    job.PrepareSignature = FurnitureStyleSignature.FromItemType(seedType);
                    job.PrepareStations = FurnitureCraftStationProfile.FromSeed(seedType);
                    if (job.PrepareStations.IsConstrained)
                    {
                        bool enhanced = FurnitureCraftStationRules.UsesEnhancedWorkbenchSubstitution(seedType, job.PrepareStations);
                        FurnitureBlueprintLog.InfoFull(
                            $"craft stations seed={seedType} tiles={job.PrepareStations.StationTiles.Count} living={job.PrepareStations.ImpliesLivingWoodStation} sawmill={job.PrepareStations.ImpliesSawmillStation} enhancedWorkbench={enhanced}");
                    }

                    job.PrepareStep = 1;
                    return false;

                case 1:
                {
                    FurnitureStyleSignature signature = job.PrepareSignature;
                    int anchor;
                    if (anchorBlockOverride > ItemID.None)
                    {
                        anchor = anchorBlockOverride;
                        FurnitureBlueprintLog.InfoFull(
                            $"recognize use confirmed anchor seed={seedType} block={anchorBlockOverride}");
                    }
                    else
                    {
                        anchor = ResolveAnchorMaterial(seedType, signature);
                    }

                    anchor = FurnitureVanillaLivingWoodBridge.RedirectReverseAnchor(seedType, anchor);
                    anchor = FurnitureSetMaterialRules.ResolveModMaterialBlock(seedType, anchor);
                    job.PrepareScheme.AnchorMaterialType = anchor;
                    job.PrepareResolvedAnchor = anchor;
                    job.PrepareStep = 2;
                    return false;
                }

                case 2:
                {
                    FurnitureStyleSignature signature = job.PrepareSignature;
                    int anchor = job.PrepareResolvedAnchor;

                    Item seedItem = new Item();
                    if (!FurnitureItemDefaults.TrySetDefaults(seedItem, seedType))
                        seedItem = new Item();

                    FurnitureStyleSignature pickSignature = BuildPickSignature(seedType, signature, anchor, seedItem);

                    if (signature.UsesPlacementStyleLine)
                    {
                        FurnitureBlueprintLog.InfoFull(
                            $"style line seed={seedType} tile={signature.PlacementTile} style={signature.PlacementStyle} blockSeed={signature.SeedIsMaterialBlock}");
                    }

                    FurnitureStyleSignature blockSig = pickSignature;
                    int materialBlock = ItemID.None;
                    if (anchor > ItemID.None)
                    {
                        Item confirmedBlock = new Item();
                        if (FurnitureItemDefaults.TrySetDefaults(confirmedBlock, anchor)
                            && FurnitureMaterialAnchor.IsValidAnchorBlock(confirmedBlock))
                            materialBlock = anchor;
                    }

                    if (materialBlock <= ItemID.None)
                        materialBlock = ResolveMaterialBlock(seedType, anchor, seedItem, blockSig);

                    materialBlock = FurnitureSetMaterialRules.ResolveModMaterialBlock(seedType, materialBlock);
                    FurnitureSetMaterialRules.ApplyLivingWoodRecipeMaterial(seedType, ref materialBlock);
                    if (anchorBlockOverride > ItemID.None
                        && !FurnitureVanillaLivingWoodBridge.TryGetRecipeWoodMaterial(seedType, out _)
                        && !FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
                        materialBlock = anchorBlockOverride;
                    if (materialBlock > ItemID.None)
                        job.PrepareScheme.AnchorMaterialType = materialBlock;

                    if (materialBlock > ItemID.None)
                        blockSig = FurnitureMaterialBlockSignature.ForProducts(materialBlock, signature, seedType);

                    job.PrepareScheme.SetSlot(FurnitureSlotKind.Block, materialBlock > ItemID.None ? materialBlock : ItemID.None);
                    job.PrepareMaterialBlock = materialBlock;
                    job.PrepareBlockSig = blockSig;
                    job.PrepareStep = 3;
                    return false;
                }

                case 3:
                    job.PrepareConfidence = FurnitureSetConfidence.EvaluatePreview(
                        seedType, job.PrepareSignature, job.PrepareMaterialBlock, job.PrepareBlockSig);
                    job.PrepareCandidates = new HashSet<int>();
                    job.PrepareCollectPhase = 0;
                    job.PrepareStep = 4;
                    return false;

                case 4:
                    if (!TickPrepareCollectOnce(job))
                        return false;
                    job.PrepareStep = 5;
                    return false;

                case 5:
                {
                    int materialBlock = job.PrepareMaterialBlock;
                    FurnitureStyleSignature blockSig = job.PrepareBlockSig;

                    FurnitureBlueprintLog.Info(
                        $"recognize begin seed={seedType} name={FurnitureItemDefaults.SafeItemName(seedType)} material={materialBlock} matName={(materialBlock > ItemID.None ? FurnitureItemDefaults.SafeItemName(materialBlock) : "-")} candidates={job.PrepareCandidates.Count}");

                    var candidateList = new List<int>(job.PrepareCandidates.Count);
                    foreach (int type in job.PrepareCandidates)
                        candidateList.Add(type);

                    prepared = new FurnitureRecognitionJob(
                        seedType, anchorBlockOverride, job.PrepareScheme, candidateList, materialBlock, blockSig, job.PrepareStations);

                    job.PrepareStep = 0;
                    job.PrepareScheme = null;
                    job.PrepareCandidates = null;
                    job.PrepareCollectPhase = 0;
                    return true;
                }

                default:
                    job.PrepareStep = 0;
                    return false;
            }
        }

        private const int PrepareCollectPhaseDone = 4;

        /// <summary>分帧收集候选；返回 true 表示候选集已就绪�?</summary>
        private static bool TickPrepareCollectOnce(FurnitureRecognitionJob job)
        {
            int seedType = job.SeedType;
            int materialBlock = job.PrepareMaterialBlock;
            FurnitureStyleSignature signature = job.PrepareSignature;
            FurnitureStyleSignature blockSig = job.PrepareBlockSig;
            FurnitureSetConfidenceReport preview = job.PrepareConfidence;
            HashSet<int> raw = job.PrepareCandidates;

            switch (job.PrepareCollectPhase)
            {
                case 0:
                    if (preview.PreferSeedCluster || preview.Tier == FurnitureSetConfidenceTier.Low)
                    {
                        FurnitureStyleClusterCatalog.ExpandFromSeed(seedType, signature, raw, materialBlock);
                        if (materialBlock > ItemID.None && raw.Count < FurnitureSetConfidence.LowMaterialCandidateCap)
                        {
                            foreach (int type in FurnitureRecognitionCaches.GetOrCollectMaterialProducts(
                                         seedType, materialBlock, blockSig))
                            {
                                if (raw.Count >= FurnitureSetConfidence.LowMaterialCandidateCap)
                                    break;
                                raw.Add(type);
                            }
                        }

                        FurnitureBlueprintLog.InfoFull(
                            $"candidates seed-cluster seed={seedType} tier={preview.Tier} style={blockSig.StyleKey} count={raw.Count}");
                    }
                    else if (materialBlock > ItemID.None)
                    {
                        foreach (int type in FurnitureRecognitionCaches.GetOrCollectMaterialProducts(
                                     seedType, materialBlock, blockSig))
                            raw.Add(type);
                    }
                    else
                    {
                        FurnitureStyleClusterCatalog.ExpandFromSeed(seedType, signature, raw, ItemID.None);
                        FurnitureBlueprintLog.InfoFull(
                            $"candidates seed-only seed={seedType} style={blockSig.StyleKey} count={raw.Count}");
                        job.PrepareCandidates = FurnitureRecognizeCandidateCap.TrimIfNeeded(raw, seedType, materialBlock, blockSig);
                        job.PrepareCollectPhase = PrepareCollectPhaseDone;
                        return true;
                    }

                    job.PrepareCollectPhase = 1;
                    return false;

                case 1:
                    if (materialBlock > ItemID.None
                        && !FurnitureBlueprintScope.StrictMaterialOnly
                        && !(preview.PreferSeedCluster || preview.Tier == FurnitureSetConfidenceTier.Low)
                        && (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType)
                            || FurnitureSetMaterialRules.UsesModSpecificMaterialBlock(seedType)
                            || raw.Count < FurnitureSetConfidence.LowMaterialCandidateCap))
                    {
                        FurnitureStyleClusterCatalog.ExpandFromSeed(seedType, signature, raw, materialBlock);
                        FurnitureCandidateExpander.Expand(seedType, blockSig, materialBlock, raw);
                        FurnitureMaterialPlacementExpander.ExpandFromMaterialAndSeed(raw, seedType, materialBlock, blockSig);
                        FurnitureBlueprintLog.InfoFull(
                            $"candidates mod-expand seed={seedType} style={blockSig.StyleKey} count={raw.Count}");
                    }

                    job.PrepareCollectPhase = 2;
                    return false;

                case 2:
                    job.PrepareCandidates = FurnitureRecognizeCandidateCap.TrimIfNeeded(raw, seedType, materialBlock, blockSig);
                    if (materialBlock > ItemID.None)
                    {
                        FurnitureBlueprintLog.InfoFull(
                            $"candidates material-first seed={seedType} style={blockSig.StyleKey} count={job.PrepareCandidates.Count}");
                    }

                    job.PrepareCollectPhase = PrepareCollectPhaseDone;
                    return true;

                default:
                    return true;
            }
        }

        internal static void ClassifyOneCandidate(int type, FurnitureRecognitionJob job)
        {
            try
            {
                ClassifyOneCandidateCore(type, job);
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"classify failed type={type} seed={job.SeedType}: {ex.Message}");
                job.RejectedClassify++;
            }
        }

        private static void ClassifyOneCandidateCore(int type, FurnitureRecognitionJob job)
        {
            int seedType = job.SeedType;
            int materialBlock = job.MaterialBlock;
            var perSlot = job.PerSlot;
            var ctx = job.Ctx;

            if (!FurnitureRecognitionCaches.TryGetProbe(type, out Item probe))
            {
                job.RejectedPlaceable++;
                return;
            }

            if (probe.createWall <= WallID.None
                && !FurnitureTileSafety.HasPlaceableTile(probe))
            {
                job.RejectedPlaceable++;
                return;
            }

            if (!FurnitureRecognitionCaches.IsPlaceableFurniture(type))
            {
                job.RejectedPlaceable++;
                return;
            }

            if (!FurnitureSlotClassifier.TryGetSlot(probe, out FurnitureSlotKind kind, out _))
            {
                job.RejectedClassify++;
                return;
            }

            FurnitureSlotKind rawKind = kind;
            kind = FurnitureWikiSlots.NormalizeClassified(kind);
            if (kind == FurnitureSlotKind.None)
            {
                job.RejectedClassify++;
                return;
            }

            if (FurnitureBuildingBlockRules.MustNotOccupyWikiFurnitureSlot(probe, kind))
            {
                job.RejectedClassify++;
                return;
            }

            if (kind == FurnitureSlotKind.Block
                && type != materialBlock
                && !FurnitureMaterialAnchor.IsValidAnchorBlock(probe))
            {
                job.RejectedBlockBucket++;
                return;
            }

            if (!perSlot.TryGetValue(kind, out List<int> list))
                return;

            list.Add(type);
            ctx.RecordClassification(type, kind);
            FurnitureRecognitionCaches.CacheClassification(type, kind, true);
        }

        internal static void FinalizeSchemeFromJob(FurnitureRecognitionJob job)
        {
            job.FinalizeStep = 0;
            int guard = 0;
            while (!TickFinalizeScheme(job, FurnitureRecognitionRunner.FrameBudgetMs) && guard++ < MaxRecognizeTicksPerSeed)
            {
            }
        }

        /// <summary>???????????????? true ?? finalize ???</summary>
        internal static bool TickFinalizeScheme(FurnitureRecognitionJob job, int budgetMs)
        {
            if (job == null)
                return true;

            _ = budgetMs;

            switch (job.FinalizeStep)
            {
                case 0:
                    BeginFinalizeScheme(job);
                    job.FinalizeStep = 1;
                    return false;

                case 1:
                    if (TickFinalizeWikiBucket(job))
                    {
                        LogFinalizeSlotSummary(job);
                        job.FinalizeStep = 2;
                        job.FinalizePlaceholderSlotIndex = 0;
                    }
                    return false;

                case 2:
                    if (TickFinalizePlaceholderSlot(job))
                        job.FinalizeStep = 3;
                    return false;

                case 3:
                    FurnitureBlueprintCrashDiagnostics.BeginSeed(job.SeedType);
                    FurnitureBlueprintCrashDiagnostics.Phase("finalize-step3", "begin");
                    CompleteFinalizeScheme(job);
                    FurnitureBlueprintCrashDiagnostics.Phase("finalize-step3", "end");
                    FurnitureBlueprintCrashDiagnostics.EndSeed();
                    job.FinalizeStep = 4;
                    return true;

                default:
                    return true;
            }
        }

        private static void BeginFinalizeScheme(FurnitureRecognitionJob job)
        {
            int seedType = job.SeedType;
            int materialBlock = job.MaterialBlock;
            FurnitureScheme scheme = job.Scheme;
            FurnitureStyleSignature blockSig = job.BlockSig;
            FurnitureCraftStationProfile stations = job.Stations;
            var perSlot = job.PerSlot;
            var ctx = job.Ctx;

            job.FinalizeCandidates = new HashSet<int>(job.CandidateList);
            ctx.SetCommonWords(FurnitureSetLineageScoring.BuildCommonWords(seedType, perSlot, materialBlock, scheme));

            EnsureSeedHomeSlot(scheme, seedType);

            var confidence = FurnitureSetConfidence.Evaluate(job);
            ctx.SetConfidence(confidence);
            FurnitureBlueprintLog.InfoFull(
                $"set-confidence seed={seedType} tier={confidence.Tier} strict={confidence.StrictSlotGate} style_align={confidence.StyleAlignmentPercent}%");

            if (materialBlock > ItemID.None)
            {
                if (perSlot[FurnitureSlotKind.Wall].Count > 0)
                {
                    int wallPick = ResolveSlotFromBucket(
                        perSlot[FurnitureSlotKind.Wall], FurnitureSlotKind.Wall, seedType, materialBlock, blockSig, stations, "wall-bucket", ctx);
                    if (wallPick > ItemID.None)
                        scheme.SetSlot(FurnitureSlotKind.Wall, wallPick);
                    else
                        TryFillWallFromAnchor(scheme, materialBlock, blockSig);
                }
                else
                {
                    TryFillWallFromAnchor(scheme, materialBlock, blockSig);
                }

                if (perSlot[FurnitureSlotKind.Platform].Count > 0)
                {
                    int platPick = ResolveSlotFromBucket(
                        perSlot[FurnitureSlotKind.Platform], FurnitureSlotKind.Platform, seedType, materialBlock, blockSig, stations, "platform-bucket", ctx);
                    if (platPick > ItemID.None)
                        scheme.SetSlot(FurnitureSlotKind.Platform, platPick);
                }
            }

            job.FinalizeWikiSlotIndex = 0;
            job.FinalizeOccupied = FurnitureSchemeOccupancy.CollectOccupied(scheme, materialBlock, seedType);
        }

        private static bool TickFinalizeWikiBucket(FurnitureRecognitionJob job)
        {
            FurnitureSlotKind[] order = FurnitureWikiSlots.RecognitionOrder;
            while (job.FinalizeWikiSlotIndex < order.Length)
            {
                FurnitureSlotKind wikiSlot = order[job.FinalizeWikiSlotIndex++];
                if (wikiSlot is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    continue;

                if (!job.PerSlot.TryGetValue(wikiSlot, out List<int> list) || list.Count == 0)
                    continue;

                int materialBlock = job.MaterialBlock;
                int anchor = job.AnchorBlock;
                int pick = ResolveSlotFromBucket(
                    list,
                    wikiSlot,
                    job.SeedType,
                    materialBlock > ItemID.None ? materialBlock : anchor,
                    job.BlockSig,
                    job.Stations,
                    "wiki-bucket",
                    job.Ctx,
                    job.FinalizeOccupied);
                if (pick > ItemID.None)
                {
                    job.Scheme.SetSlot(wikiSlot, pick);
                    FurnitureSchemeOccupancy.MarkUsed(job.FinalizeOccupied, pick);
                }

                return false;
            }

            return true;
        }

        private static void LogFinalizeSlotSummary(FurnitureRecognitionJob job)
        {
            if (job.FinalizeLoggedSlots)
                return;

            job.FinalizeLoggedSlots = true;
            int seedType = job.SeedType;
            int materialBlock = job.MaterialBlock;
            int wikiFilled = CountWikiFilled(job.Scheme);
            int candidateCount = job.FinalizeCandidates?.Count ?? job.CandidateList.Count;

            FurnitureBlueprintLog.Info(
                $"recognize slots seed={seedType} candidates={candidateCount} wiki_filled={wikiFilled}/{FurnitureWikiSlots.TotalCount} " +
                $"classify_miss={job.RejectedClassify} material={materialBlock}");
            FurnitureBlueprintLog.InfoFull(
                $"recognize slots detail seed={seedType} material={materialBlock} classify_miss={job.RejectedClassify} slots={FormatFilledSlots(job.Scheme)}");
        }

        private static bool TickFinalizePlaceholderSlot(FurnitureRecognitionJob job)
        {
            int materialBlock = job.MaterialBlock;
            if (materialBlock <= ItemID.None)
                return true;

            FurnitureSlotKind[] order = FurnitureWikiSlots.RecognitionOrder;
            while (job.FinalizePlaceholderSlotIndex < order.Length)
            {
                FurnitureSlotKind slot = order[job.FinalizePlaceholderSlotIndex++];
                if (slot is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    continue;

                if (job.Scheme.GetSlot(slot) > ItemID.None)
                    continue;

                if (!job.Ctx.TryBeginPlaceholderAttempt(slot))
                    continue;

                FurnitureBlueprintCrashDiagnostics.BeginSeed(job.SeedType);
                FurnitureBlueprintCrashDiagnostics.SlotStep(slot, "placeholder-begin");

                RefreshPlaceholderCommonWords(job);

                if (!job.FinalizeExpandedBuilt)
                {
                    FurnitureBlueprintCrashDiagnostics.Phase("placeholder", "expand-candidates");
                    job.FinalizeExpanded = BuildFinalizeExpandedCandidates(job);
                    job.FinalizeExpandedBuilt = true;
                }

                var occupied = FurnitureSchemeOccupancy.CollectOccupied(job.Scheme, materialBlock, job.SeedType);
                FurnitureBlueprintCrashDiagnostics.Phase("placeholder", $"build-pool slot={slot}");
                List<int> pool = FurniturePlaceholderPool.Build(
                    slot, job.PerSlot, job.FinalizeExpanded, occupied,
                    job.SeedType, materialBlock, job.Ctx);
                FurnitureBlueprintCrashDiagnostics.Phase("placeholder", $"pick slot={slot} pool={pool.Count}");
                int pick = FurnitureSlotPicker.PickForSlot(
                    pool, slot, job.SeedType, materialBlock, job.BlockSig, job.Stations,
                    FurnitureSlotScoring.GetMinPlaceholderScore(slot, job.SeedType, materialBlock), occupied, job.Ctx);

                EmojLogDiagnostics.LogSlotResolved(job.SeedType, slot, pick, pool.Count, "placeholder-once");
                FurnitureBlueprintCrashDiagnostics.Item(slot, pick, "placeholder-pick");

                if (pick <= ItemID.None)
                {
                    FurnitureBlueprintCrashDiagnostics.EndSeed();
                    continue;
                }

                job.Scheme.SetSlot(slot, pick);
                if (!job.PerSlot.TryGetValue(slot, out List<int> list))
                {
                    list = new List<int>();
                    job.PerSlot[slot] = list;
                }

                if (!list.Contains(pick))
                    list.Add(pick);

                FurnitureBlueprintCrashDiagnostics.Phase("placeholder", $"done slot={slot}");
                FurnitureBlueprintCrashDiagnostics.EndSeed();
                return false;
            }

            return true;
        }

        private static HashSet<int> BuildFinalizeExpandedCandidates(FurnitureRecognitionJob job)
        {
            var expanded = job.FinalizeCandidates != null
                ? new HashSet<int>(job.FinalizeCandidates)
                : new HashSet<int>(job.CandidateList);
            if (expanded.Count >= FurnitureSlotScoring.MaxPlaceholderCandidates)
                return expanded;

            if (RecipeAnalyzer.IsHighFanoutMaterial(job.MaterialBlock))
                return expanded;

            int cap = FurnitureSlotScoring.MaxPlaceholderCandidates * 2;
            if (expanded.Count < cap)
                FurnitureCandidateExpander.Expand(job.SeedType, job.BlockSig, job.MaterialBlock, expanded);

            FurnitureCandidateExpander.EnsureMaterialRoleProducts(job.MaterialBlock, job.BlockSig, expanded);

            if (expanded.Count >= cap)
                return expanded;

            foreach (int product in FurnitureMaterialProductCollector.CollectFromMaterialBlock(job.MaterialBlock, job.BlockSig))
            {
                if (expanded.Count >= cap)
                    break;
                expanded.Add(product);
            }

            return expanded;
        }

        private static void RefreshPlaceholderCommonWords(FurnitureRecognitionJob job)
        {
            int seedType = job.SeedType;
            int materialBlock = job.MaterialBlock;
            bool boost = FurnitureGenericWoodLineageRules.ShouldBoostPlaceholderCommonWords(seedType, materialBlock);
            job.Ctx.SetPlaceholderCommonWordBoost(boost);

            if (!boost)
                return;

            job.Ctx.SetCommonWords(FurnitureSetLineageScoring.BuildCommonWords(
                seedType,
                job.PerSlot,
                materialBlock,
                job.Scheme,
                relaxedOccurrence: true));
        }

        private static void CompleteFinalizeScheme(FurnitureRecognitionJob job)
        {
            FurnitureBlueprintCrashDiagnostics.Phase("finalize", "empty-wiki-slots");
            LogEmptyWikiSlots(job.Scheme, job.PerSlot, job.SeedType);

            int finalWiki = CountWikiFilled(job.Scheme);
            if (finalWiki < FurnitureWikiSlots.TotalCount)
            {
                FurnitureBlueprintLog.Warn(
                    $"recognize incomplete seed={job.SeedType} material={job.MaterialBlock} wiki={finalWiki}/{FurnitureWikiSlots.TotalCount} candidates={job.FinalizeCandidates?.Count ?? job.CandidateList.Count}");
            }

            FurnitureBlueprintCrashDiagnostics.Phase("finalize", "backfill-bathtub");
            TryBackfillBathtubFromCandidates(
                job.Scheme, job, job.SeedType, job.MaterialBlock, job.BlockSig, job.Stations, job.Ctx);
            FurnitureBlueprintCrashDiagnostics.Phase("finalize", "bed-bath-backfill");
            FurnitureBedBathtubBackfill.TryFillEmptySlots(
                job.Scheme, job, job.SeedType, job.MaterialBlock, job.BlockSig, job.Stations, job.Ctx);
            FurnitureBlueprintCrashDiagnostics.Phase("finalize", "complete");
        }

        private static void TryBackfillBathtubFromCandidates(
            FurnitureScheme scheme,
            FurnitureRecognitionJob job,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            FurnitureRecognizeContext ctx)
        {
            if (scheme.GetSlot(FurnitureSlotKind.Bathtub) > ItemID.None || materialBlock <= ItemID.None)
                return;

            int pick = FurnitureSlotPicker.PickByScoreFromProducts(
                job.CandidateList,
                FurnitureSlotKind.Bathtub,
                seedType,
                materialBlock,
                blockSig,
                stations,
                FurnitureSlotScoring.GetMinPickScore(FurnitureSlotKind.Bathtub, seedType, materialBlock),
                ctx: ctx);

            if (pick > ItemID.None)
                scheme.SetSlot(FurnitureSlotKind.Bathtub, pick);
        }

        private static int ResolveMaterialBlock(
            int seedType,
            int anchor,
            Item seedItem,
            FurnitureStyleSignature blockSig)
        {
            if (FurnitureMaterialAnchor.IsValidAnchorBlock(seedItem))
                return seedType;

            int block = FurnitureMaterialAnchor.ResolvePlaceableBlock(anchor, blockSig);
            if (block > ItemID.None)
                return block;

            if (anchor > ItemID.None)
            {
                Item anchorProbe = new Item();
                if (FurnitureItemDefaults.TrySetDefaults(anchorProbe, anchor)
                    && FurnitureMaterialAnchor.IsValidAnchorBlock(anchorProbe))
                    return anchor;
            }

            return anchor > ItemID.None ? anchor : ItemID.None;
        }

        /// <summary>Ͱ�� 1 ��ֱ�Ӳ��ã�������ս�������?/����̨/�䷽����</summary>
        private static int ResolveSlotFromBucket(
            List<int> candidates,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            string source,
            FurnitureRecognizeContext ctx,
            HashSet<int> occupied = null)
        {
            if (candidates == null || candidates.Count == 0)
                return ItemID.None;

            int minScore = FurnitureSlotScoring.GetMinPickScore(slot, seedType, materialBlock);
            int pick = FurnitureSlotPicker.PickForSlot(
                candidates, slot, seedType, materialBlock, blockSig, stations, minScore, occupied, ctx);

            if (pick <= ItemID.None && slot == FurnitureSlotKind.Bathtub)
                pick = TryPickBathtubRelaxed(candidates, seedType, materialBlock);

            if (pick <= ItemID.None && slot == FurnitureSlotKind.Bed)
                pick = TryPickBedRelaxed(candidates, seedType, materialBlock);

            EmojLogDiagnostics.LogSlotResolved(seedType, slot, pick, candidates.Count, source);
            return pick;
        }

        private static int TryPickBathtubRelaxed(List<int> candidates, int seedType, int materialBlock)
        {
            if (candidates == null)
                return ItemID.None;

            int best = ItemID.None;
            int bestRank = int.MinValue;
            foreach (int type in candidates)
            {
                if (!FurnitureBathtubRules.SharesSetWithMaterial(type, materialBlock, seedType))
                    continue;

                int rank = FurnitureSetLineageScoring.ScoreSeedLineage(type, seedType, materialBlock)
                    + FurnitureRecipeSlotSignals.ScoreNameBonus(type, FurnitureSlotKind.Bathtub);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = type;
                }
            }

            return best;
        }

        private static int TryPickBedRelaxed(List<int> candidates, int seedType, int materialBlock)
        {
            if (candidates == null)
                return ItemID.None;

            int best = ItemID.None;
            int bestRank = int.MinValue;
            foreach (int type in candidates)
            {
                if (!FurnitureNameSignals.MeetsBedPickEvidence(type, materialBlock, seedType))
                    continue;

                int rank = FurnitureSetLineageScoring.ScoreSeedLineage(type, seedType, materialBlock)
                    + FurnitureRecipeSlotSignals.ScoreNameBonus(type, FurnitureSlotKind.Bed);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = type;
                }
            }

            return best;
        }

        private static void LogEmptyWikiSlots(
            FurnitureScheme scheme,
            Dictionary<FurnitureSlotKind, List<int>> perSlot,
            int seedType)
        {
            var empty = new List<string>();
            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                if (scheme.GetSlot(kind) > ItemID.None)
                    continue;
                int pool = perSlot.TryGetValue(kind, out List<int> list) ? list.Count : 0;
                empty.Add($"{kind}(pool={pool})");
            }

            if (empty.Count > 0)
            {
                FurnitureBlueprintLog.InfoFull($"wiki empty seed={seedType} {string.Join(",", empty)}");
            }
        }

        private static FurnitureStyleSignature BuildPickSignature(
            int seedType,
            FurnitureStyleSignature signature,
            int anchor,
            Item seedItem)
        {
            if (FurnitureVanillaLivingWoodBridge.TryGetSetSignature(seedType, out FurnitureStyleSignature livingWoodSig))
                return livingWoodSig;

            if (FurnitureSetMaterialRules.TryGetModLineageSetSignature(seedType, out FurnitureStyleSignature lineageSig))
                return lineageSig;

            string styleKey = signature.StyleKey;
            int lineTile = signature.PlacementTile;
            int lineStyle = signature.PlacementStyle;
            bool useLine = signature.UsesPlacementStyleLine;

            if (seedItem != null && !seedItem.IsAir && seedItem.createTile >= TileID.Dirt
                && !FurnitureMaterialAnchor.IsValidAnchorBlock(seedItem))
            {
                FurnitureStyleSignature fromSeed = FurnitureStyleSignature.FromItemType(seedItem.type);
                if (fromSeed.UsesPlacementStyleLine)
                {
                    lineTile = fromSeed.PlacementTile;
                    lineStyle = fromSeed.PlacementStyle;
                    useLine = true;
                }
            }

            if (anchor > ItemID.None)
            {
                FurnitureStyleSignature anchorSig = FurnitureStyleSignature.FromItemTypeForRecipes(anchor);
                if (string.IsNullOrWhiteSpace(styleKey))
                    styleKey = anchorSig.StyleKey;

                if (!useLine && anchorSig.UsesPlacementStyleLine)
                {
                    lineTile = anchorSig.PlacementTile;
                    lineStyle = anchorSig.PlacementStyle;
                    useLine = true;
                }
            }

            return new FurnitureStyleSignature
            {
                ModKey = signature.ModKey,
                StyleKey = styleKey,
                PlacementTile = lineTile,
                PlacementStyle = lineStyle,
                UsesPlacementStyleLine = useLine,
                SeedIsMaterialBlock = signature.SeedIsMaterialBlock
            };
        }

        private static void BackfillEmptyWikiSlots(
            FurnitureScheme scheme,
            HashSet<int> candidates,
            int seedType,
            int anchorType,
            FurnitureStyleSignature signature)
        {
            int filledBefore = CountWikiFilled(scheme);

            foreach (FurnitureSlotKind wikiSlot in FurnitureWikiSlots.RecognitionOrder)
            {
                if (wikiSlot == FurnitureSlotKind.Block || scheme.GetSlot(wikiSlot) > ItemID.None)
                    continue;

                int best = ItemID.None;
                int bestScore = int.MinValue;

                foreach (int type in candidates)
                {
                    Item probe = new Item();
                    probe.SetDefaults(type);
                    if (!FurnitureCandidateFilter.IsPlaceableFurnitureItem(probe))
                        continue;
                    if (!FurnitureSlotClassifier.TryGetSlot(probe, out FurnitureSlotKind kind))
                        continue;
                    if (FurnitureWikiSlots.NormalizeClassified(kind) != wikiSlot)
                        continue;
                    if (!FurnitureCandidateFilter.PassesSlotRules(probe, wikiSlot))
                        continue;

                    int score = ScoreCandidate(type, seedType, anchorType, signature);
                    if (score <= int.MinValue / 8)
                        continue;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = type;
                    }
                }

                if (best > ItemID.None)
                    scheme.SetSlot(wikiSlot, best);
            }

            int filledAfter = CountWikiFilled(scheme);
            if (filledAfter > filledBefore)
            {
                FurnitureBlueprintLog.InfoFull(
                    $"backfill seed={seedType} wiki {filledBefore}->{filledAfter} slots={FormatFilledSlots(scheme)}");
            }
        }

        private static void EnsureSeedHomeSlot(FurnitureScheme scheme, int seedType)
        {
            if (!FurnitureSlotClassifier.TryGetSlotFromType(seedType, out FurnitureSlotKind kind))
                return;

            kind = FurnitureWikiSlots.NormalizeClassified(kind);
            if (kind is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                return;

            if (scheme.GetSlot(kind) <= ItemID.None)
                scheme.SetSlot(kind, seedType);
        }

        private static HashSet<int> CollectCandidates(
            int seedType,
            FurnitureStyleSignature signature,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureSetConfidenceReport preview)
        {
            HashSet<int> raw;
            if (preview.PreferSeedCluster || preview.Tier == FurnitureSetConfidenceTier.Low)
            {
                raw = new HashSet<int>();
                FurnitureStyleClusterCatalog.ExpandFromSeed(seedType, signature, raw, materialBlock);
                if (materialBlock > ItemID.None && raw.Count < FurnitureSetConfidence.LowMaterialCandidateCap)
                {
                    HashSet<int> mat = FurnitureRecognitionCaches.GetOrCollectMaterialProducts(
                        seedType, materialBlock, blockSig);
                    foreach (int type in mat)
                    {
                        if (raw.Count >= FurnitureSetConfidence.LowMaterialCandidateCap)
                            break;
                        raw.Add(type);
                    }
                }

                FurnitureBlueprintLog.InfoFull(
                    $"candidates seed-cluster seed={seedType} tier={preview.Tier} style={blockSig.StyleKey} count={raw.Count}");
            }
            else if (materialBlock > ItemID.None)
            {
                raw = FurnitureRecognitionCaches.GetOrCollectMaterialProducts(seedType, materialBlock, blockSig);
                if (!FurnitureBlueprintScope.StrictMaterialOnly
                    && (FurnitureSetMaterialRules.UsesModLineageAnchor(seedType)
                        || FurnitureSetMaterialRules.UsesModSpecificMaterialBlock(seedType)
                        || raw.Count < FurnitureSetConfidence.LowMaterialCandidateCap))
                {
                    FurnitureStyleClusterCatalog.ExpandFromSeed(seedType, signature, raw, materialBlock);
                    FurnitureCandidateExpander.Expand(seedType, blockSig, materialBlock, raw);
                    FurnitureMaterialPlacementExpander.ExpandFromMaterialAndSeed(raw, seedType, materialBlock, blockSig);
                    FurnitureBlueprintLog.InfoFull(
                        $"candidates mod-expand seed={seedType} style={blockSig.StyleKey} count={raw.Count}");
                }
            }
            else
            {
                raw = new HashSet<int>();
                FurnitureStyleClusterCatalog.ExpandFromSeed(seedType, signature, raw, ItemID.None);
                FurnitureBlueprintLog.InfoFull(
                    $"candidates seed-only seed={seedType} style={blockSig.StyleKey} count={raw.Count}");
            }

            return FurnitureRecognizeCandidateCap.TrimIfNeeded(raw, seedType, materialBlock, blockSig);
        }

        public static int ResolveAnchorMaterialPublic(int seedType, FurnitureStyleSignature signature) =>
            ResolveAnchorMaterial(seedType, signature);

        private static void TryFillWallFromAnchor(FurnitureScheme scheme, int anchor, FurnitureStyleSignature signature)
        {
            if (anchor <= ItemID.None || scheme.GetSlot(FurnitureSlotKind.Wall) > ItemID.None)
                return;

            if (FurnitureWallResolver.TryResolveWallFromBlock(anchor, signature, out int wallItem))
                scheme.SetSlot(FurnitureSlotKind.Wall, wallItem);
        }

        private static int ResolveAnchorMaterial(int seedType, FurnitureStyleSignature signature) =>
            FurnitureReverseAnchorResolver.ResolveAnchorFromSeed(seedType, signature);

        private static int PickBestCandidate(
            List<int> candidates,
            int seedType,
            int anchorType,
            FurnitureStyleSignature signature)
        {
            int best = ItemID.None;
            int bestScore = int.MinValue;
            foreach (int type in candidates)
            {
                if (!CandidateMatchesSetStyle(type, signature, anchorType))
                    continue;

                int score = ScoreCandidate(type, seedType, anchorType, signature);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = type;
                }
            }
            return best;
        }

        private static bool CandidateMatchesSetStyle(int type, FurnitureStyleSignature signature, int anchorType)
        {
            if (type <= ItemID.None)
                return false;

            string key = ExtractStyleKeyPublic(type);
            bool styleExact = !string.IsNullOrEmpty(signature.StyleKey)
                && string.Equals(key, signature.StyleKey.Trim(), StringComparison.OrdinalIgnoreCase);
            bool styleFuzzy = !styleExact
                && FurnitureStyleSignature.StyleKeyFuzzyMatch(signature.StyleKey, key);
            bool styleFamily = styleExact || styleFuzzy
                || FurnitureMaterialKeyNormalizer.SameMaterialFamily(signature.StyleKey, key)
                || FurnitureStyleSignature.StyleKeySameMaterialFamily(signature.StyleKey, key);

            if (!string.IsNullOrWhiteSpace(signature.StyleKey) && !styleFamily)
                return false;

            if (signature.SeedIsMaterialBlock)
            {
                if (styleExact)
                    return true;
                return styleFuzzy;
            }

            if (anchorType > ItemID.None
                && (FurnitureRecipeSetLinker.ProductUsesExactMaterial(type, anchorType)
                    || FurnitureRecipeSetLinker.ProductBelongsToMaterialStyle(type, anchorType, signature)))
                return true;

            if (signature.UsesPlacementStyleLine && signature.PlacementTile >= TileID.Dirt)
            {
                Item probe = new Item();
                if (!FurnitureItemDefaults.TrySetDefaults(probe, type))
                    return styleFamily;

                if (probe.createTile == signature.PlacementTile)
                {
                    if (probe.placeStyle == signature.PlacementStyle && styleFamily)
                        return true;
                    if (signature.PlacementTile >= TileID.Count && styleFamily)
                        return true;
                }
            }

            return styleFamily;
        }

        private static int ScoreCandidate(int type, int seedType, int anchorType, FurnitureStyleSignature signature)
        {
            if (type == seedType)
                return 10_000;

            int score = 0;
            FurnitureStyleSignature other = FurnitureStyleSignature.FromItemType(type);

            string key = ExtractStyleKeyPublic(type);
            bool styleExact = !string.IsNullOrEmpty(signature.StyleKey)
                && string.Equals(key, signature.StyleKey.Trim(), StringComparison.OrdinalIgnoreCase);
            bool styleFuzzy = !styleExact
                && FurnitureStyleSignature.StyleKeyFuzzyMatch(signature.StyleKey, key);

            // Gemini ��һ�㣺placeStyle ������ڶ���? StyleKey ͬʱ��������������ľ�ι��� style=5��
            if (signature.UsesPlacementStyleLine && signature.PlacementTile >= TileID.Dirt
                && other.PlacementTile == signature.PlacementTile && other.PlacementStyle == signature.PlacementStyle
                && (styleExact || styleFuzzy))
                score += 900;

            if (!string.IsNullOrEmpty(signature.StyleKey)
                && !FurnitureStyleSignature.StyleKeySameMaterialFamily(signature.StyleKey, key))
                return int.MinValue / 4;

            if (styleExact)
                score += 600;
            else if (styleFuzzy)
                score += 280;

            if (signature.BelongsToSet(seedType, type, signature))
                score += 400;

            if (type == anchorType)
                score += 300;

            // Gemini �����㣺ê����ϣ���ȷ�����������䷽��?
            if (anchorType > ItemID.None)
            {
                FurnitureCraftStationProfile stations = FurnitureCraftStationProfile.FromSeed(seedType);
                foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(type))
                {
                    if (stations.IsConstrained && !stations.RecipeCompatible(recipe))
                        continue;

                    if (RecipeAnalyzer.RecipeUsesExactIngredient(recipe, anchorType))
                    {
                        score += 450 + stations.ScoreRecipeMatch(recipe);
                        break;
                    }
                    if (RecipeAnalyzer.RecipeUsesIngredient(recipe, anchorType))
                    {
                        score += 120 + stations.ScoreRecipeMatch(recipe);
                        break;
                    }
                }
            }

            ModItem mi = ItemLoader.GetItem(type);
            if (mi != null && mi.Mod.Name == signature.ModKey)
                score += 80;

            if (FurnitureSlotClassifier.TryGetSlotFromType(type, out FurnitureSlotKind slotKind))
            {
                score += FurnitureCandidateFilter.ScoreFootprintBonus(type, slotKind);
                Item lightProbe = new Item();
                lightProbe.SetDefaults(type);
                if (FurnitureCandidateFilter.IsLightSlot(slotKind)
                    && FurnitureCandidateFilter.ProvidesLight(lightProbe))
                    score += 120;
            }

            return score;
        }

        /// <summary>从显示名去掉槽位后缀，得到套组血统词（如「生命红木」「干木」）�?</summary>
        public static string ExtractDisplayLineageMoniker(int itemType)
        {
            if (itemType <= ItemID.None)
                return "";

            Item item = new Item();
            item.SetDefaults(itemType);
            string name = (item.Name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
                return ExtractStyleKeyPublic(itemType).Trim();

            foreach (string suffix in NameSuffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && name.Length > suffix.Length)
                    name = name.Substring(0, name.Length - suffix.Length).Trim();
            }

            return name;
        }

        public static string ExtractStyleKeyPublic(int itemType)
        {
            ModItem mi = ItemLoader.GetItem(itemType);
            string name = mi?.Name ?? ItemID.Search.GetName(itemType) ?? "";
            if (string.IsNullOrEmpty(name))
                return "";

            foreach (string suffix in NameSuffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && name.Length > suffix.Length)
                    return name.Substring(0, name.Length - suffix.Length);
            }
            return name.Trim();
        }

        private static string GetModKey(int itemType)
        {
            ModItem mi = ItemLoader.GetItem(itemType);
            return mi == null ? "Terraria" : mi.Mod.Name;
        }

        private static bool IsLikelyMaterial(Item item)
        {
            if (item == null || item.IsAir)
                return false;
            if (item.type < ItemID.Sets.IsAMaterial.Length && ItemID.Sets.IsAMaterial[item.type])
                return true;
            if (HubCollectibleRules.IsMaterial(item))
                return true;
            return FurnitureTileSafety.IsPhysicallySolidTile(item.createTile);
        }

        private static int CountFilled(FurnitureScheme scheme)
        {
            int n = 0;
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                if (scheme.SlotItemTypes[i] > ItemID.None)
                    n++;
            }
            return n;
        }

        private static string FormatSlotBuckets(Dictionary<FurnitureSlotKind, List<int>> perSlot)
        {
            var parts = new List<string>();
            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                if (perSlot.TryGetValue(kind, out List<int> list) && list.Count > 0)
                    parts.Add($"{kind}:{list.Count}");
            }
            return parts.Count == 0 ? "(empty)" : string.Join(",", parts);
        }

        private static string FormatFilledSlots(FurnitureScheme scheme)
        {
            return FurnitureSchemeSlotFormatter.FormatCompact(scheme);
        }

        private static int CountWikiFilled(FurnitureScheme scheme)
        {
            int n = 0;
            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                if (scheme.GetSlot(kind) > ItemID.None)
                    n++;
            }
            return n;
        }

        private static int CountWikiClassifyMiss(HashSet<int> candidates)
        {
            int miss = 0;
            foreach (int type in candidates)
            {
                if (!FurnitureRecognitionCaches.TryGetProbe(type, out Item probe))
                    continue;
                if (!FurnitureCandidateFilter.IsPlaceableFurnitureItem(probe))
                    continue;
                if (probe.createWall <= WallID.None && !FurnitureTileSafety.HasPlaceableTile(probe))
                    continue;
                if (probe.createWall > WallID.None && probe.createTile < TileID.Dirt)
                    continue;
                if (!FurnitureSlotClassifier.TryGetSlot(probe, out FurnitureSlotKind kind))
                {
                    miss++;
                    continue;
                }
                if (FurnitureWikiSlots.NormalizeClassified(kind) == FurnitureSlotKind.None)
                    miss++;
            }
            return miss;
        }

        private static string GetDefaultDisplayName(int seedType)
        {
            Item item = new Item();
            item.SetDefaults(seedType);
            return item.Name ?? ItemID.Search.GetName(seedType) ?? seedType.ToString();
        }
    }
}
