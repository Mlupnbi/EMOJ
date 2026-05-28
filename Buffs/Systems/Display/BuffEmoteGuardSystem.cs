using System.Reflection;
using Terraria;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Display
{
    /// <summary>抑制虚拟 Buff Update 触发的玩家表情气泡（含空表情/饥饿表情刷屏）。</summary>
    public static class BuffEmoteGuardSystem
    {
        private const int EmoteSuppressDelayFrames = 360;

        private static FieldInfo _emoteTimeField;
        private static FieldInfo _emoteDelayField;
        private static FieldInfo _emoteBubbleField;

        public static void SuppressPlayerEmotes(Player player)
        {
            if (player == null)
                return;

            ResolveEmoteFields();

            if (_emoteTimeField == null || _emoteDelayField == null)
            {
                EmojLog.Warn(EmojLogChannel.Buff,
                    $"SuppressPlayerEmotes: emote fields found? time={_emoteTimeField != null}, delay={_emoteDelayField != null}");
                return;
            }

            int emoteTime = (int)(_emoteTimeField.GetValue(player) ?? 0);
            if (emoteTime > 0)
                _emoteTimeField.SetValue(player, 0);

            int emoteDelay = (int)(_emoteDelayField.GetValue(player) ?? 0);
            if (emoteDelay < EmoteSuppressDelayFrames)
                _emoteDelayField.SetValue(player, EmoteSuppressDelayFrames);

            if (_emoteBubbleField != null)
                _emoteBubbleField.SetValue(player, -1);
        }

        public static void ResetPlayerEmoteTimers(Player player) => SuppressPlayerEmotes(player);

        private static void ResolveEmoteFields()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            _emoteTimeField ??= typeof(Player).GetField("emoteTime", flags);
            _emoteDelayField ??= typeof(Player).GetField("emoteDelay", flags);
            _emoteBubbleField ??= typeof(Player).GetField("emoteBubble", flags)
                ?? typeof(Player).GetField("_emoteBubble", flags);
        }
    }
}
