using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace EvenMoreOverpoweredJourney.ItemHub.Rules
{
    /// <summary>��Ʒ������չ���ࣨĿ¼���� + �䷽�ڽ������������?</summary>
    public struct HubExtData
    {
        public int MaxStack;
        public int CreateWall;
        public int Ammo;
        public int MountType;
        public int BuffType;
        public int HeadSlot, BodySlot, LegSlot, WingSlot;
        public bool Vanity;
        public bool Dye;
        public bool ExpertOnly, MasterOnly;
        public bool Material;
        public bool QuestFish;
        public bool Bait;
        public bool TorchFlame;
        public bool DebugItem;

        // --- ������ / ���? ---
        public bool HubBlockPlacing;
        public bool HubCraftingStationPlaced;
        public bool HubFurniturePlaced;
        public bool HubWallPlaced;
        public bool HubContainerPlaced;
        public bool HubLightSourcePlaced;
        public bool HubLightHeldOrConsumableGlow;
        /// <summary>����ǽ����Ʒ��createWall ��Ӧǽ�� Main.wallLight �У���</summary>
        public bool HubLightWallGlow;
        /// <summary>������Ʒ�ֳֹ�Դ�����ڿ�ͷ�����������ɷ��ø�</summary>
        public bool HubLightHandheldTool;
        public bool HubWireBannerStatueItem;

        // --- ���� / ��ҩ ---
        public bool HubMeleeWeapon;
        public bool HubMagicWeapon;
        public bool HubRangedWeapon;
        public bool HubThrowingWeapon;
        public bool HubSummonMinionWeapon;
        public bool HubWhipWeapon;
        public bool HubSentryWeapon;
        public bool HubYoyoWeapon;
        public bool HubAmmoItem;

        // --- ���� / ���� / ��Ʒ ---
        public bool HubPickaxe, HubAxe, HubHammer;
        public bool HubArmorNonVanity;
        public bool HubArmorVanity;
        public bool HubAccessoryNoWing;
        public bool HubWingAccessory;
        public bool HubGrappleHook;
        public bool HubMountItem;
        public bool HubPetBuffItem;
        public bool HubLightPetBuffItem;

        // --- ����Ʒ / ���� ---
        public bool HubHealConsumable;
        public bool HubManaConsumable;
        public bool HubBuffPotionConsumable;
        public bool HubFoodConsumable;
        public bool HubCapturedNpcConsumable;
        public bool HubBossSpawnItem;
        public bool HubOtherConsumable;
        public bool HubGrabBagLike;
        public bool HubFishingGroupItem;
        public bool HubExtractinatorPlaceable;
        public bool HubMaterialLoose;
        public bool HubOtherLoose;
        public bool HubMaterialResearchBridge;
        /// <summary>��ģ��/���ϱ�ǩ�����κ�Ŀ¼���ǩ����ʱ���롸�������Ķ��ױ�ǡ�</summary>
        public bool HubMiscCatalogResidual;
    }

    public static class HubExtDataBuilder
    {
        public static void ResetCaches()
        {
            ItemHubStatuePairCache.Clear();
            HubMaterialResearchBridge.Clear();
            ItemHubAmmoTypeReferencedByWeaponsCache.Reset();
        }

        public static HubExtData Build(int type, Item probe, ref HubRegistry.Meta meta, bool[] deprecatedSnapshot)
        {
            HubStationTileIndex.BuildStationTileIndexFromRecipes();

            ContentSamples.CreativeHelper.ItemGroup journeyGroup =
                HubCreativeGroupRules.GetGroup(probe, out _);

            if (GenVars.statueList == null)
                WorldGen.SetupStatueList();

            bool mBridge = probe.material && HubMaterialResearchBridge.IsUpstreamToResearchable(type);
            var d = new HubExtData
            {
                MaxStack = probe.maxStack,
                CreateWall = probe.createWall,
                Ammo = probe.ammo,
                MountType = probe.mountType,
                BuffType = probe.buffType,
                HeadSlot = probe.headSlot,
                BodySlot = probe.bodySlot,
                LegSlot = probe.legSlot,
                WingSlot = probe.wingSlot,
                Vanity = probe.vanity,
                Dye = probe.dye != 0 || HubCreativeGroupRules.IsDyeGroup(journeyGroup),
                ExpertOnly = probe.expert,
                MasterOnly = probe.master,
                Material = probe.material,
                QuestFish = probe.questItem,
                Bait = probe.bait > 0,
                TorchFlame = probe.flame || probe.holdStyle == 1,
                HubMaterialResearchBridge = mBridge,
                DebugItem = ComputeDebug(type, probe, deprecatedSnapshot, mBridge)
            };

            d.HubPickaxe = probe.pick > 0;
            d.HubAxe = probe.axe > 0;
            d.HubHammer = probe.hammer > 0;

            d.HubYoyoWeapon = HubCollectibleRules.IsYoyo(probe);
            d.HubWhipWeapon = SafeIsWhip(probe);
            d.HubSentryWeapon = HubCollectibleRules.IsSentry(probe);
            d.HubSummonMinionWeapon = HubCollectibleRules.IsSummonMinion(probe) && !d.HubWhipWeapon;
            d.HubMeleeWeapon = HubCollectibleRules.IsMelee(probe) && !d.HubYoyoWeapon && !d.HubWhipWeapon;
            d.HubMagicWeapon = HubCollectibleRules.IsMagic(probe);
            d.HubRangedWeapon = HubCollectibleRules.IsRanged(probe);
            d.HubThrowingWeapon = HubCollectibleRules.IsThrowing(probe);
            d.HubAmmoItem = HubCollectibleRules.IsAmmo(probe);

            d.HubWingAccessory = HubCollectibleRules.IsWings(probe);
            d.HubGrappleHook = HubCollectibleRules.IsGrappleHook(probe);
            d.HubAccessoryNoWing = HubCollectibleRules.IsAccessory(probe) && !d.HubWingAccessory;
            d.HubArmorNonVanity = HubCollectibleRules.IsArmor(probe) && !probe.vanity;
            d.HubArmorVanity = HubCollectibleRules.IsArmor(probe) && probe.vanity;

            d.HubMountItem = HubCollectibleRules.IsMount(probe);

            int bt = probe.buffType;
            if (bt > 0 && bt < BuffLoader.BuffCount)
            {
                d.HubPetBuffItem = HubCollectibleRules.IsVanityPet(probe);
                d.HubLightPetBuffItem = HubCollectibleRules.IsLightPet(probe);
            }

            int ct = probe.createTile;
            int maxTiles = TileLoader.TileCount;
            bool paintingSortRow =
                type > 0 &&
                type < ItemID.Sets.SortingPriorityPainting.Length &&
                ItemID.Sets.SortingPriorityPainting[type] > -1;
            bool inCraftSet = ct >= 0 && ct < maxTiles &&
                HubStationTileIndex.StationTileIdsFromRecipes.Contains(ct);

            if (ct >= 0 && ct < maxTiles)
                AssignPlacedTileCategory(type, probe, ct, journeyGroup, inCraftSet, paintingSortRow, ref d);
            else
                AssignNonTilePlaceableCategory(type, probe, journeyGroup, paintingSortRow, ref d);

            d.HubExtractinatorPlaceable =
                type != ItemID.None &&
                type < ItemID.Sets.ExtractinatorMode.Length &&
                ItemID.Sets.ExtractinatorMode[type] > -1;

            d.HubLightHeldOrConsumableGlow = d.TorchFlame && probe.consumable && probe.createTile == -1;
            int cw = probe.createWall;
            d.HubLightWallGlow = cw > WallID.None && cw < WallLoader.WallCount && Main.wallLight[cw];
            d.HubLightHandheldTool =
                d.TorchFlame && !probe.consumable && probe.createTile == -1 && probe.createWall <= WallID.None;

            d.HubWallPlaced =
                journeyGroup == ContentSamples.CreativeHelper.ItemGroup.Walls ||
                (probe.createWall > 0 && !ItemHubTileRules.IsExcludedWall(probe.createWall));

            d.HubHealConsumable = HubCollectibleRules.IsHealPotion(probe);
            d.HubManaConsumable = HubCollectibleRules.IsManaPotion(probe);
            d.HubBuffPotionConsumable = HubCollectibleRules.IsBuffPotion(probe);
            d.HubFoodConsumable =
                HubCreativeGroupRules.IsFoodGroup(journeyGroup) ||
                probe.buffType == BuffID.WellFed ||
                probe.buffType == BuffID.WellFed2 ||
                probe.buffType == BuffID.WellFed3 ||
                (type > 0 && type < ItemID.Sets.IsFood.Length && ItemID.Sets.IsFood[type]);

            d.HubCapturedNpcConsumable = HubCollectibleRules.IsCapturedNpc(probe);
            d.HubBossSpawnItem = HubCollectibleRules.IsBossSummon(probe);

            d.HubGrabBagLike =
                probe.IsACoin ||
                (type > 0 && type < ItemID.Sets.BossBag.Length && ItemID.Sets.BossBag[type]) ||
                (type > 0 && type < ItemID.Sets.IsFishingCrate.Length && ItemID.Sets.IsFishingCrate[type]) ||
                (type > 0 && type < ItemID.Sets.IsFishingCrateHardmode.Length && ItemID.Sets.IsFishingCrateHardmode[type]);

            d.HubFishingGroupItem =
                HubCollectibleRules.IsFishingPole(probe) ||
                HubCollectibleRules.IsBait(probe) ||
                HubCollectibleRules.IsQuestFish(probe);

            d.HubMaterialLoose = HubCollectibleRules.IsMaterial(probe);

            d.HubOtherConsumable = HubCollectibleRules.IsOtherConsumable(probe);

            d.HubOtherLoose = !d.DebugItem && HubCollectibleRules.BelongsInOther(probe);

            bool catalogAny =
                (meta.CreateTile >= 0 && d.HubBlockPlacing) ||
                (d.HubLightSourcePlaced || d.HubLightHeldOrConsumableGlow || d.HubLightWallGlow || d.HubLightHandheldTool) ||
                d.HubWallPlaced ||
                (meta.CreateTile >= 0 && d.HubFurniturePlaced) ||
                (meta.CreateTile >= 0 && d.HubCraftingStationPlaced) ||
                (meta.CreateTile >= 0 && d.HubContainerPlaced) ||
                d.HubWireBannerStatueItem ||
                d.HubMeleeWeapon || d.HubMagicWeapon || d.HubRangedWeapon || d.HubAmmoItem ||
                d.HubSummonMinionWeapon || d.HubWhipWeapon || d.HubThrowingWeapon || d.HubSentryWeapon || d.HubYoyoWeapon ||
                d.HubPickaxe || d.HubAxe || d.HubHammer ||
                d.HubArmorNonVanity || d.HubAccessoryNoWing || d.HubWingAccessory || d.HubGrappleHook ||
                d.HubArmorVanity || d.HubMountItem || d.HubPetBuffItem || d.HubLightPetBuffItem ||
                d.HubHealConsumable || d.HubManaConsumable || d.HubBuffPotionConsumable || d.HubFoodConsumable ||
                d.HubCapturedNpcConsumable || d.HubBossSpawnItem || d.HubGrabBagLike || d.HubOtherConsumable ||
                d.ExpertOnly || d.MasterOnly || d.Dye || d.HubExtractinatorPlaceable || d.HubFishingGroupItem ||
                d.HubOtherLoose || d.HubMaterialResearchBridge;

            d.HubMiscCatalogResidual = !d.DebugItem && !d.HubMaterialLoose && !catalogAny;

            return d;
        }

        private static void AssignPlacedTileCategory(
            int type,
            Item probe,
            int ct,
            ContentSamples.CreativeHelper.ItemGroup journeyGroup,
            bool inCraftSet,
            bool paintingSortRow,
            ref HubExtData d)
        {
            bool frameImportant = Main.tileFrameImportant[ct];
            bool statuePlaque = ItemHubStatuePairCache.IsStatuePair(ct, probe.placeStyle);
            bool combatBanner = IsCombatBannerItem(type) || ct == TileID.Banners;

            if (journeyGroup == ContentSamples.CreativeHelper.ItemGroup.Wiring ||
                IsWiringOrStatueItem(type, probe, ct) ||
                (combatBanner && !IsDecorativeBannerItem(type)))
            {
                d.HubWireBannerStatueItem = true;
                return;
            }

            if (journeyGroup == ContentSamples.CreativeHelper.ItemGroup.Torches ||
                journeyGroup == ContentSamples.CreativeHelper.ItemGroup.Glowsticks ||
                ItemHubTileRules.IsPlacedLightTile(ct))
            {
                d.HubLightSourcePlaced = true;
                return;
            }

            if (inCraftSet || journeyGroup == ContentSamples.CreativeHelper.ItemGroup.CraftingObjects)
            {
                d.HubCraftingStationPlaced = true;
                return;
            }

            if (ItemHubTileRules.IsContainerTile(ct))
            {
                d.HubContainerPlaced = true;
                return;
            }

            if (journeyGroup == ContentSamples.CreativeHelper.ItemGroup.Blocks ||
                ItemHubTileRules.IsSolidBlockTile(ct))
            {
                d.HubBlockPlacing = true;
                return;
            }

            if (frameImportant &&
                ct != TileID.Campfire &&
                !statuePlaque &&
                ct != TileID.Banners &&
                !IsCombatBannerItem(type) &&
                !IsDecorativeBannerItem(type))
            {
                d.HubFurniturePlaced = true;
            }
        }

        private static void AssignNonTilePlaceableCategory(
            int type,
            Item probe,
            ContentSamples.CreativeHelper.ItemGroup journeyGroup,
            bool paintingSortRow,
            ref HubExtData d)
        {
            if (IsCombatBannerItem(type) || IsWiringOrStatueItem(type, probe, -1))
            {
                d.HubWireBannerStatueItem = true;
                return;
            }

            if (journeyGroup == ContentSamples.CreativeHelper.ItemGroup.Torches ||
                journeyGroup == ContentSamples.CreativeHelper.ItemGroup.Glowsticks)
            {
                return;
            }

            if (IsDecorativeBannerItem(type) ||
                (paintingSortRow && !IsCombatBannerItem(type)) ||
                (journeyGroup == ContentSamples.CreativeHelper.ItemGroup.PlacableObjects &&
                    !IsCombatBannerItem(type)))
                d.HubFurniturePlaced = true;
        }

        /// <summary>ս�����ģ�BannerStrength.Enabled����������?/����/���ġ�</summary>
        private static bool IsCombatBannerItem(int type)
        {
            if (type <= 0)
                return false;
            try
            {
                if (type >= ItemID.Sets.BannerStrength.Length)
                    return false;
                ItemID.BannerEffect be = ItemID.Sets.BannerStrength[type];
                return be.Enabled;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>װ�������ģ�BannerStrength �����õ�δ����ս���ӳɣ�������Ҿ߶��ǵ�·��?</summary>
        private static bool IsDecorativeBannerItem(int type)
        {
            if (type <= 0 || IsCombatBannerItem(type))
                return false;
            try
            {
                if (type >= ItemID.Sets.BannerStrength.Length)
                    return false;
                ItemID.BannerEffect be = ItemID.Sets.BannerStrength[type];
                return !be.Enabled && be.NormalDamageDealt > 0f;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsWiringOrStatueItem(int type, Item probe, int createTile)
        {
            try
            {
                if (type > 0 && type < ItemID.Sets.SortingPriorityWiring.Length && ItemID.Sets.SortingPriorityWiring[type] > -1)
                    return true;
            }
            catch
            {
                /* */
            }

            if (probe.mech)
                return true;

            if (createTile >= 0 && ItemHubStatuePairCache.IsStatuePair(createTile, probe.placeStyle))
                return true;

            return false;
        }

        private static bool SafeIsWhip(Item probe)
        {
            int s = probe.shoot;
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

        /// <summary>
        /// ����/���ԣ��ǲ����ҹ���ǰ�����б���? Deprecated��
        /// ��������;�о�Ŀ¼ȱʧ�ж������򼸺�ȫ��ģ�����������Բ����ֿղۣ���
        /// </summary>
        private static bool ComputeDebug(int type, Item probe, bool[] deprecatedSnapshot, bool materialUpstreamResearchable)
        {
            if (type <= ItemID.None)
                return false;
            if (materialUpstreamResearchable || probe.material)
                return false;

            return deprecatedSnapshot != null &&
                type < deprecatedSnapshot.Length &&
                deprecatedSnapshot[type];
        }
    }

    /// <summary>�������� <see cref="Item.useAmmo"/> �����õĵ�ҩ���ͣ��� AmmoID / ��־����Ʒ type һ�£������ڡ�����Ϊ��ҩ����չ���ࡣ</summary>
    internal static class ItemHubAmmoTypeReferencedByWeaponsCache
    {
        private static HashSet<int> _types = new HashSet<int>();
        private static bool _built;

        public static void Reset()
        {
            _built = false;
            _types.Clear();
        }

        public static void EnsureBuilt(int maxTypes)
        {
            if (_built)
                return;
            _built = true;
            Item p = new Item();
            for (int i = 1; i < maxTypes; i++)
            {
                try
                {
                    p.SetDefaults(i);
                }
                catch
                {
                    continue;
                }

                if (p.IsAir || p.damage <= 0 || p.useAmmo <= AmmoID.None)
                    continue;
                _types.Add(p.useAmmo);
            }
        }

        public static bool Contains(int itemType) => _types.Contains(itemType);
    }

    /// <summary>�� GenVars.statueList Ԥ���� HashSet�������ÿ����Ʒ����ɨ�������</summary>
    internal static class ItemHubStatuePairCache
    {
        private static HashSet<long> _pairs;

        public static void Clear() => _pairs = null;

        public static bool IsStatuePair(int tileId, int placeStyle)
        {
            if (tileId < 0)
                return false;
            Ensure();
            return _pairs.Contains(Pack(tileId, placeStyle));
        }

        private static long Pack(int t, int s) => ((long)t << 32) | (uint)s;

        private static void Ensure()
        {
            if (_pairs != null)
                return;
            _pairs = new HashSet<long>();
            if (GenVars.statueList == null)
                WorldGen.SetupStatueList();
            foreach (Point16 p in GenVars.statueList)
                _pairs.Add(Pack(p.X, p.Y));
        }
    }
}
