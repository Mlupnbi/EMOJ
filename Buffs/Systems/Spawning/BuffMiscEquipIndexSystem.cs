using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Spawning
{
    /// <summary>Buff �� �ɷ��� miscEquips ����Ʒ type�����ݼ���ʱ�������� PreUpdate ��λ�滻��</summary>
    public sealed class BuffMiscEquipIndexSystem : ModSystem
    {
        public static Dictionary<int, int> BuffToItemType { get; } = new Dictionary<int, int>();

        public static int GetMiscEquipSlotIndex(string category) => category switch
        {
            BuffCategories.Pet => 0,
            BuffCategories.LightPet => 1,
            BuffCategories.Minecart => 2,
            BuffCategories.Mount => 3,
            _ => -1
        };

        public override void PostSetupContent() => BuffListCatalog.Rebuild();

        /// <summary>���ѷ����ĳ���/��������/����/�� Buff ���� Buff����Ʒӳ�䣨�� PreUpdate misc �ۣ���</summary>
        public static void RebuildItemMap()
        {
            BuffToItemType.Clear();

            foreach (var pair in ContentSamples.ItemsByType.OrderBy(p => p.Key))
            {
                Item item = pair.Value;
                if (item == null || item.IsAir || item.buffType <= 0)
                    continue;

                RegisterItemForBuff(item.buffType, item.type);
            }

            foreach (int buffId in BuffCategoryIndexSystem.EnumerateMiscExclusiveBuffIds())
                EnsureItemMappedForMiscBuff(buffId);

            BuffEntityIndexSystem.RegisterNonMaintainableBuffs(BuffToItemType.Keys);
        }

        private static void EnsureItemMappedForMiscBuff(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return;

            if (!BuffCategoryIndexSystem.IsMiscExclusiveCategory(BuffCategoryIndexSystem.GetCategory(buffId)))
                return;

            if (BuffToItemType.ContainsKey(buffId))
                return;

            foreach (var pair in ContentSamples.ItemsByType)
            {
                Item item = pair.Value;
                if (item == null || item.IsAir || item.buffType != buffId)
                    continue;

                RegisterItemForBuff(buffId, item.type);
                if (BuffToItemType.ContainsKey(buffId))
                    return;
            }
        }

        private static void RegisterItemForBuff(int buffType, int itemType)
        {
            if (buffType <= 0 || itemType <= 0)
                return;

            string category = BuffCategoryIndexSystem.GetCategory(buffType);
            if (GetMiscEquipSlotIndex(category) < 0)
                return;

            if (!BuffToItemType.TryGetValue(buffType, out int existing))
            {
                BuffToItemType[buffType] = itemType;
                return;
            }

            bool existingVanilla = existing < ItemID.Count;
            bool itemVanilla = itemType < ItemID.Count;
            if (!existingVanilla && itemVanilla)
                BuffToItemType[buffType] = itemType;
        }
    }
}
