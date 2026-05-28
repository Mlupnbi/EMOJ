using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>部分模组物品 SetDefaults 可能抛错；识别热路径统一经此入口。</summary>
    internal static class FurnitureItemDefaults
    {
        public static bool TrySetDefaults(Item item, int type)
        {
            if (item == null || type <= ItemID.None)
                return false;

            try
            {
                item.SetDefaults(type);
                return true;
            }
            catch (Exception ex)
            {
                FurnitureBlueprintLog.Warn($"SetDefaults failed type={type}: {ex.Message}");
                return false;
            }
        }

        public static string SafeItemName(int type)
        {
            if (type <= ItemID.None)
                return "none";

            if (ContentSamples.ItemsByType.TryGetValue(type, out Item sample)
                && sample != null
                && !string.IsNullOrWhiteSpace(sample.Name))
                return sample.Name;

            Item item = new Item();
            if (!TrySetDefaults(item, type))
                return $"type:{type}";

            return string.IsNullOrWhiteSpace(item.Name) ? $"type:{type}" : item.Name;
        }
    }
}
