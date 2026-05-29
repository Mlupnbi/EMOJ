using System.Collections.Generic;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Templates;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.Placement
{
    /// <summary>大图模板分帧放置：清空 → 框架 → 校验 → 家具。</summary>
    public sealed class BlueprintTemplatePlacementRunner : ModSystem
    {
        public const int AsyncCellThreshold = 180;
        public const int MaxOpsPerTick = 48;

        private enum SessionPhase : byte
        {
            Clear = 0,
            Framework = 1,
            Furniture = 2
        }

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

            switch (_session.Phase)
            {
                case SessionPhase.Clear:
                    BlueprintTemplatePlacer.ClearPlacementArea(
                        _session.Template,
                        _session.Scheme,
                        _session.Origin);
                    _session.Phase = SessionPhase.Framework;
                    _session.Index = 0;
                    return;

                case SessionPhase.Framework:
                    if (!AdvanceOps(player, _session.FrameworkOps))
                        return;

                    if (!BlueprintTemplatePlacer.VerifyFramework(
                            _session.Template,
                            _session.Scheme,
                            _session.Origin,
                            _session.FrameworkOps,
                            _session.Mode))
                    {
                        if (_session.Mode == BlueprintPlacementMode.Strict)
                        {
                            BlueprintTemplatePlacer.AbortWithReason(BlueprintTemplatePlacer.PlaceRejectReason.Framework);
                            _session = null;
                            return;
                        }
                    }

                    _session.Phase = SessionPhase.Furniture;
                    _session.Index = 0;
                    return;

                case SessionPhase.Furniture:
                    if (!AdvanceOps(player, _session.FurnitureOps))
                        return;

                    BlueprintTemplatePlacer.SyncTileSquares(
                        _session.Origin,
                        _session.Template.Width,
                        _session.Template.Height);
                    SoundEngine.PlaySound(SoundID.Item14, player.Center);
                    FurnitureBlueprintLog.Info(
                        $"template async place ok at {_session.Origin.X},{_session.Origin.Y} framework={_session.FrameworkOps.Count} furniture={_session.FurnitureOps.Count}");
                    _session = null;
                    return;
            }
        }

        private bool AdvanceOps(Player player, List<BlueprintTemplatePlacer.PlacementOp> ops)
        {
            if (ops == null || ops.Count == 0)
                return true;

            int budget = MaxOpsPerTick;
            while (budget > 0 && _session.Index < ops.Count)
            {
                BlueprintTemplatePlacer.ExecuteOp(
                    player,
                    _session.Template,
                    _session.Scheme,
                    _session.Origin,
                    ops[_session.Index],
                    _session.Consume);
                _session.Index++;
                budget--;
            }

            return _session.Index >= ops.Count;
        }

        internal static bool TryEnqueue(
            Player player,
            BlueprintTemplate template,
            FurnitureScheme scheme,
            Point origin,
            bool consumeMaterials,
            BlueprintPlacementMode mode,
            BlueprintTemplatePlacer.PlacementPlan plan)
        {
            if (_instance == null || _instance._session != null || plan.TotalOps == 0)
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
                plan);
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
            public readonly List<BlueprintTemplatePlacer.PlacementOp> FrameworkOps;
            public readonly List<BlueprintTemplatePlacer.PlacementOp> FurnitureOps;
            public SessionPhase Phase;
            public int Index;

            public PlacementSession(
                int playerIndex,
                BlueprintTemplate template,
                FurnitureScheme scheme,
                Point origin,
                bool consume,
                BlueprintPlacementMode mode,
                BlueprintTemplatePlacer.PlacementPlan plan)
            {
                PlayerIndex = playerIndex;
                Template = template;
                Scheme = scheme;
                Origin = origin;
                Consume = consume;
                Mode = mode;
                FrameworkOps = plan.FrameworkOps;
                FurnitureOps = plan.FurnitureOps;
                Phase = SessionPhase.Clear;
            }
        }
    }
}
