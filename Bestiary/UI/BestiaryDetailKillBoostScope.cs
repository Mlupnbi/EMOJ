using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Bestiary;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>
    /// 脸① / 脸④ 预览详情时临时抬高击杀计数，使原版 <see cref="UIBestiaryEntryInfoPage"/> 显示立绘/背景/星级/群系。
    /// </summary>
    internal sealed class BestiaryDetailKillBoostScope : IDisposable
    {
        private readonly int _netId;
        private readonly int _savedKills;
        private readonly bool _applied;

        private BestiaryDetailKillBoostScope(int netId, int savedKills, bool applied)
        {
            _netId = netId;
            _savedKills = savedKills;
            _applied = applied;
        }

        public static BestiaryDetailKillBoostScope TryEnter(BestiaryEntry entry, BestiaryFaceMode face)
        {
            if (entry == null)
                return null;

            if (face != BestiaryFaceMode.AllVisible && face != BestiaryFaceMode.UnlockedOnly)
                return null;

            if (!BestiaryEntryResolver.TryGetNpcNetId(entry, out int netId) || netId <= 0)
                return null;

            int saved = 0;
            TryGetKillCount(netId, out saved);

            int target = Math.Max(GetFullUnlockKillTarget(entry, netId), 50);
            if (saved >= target)
                return new BestiaryDetailKillBoostScope(netId, saved, false);

            if (!TrySetKillCount(netId, target))
                return new BestiaryDetailKillBoostScope(netId, saved, false);

            return new BestiaryDetailKillBoostScope(netId, saved, true);
        }

        public void Dispose()
        {
            if (!_applied)
                return;

            TrySetKillCount(_netId, _savedKills);
        }

        private static int GetFullUnlockKillTarget(BestiaryEntry entry, int netId)
        {
            const int fallback = 50;
            if (Main.BestiaryTracker == null)
                return fallback;

            object killsTracker = Main.BestiaryTracker.GetType()
                .GetProperty("Kills", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(Main.BestiaryTracker);
            if (killsTracker == null)
                return fallback;

            MethodInfo needed = killsTracker.GetType().GetMethod(
                "GetKillCountNeeded",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(int) },
                null);

            if (needed != null)
            {
                try
                {
                    object result = needed.Invoke(killsTracker, new object[] { netId });
                    int n = result is int i ? i : Convert.ToInt32(result);
                    if (n > 0)
                        return n;
                }
                catch
                {
                    // ignored
                }
            }

            return fallback;
        }

        private static bool TryGetKillCount(int netId, out int kills)
        {
            kills = 0;
            if (Main.BestiaryTracker == null)
                return false;

            object killsTracker = Main.BestiaryTracker.GetType().GetProperty("Kills", BindingFlags.Instance | BindingFlags.Public)?.GetValue(Main.BestiaryTracker);
            if (killsTracker == null)
                return false;

            MethodInfo get = FindKillMethod(killsTracker.GetType(), "GetKillCount", 1);
            if (get == null)
                return false;

            try
            {
                object result = get.Invoke(killsTracker, new object[] { netId });
                kills = result is int i ? i : Convert.ToInt32(result);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySetKillCount(int netId, int kills)
        {
            if (Main.BestiaryTracker == null)
                return false;

            object killsTracker = Main.BestiaryTracker.GetType().GetProperty("Kills", BindingFlags.Instance | BindingFlags.Public)?.GetValue(Main.BestiaryTracker);
            if (killsTracker == null)
                return false;

            if (TryDirectSetKills(netId, kills))
                return true;

            MethodInfo set = FindKillMethod(killsTracker.GetType(), "SetKillCount", 2)
                ?? FindKillMethod(killsTracker.GetType(), "SetKills", 2);

            if (set != null)
            {
                try
                {
                    set.Invoke(killsTracker, new object[] { netId, kills });
                    return true;
                }
                catch
                {
                    // fall through
                }
            }

            MethodInfo register = killsTracker.GetType().GetMethod("RegisterKill", BindingFlags.Instance | BindingFlags.Public);
            if (register != null && kills > 0)
            {
                try
                {
                    for (int i = 0; i < kills; i++)
                        register.Invoke(killsTracker, new object[] { netId });
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }

        private static MethodInfo FindKillMethod(Type type, string name, int paramCount)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MethodInfo m in type.GetMethods(flags))
            {
                if (m.Name != name || m.GetParameters().Length != paramCount)
                    continue;

                return m;
            }

            return null;
        }

        private static bool TryDirectSetKills(int netId, int kills)
        {
            try
            {
                if (Main.BestiaryTracker?.Kills == null)
                    return false;

                var tracker = Main.BestiaryTracker.Kills;
                MethodInfo set = tracker.GetType().GetMethod(
                    "SetKillCount",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(int), typeof(int) },
                    null);

                if (set != null)
                {
                    set.Invoke(tracker, new object[] { netId, kills });
                    return true;
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }
    }
}
