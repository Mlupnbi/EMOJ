using System.Collections.Generic;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Placement
{
    /// <summary>大图模板分帧放置，避免单帧大量 KillTile / SendTileSquare 导致卡死闪退。</summary>
    public sealed class BlueprintTemplatePlacementRunner : ModSystem
    {
        public const int AsyncCellThreshold = 180;
        public const int MaxOpsPerTick = 48;

        private static BlueprintTemplatePlacementRunner _instance;
        private PlacementSession _session;

        public static bool IsBusy => _instance?._session != null;

        public override void OnModLoad() => _instance = this;

        public override void PostUpdateWorld()
        {
            if (_session == null)
                return;

            if (!TryGetSessionPlayer(out Player player))
            {
                _session = null;
                return;
            }

            if (!_session.AreaCleared)
            {
                BlueprintTemplatePlacer.ClearPlacementArea(
                    _session.Template,
                    _session.Scheme,
                    _session.Origin);
                _session.AreaCleared = true;
            }

            int budget = MaxOpsPerTick;
            while (budget > 0 && _session.HasMore)
            {
                if (!BlueprintTemplatePlacer.ExecuteOp(
                        player,
                        _session.Template,
                        _session.Scheme,
                        _session.Origin,
                        _session.Ops[_session.Index],
                        _session.Consume))
                {
                    // 单格失败不中止整次放置（与同步路径一致）
                }

                _session.Index++;
                budget--;
            }

            if (_session.HasMore)
                return;

            BlueprintTemplatePlacer.SyncTileSquares(_session.Origin, _session.Template.Width, _session.Template.Height);
            SoundEngine.PlaySound(SoundID.Item14, player.Center);
            FurnitureBlueprintLog.Info(
                $"template async place ok at {_session.Origin.X},{_session.Origin.Y} ops={_session.Ops.Count}");
            _session = null;
        }

        internal static bool TryEnqueue(
            Player player,
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            bool consumeMaterials,
            BlueprintPlacementMode mode,
            List<BlueprintTemplatePlacer.PlacementOp> ops)
        {
            if (_instance == null || _instance._session != null || ops == null || ops.Count == 0)
                return false;

            if (player.TryGetModPlayer(out FurnitureBlueprintPlayer fb) && fb.RecognitionBusy)
                return false;

            _instance._session = new PlacementSession(
                player.whoAmI,
                template,
                scheme,
                origin,
                consumeMaterials,
                mode,
                ops);
            return true;
        }

        private bool TryGetSessionPlayer(out Player player)
        {
            player = null;
            if (_session == null)
                return false;

            player = Main.player[_session.PlayerIndex];
            if (!player.active || player.dead)
            {
                _session = null;
                return false;
            }

            return true;
        }

        private sealed class PlacementSession
        {
            public readonly int PlayerIndex;
            public readonly BlueprintTemplate Template;
            public readonly FurnitureScheme Scheme;
            public readonly Point Origin;
            public readonly bool Consume;
            public readonly BlueprintPlacementMode Mode;
            public readonly List<BlueprintTemplatePlacer.PlacementOp> Ops;
            public int Index;
            public bool AreaCleared;

            public PlacementSession(
                int playerIndex,
                BlueprintTemplate template,
                FurnitureScheme scheme,
                Point origin,
                bool consume,
                BlueprintPlacementMode mode,
                List<BlueprintTemplatePlacer.PlacementOp> ops)
            {
                PlayerIndex = playerIndex;
                Template = template;
                Scheme = scheme;
                Origin = origin;
                Consume = consume;
                Mode = mode;
                Ops = ops;
            }

            public bool HasMore => Index < Ops.Count;
        }
    }
}
