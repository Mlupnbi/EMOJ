using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    internal static class FurnitureBlueprintLog
    {
        private const EmojLogChannel Ch = EmojLogChannel.Blueprint;

        public static void Info(string message) => EmojLog.Info(Ch, message);

        public static void InfoFull(string message) => EmojLog.InfoFull(Ch, message);

        public static void Warn(string message) => EmojLog.Warn(Ch, message);

        public static void Error(string message, System.Exception ex = null) => EmojLog.Error(Ch, message, ex);

        public static void InfoOnce(string key, string message) => EmojLog.InfoOnce(Ch, key, message);
    }
}
