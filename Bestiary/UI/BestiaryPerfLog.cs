using System;
using System.Diagnostics;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    internal static class BestiaryPerfLog
    {
        public static void LogElapsed(string label, long elapsedMs, int count = -1)
        {
            if (count >= 0)
            {
                EmojLog.InfoOnce(EmojLogChannel.Ui, $"bestiary:perf:{label}", $"bestiary:perf:{label} {elapsedMs}ms count={count}");
                return;
            }

            EmojLog.InfoOnce(EmojLogChannel.Ui, $"bestiary:perf:{label}", $"bestiary:perf:{label} {elapsedMs}ms");
        }

        public static long Measure(Action action)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }
}
