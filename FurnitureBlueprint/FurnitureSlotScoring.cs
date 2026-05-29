using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public static class FurnitureSlotScoring
    {
        public const int MinBucketPickScore = 3_200;
        public const int MinBathtubPickScoreRelaxed = 900;
        public const int MinBedPickScoreRelaxed = 900;
        public const int MinPlaceholderScore = 5_200;
        public const int MinBedBathPlaceholderScoreRelaxed = 2_200;
        public const int MaxPlaceholderCandidates = 48;

        public const int MinClassifyRecipeScore = 2_200;
        public const int ClassifyRecipeMargin = 600;

        public const int SeedExactBonus = 25_000;
        public const int MaterialLinkBase = 280;
        public const int StyleExactBonus = 1_800;
        public const int StyleFuzzyBonus = 720;
        public const int StyleFamilyBonus = 260;
        public const int MaterialRecipeBonus = 680;
        public const int FootprintPerfect = 5_500;
        public const int FootprintClose = 1_300;
        public const int RoomNeedsAlign = 850;
        public const int RegistryExact = 1_100;
        public const int ClassifyAlignBonus = 480;
        public const int SlotRulesBonus = 180;
        public const int SameModBonus = 50;
        public const int StationMatchCap = 200;
        public const int NameStrong = 2_800;
        public const int NameMedium = 1_600;
        public const int NameWeak = 750;
        public const int MaterialPartNameStrong = 3_400;

        public static int ScoreCandidate(
            int type,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            FurnitureRecognizeContext ctx = null)
        {
            if (ctx != null)
                return ctx.GetCandidateScore(type, slot, seedType, materialBlock, blockSig, stations);

            return ComputeCandidateScore(type, slot, seedType, materialBlock, blockSig, stations, null);
        }

        internal static int ComputeCandidateScore(
            int type,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureStyleSignature blockSig,
            FurnitureCraftStationProfile stations,
            FurnitureRecognizeContext ctx)
        {
            if (!IsScorableForSlot(type, slot, materialBlock))
                return 0;

            if (type == seedType)
                return SeedExactBonus;

            if (FurnitureNameSignals.IsDecorativeMark(type))
                return 0;

            if (slot == FurnitureSlotKind.Chair
                && !FurnitureNameSignals.MeetsChairPickEvidence(type, materialBlock, ctx))
                return 0;

            if (slot == FurnitureSlotKind.Bed
                && !FurnitureNameSignals.MeetsBedPickEvidence(type, materialBlock, seedType))
                return 0;

            if (slot == FurnitureSlotKind.Bathtub
                && !FurnitureNameSignals.MeetsBathtubPickEvidence(type, materialBlock, seedType))
                return 0;

            if (slot == FurnitureSlotKind.Workbench
                && !FurnitureNameSignals.MeetsWorkbenchPickEvidence(type, materialBlock, seedType))
                return 0;

            if (slot == FurnitureSlotKind.Sink
                && !FurnitureNameSignals.MeetsSinkPickEvidence(type, materialBlock, seedType))
                return 0;

            if (slot == FurnitureSlotKind.Table
                && !FurnitureNameSignals.MeetsTablePickEvidence(type, materialBlock, seedType))
                return 0;

            if (slot == FurnitureSlotKind.Candelabra
                && FurnitureSlotNameRules.PreferLampOverCandelabra(type))
                return 0;

            if (FurnitureBuildingBlockRules.MustNotOccupyWikiFurnitureSlot(type, slot))
                return 0;

            if (materialBlock > ItemID.None && !IsMaterialLinked(type, materialBlock, seedType, slot, blockSig))
                return 0;

            int score = MaterialLinkBase;

            string blockKey = blockSig.StyleKey?.Trim() ?? FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
            string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(type);

            bool styleExact = !string.IsNullOrEmpty(blockKey)
                && string.Equals(productKey, blockKey, System.StringComparison.OrdinalIgnoreCase);
            bool styleFuzzy = !styleExact && FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, productKey);
            bool styleFamily = styleExact || styleFuzzy
                || FurnitureMaterialKeyNormalizer.SameMaterialFamily(blockKey, productKey);

            if (styleExact)
                score += StyleExactBonus;
            else if (styleFuzzy)
                score += StyleFuzzyBonus;
            else if (styleFamily)
                score += StyleFamilyBonus;

            if (FurnitureRecipeSetLinker.ProductUsesExactMaterial(type, materialBlock))
                score += MaterialRecipeBonus;

            score += ScoreStationAlignment(type, materialBlock, stations);

            int geom = ScoreGeometry(type, slot);
            bool lacksNameEvidence = FurnitureRecipeSlotSignals.ScoreNameBonus(type, slot) <= 0
                && FurnitureNameSignals.ScoreMaterialPartName(type, materialBlock, slot, seedType) <= 0;
            if (lacksNameEvidence && geom >= FootprintPerfect
                && slot is FurnitureSlotKind.Bathtub or FurnitureSlotKind.Bed or FurnitureSlotKind.Sofa or FurnitureSlotKind.Dresser
                    or FurnitureSlotKind.Workbench or FurnitureSlotKind.Sink or FurnitureSlotKind.Table
                    or FurnitureSlotKind.Clock or FurnitureSlotKind.Door or FurnitureSlotKind.Chest)
                geom = FootprintClose;
            score += geom;

            score += FurnitureRecipeSlotSignals.ScoreNameBonus(type, slot);
            score += FurnitureNameSignals.ScoreMaterialPartName(type, materialBlock, slot, seedType);
            int lineage = FurnitureSetLineageScoring.ScoreSeedLineage(type, seedType, materialBlock);
            if (ctx != null && ctx.PlaceholderCommonWordBoost && lineage <= 0)
            {
                lineage += ScorePlaceholderPartialLineage(type, seedType, materialBlock);
            }
            if (lineage <= -FurnitureSetLineageScoring.MaterialOnlyPartial / 2
                && slot is not FurnitureSlotKind.Bathtub)
                return 0;
            score += lineage;
            if (ctx != null)
                score += FurnitureSetLineageScoring.ScoreCommonWords(type, ctx.CommonWords, ctx.PlaceholderCommonWordBoost);

            score += FurnitureSlotNameRules.ScoreSlotConflict(type, slot);

            if (ctx?.ConfidenceTier == FurnitureSetConfidenceTier.High && HasInternalSlotKeyword(type, slot))
                score += SlotRulesBonus * 2;

            score += ctx != null
                ? ctx.GetRecipeScore(type, slot, materialBlock)
                : FurnitureRecipeSlotSignals.ComputeRecipeScore(type, slot, materialBlock);

            if (ctx != null && ctx.TryGetClassification(type, out FurnitureSlotKind classified)
                && FurnitureWikiSlots.NormalizeClassified(classified) == slot)
                score += ClassifyAlignBonus;

            ModItem mi = ItemLoader.GetItem(type);
            if (mi != null && mi.Mod.Name == blockSig.ModKey)
                score += SameModBonus;

            return score;
        }

        /// <summary>占位回填池：expanded 候选必须能证明属于该槽，禁止「同材质 +  footprint」乱占格。</summary>
        public static bool IsEligiblePlaceholderCandidate(
            int type,
            FurnitureSlotKind slot,
            int materialBlock,
            int seedType,
            FurnitureRecognizeContext ctx)
        {
            if (type <= ItemID.None || type == seedType)
                return type == seedType;

            if (!PassesSlotPickGate(type, slot, seedType, materialBlock, ctx))
                return false;

            if (!IsScorableForSlot(type, slot, materialBlock))
                return false;

            return true;
        }

        /// <summary>种子若分类到 wiki 槽，finalize 时允许占用该槽（不被 occupied 拦截）。</summary>
        public static bool IsSeedHomeSlot(int type, FurnitureSlotKind slot, int seedType)
        {
            if (type != seedType || seedType <= ItemID.None)
                return false;

            if (!FurnitureSlotClassifier.TryGetSlotFromType(seedType, out FurnitureSlotKind seedSlot))
                return false;

            return FurnitureWikiSlots.NormalizeClassified(seedSlot) == slot;
        }

        /// <summary>按套组置信度分层：High 严格归属；Low 仅拦 keyword 冲突。</summary>
        public static bool PassesSlotPickGate(
            int type,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureRecognizeContext ctx)
        {
            if (FurnitureBuildingBlockRules.MustNotOccupyWikiFurnitureSlot(type, slot))
                return false;

            FurnitureSetConfidenceTier tier = ctx?.ConfidenceTier ?? FurnitureSetConfidenceTier.Medium;
            switch (tier)
            {
                case FurnitureSetConfidenceTier.High:
                    return CandidateBelongsInSlot(type, slot, seedType, materialBlock, ctx);

                case FurnitureSetConfidenceTier.Low:
                    return !ConflictsInternalSlotKeyword(type, slot);

                default:
                    if (CandidateBelongsInSlot(type, slot, seedType, materialBlock, ctx))
                        return true;

                    if (ConflictsInternalSlotKeyword(type, slot))
                        return false;

                    if (FurnitureRecipeSlotSignals.ScoreNameBonus(type, slot) >= NameWeak)
                        return true;

                    if (ctx != null
                        && ctx.TryGetClassification(type, out FurnitureSlotKind classified)
                        && FurnitureWikiSlots.NormalizeClassified(classified) == slot)
                        return true;

                    return false;
            }
        }

        /// <summary>选优/占位：internal 名或分类必须与目标槽一致，避免 MarbleDoor→Chair 等 scramble。</summary>
        public static bool CandidateBelongsInSlot(
            int type,
            FurnitureSlotKind slot,
            int seedType,
            int materialBlock,
            FurnitureRecognizeContext ctx)
        {
            if (type <= ItemID.None)
                return false;

            if (type == seedType && FurnitureSlotClassifier.TryGetSlotFromType(seedType, out FurnitureSlotKind seedSlot)
                && FurnitureWikiSlots.NormalizeClassified(seedSlot) == slot)
                return true;

            if (FurnitureStylePrefixCatalog.RequiresStyleGate(seedType, materialBlock)
                && type != seedType
                && !FurnitureStylePrefixCatalog.ProductMatchesSeedStyle(type, seedType, materialBlock))
                return false;

            int nameBonus = FurnitureRecipeSlotSignals.ScoreNameBonus(type, slot);
            if (nameBonus >= NameMedium)
                return true;

            if (HasInternalSlotKeyword(type, slot))
                return true;

            if (ConflictsInternalSlotKeyword(type, slot))
                return false;

            if (ctx != null && ctx.TryGetClassification(type, out FurnitureSlotKind classified)
                && FurnitureWikiSlots.NormalizeClassified(classified) == slot)
                return true;

            if (FurnitureSlotClassifier.TryGetSlotFromType(type, out FurnitureSlotKind tileClass)
                && FurnitureWikiSlots.NormalizeClassified(tileClass) == slot)
                return nameBonus > 0 || tileClass == slot;

            if (TryInferClassifySlot(type, out FurnitureSlotKind inferred, out int score)
                && FurnitureWikiSlots.NormalizeClassified(inferred) == slot
                && score >= MinClassifyRecipeScore)
                return true;

            return slot switch
            {
                FurnitureSlotKind.Chair => FurnitureNameSignals.MeetsChairPickEvidence(type, materialBlock, ctx),
                FurnitureSlotKind.Bed => FurnitureNameSignals.MeetsBedPickEvidence(type, materialBlock, seedType),
                FurnitureSlotKind.Bathtub => FurnitureNameSignals.MeetsBathtubPickEvidence(type, materialBlock, seedType),
                FurnitureSlotKind.Workbench => FurnitureNameSignals.MeetsWorkbenchPickEvidence(type, materialBlock, seedType),
                FurnitureSlotKind.Sink => FurnitureNameSignals.MeetsSinkPickEvidence(type, materialBlock, seedType),
                FurnitureSlotKind.Table => FurnitureNameSignals.MeetsTablePickEvidence(type, materialBlock, seedType),
                _ => nameBonus > 0
            };
        }

        public static bool HasInternalSlotSuffix(int type, FurnitureSlotKind slot) =>
            HasInternalSlotKeyword(type, slot);

        private static bool HasInternalSlotKeyword(int type, FurnitureSlotKind slot)
        {
            string internalName = GetInternalName(type);
            if (string.IsNullOrEmpty(internalName))
                return false;

            return slot switch
            {
                FurnitureSlotKind.Chair => internalName.Contains("chair") && !internalName.Contains("work"),
                FurnitureSlotKind.Door => internalName.Contains("door"),
                FurnitureSlotKind.Dresser => internalName.Contains("dresser"),
                FurnitureSlotKind.Sofa => internalName.Contains("sofa"),
                FurnitureSlotKind.Lamp => internalName.Contains("lamp") && !internalName.Contains("candelabra"),
                FurnitureSlotKind.Lantern => internalName.Contains("lantern"),
                FurnitureSlotKind.Chest => internalName.Contains("chest"),
                FurnitureSlotKind.Clock => internalName.Contains("clock"),
                FurnitureSlotKind.Sink => internalName.Contains("sink"),
                FurnitureSlotKind.Bathtub => internalName.Contains("bathtub"),
                FurnitureSlotKind.Workbench => internalName.Contains("workbench") || internalName.Contains("workbench"),
                FurnitureSlotKind.Table => internalName.Contains("table") && !internalName.Contains("work"),
                FurnitureSlotKind.Bed => internalName.Contains("bed"),
                FurnitureSlotKind.Candle => internalName.Contains("candle") && !internalName.Contains("candelabra"),
                FurnitureSlotKind.Candelabra => internalName.Contains("candelabra"),
                FurnitureSlotKind.Chandelier => internalName.Contains("chandelier"),
                FurnitureSlotKind.Piano => internalName.Contains("piano") || internalName.Contains("synth"),
                FurnitureSlotKind.Toilet => internalName.Contains("toilet"),
                FurnitureSlotKind.Bookcase => internalName.Contains("bookcase") || internalName.Contains("bookshelf"),
                _ => false
            };
        }

        private static bool ConflictsInternalSlotKeyword(int type, FurnitureSlotKind slot)
        {
            foreach (FurnitureSlotKind other in FurnitureWikiSlots.RecognitionOrder)
            {
                if (other == slot
                    || other is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    continue;

                if (HasInternalSlotKeyword(type, other))
                    return true;
            }

            return false;
        }

        private static string GetInternalName(int type) =>
            FurnitureSchemeSlotFormatter.GetInternalEnglishName(type).ToLowerInvariant();

        public static bool IsScorableForSlot(int type, FurnitureSlotKind slot, int materialBlock)
        {
            if (type <= ItemID.None)
                return false;

            if (!FurnitureRecognitionCaches.IsPlaceableFurniture(type))
                return false;

            if (!FurnitureRecognitionCaches.TryGetProbe(type, out Item item))
                return false;

            if (materialBlock > ItemID.None && type == materialBlock)
                return false;

            if (slot is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                return true;

            if (FurnitureBuildingBlockRules.IsPlainMaterialBrick(item))
                return false;

            if (FurnitureNameSignals.IsDecorativeMark(type))
                return false;

            return item.createTile >= TileID.Dirt || item.createWall > WallID.None;
        }

        public static bool TryInferClassifySlot(int productType, out FurnitureSlotKind kind, out int score)
        {
            kind = FurnitureSlotKind.None;
            score = 0;
            if (productType <= ItemID.None || !IsScorableForWikiPiece(productType))
                return false;

            int second = 0;

            foreach (FurnitureSlotKind slot in FurnitureWikiSlots.RecognitionOrder)
            {
                if (slot is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    continue;

                int s = ScoreClassifyOnly(productType, slot);
                if (s <= score)
                {
                    if (s > second && s < score)
                        second = s;
                    continue;
                }

                if (score > second)
                    second = score;
                score = s;
                kind = slot;
            }

            if (score < MinClassifyRecipeScore || score - second < ClassifyRecipeMargin)
            {
                kind = FurnitureSlotKind.None;
                return false;
            }

            return true;
        }

        public static int ScoreClassifyOnly(int type, FurnitureSlotKind slot)
        {
            if (!IsScorableForSlot(type, slot, ItemID.None))
                return 0;

            int score = FurnitureRecipeSlotSignals.ScoreNameBonus(type, slot);
            score += FurnitureRecipeSlotSignals.ComputeRecipeScore(type, slot, ItemID.None);
            score += ScoreGeometry(type, slot);

            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, type))
                return score;

            int tile = item.createTile;
            int style = item.placeStyle;
            if (!FurnitureTileSafety.IsValidTileId(tile) || tile < TileID.Dirt)
                return score;

            if (FurnitureTileSlotRegistry.TryGetSlotExact(tile, style, out FurnitureSlotKind reg)
                && FurnitureWikiSlots.NormalizeClassified(reg) == slot)
                score += RegistryExact;

            if (FurnitureSlotClassifier.TryClassifyByRoomNeedsPublic(tile, style, out FurnitureSlotKind rn)
                && FurnitureWikiSlots.NormalizeClassified(rn) == slot)
                score += RoomNeedsAlign / 2;

            return score;
        }

        public static int ScoreGeometry(int type, FurnitureSlotKind slot)
        {
            int fp = FurnitureCandidateFilter.ScoreFootprintBonus(type, slot);
            if (fp >= FootprintPerfect)
                return FootprintPerfect;
            if (fp >= FootprintClose)
                return FootprintClose;

            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, type))
                return 0;
            if (!FurnitureTileSafety.IsValidTileId(item.createTile) || item.createTile < TileID.Dirt)
                return 0;

            if (FurnitureSlotClassifier.TryClassifyByRoomNeedsPublic(item.createTile, item.placeStyle, out FurnitureSlotKind rn)
                && FurnitureWikiSlots.NormalizeClassified(rn) == slot)
                return RoomNeedsAlign;

            return 0;
        }

        private static bool IsScorableForWikiPiece(int type)
        {
            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, type))
                return false;
            if (!FurnitureCandidateFilter.IsPlaceableFurnitureItem(item))
                return false;
            if (FurnitureMaterialAnchor.IsValidAnchorBlock(item))
                return false;
            return true;
        }

        private static int ScoreStationAlignment(int type, int materialBlock, FurnitureCraftStationProfile stations)
        {
            if (!stations.IsConstrained || materialBlock <= ItemID.None)
                return 0;

            int total = 0;
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(type))
            {
                if (!RecipeAnalyzer.RecipeUsesIngredient(recipe, materialBlock))
                    continue;
                total += stations.ScoreRecipeMatch(recipe);
                if (total >= StationMatchCap)
                    return StationMatchCap;
            }

            return total;
        }

        private static bool IsMaterialLinked(
            int type,
            int materialBlock,
            int seedType,
            FurnitureSlotKind slot,
            FurnitureStyleSignature blockSig)
        {
            if (type == seedType)
                return true;

            string blockKey = blockSig.StyleKey?.Trim() ?? FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
            string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(type);
            if (FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, productKey)
                || FurnitureMaterialKeyNormalizer.StyleKeysMatch(blockKey, productKey)
                || FurnitureMaterialKeyNormalizer.SameMaterialFamily(blockKey, productKey))
                return true;

            if (materialBlock > ItemID.None && FurnitureRecipeSetLinker.ProductUsesExactMaterial(type, materialBlock))
                return true;

            if (materialBlock > ItemID.None && !RecipeAnalyzer.IsHighFanoutMaterial(materialBlock)
                && FurnitureRecipeSetLinker.ProductUsesMaterial(type, materialBlock))
                return true;

            if (blockSig.UsesPlacementStyleLine && blockSig.PlacementTile >= TileID.Dirt)
            {
                Item probe = new Item();
                probe.SetDefaults(type);
                if (probe.createTile == blockSig.PlacementTile)
                    return true;
            }

            if (slot == FurnitureSlotKind.Bathtub
                && FurnitureBathtubRules.SharesSetWithMaterial(type, materialBlock, seedType))
                return true;

            if (slot == FurnitureSlotKind.Bed
                && materialBlock > ItemID.None
                && FurnitureRecipeSetLinker.ProductUsesExactMaterial(type, materialBlock))
            {
                if (RecipeAnalyzer.IsHighFanoutMaterial(materialBlock)
                    && PassesModLineageWoodProductLink(type, seedType, materialBlock, blockKey, productKey))
                    return true;

                if (FurnitureRecipeSlotSignals.ComputeRecipeScore(type, slot, materialBlock) >= 300
                    || ItemUsesBedTile(type))
                    return true;
            }

            return slot == FurnitureSlotKind.Chest
                && FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, productKey);
        }

        private static bool ItemUsesBedTile(int type)
        {
            if (!FurnitureRecognitionCaches.TryGetProbe(type, out Item item))
                return false;
            return item.createTile == TileID.Beds;
        }

        private static int ScorePlaceholderPartialLineage(int type, int seedType, int materialBlock)
        {
            if (!FurnitureGenericWoodLineageRules.ShouldBoostPlaceholderCommonWords(seedType, materialBlock))
                return 0;

            Item prod = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(prod, type))
                return 0;

            string prodLower = (prod.Name ?? "").ToLowerInvariant();
            string moniker = FurnitureSetLineageScoring.ExtractSeedLineageMoniker(seedType).ToLowerInvariant();
            if (moniker.Length >= 2 && prodLower.Contains(moniker))
                return FurnitureGenericWoodLineageRules.PlaceholderPartialLineageBoost;

            foreach (string token in FurnitureSetLineageScoring.BuildCommonWords(
                         seedType, null, materialBlock, null, relaxedOccurrence: true))
            {
                if (string.IsNullOrWhiteSpace(token) || token.Length < 2)
                    continue;
                if (prodLower.Contains(token.Trim().ToLowerInvariant()))
                    return FurnitureGenericWoodLineageRules.PlaceholderPartialLineageBoost;
            }

            return 0;
        }

        public static int GetMinPickScore(FurnitureSlotKind slot, int seedType = ItemID.None, int materialBlock = ItemID.None) =>
            slot switch
            {
                FurnitureSlotKind.Bathtub when CanUseRelaxedPickThreshold(seedType, materialBlock) =>
                    MinBathtubPickScoreRelaxed,
                FurnitureSlotKind.Bed when CanUseRelaxedPickThreshold(seedType, materialBlock) =>
                    MinBedPickScoreRelaxed,
                _ => MinBucketPickScore
            };

        public static int GetMinPlaceholderScore(
            FurnitureSlotKind slot,
            int seedType = ItemID.None,
            int materialBlock = ItemID.None)
        {
            if (slot is not (FurnitureSlotKind.Bathtub or FurnitureSlotKind.Bed))
                return MinPlaceholderScore;

            if (CanUseRelaxedPickThreshold(seedType, materialBlock))
                return MinBedPickScoreRelaxed;

            if (CanUseRelaxedBedBathPlaceholder(seedType, materialBlock))
                return MinBedBathPlaceholderScoreRelaxed;

            return MinPlaceholderScore;
        }

        private static bool CanUseRelaxedBedBathPlaceholder(int seedType, int materialBlock)
        {
            if (seedType <= ItemID.None)
                return false;

            if (FurnitureSlotClassifier.TryGetSlotFromType(seedType, out FurnitureSlotKind seedSlot)
                && FurnitureWikiSlots.NormalizeClassified(seedSlot) == FurnitureSlotKind.Bed)
                return false;

            if (FurnitureGenericWoodLineageRules.IsWeakLineageSeed(seedType))
                return true;

            ModItem seedMod = ItemLoader.GetItem(seedType);
            if (seedMod == null || seedMod.Mod.Name == "Terraria")
                return false;

            if (materialBlock <= ItemID.None)
                return true;

            ModItem matMod = ItemLoader.GetItem(materialBlock);
            return matMod != null && seedMod.Mod.Name == matMod.Mod.Name;
        }

        private static bool CanUseRelaxedPickThreshold(int seedType, int materialBlock) =>
            seedType > ItemID.None
            && materialBlock > ItemID.None
            && FurnitureGenericWoodLineageRules.IsMaterialAlignedWithSeedLineage(seedType, materialBlock);

        /// <summary>高扇出材料（Wood 等）+ 血统套组：仅凭「消耗该材料」不足以视为同套。</summary>
        internal static bool PassesModLineageWoodProductLink(
            int productType,
            int seedType,
            int materialBlock,
            string blockKey,
            string productKey)
        {
            int lineage = FurnitureSetLineageScoring.ScoreSeedLineage(productType, seedType, materialBlock);
            if (lineage >= FurnitureSetLineageScoring.LineageStrong)
                return true;
            if (lineage < 0)
                return false;

            if (FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, productKey)
                || FurnitureMaterialKeyNormalizer.StyleKeysMatch(blockKey, productKey))
                return true;

            if (seedType > ItemID.None && FurnitureSetMaterialRules.UsesModLineageAnchor(seedType))
            {
                ModItem seedMod = ItemLoader.GetItem(seedType);
                ModItem prodMod = ItemLoader.GetItem(productType);
                if (seedMod != null && prodMod != null
                    && seedMod.Mod.Name != "Terraria"
                    && prodMod.Mod.Name == "Terraria")
                    return false;

                if (seedMod != null && prodMod != null
                    && seedMod.Mod.Name != "Terraria"
                    && seedMod.Mod.Name == prodMod.Mod.Name)
                {
                    if (FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey, productKey)
                        || FurnitureMaterialKeyNormalizer.StyleKeysMatch(blockKey, productKey))
                        return true;

                    return lineage >= FurnitureSetLineageScoring.LineageStrong / 2;
                }
            }

            return lineage > 0;
        }
    }
}
