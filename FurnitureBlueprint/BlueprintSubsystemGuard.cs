using EvenMoreOverpoweredJourney.FurnitureBlueprint.Placement;
using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 识别、放置与预览 rebuild 之间的互锁门控。
    /// 仅做防护与隔离，不参与放置/识别的执行逻辑。
    /// </summary>
    public static class BlueprintSubsystemGuard
    {
        public static bool IsRecognitionBusy =>
            Main.LocalPlayer?.TryGetModPlayer(out FurnitureBlueprintPlayer fb) == true && fb.RecognitionBusy;

        public static bool IsSeedProbeBusy =>
            Main.LocalPlayer?.TryGetModPlayer(out FurnitureBlueprintPlayer fb) == true && fb.SeedProbeBusy;

        public static bool IsWorkspaceBusy => IsRecognitionBusy || IsSeedProbeBusy;

        public static bool IsPlacementBusy => BlueprintTemplatePlacementRunner.IsBusy;

        public static bool IsSubsystemBusy => IsWorkspaceBusy || IsPlacementBusy;

        public static bool CanRebuildPreviewCache => !IsSubsystemBusy;

        public static bool CanStartRecognition => !IsPlacementBusy && !IsSeedProbeBusy;

        public static bool CanStartSeedProbe => !IsPlacementBusy && !IsRecognitionBusy;

        public static bool CanStartPlacement => !IsWorkspaceBusy;
    }
}
