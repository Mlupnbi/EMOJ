using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using EvenMoreOverpoweredJourney.Research;
using EvenMoreOverpoweredJourney.Research.Crafting;

namespace EvenMoreOverpoweredJourney.Research.Players
{
    /// <summary>
    /// 制作环境：SeenTiles（见过）+ ResearchedTiles/Flags（研究解锁）+ 液体/群系（ImproveGame / UpgradeResearch 合并语义）。
    /// </summary>
    public class ResearchCraftingPlayer : ModPlayer
    {
        internal static bool[] SeenTiles;
        internal static bool[] ResearchedTiles;
        internal static CraftEnvironmentFlags SeenEnvironment;
        internal static CraftEnvironmentFlags ResearchedEnvironment;

        private bool[] _seenTiles;
        private bool[] _researchedTiles;
        private CraftEnvironmentFlags _seenEnvironment;
        private CraftEnvironmentFlags _researchedEnvironment;
        private bool _encounteredShimmer;

        public bool EncounteredShimmer => _encounteredShimmer;

        public static bool HasEncounteredShimmer =>
            Main.netMode == NetmodeID.Server || (Local?.EncounteredShimmer ?? false);

        private static ResearchCraftingPlayer Local =>
            Main.LocalPlayer?.GetModPlayer<ResearchCraftingPlayer>();

        public override void Initialize()
        {
            _seenTiles = new bool[TileLoader.TileCount];
            _researchedTiles = new bool[TileLoader.TileCount];
            _encounteredShimmer = false;
            _seenEnvironment = CraftEnvironmentFlags.None;
            _researchedEnvironment = CraftEnvironmentFlags.None;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["EncounteredShimmer"] = _encounteredShimmer;
            tag["SeenTiles"] = PackTileList(_seenTiles);
            tag["ResearchedTiles"] = PackTileList(_researchedTiles);
            tag["SeenEnvironment"] = (ushort)_seenEnvironment;
            tag["ResearchedEnvironment"] = (ushort)_researchedEnvironment;
        }

        public override void LoadData(TagCompound tag)
        {
            _encounteredShimmer = tag.GetBool("EncounteredShimmer");

            if (_seenTiles == null || _seenTiles.Length != TileLoader.TileCount)
                _seenTiles = new bool[TileLoader.TileCount];
            if (_researchedTiles == null || _researchedTiles.Length != TileLoader.TileCount)
                _researchedTiles = new bool[TileLoader.TileCount];

            ApplyTileList(tag.GetList<int>("SeenTiles"), _seenTiles);
            ApplyTileList(tag.GetList<int>("ResearchedTiles"), _researchedTiles);

            _seenEnvironment = (CraftEnvironmentFlags)tag.GetShort("SeenEnvironment");
            _researchedEnvironment = (CraftEnvironmentFlags)tag.GetShort("ResearchedEnvironment");
        }

        public override void OnEnterWorld()
        {
            PublishStaticState();
            RebuildResearchedCraftEnvironment();

            CraftEnvironmentFlags persistedSeen = _seenEnvironment;
            RebuildSeenEnvironmentFromTiles();
            _seenEnvironment |= persistedSeen;

            if (Main.netMode != NetmodeID.Server)
            {
                ScanWorldCraftingTilesNearPlayer(100);
                ScanAdjacentCraftingTiles();
                ScanSeenEnvironmentFromPlayer();
            }
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            PublishStaticState();
            ScanAdjacentCraftingTiles();
            ScanSeenEnvironmentFromPlayer();
            ScanShimmerContact();
        }

        public void OnItemFullyResearched(int itemType)
        {
            if (!RecipeAnalyzer.IsJourneyWorld || itemType <= ItemID.None)
                return;

            bool changed = ApplyResearchedItem(itemType);
            if (changed)
                RecipeBrowserNestedCraft.InvalidateCaches();
        }

