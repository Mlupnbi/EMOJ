using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Bestiary.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Shell
{
    public class OPJourneyUISystem : ModSystem
    {
        internal OPJourneyUI opJourneyUI;
        private UserInterface _opInterface;

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
            OPJourneyUI.Visible = false;
            HubRegistry.Reset();
            HubRegistry.EnsureBuilt();
            BestiaryListCatalog.Rebuild();
            opJourneyUI?.ItemHubSecondaryPanel?.RebuildScroll();
            opJourneyUI?.BestiarySecondaryPanel?.SetOpen(false);
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
            if (OPJourneyUI.Visible)
                _opInterface?.Update(gameTime);
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
                    if (OPJourneyUI.Visible)
                        _opInterface?.Draw(Main.spriteBatch, new GameTime());
                    return true;
                },
                InterfaceScaleType.UI)
            );
        }
    }
}
