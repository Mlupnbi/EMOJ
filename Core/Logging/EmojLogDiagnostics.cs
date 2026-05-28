using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.FurnitureBlueprint;
using EvenMoreOverpoweredJourney.Buffs.Systems.Catalog;
using EvenMoreOverpoweredJourney.Buffs.Systems.Virtual;
using EvenMoreOverpoweredJourney.Buffs.Systems.Managed;
using EvenMoreOverpoweredJourney.Buffs.Systems.Combat;
using EvenMoreOverpoweredJourney.Buffs.Systems.Spawning;
using EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus;
using EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport;
using EvenMoreOverpoweredJourney.Buffs.Systems.FedState;
using EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics;
using EvenMoreOverpoweredJourney.Buffs.Systems.Display;

namespace EvenMoreOverpoweredJourney.Core.Logging
{
    /// <summary>????????????????????????? <see cref="EmojLog.IsFullMode"/>????</summary>
    public static class EmojLogDiagnostics
    {
        public static void ResetBuffTrace() { }

        public static void LogBuffPlayerSnapshot(Player player, BuffResearchPlayer mp, string reason)
        {
            if (!EmojLog.IsFullMode || player == null || mp == null)
                return;

            EmojLog.InfoFull(EmojLogChannel.Buff,
                $"{reason} | active={mp.ActiveBuffs.Count} disabled={mp.DisabledBuffs.Count} pinned={mp.PinnedPhysicalBuffs.Count} " +
                $"virtualQ={mp.VirtualEffectQueue.Count} stat={mp.VirtualStatQueueCount} combat={mp.VirtualCombatVisualQueueCount} " +
                $"scratch={OPJourneyConfig.UseVirtualScratchApply()} aggregate={mp.UseAggregatedVirtualBarDisplay()} " +
                $"maxMinions={player.maxMinions} slotsMinions={player.slotsMinions:F2}");
        }

        public static void LogBuffToggle(string action, int buffId, BuffResearchPlayer mp)
        {
            if (!EmojLog.IsActive || mp == null)
                return;

            bool virtualized = BuffVirtualEffectSystem.UsesVirtualEffect(buffId, mp);
            bool setBonus = BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId);
            EmojLog.Info(EmojLogChannel.Buff,
                $"{action} buff={buffId} virtual={virtualized} setBonus={setBonus} pinned={mp.PinnedPhysicalBuffs.Contains(buffId)}");
        }

        public static void LogBulkToggle(string category, bool enable, int enabled, int skipped, int skippedSetBonus)
        {
            if (!EmojLog.IsActive)
                return;

            EmojLog.Info(EmojLogChannel.Buff,
                $"BulkToggle category={category} enable={enable} enabled={enabled} skipped={skipped} skippedSetBonus={skippedSetBonus}");

            if (EmojLog.IsFullMode && Main.LocalPlayer != null)
                LogBuffPlayerSnapshot(Main.LocalPlayer, Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>(), "after-bulk");
        }

        /// <summary>
        /// Record a furniture candidate diagnostic snapshot. Only written in Full log mode.
        /// Fields collected are compact and stable for later analysis.
        /// </summary>
        public static void LogFurnitureCandidateSnapshot(int seedType, Item probe,
            bool isPlaceable, bool classified, bool roomNeedsMatched, bool geometryMatched, bool nameMatched,
            bool tileDataPresent, int tileWidth, int tileHeight, int placeStyle, string productMod, string rejectReason,
            string tierHint = null, FurnitureSlotKind slot = FurnitureSlotKind.None)
        {
            if (!EmojLog.IsFullMode || probe == null)
                return;

            string tierPart = string.IsNullOrEmpty(tierHint) ? "" : $" tier={tierHint}";
            string slotPart = slot == FurnitureSlotKind.None ? "" : $" slot={slot}";

            EmojLog.InfoFull(EmojLogChannel.Blueprint,
                $"furn-cand seed={seedType} probeType={probe.type} name={probe.Name} createTile={probe.createTile} placeStyle={placeStyle}{slotPart}{tierPart} " +
                $"placeable={isPlaceable} classified={classified} roomNeeds={roomNeedsMatched} geom={geometryMatched} nameMatch={nameMatched} " +
                $"tileDataPresent={tileDataPresent} tileW={tileWidth} tileH={tileHeight} productMod={productMod ?? "vanilla"} reject={rejectReason}");
        }

        public static void LogFurnitureClassified(int seedType, Item probe, FurnitureSlotKind slot, string tier,
            int tileWidth, int tileHeight)
        {
            if (!EmojLog.IsFullMode || probe == null)
                return;

            EmojLog.InfoFull(EmojLogChannel.Blueprint,
                $"furn-classify seed={seedType} type={probe.type} name={probe.Name} tile={probe.createTile} style={probe.placeStyle} " +
                $"slot={slot} tier={tier} tileW={tileWidth} tileH={tileHeight}");
        }

        public static void LogFurnitureBucketItems(int seedType, Dictionary<FurnitureSlotKind, List<int>> perSlot)
        {
            if (!EmojLog.IsFullMode || perSlot == null)
                return;

            const int maxTypesInLog = 24;
            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                if (!perSlot.TryGetValue(kind, out List<int> list) || list == null || list.Count == 0)
                    continue;

                string types = FormatIdList(list, maxTypesInLog);
                EmojLog.InfoFull(EmojLogChannel.Blueprint,
                    $"bucket-items seed={seedType} slot={kind} count={list.Count} types=[{types}]");
            }
        }

        public static void LogSlotPickScores(int seedType, FurnitureSlotKind slot, int materialBlock, int choice,
            IEnumerable<(int type, int score)> scores)
        {
            if (!EmojLog.IsFullMode || scores == null)
                return;

            const int maxRows = 20;
            var parts = new List<string>();
            bool truncated = false;
            foreach ((int type, int score) row in scores)
            {
                if (parts.Count >= maxRows)
                {
                    truncated = true;
                    break;
                }

                parts.Add($"{row.type}:{row.score}");
            }

            string tail = truncated ? ",..." : "";
            EmojLog.InfoFull(EmojLogChannel.Blueprint,
                $"pick-scores seed={seedType} slot={slot} material={materialBlock} choice={choice} scores=[{string.Join(",", parts)}{tail}]");
        }

        private static string FormatIdList(List<int> list, int max)
        {
            if (list == null || list.Count == 0)
                return "";

            if (list.Count <= max)
                return string.Join(",", list);

            var slice = new List<string>(max);
            for (int i = 0; i < max; i++)
                slice.Add(list[i].ToString());
            return string.Join(",", slice) + ",...";
        }

        public static void LogSlotResolved(int seedType, FurnitureSlotKind slot, int pick, int poolSize, string source)
        {
            if (!EmojLog.IsFullMode)
                return;

            EmojLog.InfoFull(EmojLogChannel.Blueprint,
                $"slot-resolve seed={seedType} slot={slot} pick={pick} pool={poolSize} source={source}");
        }
    }
}
