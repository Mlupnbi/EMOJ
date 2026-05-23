using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Shell
{
    public class OPJourneyUISystem : ModSystem
    {
        internal OPJourneyUI opJourneyUI;
        private UserInterface _opInterface;
        private GameTime _lastUiGameTime;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            CreateInterface();
        }

        public override void PostSetupContent()
        {
            if (Main.dedServ)
                return;
            // ��Ԥ��Ŀ¼��ɸѡ UI �ڽ�����󹹽�������?? PostSetup �׶μ�λ��δ��������
            HubRegistry.EnsureBuilt();
            BestiaryListCatalog.Rebuild();
            BestiaryFilterIndex.Rebuild();
        }

        public override void OnWorldLoad()
        {
            if (Main.dedServ)
                return;
            ResetUIStateForWorldLoad();
            EmojLog.Info(EmojLogChannel.Ui, "OPJourneyUI world load reset");
        }

        private void ResetUIStateForWorldLoad()
        {
            OPJourneyUI.HideAndResetForWorld();
            SyncInterfaceVisibility();
            HubRegistry.Reset();
            HubRegistry.EnsureBuilt();
            BestiaryListCatalog.Rebuild();
            opJourneyUI?.ItemHubSecondaryPanel?.RebuildScroll();
        }

        private void CreateInterface()
        {
            opJourneyUI = new OPJourneyUI();
            opJourneyUI.Activate();
            if (_opInterface == null)
                _opInterface = new UserInterface();
            _opInterface.SetState(opJourneyUI);
        }

        public override void Unload()
        {
            EmojLog.Info(EmojLogChannel.Ui, "OPJourneyUI unload");
            _opInterface?.SetState(null);
            _opInterface = null;
            opJourneyUI = null;
            HubRegistry.Reset();
            OPJourneyUI.ClearStatics();
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _lastUiGameTime = gameTime;
            SyncInterfaceVisibility();
            if (!OPJourneyUI.Visible)
                return;

            _opInterface?.Update(gameTime);
        }

        internal void SyncInterfaceVisibility()
        {
            if (_opInterface == null)
                return;

            _opInterface.IsVisible = OPJourneyUI.Visible;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex == -1)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "EvenMoreOverpoweredJourney: Main UI",
                delegate
                {
                    if (!OPJourneyUI.Visible || _opInterface?.CurrentState == null || _lastUiGameTime == null)
                        return true;

                    SyncInterfaceVisibility();
                    _opInterface.Draw(Main.spriteBatch, _lastUiGameTime);
                    return true;
                },
                InterfaceScaleType.UI)
            );
        }
    }
}
