using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Combat
{
    /// <summary>ս���ٻ����ʹ�/�ڱ���Buff ����Ʒӳ�䣬������ȷά�� maxMinions / maxTurrets��</summary>
    public static class BuffCombatSummonSystem
    {
        private static readonly Dictionary<int, int> BuffToItemType = new Dictionary<int, int>();

        public static void RebuildItemMap()
        {
            BuffToItemType.Clear();
            foreach (int buffId in BuffCategoryIndexSystem.MinionBuffIds)
                MapItemForBuff(buffId);

            foreach (int buffId in BuffCategoryIndexSystem.SentryBuffIds)
                MapItemForBuff(buffId);
        }

        public static bool TryGetSummonItem(int buffId, out int itemType) =>
            BuffToItemType.TryGetValue(buffId, out itemType);

        /// <summary>�� Buff �����ٻ����ȡռ�õ��ʹ���λ����</summary>
        public static bool TryGetMinionSlotCost(int buffId, out float slotCost)
        {
            slotCost = 1f;
            if (BuffCategoryIndexSystem.GetCategory(buffId) != BuffCategories.Minion)
                return false;

            if (!TryGetSummonItem(buffId, out int itemType) || itemType <= ItemID.None)
                return false;

            Item probe = new Item();
            probe.SetDefaults(itemType);
            if (probe.shoot <= ProjectileID.None)
                return false;

            if (ContentSamples.ProjectilesByType.TryGetValue(probe.shoot, out Projectile sample) && sample != null)
                slotCost = Math.Max(0.25f, sample.minionSlots);

            return true;
        }

        public static void OnExclusiveBuffEnabled(Player player, BuffResearchPlayer modPlayer, int buffId, string category)
        {
            if (player == null || modPlayer == null || buffId <= 0)
                return;

            ClearOthersInCategory(modPlayer, buffId, category);
            ClearCategoryEntities(player, category);
            modPlayer.SetTrackedCombatSummonBuff(category, buffId);
            ApplyBuffAndFillSlots(player, buffId, category);
        }

        public static void OnExclusiveBuffDisabled(Player player, BuffResearchPlayer modPlayer, string category)
        {
            if (player == null || modPlayer == null)
                return;

            int buffId = modPlayer.GetTrackedCombatSummonBuff(category);
            if (buffId > 0)
                BuffResearchPlayer.ClearManagedBuff(player, buffId);

            ClearCategoryEntities(player, category);
            modPlayer.SetTrackedCombatSummonBuff(category, 0);
        }

        public static void Maintain(Player player, BuffResearchPlayer modPlayer)
        {
            if (player == null || modPlayer == null)
                return;

            MaintainCategory(player, modPlayer, BuffCategories.Minion);
            MaintainCategory(player, modPlayer, BuffCategories.Sentry);
        }

        private static void MaintainCategory(Player player, BuffResearchPlayer modPlayer, string category)
        {
            int desiredBuff = GetActiveBuffInCategory(modPlayer, category);
            int tracked = modPlayer.GetTrackedCombatSummonBuff(category);

            if (desiredBuff <= 0)
            {
                if (tracked > 0)
                {
                    BuffResearchPlayer.ClearManagedBuff(player, tracked);
                    ClearCategoryEntities(player, category);
                    modPlayer.SetTrackedCombatSummonBuff(category, 0);
                }

                return;
            }

            if (tracked != desiredBuff)
            {
                if (tracked > 0)
                    BuffResearchPlayer.ClearManagedBuff(player, tracked);

                ClearCategoryEntities(player, category);
                modPlayer.SetTrackedCombatSummonBuff(category, desiredBuff);
                EmojLog.Info(EmojLogChannel.Summon,
                    $"tracked {category} {tracked} -> {desiredBuff} maxMinions={player.maxMinions}");
                ApplyBuffAndFillSlots(player, desiredBuff, category);
                return;
            }

            if (!BuffResearchPlayer.PlayerHasBuff(player, desiredBuff))
                ApplyBuffAndFillSlots(player, desiredBuff, category);
            else
                TryFillSlots(player, desiredBuff, category);
        }

        private static int GetActiveBuffInCategory(BuffResearchPlayer modPlayer, string category)
        {
            foreach (int buffId in modPlayer.ActiveBuffs)
            {
                if (modPlayer.DisabledBuffs.Contains(buffId))
                    continue;

                if (BuffCategoryIndexSystem.GetCategory(buffId) == category)
                    return buffId;
            }

            return 0;
        }

        private static void ClearOthersInCategory(BuffResearchPlayer modPlayer, int keepBuffId, string category)
        {
            var remove = new List<int>();
            foreach (int buffId in modPlayer.ActiveBuffs)
            {
                if (buffId == keepBuffId)
                    continue;

                if (BuffCategoryIndexSystem.GetCategory(buffId) == category)
                    remove.Add(buffId);
            }

            foreach (int buffId in remove)
            {
                modPlayer.ActiveBuffs.Remove(buffId);
                BuffResearchPlayer.ClearManagedBuff(Main.LocalPlayer, buffId);
            }
        }

        private static void ApplyBuffAndFillSlots(Player player, int buffId, string category)
        {
            if (!BuffResearchPlayer.PlayerHasBuff(player, buffId))
                player.AddBuff(buffId, BuffResearchPlayer.ActiveBuffDurationFrames, true, false);

            if (buffId > 0 && buffId < Main.buffNoTimeDisplay.Length)
                Main.buffNoTimeDisplay[buffId] = true;

            TryFillSlots(player, buffId, category);
        }

        private static void TryFillSlots(Player player, int buffId, string category)
        {
            if (!TryGetSummonItem(buffId, out int itemType) || itemType <= ItemID.None)
                return;

            Item item = CreateSummonItem(itemType);
            if (category == BuffCategories.Sentry)
                FillSentrySlots(player, item);
            else
                FillMinionSlots(player, item);
        }

        private static Item CreateSummonItem(int itemType)
        {
            Item item = new Item();
            item.SetDefaults(itemType);
            item.Prefix(PrefixID.Ruthless);
            return item;
        }

        private static void FillMinionSlots(Player player, Item item)
        {
            if (item.shoot <= ProjectileID.None)
                return;

            float slotCost = 1f;
            if (ContentSamples.ProjectilesByType.TryGetValue(item.shoot, out Projectile sample) && sample != null)
                slotCost = Math.Max(0.25f, sample.minionSlots);

            BuffVirtualEffectSummonGuard.Clamp(player);

            float predictedSlots = player.slotsMinions;
            int maxAttempts = Math.Min(Math.Max(player.maxMinions + 2, 4), 16);
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (predictedSlots >= player.maxMinions)
                    break;

                if (!SpawnMinionProjectile(player, item))
                    break;

                predictedSlots += slotCost;
            }
        }

        private static void FillSentrySlots(Player player, Item item)
        {
            if (item.shoot <= ProjectileID.None)
                return;

            BuffVirtualEffectSummonGuard.Clamp(player);

            int predicted = CountPlayerSentries(player);
            int maxAttempts = Math.Min(Math.Max(player.maxTurrets + 2, 4), 16);
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (predicted >= player.maxTurrets)
                    break;

                if (!SpawnSentryProjectile(player, item))
                    break;

                predicted++;
            }
        }

        private static bool SpawnMinionProjectile(Player player, Item item)
        {
            int type = item.shoot;
            if (type <= ProjectileID.None || type >= ProjectileLoader.ProjectileCount)
                return false;

            if (TryEmpowerSegmentMinion(player, type))
                return true;

            int damage = player.GetWeaponDamage(item);
            float knockback = player.GetWeaponKnockback(item);
            IEntitySource source = player.GetSource_ItemUse(item);
            Vector2 spawn = player.Center + new Vector2(Main.rand.NextFloat(-12f, 12f), Main.rand.NextFloat(-8f, 8f));
            int projIndex = Projectile.NewProjectile(source, spawn, Vector2.Zero, type, damage, knockback, player.whoAmI);
            if (projIndex < 0 || projIndex >= Main.maxProjectiles)
                return false;

            Projectile proj = Main.projectile[projIndex];
            proj.originalDamage = damage;
            proj.minion = true;
            return true;
        }

        private static bool TryEmpowerSegmentMinion(Player player, int projectileType)
        {
            if (!IsSegmentMinionHead(projectileType))
                return false;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.owner != player.whoAmI || p.type != projectileType)
                    continue;

                p.minionSlots += 1f;
                return true;
            }

            return false;
        }

        private static bool IsSegmentMinionHead(int projectileType) =>
            projectileType == ProjectileID.StardustDragon1 || projectileType == ProjectileID.StormTigerGem;

        private static bool SpawnSentryProjectile(Player player, Item item)
        {
            int type = item.shoot;
            if (type <= ProjectileID.None || type >= ProjectileLoader.ProjectileCount)
                return false;

            Vector2 feet = player.Bottom;
            int tileX = (int)(feet.X / 16f);
            int tileY = (int)(feet.Y / 16f);
            Vector2 spawn = new Vector2(tileX * 16f + 8f, tileY * 16f - 8f);

            int damage = player.GetWeaponDamage(item);
            float knockback = player.GetWeaponKnockback(item);
            IEntitySource source = player.GetSource_ItemUse(item);
            int projIndex = Projectile.NewProjectile(source, spawn, Vector2.Zero, type, damage, knockback, player.whoAmI);
            if (projIndex < 0 || projIndex >= Main.maxProjectiles)
                return false;

            Projectile proj = Main.projectile[projIndex];
            proj.originalDamage = damage;
            proj.sentry = true;
            return true;
        }

        private static int CountPlayerSentries(Player player)
        {
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.sentry)
                    count++;
            }

            return count;
        }

        public static void ClearCategoryEntities(Player player, string category)
        {
            if (player == null)
                return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != player.whoAmI)
                    continue;

                if (category == BuffCategories.Sentry)
                {
                    if (proj.sentry)
                        proj.Kill();
                }
                else if (proj.minion && !proj.sentry)
                {
                    proj.Kill();
                }
            }
        }

        private static void MapItemForBuff(int buffId)
        {
            string category = BuffCategoryIndexSystem.GetCategory(buffId);
            bool wantSentry = category == BuffCategories.Sentry;
            int bestType = ItemID.None;
            bool bestVanilla = false;

            foreach (var pair in ContentSamples.ItemsByType)
            {
                Item item = pair.Value;
                if (item == null || item.IsAir || item.buffType != buffId)
                    continue;

                if (item.mountType != -1)
                    continue;

                bool isSentryItem = BuffSummonProjectileHelper.ItemShootIsSentry(item);
                if (wantSentry && !isSentryItem)
                    continue;

                if (!wantSentry && isSentryItem)
                    continue;

                bool vanilla = item.type < ItemID.Count;
                if (bestType <= ItemID.None || (!bestVanilla && vanilla))
                {
                    bestType = item.type;
                    bestVanilla = vanilla;
                }
            }

            if (bestType > ItemID.None)
                BuffToItemType[buffId] = bestType;
        }
    }
}
