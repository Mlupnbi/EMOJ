using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.WorldBuilding;

namespace EvenMoreOverpoweredJourney.ItemHub.Rules
{
        /// <summary>
    /// 物品筛选与模组筛选逻辑对齐参考 collectible-checklist 模组（findableItems、PassModFilter、SharedUI 分类谓词）。
    /// 参考：https://github.com/JavidPack/ItemChecklist
    /// </summary>
    public static class HubCollectibleRules
    {
        /// <summary>�� IC <c>vanillaIDsInSortOrder</c> ��ͬ��ContentSamples �в����ڵ� type Ϊ -1��</summary>
        public static int[] CreativeSortOrderByType { get; private set; }

        public static void Reset()
        {
            CreativeSortOrderByType = null;
        }

        /// <summary>���� IC �����������ȵǼ� ContentSamples�����ô������򸲸�˳�򣩡�</summary>
        public static void EnsureCreativeSortOrderBuilt()
        {
            if (CreativeSortOrderByType != null && CreativeSortOrderByType.Length == ItemLoader.ItemCount)
                return;

            int max = ItemLoader.ItemCount;
            CreativeSortOrderByType = new int[max];
            for (int i = 0; i < max; i++)
                CreativeSortOrderByType[i] = -1;

            int sampleOrder = 0;
            for (int i = 0; i < max; i++)
            {
                if (!ContentSamples.ItemsByType.TryGetValue(i, out Item sample))
                    continue;
                if (sample == null || sample.type == ItemID.None)
                    continue;

                CreativeSortOrderByType[sample.type] = sampleOrder++;
            }

            var list = ContentSamples.ItemsByType.Values.ToList();
            var orderedEnumerable = from x in list
                select new ContentSamples.CreativeHelper.ItemGroupAndOrderInGroup(x) into x
                group x by x.Group into @group
                orderby (int)@group.Key
                select @group;

            int order = 0;
            foreach (var item2 in orderedEnumerable)
            {
                foreach (ContentSamples.CreativeHelper.ItemGroupAndOrderInGroup item3 in item2)
                {
                    if (item3.ItemType > 0 && item3.ItemType < max)
                        CreativeSortOrderByType[item3.ItemType] = order++;
                }
            }
        }

        /// <summary>���� CollectibleChecklistPlayer.Initialize �� findableItems ������</summary>
        public static bool IsFindable(int type)
        {
            if (type <= ItemID.None)
                return false;
            if (ItemID.Sets.Deprecated[type])
                return false;
            if (ItemLoader.GetItem(type) is UnloadedItem)
                return false;

            EnsureCreativeSortOrderBuilt();
            return CreativeSortOrderByType != null &&
                type < CreativeSortOrderByType.Length &&
                CreativeSortOrderByType[type] != -1;
        }

        /// <summary>���� CollectibleChecklistUI.PassModFilter��currentMod 0=ȫ�����˴��� null/�ձ�ʾ�����ˣ���</summary>
        public static bool PassModFilter(Item item, string modKey)
        {
            if (item == null || item.type <= ItemID.None)
                return false;
            if (string.IsNullOrEmpty(modKey))
                return true;

            if (HubModFilters.IsVanillaFilterKey(modKey))
                return item.ModItem == null;

            return item.ModItem != null && item.ModItem.Mod.Name == modKey;
        }

        public static bool PassModFilter(int type, string modKey)
        {
            if (type <= ItemID.None)
                return false;
            Item item = HubCatalog.Ready ? HubCatalog.GetDisplayItemReference(type) : null;
            if (item == null && ContentSamples.ItemsByType.TryGetValue(type, out Item sample))
                item = sample;
            return PassModFilter(item, modKey);
        }

        // --- SharedUI ���� / ���� ---

        public static bool IsMelee(Item x) =>
            x.CountsAsClass(DamageClass.Melee) && !(x.pick > 0 || x.axe > 0 || x.hammer > 0);

        public static bool IsYoyo(Item x) =>
            x.type > ItemID.None && x.type < ItemID.Sets.Yoyo.Length && ItemID.Sets.Yoyo[x.type];

