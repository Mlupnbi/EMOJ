using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.ItemHub.Catalog
{
    /// <summary>����;�о� / Creative �˵�һ�µ���Ʒ���飨recipe browser / Item Checklist ͬԴ����</summary>
    internal static class HubCreativeGroupRules
    {
        internal static ContentSamples.CreativeHelper.ItemGroup GetGroup(Item item, out int orderInGroup)
        {
            orderInGroup = 0;
            if (item == null || item.IsAir)
                return ContentSamples.CreativeHelper.ItemGroup.EverythingElse;
            try
            {
                return ContentSamples.CreativeHelper.GetItemGroup(item, out orderInGroup);
            }
            catch
            {
                return ContentSamples.CreativeHelper.ItemGroup.EverythingElse;
            }
        }

        internal static bool IsFishingGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.FishingRods ||
            g == ContentSamples.CreativeHelper.ItemGroup.FishingQuestFish ||
            g == ContentSamples.CreativeHelper.ItemGroup.Fish ||
            g == ContentSamples.CreativeHelper.ItemGroup.FishingBait;

        internal static bool IsGrabBagGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.GoodieBags ||
            g == ContentSamples.CreativeHelper.ItemGroup.BossBags ||
            g == ContentSamples.CreativeHelper.ItemGroup.Crates ||
            g == ContentSamples.CreativeHelper.ItemGroup.Coin;

        internal static bool IsHealGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.LifePotions;

        internal static bool IsManaGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.ManaPotions;

        internal static bool IsBuffPotionGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.BuffPotion ||
            g == ContentSamples.CreativeHelper.ItemGroup.Flask;

        internal static bool IsFoodGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.Food;

        internal static bool IsBossSpawnerGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.BossSpawners ||
            g == ContentSamples.CreativeHelper.ItemGroup.BossItem;

        internal static bool IsDyeGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.Dye ||
            g == ContentSamples.CreativeHelper.ItemGroup.HairDye ||
            g == ContentSamples.CreativeHelper.ItemGroup.DyeMaterial;

        internal static bool IsMountGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.Mount ||
            g == ContentSamples.CreativeHelper.ItemGroup.Minecart ||
            g == ContentSamples.CreativeHelper.ItemGroup.Golf;

        internal static bool IsMaterialGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.Material;

        internal static bool IsMiscOtherGroup(ContentSamples.CreativeHelper.ItemGroup g) =>
            g == ContentSamples.CreativeHelper.ItemGroup.EverythingElse ||
            g == ContentSamples.CreativeHelper.ItemGroup.RemainingUseItems;
    }
}
