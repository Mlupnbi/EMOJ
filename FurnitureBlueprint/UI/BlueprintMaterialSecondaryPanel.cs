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
    /// <summary>可选材料块列表：紧贴主窗材料行下方，宽度随候选数量收缩。</summary>
    public sealed class BlueprintMaterialSecondaryPanel : UIElement
    {
        private readonly UIPanel _body;
        private readonly List<BlueprintSeedSlot> _slots = new();
        private IReadOnlyList<int> _candidates = Array.Empty<int>();

        public bool IsOpen { get; private set; }

        public event Action<int> OnMaterialPicked;

        public BlueprintMaterialSecondaryPanel()
        {
            _body = new UIPanel
            {
                BackgroundColor = new Color(22, 28, 38) * 0.98f,
                BorderColor = OPJourneyUiColors.PanelBorder
            };
            _body.SetPadding(0);
            _body.Width.Set(0, 1f);
            _body.Height.Set(0, 1f);
            Append(_body);
            IgnoresMouseInteraction = true;
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            IgnoresMouseInteraction = !open;
            if (open)
                Rebuild();
        }

        public void SetCandidates(IReadOnlyList<int> types)
        {
            _candidates = types ?? Array.Empty<int>();
            if (IsOpen)
                Rebuild();
        }

        public float GetPreferredWidth() => BlueprintMaterialPickerLayout.PreferredWidth(_candidates.Count);

        public float GetPreferredHeight() => BlueprintMaterialPickerLayout.PreferredHeight(_candidates.Count);

        private void Rebuild()
        {
            _body.RemoveAllChildren();
            _slots.Clear();

            float pad = BlueprintMaterialPickerLayout.PanelPadding;

            _body.Append(new UIText(EOPJText.UIOr("Blueprint.MaterialPickerTitle", "\u9009\u62e9\u6750\u6599\u5757"), 0.78f)
            {
                Left = { Pixels = pad },
                Top = { Pixels = 4f },
                IsWrapped = false,
                TextColor = Color.White
            });

            if (_candidates.Count == 0)
            {
                _body.Append(new UIText(EOPJText.UIOr("Blueprint.MaterialPickerEmpty", "无可用材料块"), 0.65f)
                {
                    Left = { Pixels = pad },
                    Top = { Pixels = BlueprintMaterialPickerLayout.TitleRowHeight },
                    Width = { Pixels = -(pad * 2f), Percent = 1f },
                    TextColor = Color.Gray,
                    IsWrapped = true
                });
                return;
            }

            BlueprintMaterialPickerLayout.Measure(
                _candidates.Count, out int cols, out _, out float innerW, out float innerH);

            float gridTop = BlueprintMaterialPickerLayout.TitleRowHeight;
            float stride = BlueprintMaterialPickerLayout.CellStride;
            float slotSize = BlueprintMaterialPickerLayout.SlotSize;

            var grid = new UIElement();
            grid.Left.Set(pad, 0);
            grid.Top.Set(gridTop, 0);
            grid.Width.Set(innerW, 0);
            grid.Height.Set(innerH, 0);
            _body.Append(grid);

            for (int i = 0; i < _candidates.Count; i++)
            {
                int type = _candidates[i];
                int col = i % cols;
                int row = i / cols;
                int captured = type;

                var cell = new UIElement
                {
                    Left = { Pixels = col * stride },
                    Top = { Pixels = row * stride },
                    Width = { Pixels = slotSize },
                    Height = { Pixels = slotSize }
                };
                cell.OnLeftClick += (_, _) =>
                {
                    OnMaterialPicked?.Invoke(captured);
                    SetOpen(false);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                };

                var slot = new BlueprintSeedSlot
                {
                    ReturnPhysicalOnPlace = false,
                    ReturnPhysicalOnClear = false,
                    PickOnly = true
                };
                slot.Width.Set(slotSize, 0f);
                slot.Height.Set(slotSize, 0f);
                slot.item.SetDefaults(type);
                slot.IgnoresMouseInteraction = true;
                cell.Append(slot);
                _slots.Add(slot);
                grid.Append(cell);
            }
        }
    }
}