        public static bool IsMagic(Item x) => x.CountsAsClass(DamageClass.Magic);

        public static bool IsRanged(Item x) => x.CountsAsClass(DamageClass.Ranged) && x.ammo == 0;

        public static bool IsThrowing(Item x) => x.CountsAsClass(DamageClass.Throwing);

        public static bool IsSummonMinion(Item x) =>
            x.CountsAsClass(DamageClass.Summon) && !x.sentry;

        public static bool IsSentry(Item x) => x.CountsAsClass(DamageClass.Summon) && x.sentry;

        public static bool IsPickaxe(Item x) => x.pick > 0;

        public static bool IsAxe(Item x) => x.axe > 0;

        public static bool IsHammer(Item x) => x.hammer > 0;

        // --- ���� / ��Ʒ ---

        public static bool IsHeadArmor(Item x) => x.headSlot != -1;

        public static bool IsBodyArmor(Item x) => x.bodySlot != -1;

        public static bool IsLegArmor(Item x) => x.legSlot != -1;

        public static bool IsArmor(Item x) => IsHeadArmor(x) || IsBodyArmor(x) || IsLegArmor(x);

        public static bool IsAccessory(Item x) => x.accessory;

        public static bool IsWings(Item x) => x.wingSlot > 0;

        public static bool IsGrappleHook(Item x) =>
            x.shoot > ProjectileID.None && x.shoot < Main.projHook.Length && Main.projHook[x.shoot];

        // --- ���? / ǽ ---

        public static bool IsPlaceableTile(Item x) => x.createTile != -1;

        public static bool IsContainerTile(Item x) =>
            x.createTile != -1 && x.createTile < TileLoader.TileCount && Main.tileContainer[x.createTile];

        public static bool IsWiringItem(Item x) =>
            x.type != ItemID.None && x.type < ItemID.Sets.SortingPriorityWiring.Length &&
            ItemID.Sets.SortingPriorityWiring[x.type] > -1;

        public static bool IsStatueItem(Item x)
        {
            EnsureStatueList();
            return x.createTile != -1 &&
                GenVars.statueList.Any(point => point.X == x.createTile && point.Y == x.placeStyle);
        }

        public static bool IsDoorTile(Item x) =>
            x.createTile > TileID.Dirt && TileID.Sets.RoomNeeds.CountsAsDoor.Contains(x.createTile);

        public static bool IsChairTile(Item x) =>
            x.createTile > TileID.Dirt && TileID.Sets.RoomNeeds.CountsAsChair.Contains(x.createTile);

        public static bool IsTableTile(Item x) =>
            x.createTile > TileID.Dirt && TileID.Sets.RoomNeeds.CountsAsTable.Contains(x.createTile);

        public static bool IsLightSourceTile(Item x) =>
            x.createTile > TileID.Dirt && TileID.Sets.RoomNeeds.CountsAsTorch.Contains(x.createTile);

        public static bool IsTorchTile(Item x) =>
            x.createTile > TileID.Dirt && x.createTile < TileLoader.TileCount && TileID.Sets.Torch[x.createTile];

        public static bool IsWall(Item x) => x.createWall != -1;

        // --- ��ҩ / ҩˮ / ���� ---

        public static bool IsAmmo(Item x) => x.ammo != 0;

        public static bool IsPotionUseSound(Item x) => x.UseSound?.IsTheSameAs(SoundID.Item3) == true;

        public static bool IsHealPotion(Item x) => x.healLife > 0;

        public static bool IsManaPotion(Item x) => x.healMana > 0;

        public static bool IsBuffPotion(Item x) => IsPotionUseSound(x) && x.buffType > 0;

        public static bool IsExpertItem(Item x) => x.expert;

        public static bool IsVanityPet(Item x) =>
            x.buffType > 0 && x.buffType < BuffLoader.BuffCount && Main.vanityPet[x.buffType];

        public static bool IsLightPet(Item x) =>
            x.buffType > 0 && x.buffType < BuffLoader.BuffCount && Main.lightPet[x.buffType];