        public void RebuildResearchedCraftEnvironment()
        {
            if (_researchedTiles == null || _researchedTiles.Length != TileLoader.TileCount)
                _researchedTiles = new bool[TileLoader.TileCount];
            else
                System.Array.Clear(_researchedTiles, 0, _researchedTiles.Length);

            _researchedEnvironment = CraftEnvironmentFlags.None;

            if (!RecipeAnalyzer.IsJourneyWorld)
            {
                PublishStaticState();
                return;
            }

            for (int itemType = 1; itemType < ItemLoader.ItemCount; itemType++)
            {
                if (!RecipeAnalyzer.IsFullyResearched(itemType))
                    continue;
                ApplyResearchedItem(itemType);
            }

            PublishStaticState();
        }

        private void RebuildSeenEnvironmentFromTiles()
        {
            CraftEnvironmentFlags flags = CraftEnvironmentFlags.None;
            if (_seenTiles != null)
            {
                for (int i = 0; i < _seenTiles.Length; i++)
                {
                    if (_seenTiles[i])
                        flags |= ResearchCraftEnvironment.FlagsFromTile(i);
                }
            }

            _seenEnvironment = flags;
        }

        public static bool IsEnvironmentUnlocked(CraftEnvironmentFlags flag)
        {
            if (flag == CraftEnvironmentFlags.None)
                return true;

            CraftEnvironmentFlags merged = SeenEnvironment | ResearchedEnvironment;
            Player player = Main.LocalPlayer;
            if (player != null && player.active)
                merged |= ResearchCraftEnvironment.CollectInventoryEnvironmentFlags(player);

            return (merged & flag) == flag;
        }

        public static bool IsCraftingEnvironmentUnlocked(int tileType)
        {
            if (tileType <= 0)
                return true;

            foreach (int expanded in CraftingStationAdjacency.Expand(tileType))
            {
                if (IsCraftingStationSeen(expanded))
                    return true;
                if (ResearchedTiles != null
                    && expanded >= 0
                    && expanded < ResearchedTiles.Length
                    && ResearchedTiles[expanded])
                {
                    return true;
                }
            }

            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return false;

            foreach (int expanded in CraftingStationAdjacency.Expand(tileType))
            {
                if (player.adjTile != null
                    && expanded >= 0
                    && expanded < player.adjTile.Length
                    && player.adjTile[expanded])
                {
                    return true;
                }
            }

            if (PlayerCarriesCraftingTile(player, tileType))
                return true;

            return false;
        }

        private static bool PlayerCarriesCraftingTile(Player player, int tileType)
        {
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item it = player.inventory[i];
                if (!it.IsAir && ResearchCraftEnvironment.ItemProvidesCraftingTile(it.type, tileType))
                    return true;
            }

            if (ScanBankForCraftingTile(player.bank.item, tileType)
                || ScanBankForCraftingTile(player.bank2.item, tileType)
                || ScanBankForCraftingTile(player.bank3.item, tileType)
                || ScanBankForCraftingTile(player.bank4.item, tileType))
            {
                return true;
            }

            return false;
        }

        private static bool ScanBankForCraftingTile(Item[] bank, int tileType)
        {
            if (bank == null)
                return false;

            foreach (Item it in bank)
            {
                if (it != null && !it.IsAir && ResearchCraftEnvironment.ItemProvidesCraftingTile(it.type, tileType))
                    return true;
            }

            return false;
        }

