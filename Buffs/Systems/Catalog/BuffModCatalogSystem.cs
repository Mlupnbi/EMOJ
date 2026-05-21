using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Catalog
{
    /// <summary>�г��� Buff �б��г��ֵ�ģ�飨�� Terraria����</summary>
    public sealed class BuffModCatalogSystem : ModSystem
    {
        public static IReadOnlyList<string> ModKeys { get; private set; } = new List<string>();

        public override void PostSetupContent() => Rebuild();

        public static void Rebuild()
        {
            var mods = new HashSet<string>();
            for (int buffId = 1; buffId < BuffLoader.BuffCount; buffId++)
            {
                if (!BuffListCatalog.IsListable(buffId))
                    continue;

                mods.Add(GetModKey(buffId));
            }

            var ordered = new List<string> { "Terraria" };
            ordered.AddRange(mods.Where(m => m != "Terraria").OrderBy(m => m, System.StringComparer.OrdinalIgnoreCase));
            ModKeys = ordered;
        }

        public static string GetModKey(int buffId)
        {
            if (buffId <= 0)
                return "Terraria";

            if (buffId < BuffID.Count)
                return "Terraria";

            ModBuff mb = ModContent.GetModBuff(buffId);
            return mb?.Mod?.Name ?? "Unknown";
        }
    }
}
