using System;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>识别完成时通知家具页刷新 22 格（避免仅设 flag 但 UI 未 tick 时不显示）。</summary>
    public static class FurnitureBlueprintUiBridge
    {
        public static event Action SchemeApplied;

        public static void NotifySchemeApplied() => SchemeApplied?.Invoke();
    }
}
