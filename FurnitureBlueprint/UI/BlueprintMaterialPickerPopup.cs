using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint.UI
{
    /// <summary>材料候选网格布局常量（二级窗与浮层共用）。</summary>
    public static class BlueprintMaterialPickerLayout
    {
        public const int MaxColumns = 4;
        public const float SlotGap = 4f;
        public const float PanelPadding = 8f;
        public const float TitleRowHeight = 24f;

        public static float SlotSize => FurnitureBlueprintPageLayout.MaterialSlotSize;

        public static float CellStride => SlotSize + SlotGap;

        public static void Measure(int count, out int cols, out int rows, out float innerW, out float innerH)
        {
            cols = Math.Min(MaxColumns, Math.Max(1, count));
            rows = (count + cols - 1) / cols;
            innerW = cols * SlotSize + (cols - 1) * SlotGap;
            innerH = rows * SlotSize + (rows - 1) * SlotGap;
        }

        public static float PreferredWidth(int count)
        {
            if (count <= 0)
                return 0f;
            Measure(count, out _, out _, out float innerW, out _);
            return innerW + PanelPadding * 2f;
        }

        public static float PreferredHeight(int count)
        {
            if (count <= 0)
                return 0f;
            Measure(count, out _, out _, out _, out float innerH);
            return TitleRowHeight + innerH + PanelPadding * 2f;
        }
    }

    /// <summary>材料块槽上方的原料多选浮层（备用布局，与二级窗共用度量）。</summary>
    public sealed class BlueprintMaterialPickerPopup : UIElement
    {
        private readonly UIPanel _panel;
        private readonly List<BlueprintSeedSlot> _slots = new();
        private IReadOnlyList<int> _candidates = Array.Empty<int>();

        public bool IsExpanded { get; private set; }

        public event Action<int> OnMaterialPicked;

        public BlueprintMaterialPickerPopup()
        {
            _panel = new UIPanel
            {
                BackgroundColor = new Color(22, 28, 38) * 0.96f,
                BorderColor = OPJourneyUiColors.PanelBorder
            };
            _panel.SetPadding(BlueprintMaterialPickerLayout.PanelPadding);
            Append(_panel);
            IgnoresMouseInteraction = true;
        }

        public void SetExpanded(bool expanded)
        {
            IsExpanded = expanded;
            IgnoresMouseInteraction = !expanded;
            if (expanded)
                Rebuild();
            else
            {
                Width.Set(0f, 0f);
                Height.Set(0f, 0f);
            }
        }

        public void SetCandidates(IReadOnlyList<int> types)
        {
            _candidates = types ?? Array.Empty<int>();
            if (IsExpanded)
                Rebuild();
        }

        public void LayoutAbove(float anchorLeft, float anchorTop)
        {
            if (!IsExpanded || _candidates.Count == 0)
                return;

            float w = BlueprintMaterialPickerLayout.PreferredWidth(_candidates.Count);
            float h = BlueprintMaterialPickerLayout.PreferredHeight(_candidates.Count);

            float left = anchorLeft;
            float top = anchorTop - h - 4f;
            if (top < 4f)
                top = anchorTop + FurnitureBlueprintPageLayout.MaterialRowHeight + 4f;

            float maxRight = OPJourneyShellMetrics.ContentInsetLeft + OPJourneyShellMetrics.ChromeWidth;
            if (left + w > maxRight)
                left = Math.Max(OPJourneyShellMetrics.ContentInsetLeft, maxRight - w);

            Width.Set(w, 0f);
            Height.Set(h, 0f);
            Left.Set(left, 0f);
            Top.Set(top, 0f);
        }

        private void Rebuild()
        {
            _panel.RemoveAllChildren();
            _slots.Clear();

            if (_candidates.Count == 0)
            {
                SetExpanded(false);
                return;
            }

            BlueprintMaterialPickerLayout.Measure(
                _candidates.Count, out int cols, out _, out float innerW, out float innerH);
            _panel.SetPadding(0);
            _panel.Width.Set(innerW, 0f);
            _panel.Height.Set(innerH, 0f);

            float stride = BlueprintMaterialPickerLayout.CellStride;
            float slotSize = BlueprintMaterialPickerLayout.SlotSize;

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
                    SetExpanded(false);
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
                _panel.Append(cell);
            }
        }
    }
}
