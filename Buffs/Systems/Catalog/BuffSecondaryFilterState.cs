using System.Collections.Generic;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Catalog
{
    public sealed class BuffSecondaryFilterState
    {
        /// <summary>�� = �����ˣ��ǿ� = ����ʾ��ѡģ�� Buff��</summary>
        public readonly HashSet<string> ActiveModKeys = new HashSet<string>();

        public void ToggleMod(string modKey)
        {
            if (string.IsNullOrEmpty(modKey))
                return;

            if (ActiveModKeys.Contains(modKey))
                ActiveModKeys.Remove(modKey);
            else
                ActiveModKeys.Add(modKey);
        }

        public void Reset() => ActiveModKeys.Clear();

        public bool AllowsBuff(int buffId)
        {
            if (ActiveModKeys.Count == 0)
                return true;

            return ActiveModKeys.Contains(BuffModCatalogSystem.GetModKey(buffId));
        }
    }
}
