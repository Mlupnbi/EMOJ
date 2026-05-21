using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Catalog
{
    /// <summary>��ģ����/Buff ���־û���������������� int �����غ��λ��</summary>
    public static class BuffStableKey
    {
        public const string VanillaMod = "Terraria";

        public static string ToKey(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return null;

            if (buffId < BuffID.Count)
            {
                string name = BuffID.Search.GetName(buffId);
                return string.IsNullOrEmpty(name) ? null : $"{VanillaMod}/{name}";
            }

            ModBuff buff = BuffLoader.GetBuff(buffId);
            return buff?.FullName;
        }

        public static bool TryResolve(string key, out int buffId)
        {
            buffId = 0;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            int slash = key.IndexOf('/');
            if (slash <= 0 || slash >= key.Length - 1)
                return false;

            string modName = key.Substring(0, slash);
            string buffName = key.Substring(slash + 1);
            if (string.IsNullOrEmpty(buffName))
                return false;

            if (modName.Equals(VanillaMod, System.StringComparison.OrdinalIgnoreCase))
            {
                int id = BuffID.Search.GetId(buffName);
                if (id > 0)
                {
                    buffId = id;
                    return true;
                }

                return false;
            }

            if (ModContent.TryFind(modName, buffName, out ModBuff modBuff))
            {
                buffId = modBuff.Type;
                return true;
            }

            return false;
        }
    }
}