        public static bool IsMount(Item x) => x.mountType != -1;

        public static bool IsCart(Item x) =>
            x.mountType != -1 && x.mountType < MountLoader.MountCount && MountID.Sets.Cart[x.mountType];

        public static bool IsDye(Item x) => x.dye != 0;

        public static bool IsHairDye(Item x) => x.hairDye != -1;

        public static bool IsBossSummon(Item x)
        {
            if (x.type == ItemID.None || x.type >= ItemID.Sets.SortingPriorityBossSpawns.Length)
                return x.netID == ItemID.PirateMap;

            return (ItemID.Sets.SortingPriorityBossSpawns[x.type] != -1 &&
                    x.type != ItemID.LifeCrystal && x.type != ItemID.ManaCrystal && x.type != ItemID.CellPhone &&
                    x.type != ItemID.IceMirror && x.type != ItemID.MagicMirror && x.type != ItemID.LifeFruit &&
                    x.netID != ItemID.TreasureMap) ||
                x.netID == ItemID.PirateMap;
        }

        public static bool IsGeneralConsumable(Item x) =>
            !(x.createWall > 0 || x.createTile > -1) && !(x.ammo > 0 && !x.notAmmo) && x.consumable;

        public static bool IsCapturedNpc(Item x) => x.makeNPC != 0;

        public static bool IsFishingPole(Item x) => x.fishingPole > 0;

        public static bool IsBait(Item x) => x.bait > 0;

        public static bool IsQuestFish(Item x) => x.questItem;

        public static bool IsExtractinator(Item x) =>
            x.type != ItemID.None && x.type < ItemID.Sets.ExtractinatorMode.Length && ItemID.Sets.ExtractinatorMode[x.type] > -1;

        public static bool IsMaterial(Item x) =>
            x.type != ItemID.None && x.type < ItemID.Sets.IsAMaterial.Length && ItemID.Sets.IsAMaterial[x.type];

        /// <summary>���� SharedUI.BelongsInOther����������һ�����ࣨ������ȫ��������������ʱ���С�</summary>
        public static bool BelongsInOther(Item x)
        {
            // ���� SharedUI.BelongsInOther��������ȫ�����롸��������������鸸��? belongs Ϊ createTile!=-1
            if (BelongsWeaponsTree(x) || BelongsToolsTree(x) || BelongsArmorTree(x) ||
                IsPlaceableTile(x) || IsWall(x) || BelongsAccessoriesTree(x) ||
                IsAmmo(x) || BelongsPotionsTree(x) || IsExpertItem(x) ||
                BelongsPetsTree(x) || BelongsMountsTree(x) || IsGrappleHook(x) ||
                BelongsDyesTree(x) || IsBossSummon(x) || BelongsConsumablesTree(x) ||
                BelongsFishingTree(x) || IsExtractinator(x))
                return false;
            return true;
        }

        public static bool BelongsWeaponsTree(Item x) =>
            IsMelee(x) || IsYoyo(x) || IsMagic(x) || IsRanged(x) || IsThrowing(x) ||
            IsSummonMinion(x) || IsSentry(x);

        public static bool BelongsToolsTree(Item x) => IsPickaxe(x) || IsAxe(x) || IsHammer(x);

        public static bool BelongsArmorTree(Item x) => IsHeadArmor(x) || IsBodyArmor(x) || IsLegArmor(x);

        public static bool BelongsTilesTree(Item x) =>
            IsContainerTile(x) || IsWiringItem(x) || IsStatueItem(x) ||
            IsDoorTile(x) || IsChairTile(x) || IsTableTile(x) ||
            IsLightSourceTile(x) || IsTorchTile(x);

        public static bool BelongsAccessoriesTree(Item x) => IsAccessory(x) || IsWings(x);

        public static bool BelongsPotionsTree(Item x) =>
            IsPotionUseSound(x) || IsHealPotion(x) || IsManaPotion(x) || IsBuffPotion(x);

        public static bool BelongsPetsTree(Item x) => IsVanityPet(x) || IsLightPet(x);

