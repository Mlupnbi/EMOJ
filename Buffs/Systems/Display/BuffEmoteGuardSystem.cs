using System.Reflection;
using Terraria;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Display
{
    /// <summary>���� Buff ���� Update ��������ұ����ʱ������ձ���/ˢ����</summary>
    public static class BuffEmoteGuardSystem
    {
        private static FieldInfo _emoteTimeField;
        private static FieldInfo _emoteDelayField;

        public static void ResetPlayerEmoteTimers(Player player)
        {
            if (player == null)
                return;

            _emoteTimeField ??= typeof(Player).GetField("emoteTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _emoteDelayField ??= typeof(Player).GetField("emoteDelay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            _emoteTimeField?.SetValue(player, 0);
            _emoteDelayField?.SetValue(player, 0);
        }
    }
}
