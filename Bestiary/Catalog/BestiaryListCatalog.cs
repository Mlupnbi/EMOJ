using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Bestiary.UI;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    public sealed class BestiaryListCatalog : ModSystem
    {
        public static bool Ready { get; private set; }

        /// <summary>与 <see cref="Main.BestiaryDB.Entries"/> 对齐的条目数（原版图鉴总数）。</summary>
        public static int BestiaryDbEntryCount { get; private set; }

        /// <summary>编入超级图鉴目录的条数（隐藏 NPC 除外）。</summary>
        public static int EntryCount { get; private set; }

        public static int SkippedHiddenCount { get; private set; }
        public static int SupplementaryUnregisteredCount { get; private set; }

        public static IReadOnlyList<BestiaryNpcMeta> All => _all;
        public static IReadOnlyDictionary<int, BestiaryNpcMeta> ByNetId => _byNetId;

        public static bool TryFindMetaByEntry(BestiaryEntry entry, out BestiaryNpcMeta meta)
        {
            meta = null;
            if (entry == null)
                return false;

            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].Entry == entry)
                {
                    meta = _all[i];
                    return true;
                }
            }

            return false;
        }

        private static readonly List<BestiaryNpcMeta> _all = new List<BestiaryNpcMeta>();
        private static readonly Dictionary<int, BestiaryNpcMeta> _byNetId = new Dictionary<int, BestiaryNpcMeta>();
        private static readonly HashSet<int> _cataloguedNetIds = new HashSet<int>();

        public override void PostSetupContent() => EnsureFresh();

        public override void OnWorldLoad() => EnsureFresh();

        /// <summary>所有模组 PostSetupContent 之后再构建，避免条数停在 ~884。</summary>
        public static void EnsureFresh()
        {
            int dbCount = Main.BestiaryDB?.Entries?.Count ?? 0;
            if (!Ready || BestiaryDbEntryCount != dbCount || EntryCount < dbCount)
            {
                Rebuild();
                return;
            }

            if (_all.Count > 1)
                _all.Sort(CompareMeta);
        }

        public static void Rebuild()
        {
            long ms = BestiaryPerfLog.Measure(RebuildCore);
            BestiaryPerfLog.LogElapsed("catalog-rebuild", ms, EntryCount);
        }

        private static void RebuildCore()
        {
            _all.Clear();
            _byNetId.Clear();
            _cataloguedNetIds.Clear();
            Ready = false;
            SkippedHiddenCount = 0;
            SupplementaryUnregisteredCount = 0;

            if (Main.dedServ || Main.BestiaryDB?.Entries == null)
                return;

            IReadOnlyList<BestiaryEntry> entries = Main.BestiaryDB.Entries;
            BestiaryDbEntryCount = entries.Count;

            for (int i = 0; i < entries.Count; i++)
            {
                BestiaryEntry entry = entries[i];
                if (entry == null)
                    continue;

                if (!BestiaryEntryResolver.TryGetNpcNetId(entry, out int netId))
                    netId = 0;

                if (!TryBuildMeta(i, netId, entry, hasEntry: true, out BestiaryNpcMeta meta))
                    continue;

                if (netId > 0 && IsHiddenFromBestiary(netId))
                {
                    meta.HiddenByNpcDrawModifier = true;
                    SkippedHiddenCount++;
                }

                _all.Add(meta);
                if (netId > 0)
                {
                    _cataloguedNetIds.Add(netId);
                    _byNetId[netId] = meta;
                }
            }

            for (int netId = 1; netId < NPCLoader.NPCCount; netId++)
            {
                if (_cataloguedNetIds.Contains(netId) || !IsSafeNpcNetId(netId) || IsHiddenFromBestiary(netId))
                    continue;

                if (!TryBuildMeta(-1, netId, entry: null, hasEntry: false, out BestiaryNpcMeta meta))
                    continue;

                meta.Band = BestiaryNpcBand.Unregistered;
                _all.Add(meta);
                _byNetId[netId] = meta;
                SupplementaryUnregisteredCount++;
            }

            _all.Sort(CompareMeta);
            EntryCount = _all.Count;
            Ready = true;
            EmojLog.Info(EmojLogChannel.Core,
                $"BestiaryListCatalog db={BestiaryDbEntryCount} catalog={EntryCount} hidden={SkippedHiddenCount} unregistered+={SupplementaryUnregisteredCount}");
        }

        private static bool IsSafeNpcNetId(int netId)
        {
            if (netId <= 0 || netId >= NPCLoader.NPCCount)
                return false;

            return NPCLoader.GetNPC(netId) != null;
        }

        private static bool TryProbeNpc(int netId, out NPC npc)
        {
            npc = null;
            if (!IsSafeNpcNetId(netId))
                return false;

            try
            {
                npc = new NPC();
                npc.SetDefaults(netId);
                return true;
            }
            catch (Exception ex)
            {
                EmojLog.Warn(EmojLogChannel.Core, $"BestiaryListCatalog skip netId={netId} SetDefaults: {ex.Message}");
                return false;
            }
        }

        private static int CompareMeta(BestiaryNpcMeta a, BestiaryNpcMeta b)
        {
            int byDisplay = BestiaryNpcMetaSort.Compare(a, b);
            if (byDisplay != 0)
                return byDisplay;

            int byCatalog = a.CatalogIndex.CompareTo(b.CatalogIndex);
            if (byCatalog != 0)
                return byCatalog;

            int byNet = a.NetId.CompareTo(b.NetId);
            if (byNet != 0)
                return byNet;

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHiddenFromBestiary(int netId)
        {
            if (!IsSafeNpcNetId(netId))
                return false;

            if (NPCID.Sets.NPCBestiaryDrawOffset.TryGetValue(netId, out NPCID.Sets.NPCBestiaryDrawModifiers modifiers))
                return modifiers.Hide;

            return false;
        }

        private static bool TryBuildMeta(int catalogIndex, int netId, BestiaryEntry entry, bool hasEntry, out BestiaryNpcMeta meta)
        {
            meta = null;

            if (netId <= 0 && entry != null)
                BestiaryEntryResolver.TryGetNpcNetId(entry, out netId);

            string modKey;
            string displayName = BestiaryEntryResolver.ResolveDisplayName(entry, netId, catalogIndex);
            if (netId > 0 && IsSafeNpcNetId(netId))
            {
                Mod npcMod = NPCLoader.GetNPC(netId)?.Mod;
                modKey = npcMod != null ? npcMod.Name : "Terraria";
            }
            else
            {
                modKey = "Terraria";
            }

            BestiaryDisplayIndexResolver.Result display = BestiaryDisplayIndexResolver.Resolve(entry, catalogIndex, netId);
            meta = new BestiaryNpcMeta
            {
                CatalogIndex = catalogIndex,
                BestiarySortIndex = display.SortIndex,
                BestiaryDisplayIndex = display.LabelIndex,
                HasBestiaryDisplayLabel = display.HasLabel,
                NetId = netId,
                StableKey = catalogIndex >= 0
                    ? BestiaryStableKey.ToEntryKey(catalogIndex, netId)
                    : BestiaryStableKey.ToKey(netId),
                ModKey = modKey,
                DisplayName = displayName,
                HasBestiaryEntry = hasEntry,
                Entry = entry,
                Band = netId > 0 ? ClassifyBand(netId, entry) : BestiaryNpcBand.Other,
                IsEventEnemy = ClassifyEventEnemy(entry)
            };

            if (meta.IsEventEnemy && meta.Band != BestiaryNpcBand.TownNpc && meta.Band != BestiaryNpcBand.Critter)
                meta.Band = BestiaryNpcBand.Event;

            if (netId > 0)
            {
                BestiaryPortraitMetrics.GetPortraitDimensions(netId, out int pw, out int ph);
                meta.PortraitWidth = pw;
                meta.PortraitHeight = ph;
            }

            return true;
        }

        private static string EOPJTextFallbackEntry(int catalogIndex) =>
            $"#{catalogIndex}";

        private static BestiaryNpcBand ClassifyBand(int netId, BestiaryEntry entry)
        {
            if (NPCID.Sets.TownNPCBestiaryPriority.Contains(netId))
                return BestiaryNpcBand.TownNpc;

            if (InBoolSet(NPCID.Sets.CountsAsCritter, netId) ||
                NPCID.Sets.NormalGoldCritterBestiaryPriority.Contains(netId))
                return BestiaryNpcBand.Critter;

            if (InBoolSet(NPCID.Sets.ShouldBeCountedAsBoss, netId))
                return BestiaryNpcBand.Boss;

            if (NPCID.Sets.BossBestiaryPriority.Contains(netId))
                return BestiaryNpcBand.MiniBoss;

            if (TryProbeNpc(netId, out NPC probe))
            {
                if (probe.townNPC)
                    return BestiaryNpcBand.TownNpc;

                if (entry != null && IsLikelyCombatEnemy(probe))
                    return BestiaryNpcBand.NormalEnemy;
            }

            return BestiaryNpcBand.Other;
        }

        private static bool InBoolSet(bool[] set, int netId) =>
            set != null && netId >= 0 && netId < set.Length && set[netId];

        private static bool IsLikelyCombatEnemy(NPC npc) =>
            npc.lifeMax > 0 && npc.damage > 0 && !npc.friendly && !npc.townNPC;

        private static bool ClassifyEventEnemy(BestiaryEntry entry)
        {
            if (entry?.Info == null)
                return false;

            foreach (IBestiaryInfoElement el in entry.Info)
            {
                string typeName = el.GetType().Name;
                if (typeName.Contains("Event", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Invasion", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Moon", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Army", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