        /// <summary>绿脸查询前刷新玩家附近合成环境（SeenTiles / 液体等）。</summary>
        public void RefreshEnvironmentForResearchQuery()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            RebuildResearchedCraftEnvironment();
            ScanWorldCraftingTilesNearPlayer(100);
            ScanAdjacentCraftingTiles();
            ScanSeenEnvironmentFromPlayer();
            PublishStaticState();
        }

        public static int GetEnvironmentSignature() =>
            ResearchCraftEnvironment.BuildEnvironmentSignature(
                SeenEnvironment,
                ResearchedEnvironment,
                SeenTiles,
                ResearchedTiles);

        public static int CountSeenTiles() => CountTrue(SeenTiles);

        public static int CountResearchedTiles() => CountTrue(ResearchedTiles);

        private bool ApplyResearchedItem(int itemType)
        {
            int tilesBefore = CountTrue(_researchedTiles);
            CraftEnvironmentFlags envBefore = _researchedEnvironment;

            _researchedEnvironment |= ResearchCraftEnvironment.ApplyItemToResearchedEnvironment(itemType, _researchedTiles);

            return CountTrue(_researchedTiles) > tilesBefore || _researchedEnvironment != envBefore;
        }

        private void PublishStaticState()
        {
            SeenTiles = _seenTiles;
            ResearchedTiles = _researchedTiles;
            SeenEnvironment = _seenEnvironment;
            ResearchedEnvironment = _researchedEnvironment;
        }

        private void ScanSeenEnvironmentFromPlayer()
        {
            CraftEnvironmentFlags captured = ResearchCraftEnvironment.CaptureSeenFromPlayer(Player);
            CraftEnvironmentFlags next = _seenEnvironment | captured;
            if (next == _seenEnvironment)
                return;

            _seenEnvironment = next;
            PublishStaticState();
            RecipeBrowserNestedCraft.InvalidateCaches();
        }

        private void ScanWorldCraftingTilesNearPlayer(int radius)
        {
            if (_seenTiles == null)
                return;

            Point center = Player.Center.ToTileCoordinates();
            for (int x = center.X - radius; x < center.X + radius; x++)
            {
                for (int y = center.Y - radius; y < center.Y + radius; y++)
                {
                    if (!WorldGen.InWorld(x, y, 0))
                        continue;

                    Tile tile = Main.tile[x, y];
                    if (tile == null || !tile.HasTile)
                        continue;

                    MarkSeenTile(tile.TileType);
                }
            }
        }

        private void MarkSeenTile(int tileType)
        {
            if (_seenTiles == null || tileType < 0)
                return;

            CraftingStationAdjacency.MarkExpanded(_seenTiles, tileType);
            _seenEnvironment |= ResearchCraftEnvironment.FlagsFromTile(tileType);
        }

        private void ScanAdjacentCraftingTiles()
        {
            if (_seenTiles == null)
                return;

            for (int i = 0; i < _seenTiles.Length; i++)
            {
                if (!Player.adjTile[i])
                    continue;

                CraftingStationAdjacency.MarkExpanded(_seenTiles, i);
                _seenEnvironment |= ResearchCraftEnvironment.FlagsFromTile(i);
            }
        }

        private void ScanShimmerContact()
        {
            if (_encounteredShimmer)
                return;

            if (Player.shimmerWet || Player.shimmering)
            {
                MarkShimmerEncountered();
                return;
            }

            Point tilePos = Player.Center.ToTileCoordinates();
            if (tilePos.X < 0 || tilePos.Y < 0 || tilePos.X >= Main.maxTilesX || tilePos.Y >= Main.maxTilesY)
                return;

            Tile tile = Main.tile[tilePos.X, tilePos.Y];
            if (tile != null && tile.HasTile && tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Shimmer)
                MarkShimmerEncountered();
        }

        private void MarkShimmerEncountered()
        {
            if (_encounteredShimmer)
                return;

            _encounteredShimmer = true;
            RecipeBrowserNestedCraft.InvalidateCaches();
        }

        public static bool IsCraftingStationSeen(int tileType)
        {
            if (tileType <= 0)
                return true;
            if (SeenTiles == null)
                return false;

            foreach (int expanded in CraftingStationAdjacency.Expand(tileType))
            {
                if (expanded >= 0 && expanded < SeenTiles.Length && SeenTiles[expanded])
                    return true;
            }

            return false;
        }

        private static List<int> PackTileList(bool[] tiles)
        {
            var list = new List<int>();
            if (tiles == null)
                return list;

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i])
                    list.Add(i);
            }

            return list;
        }

        private static void ApplyTileList(IList<int> source, bool[] target)
        {
            if (source == null || target == null)
                return;

            foreach (int tileId in source)
            {
                if (tileId >= 0 && tileId < target.Length)
                    target[tileId] = true;
            }
        }

        private static int CountTrue(bool[] flags)
        {
            if (flags == null)
                return 0;

            int count = 0;
            for (int i = 0; i < flags.Length; i++)
            {
                if (flags[i])
                    count++;
            }

            return count;
        }
    }
}
