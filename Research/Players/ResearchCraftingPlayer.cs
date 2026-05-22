using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Terraria;

using Terraria.ID;

using Terraria.ModLoader;

using Terraria.ModLoader.IO;

using EvenMoreOverpoweredJourney.Research.Crafting;



namespace EvenMoreOverpoweredJourney.Research.Players

{

    /// <summary>

    /// 追踪已「见过」的合成台（对齐 Recipe Browser seenTiles + 进世界 200 格扫描）与微光接触。

    /// </summary>

    public class ResearchCraftingPlayer : ModPlayer

    {

        internal static bool[] SeenTiles;



        private bool[] _seenTiles;

        private bool _encounteredShimmer;



        public bool EncounteredShimmer => _encounteredShimmer;



        public static bool HasEncounteredShimmer =>

            Main.netMode == NetmodeID.Server || (Local?.EncounteredShimmer ?? false);



        private static ResearchCraftingPlayer Local =>

            Main.LocalPlayer?.GetModPlayer<ResearchCraftingPlayer>();



        public override void Initialize()

        {

            _seenTiles = new bool[TileLoader.TileCount];

            _encounteredShimmer = false;

        }



        public override void SaveData(TagCompound tag)

        {

            tag["EncounteredShimmer"] = _encounteredShimmer;

            var list = new List<int>();

            if (_seenTiles != null)

            {

                for (int i = 0; i < _seenTiles.Length; i++)

                {

                    if (_seenTiles[i])

                        list.Add(i);

                }

            }

            tag["SeenTiles"] = list;

        }



        public override void LoadData(TagCompound tag)

        {

            _encounteredShimmer = tag.GetBool("EncounteredShimmer");

            if (_seenTiles == null || _seenTiles.Length != TileLoader.TileCount)

                _seenTiles = new bool[TileLoader.TileCount];

            foreach (int t in tag.GetList<int>("SeenTiles"))

            {

                if (t >= 0 && t < _seenTiles.Length)

                    _seenTiles[t] = true;

            }

        }



        public override void OnEnterWorld()

        {

            SeenTiles = _seenTiles;

            if (Main.netMode != NetmodeID.Server)

            {

                ScanWorldCraftingTilesNearPlayer(100);

                ScanAdjacentCraftingTiles();

            }

        }



        public override void PostUpdate()

        {

            if (Player.whoAmI != Main.myPlayer)

                return;

            SeenTiles = _seenTiles;

            ScanAdjacentCraftingTiles();

            ScanShimmerContact();

        }



        /// <summary>照搬 RecipeBrowserPlayer.OnEnterWorld：扫描玩家周围已放置的合成台格。</summary>

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



                    int tileType = tile.TileType;

                    if (_seenTiles[tileType])

                        continue;



                    foreach (int adj in PopulateAdjTilesForTile(tileType))

                    {

                        if (adj >= 0 && adj < _seenTiles.Length)

                            _seenTiles[adj] = true;

                    }

                }

            }

        }



        private static IEnumerable<int> PopulateAdjTilesForTile(int tileType)

        {

            yield return tileType;

            ModTile modTile = TileLoader.GetTile(tileType);

            if (modTile?.AdjTiles != null)

            {

                foreach (int adj in modTile.AdjTiles)

                    yield return adj;

            }

            if (tileType == 302)
                yield return 17;
            if (tileType == 77)
                yield return 17;
            if (tileType == 133)
            {
                yield return 17;
                yield return 77;
            }
            if (tileType == 134)
                yield return 16;
            if (tileType == 354)
                yield return 14;
            if (tileType == 469)
                yield return 14;
            if (tileType == 487)
                yield return 14;
            if (tileType == 355)
            {
                yield return 13;
                yield return 14;
            }

        }



        private void ScanAdjacentCraftingTiles()

        {

            if (_seenTiles == null)

                return;

            for (int i = 0; i < _seenTiles.Length; i++)

            {

                if (Player.adjTile[i] && !_seenTiles[i])

                    _seenTiles[i] = true;

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

            int px = Player.Center.ToTileCoordinates().X;

            int py = Player.Center.ToTileCoordinates().Y;

            if (px < 0 || py < 0 || px >= Main.maxTilesX || py >= Main.maxTilesY)

                return;

            Tile tile = Main.tile[px, py];

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



        public static bool IsCraftingStationSeen(int tileType) =>

            tileType <= 0 || (SeenTiles != null && tileType < SeenTiles.Length && SeenTiles[tileType]);

    }

}


