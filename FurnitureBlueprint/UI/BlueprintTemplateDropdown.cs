using System;

using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Terraria;

using Terraria.Audio;

using Terraria.GameContent.UI.Elements;

using Terraria.ID;

using Terraria.UI;

using EvenMoreOverpoweredJourney.Core.Localization;

using EvenMoreOverpoweredJourney.Shell.UI;



namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI

{

    /// <summary>建筑方案：点击展开列表（向上展开），选择户型模板。</summary>

    public sealed class BlueprintTemplateDropdown : UIElement

    {

        private const float ButtonHeight = 24f;

        private const float RowHeight = 28f;

        private const float ListPadding = 4f;



        private readonly UIPanel _button;

        private readonly UIText _buttonText;

        private readonly UIPanel _listHost;

        private readonly UIList _list;

        private bool _expanded;



        public BlueprintTemplateDropdown()

        {

            Width.Set(0, 1f);

            Height.Set(ButtonHeight, 0);



            _listHost = new UIPanel

            {

                Width = { Percent = 1f },

                BackgroundColor = new Color(22, 28, 38) * 0.95f,

                BorderColor = OPJourneyUiColors.PanelBorder

            };

            _listHost.Height.Set(0f, 0f);

            _listHost.IgnoresMouseInteraction = true;

            Append(_listHost);



            _list = new UIList { ListPadding = 3f };

            _list.Width.Set(0, 1f);

            _list.Height.Set(0, 1f);

            _listHost.Append(_list);



            _button = new UIPanel

            {

                VAlign = 1f,

                Width = { Percent = 1f },

                Height = { Pixels = ButtonHeight }

            };

            _button.BackgroundColor = new Color(40, 50, 70) * 0.9f;

            _button.BorderColor = new Color(90, 110, 140);

            _button.OnLeftClick += (_, _) => ToggleExpanded();

            Append(_button);



            _buttonText = new UIText("", 0.68f)

            {

                HAlign = 0.5f,

                VAlign = 0.5f,

                TextColor = Color.White

            };

            _button.Append(_buttonText);



            SyncFromPlayer();

        }



        public bool IsExpanded => _expanded;



        public void SetExpanded(bool expanded)

        {

            _expanded = expanded;

            int n = BuiltinBlueprintTemplates.All?.Count ?? 1;

            float listH = n * RowHeight + ListPadding;



            if (expanded)

            {

                Top.Set(-listH, 0);

                _listHost.Top.Set(-(listH + ButtonHeight + 2f), 0);

                _listHost.Height.Set(listH, 0);

                _listHost.IgnoresMouseInteraction = false;

                Height.Set(ButtonHeight, 0);

                RebuildList();

            }

            else

            {

                Top.Set(0, 0);

                _listHost.Top.Set(0, 0);

                _listHost.Height.Set(0f, 0f);

                _listHost.IgnoresMouseInteraction = true;

                Height.Set(ButtonHeight, 0);

            }

        }



        public void SyncFromPlayer()

        {

            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();

            BlueprintLayout layout = BuiltinBlueprintTemplates.ResolveActiveLayout(fb);

            _buttonText.SetText(EOPJText.UI(layout?.DisplayNameKey ?? "Blueprint.Template.SimpleNpcRoom"));

            if (_expanded)

                RebuildList();

        }



        private void ToggleExpanded()

        {

            SetExpanded(!_expanded);

            SoundEngine.PlaySound(SoundID.MenuTick);

        }



        private void RebuildList()

        {

            _list.Clear();

            IReadOnlyList<BlueprintLayout> all = BuiltinBlueprintTemplates.All;

            if (all == null)

                return;



            FurnitureBlueprintPlayer fb = Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>();

            string active = fb?.ActiveTemplateId ?? "";



            foreach (BlueprintLayout layout in all)

            {

                if (layout == null)

                    continue;

                bool sel = layout.Id == active;

                _list.Add(new TemplatePickerRow(layout, sel, OnPick));

            }

        }



        private void OnPick(BlueprintLayout layout)

        {

            if (layout == null)

                return;

            Main.LocalPlayer?.GetModPlayer<FurnitureBlueprintPlayer>()?.ApplyTemplateDefaults(layout);

            SetExpanded(false);

            SyncFromPlayer();

            SoundEngine.PlaySound(SoundID.MenuTick);

        }

    }

}

