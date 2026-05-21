using Terraria;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Catalog
{
    /// <summary>����������ҳ����Ч Buff �������ų������Ƶ��ڲ�ռλ����</summary>
    public sealed class BuffListCatalog : ModSystem
    {
        public static int ListableBuffCount { get; private set; }

        public override void PostSetupContent() => Rebuild();

        public static void Rebuild()
        {
            int total = 0;
            for (int i = 1; i < BuffLoader.BuffCount; i++)
            {
                if (IsListable(i))
                    total++;
            }

            ListableBuffCount = total;
            BuffMountCategorySystem.RebuildIndexes();
            BuffCategoryIndexSystem.Rebuild();
            BuffCombatSummonSystem.RebuildItemMap();
            BuffModCatalogSystem.Rebuild();
            SetBonusArmorResolver.ClearCache();
            SetBonusHookSystem.ResetRuntimeState();
            BuffSourceIndexSystem.Rebuild();
            BuffEntityIndexSystem.Rebuild();
            BuffVirtualEffectSafety.Rebuild();
            BuffVirtualEffectClassifier.Rebuild();
            BuffMiscEquipIndexSystem.RebuildItemMap();
            EmojLog.Info(EmojLogChannel.Core, $"BuffListCatalog rebuilt listable={ListableBuffCount}");
        }

        public static bool IsListable(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (!BuffPlayerApplicability.IsMeantForPlayer(buffId))
                return false;

            string name = BuffDisplayNameHelper.GetDisplayName(buffId);
            return !string.IsNullOrWhiteSpace(name);
        }
    }
}