        public static bool BelongsMountsTree(Item x) => IsMount(x) || IsCart(x);

        public static bool BelongsDyesTree(Item x) => IsDye(x) || IsHairDye(x);

        public static bool BelongsConsumablesTree(Item x) => IsGeneralConsumable(x) || IsCapturedNpc(x);

        public static bool BelongsFishingTree(Item x) => IsFishingPole(x) || IsBait(x) || IsQuestFish(x);

        /// <summary>Item Checklist ��ƽ������? <c>ic.*</c>��</summary>
        public static bool MatchesIcCategoryTag(Item x, string tag)
        {
            if (x == null || x.type <= ItemID.None || string.IsNullOrEmpty(tag))
                return false;

            return tag switch
            {
                "ic.melee" => IsMelee(x) && !IsYoyo(x),
                "ic.yoyo" => IsYoyo(x),
                "ic.magic" => IsMagic(x),
                "ic.ranged" => IsRanged(x),
                "ic.throwing" => IsThrowing(x),
                "ic.summon" => IsSummonMinion(x) && !IsWhip(x),
                "ic.sentry" => IsSentry(x),
                "ic.pickaxe" => IsPickaxe(x),
                "ic.axe" => IsAxe(x),
                "ic.hammer" => IsHammer(x),
                "ic.head" => IsHeadArmor(x),
                "ic.body" => IsBodyArmor(x),
                "ic.legs" => IsLegArmor(x),
                "ic.vanity_armor" => x.vanity && IsArmor(x),
                "ic.non_vanity_armor" => !x.vanity && IsArmor(x),
                "ic.tiles" => IsPlaceableTile(x),
                "ic.containers" => IsContainerTile(x),
                "ic.wiring" => IsWiringItem(x),
                "ic.statues" => IsStatueItem(x),
                "ic.doors" => IsDoorTile(x),
                "ic.chairs" => IsChairTile(x),
                "ic.tables" => IsTableTile(x),
                "ic.light_sources" => IsLightSourceTile(x),
                "ic.torches" => IsTorchTile(x),
                "ic.walls" => IsWall(x),
                "ic.accessories" => IsAccessory(x) && !IsWings(x),
                "ic.wings" => IsWings(x),
                "ic.ammo" => IsAmmo(x),
                "ic.potions" => IsPotionUseSound(x),
                "ic.health" => IsHealPotion(x),
                "ic.mana" => IsManaPotion(x),
                "ic.buff_potion" => IsBuffPotion(x),
                "ic.expert" => IsExpertItem(x),
                "ic.pets" => IsVanityPet(x),
                "ic.light_pets" => IsLightPet(x),
                "ic.mounts" => IsMount(x) && !IsCart(x),
                "ic.carts" => IsCart(x),
                "ic.hooks" => IsGrappleHook(x),
                "ic.dyes" => IsDye(x),
                "ic.hair_dyes" => IsHairDye(x),
                "ic.boss_summon" => IsBossSummon(x),
                "ic.consumables" => IsGeneralConsumable(x),
                "ic.captured_npc" => IsCapturedNpc(x),
                "ic.fishing_pole" => IsFishingPole(x),
                "ic.bait" => IsBait(x),
                "ic.quest_fish" => IsQuestFish(x),
                "ic.extractinator" => IsExtractinator(x),
                "ic.materials" => IsMaterial(x),
                "ic.other" => BelongsInOther(x),
                _ => false
            };
        }

