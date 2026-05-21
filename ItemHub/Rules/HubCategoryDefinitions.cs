using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.ItemHub.Rules
{
    /// <summary>
    /// Item Checklist SharedUI 分类 1:1 扁平化为筛选按钮（有子类则单独成类）。
    /// </summary>
    public static class HubCategoryDefinitions
    {
        public readonly struct Entry
        {
            public readonly string Tag;
            public readonly string LocKey;
            public readonly int PreviewItemId;

            public Entry(string tag, string locKey, int previewItemId)
            {
                Tag = tag;
                LocKey = locKey;
                PreviewItemId = previewItemId;
            }
        }

        public static IReadOnlyList<Entry> All { get; private set; }

        public static void EnsureInitialized()
        {
            if (All != null)
                return;

            All = new List<Entry>
            {
                // 武器
                new("ic.melee", "ItemHubIc_Melee", ItemID.CopperBroadsword),
                new("ic.yoyo", "ItemHubIc_Yoyo", ItemID.Code1),
                new("ic.magic", "ItemHubIc_Magic", ItemID.GoldenShower),
                new("ic.ranged", "ItemHubIc_Ranged", ItemID.FlintlockPistol),
                new("ic.throwing", "ItemHubIc_Throwing", ItemID.Shuriken),
                new("ic.summon", "ItemHubIc_Summon", ItemID.SlimeStaff),
                new("ic.sentry", "ItemHubIc_Sentry", ItemID.DD2LightningAuraT1Popper),
                // 工具
                new("ic.pickaxe", "ItemHubIc_Pickaxe", ItemID.CopperPickaxe),
                new("ic.axe", "ItemHubIc_Axe", ItemID.CopperAxe),
                new("ic.hammer", "ItemHubIc_Hammer", ItemID.CopperHammer),
                // 盔甲（含 IC 盔甲子面板互斥筛选）
                new("ic.head", "ItemHubIc_Head", ItemID.SilverHelmet),
                new("ic.body", "ItemHubIc_Body", ItemID.SilverChainmail),
                new("ic.legs", "ItemHubIc_Legs", ItemID.SilverGreaves),
                new("ic.vanity_armor", "ItemHubIc_VanityArmor", ItemID.BunnyHood),
                new("ic.non_vanity_armor", "ItemHubIc_NonVanityArmor", ItemID.GoldHelmet),
                // 物块
                new("ic.tiles", "ItemHubIc_Tiles", ItemID.Sign),
                new("ic.containers", "ItemHubIc_Containers", ItemID.GoldChest),
                new("ic.wiring", "ItemHubIc_Wiring", ItemID.Wire),
                new("ic.statues", "ItemHubIc_Statues", ItemID.HeartStatue),
                new("ic.doors", "ItemHubIc_Doors", ItemID.WoodenDoor),
                new("ic.chairs", "ItemHubIc_Chairs", ItemID.WoodenChair),
                new("ic.tables", "ItemHubIc_Tables", ItemID.PalmWoodTable),
                new("ic.light_sources", "ItemHubIc_LightSources", ItemID.ChineseLantern),
                new("ic.torches", "ItemHubIc_Torches", ItemID.RainbowTorch),
                new("ic.walls", "ItemHubIc_Walls", ItemID.PearlstoneBrickWall),
                // 饰品
                new("ic.accessories", "ItemHubIc_Accessories", ItemID.HermesBoots),
                new("ic.wings", "ItemHubIc_Wings", ItemID.LeafWings),
                new("ic.ammo", "ItemHubIc_Ammo", ItemID.MusketBall),
                // 药水
                new("ic.potions", "ItemHubIc_Potions", ItemID.HealingPotion),
                new("ic.health", "ItemHubIc_Health", ItemID.HealingPotion),
                new("ic.mana", "ItemHubIc_Mana", ItemID.ManaPotion),
                new("ic.buff_potion", "ItemHubIc_BuffPotion", ItemID.RagePotion),
                // 其它
                new("ic.expert", "ItemHubIc_Expert", ItemID.EoCShield),
                new("ic.pets", "ItemHubIc_Pets", ItemID.ZephyrFish),
                new("ic.light_pets", "ItemHubIc_LightPets", ItemID.FairyBell),
                new("ic.mounts", "ItemHubIc_Mounts", ItemID.SlimySaddle),
                new("ic.carts", "ItemHubIc_Carts", ItemID.Minecart),
                new("ic.hooks", "ItemHubIc_Hooks", ItemID.AmethystHook),
                new("ic.dyes", "ItemHubIc_Dyes", ItemID.OrangeDye),
                new("ic.hair_dyes", "ItemHubIc_HairDyes", ItemID.BiomeHairDye),
                new("ic.boss_summon", "ItemHubIc_BossSummon", ItemID.MechanicalSkull),
                new("ic.consumables", "ItemHubIc_Consumables", ItemID.PurificationPowder),
                new("ic.captured_npc", "ItemHubIc_CapturedNpc", ItemID.GoldBunny),
                new("ic.fishing_pole", "ItemHubIc_FishingPole", ItemID.WoodFishingPole),
                new("ic.bait", "ItemHubIc_Bait", ItemID.ApprenticeBait),
                new("ic.quest_fish", "ItemHubIc_QuestFish", ItemID.FallenStarfish),
                new("ic.extractinator", "ItemHubIc_Extractinator", ItemID.Extractinator),
                new("ic.materials", "ItemHubIc_Materials", ItemID.SpellTome),
                new("ic.other", "ItemHubIc_Other", ItemID.UnicornonaStick),
            };
        }

        public static bool TryGetPreviewItemId(string tag, out int itemId)
        {
            EnsureInitialized();
            foreach (Entry e in All)
            {
                if (e.Tag == tag)
                {
                    itemId = e.PreviewItemId;
                    return true;
                }
            }

            itemId = ItemID.None;
            return false;
        }
    }
}
