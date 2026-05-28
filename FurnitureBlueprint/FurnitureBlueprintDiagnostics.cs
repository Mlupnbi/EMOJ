using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>识别崩溃/慢查询诊断：写入 tModLoader 日志，便于对照控制台。</summary>
    public static class FurnitureBlueprintDiagnostics
    {
        public static string ClientLogPath =>
            Path.Combine(Main.SavePath, "Logs", "client.log");

        public static void LogRecognizePhase(string phase, int seed, int block, string detail = null)
        {
            string msg =
                $"[Blueprint] {phase} seed={seed} ({FurnitureItemDefaults.SafeItemName(seed)}) block={block} ({FurnitureItemDefaults.SafeItemName(block)})"
                + (string.IsNullOrEmpty(detail) ? "" : $" {detail}");
            ModContent.GetInstance<EvenMoreOverpoweredJourney>()?.Logger.Info(msg);
        }

        public static void LogRecognizeFailure(int seed, int block, Exception ex, string phase)
        {
            string msg =
                $"[Blueprint] FAILED phase={phase} seed={seed} block={block}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            ModContent.GetInstance<EvenMoreOverpoweredJourney>()?.Logger.Error(msg);
            FurnitureBlueprintLog.Warn(msg);
        }

        public static void LogRealtimeHint()
        {
            ModContent.GetInstance<EvenMoreOverpoweredJourney>()?.Logger.Info(
                $"[Blueprint] realtime logs: keep tModLoader console open, or read {ClientLogPath}");
        }
    }
}