        /// <summary>���� UI ��ǩ �� CollectibleChecklist ν�ʡ�</summary>
        public static bool MatchesHubTag(Item x, string tag)
        {
            if (x == null || x.type <= ItemID.None)
                return false;

            if (tag.StartsWith("mod.", System.StringComparison.Ordinal))
            {
                string mk = tag.Substring(4);
                return PassModFilter(x, mk);
            }

            if (tag.StartsWith("ic.", System.StringComparison.Ordinal))
                return MatchesIcCategoryTag(x, tag);

            return tag switch
            {
                "wpn.melee" => IsMelee(x) && !IsYoyo(x),
                "wpn.magic" => IsMagic(x),
                "wpn.ranged" => IsRanged(x),
                "wpn.ammo" => IsAmmo(x),
                "wpn.whip" => IsWhip(x),
                "wpn.summon" => IsSummonMinion(x) && !IsWhip(x),
                "wpn.sentry" => IsSentry(x),
                "wpn.thrown" => IsThrowing(x),
                "wpn.yoyo" => IsYoyo(x),
                "eq.pick" => IsPickaxe(x),
                "eq.axe" => IsAxe(x),
                "eq.hammer" => IsHammer(x),
                "eq.armor" => IsArmor(x) && !x.vanity,
                "eq.acc" => IsAccessory(x) && !IsWings(x),
                "eq.wing" => IsWings(x),
                "eq.grapple" => IsGrappleHook(x),
                "van.fashion" => IsArmor(x) && x.vanity,
                "van.mount" => IsMount(x),
                "van.pet" => IsVanityPet(x),
                "van.lightpet" => IsLightPet(x),
                "tile.chest" => IsContainerTile(x),
                "tile.wire" => IsWiringItem(x) || IsStatueItem(x),
                "tile.furniture" =>
                    IsDoorTile(x) || IsChairTile(x) || IsTableTile(x) || IsStatueItem(x),
                "tile.light" => IsLightSourceTile(x) || IsTorchTile(x),
                "tile.wall" => IsWall(x),
                "tile.station" => false, // �� ExtData �ϳ�վ������ TagPredicates �в�ȫ
                "tile.block" => false, // �� ExtData ʵ�Ŀ��� TagPredicates �в�ȫ
                "con.heal" => IsHealPotion(x),
                "con.mana" => IsManaPotion(x),
                "con.buff" => IsBuffPotion(x),
                "con.food" => IsFood(x),
                "con.spawn" => IsCapturedNpc(x),
                "con.boss" => IsBossSummon(x),
                "con.bag" => IsGrabBag(x),
                "con.othercons" => IsOtherConsumable(x),
                "misc.expert" => IsExpertItem(x),
                "misc.master" => x.master,
                "misc.dye" => IsDye(x) || IsHairDye(x),
                "misc.extract" => IsExtractinator(x),
                "misc.fish" => IsFishingPole(x) || IsBait(x) || IsQuestFish(x),
                "misc.material" => IsMaterial(x),
                "misc.other" => BelongsInOther(x),
                "misc.debug" => false,
                _ => false
            };
        }

        private static bool IsWhip(Item x)
        {
            int s = x.shoot;
            if (s <= ProjectileID.None || s >= ProjectileLoader.ProjectileCount)
                return false;
            try
            {
                return ProjectileID.Sets.IsAWhip[s];
            }
            catch
            {
                return false;
            }
        }

        private static bool IsFood(Item x) =>
            x.buffType == BuffID.WellFed || x.buffType == BuffID.WellFed2 || x.buffType == BuffID.WellFed3 ||
            (x.type != ItemID.None && x.type < ItemID.Sets.IsFood.Length && ItemID.Sets.IsFood[x.type]);

        private static bool IsGrabBag(Item x) =>
            x.IsACoin ||
            (x.type != ItemID.None && x.type < ItemID.Sets.BossBag.Length && ItemID.Sets.BossBag[x.type]) ||
            (x.type != ItemID.None && x.type < ItemID.Sets.IsFishingCrate.Length && ItemID.Sets.IsFishingCrate[x.type]) ||
            (x.type != ItemID.None && x.type < ItemID.Sets.IsFishingCrateHardmode.Length && ItemID.Sets.IsFishingCrateHardmode[x.type]);

        public static bool IsOtherConsumable(Item x) =>
            IsGeneralConsumable(x) &&
            !IsHealPotion(x) && !IsManaPotion(x) && !IsBuffPotion(x) && !IsFood(x) &&
            !IsCapturedNpc(x) && !IsBossSummon(x) && !IsGrabBag(x);

        private static void EnsureStatueList()
        {
            if (GenVars.statueList == null)
                WorldGen.SetupStatueList();
        }
    }
}
