using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Virtual
{
    /// <summary>
    /// ���� Buff��ƽ��ģʽ�����Զӣ�ÿ֡������Ч�ӣ���Ƶ����ͳһģʽÿ֡ȫ�б��?
    /// ʩ��ʹ�� <see cref="ModBuff.Update"/>������ <see cref="Player.UpdateBuffs"/>��
    /// </summary>
    public static class BuffVirtualEffectSystem
    {
        private const int VirtualScratchDuration = 36000;

        public static readonly HashSet<int> VanillaPhysicalOnlyBuffs = new HashSet<int>();

        private static bool[] _projActiveBefore;
        private static uint _lastVirtualApplyFrame = uint.MaxValue;

        static BuffVirtualEffectSystem()
        {
            AddVanillaPhysicalOnly(
                BuffID.WellFed,
                BuffID.WellFed2,
                BuffID.WellFed3,
                BuffID.Sonar,
                BuffID.WaterCandle,
                BuffID.PeaceCandle,
                BuffID.ShadowCandle,
                BuffID.Invisibility,
                BuffID.PaladinsShield);

            foreach (FieldInfo field in typeof(BuffID).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.Name.StartsWith("WeaponImbue", System.StringComparison.Ordinal))
                    continue;

                if (field.FieldType == typeof(int))
                {
                    int id = (int)field.GetValue(null);
                    if (id > 0)
                        VanillaPhysicalOnlyBuffs.Add(id);
                }
                else if (field.FieldType == typeof(short))
                {
                    int id = (short)field.GetValue(null);
                    if (id > 0)
                        VanillaPhysicalOnlyBuffs.Add(id);
                }
            }
        }

        private static void AddVanillaPhysicalOnly(params int[] buffIds)
        {
            foreach (int buffId in buffIds)
            {
                if (buffId > 0)
                    VanillaPhysicalOnlyBuffs.Add(buffId);
            }
        }

        private static void SnapshotProjectiles()
        {
            if (_projActiveBefore == null || _projActiveBefore.Length != Main.maxProjectiles)
                _projActiveBefore = new bool[Main.maxProjectiles];

            for (int i = 0; i < Main.maxProjectiles; i++)
                _projActiveBefore[i] = Main.projectile[i].active;
        }

        private static bool NeedsProjectileGuard(int buffId) =>
            BuffEntityIndexSystem.IsEntitySpawningBuff(buffId) ||
            BuffEntityIndexSystem.NonMaintainableBuffIds.Contains(buffId);

        private static void TryConsumeNewPlayerProjectile(Player player, int buffId, ref bool needsQueueRebuild)
        {
            bool logged = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || _projActiveBefore[i] || p.owner != player.whoAmI)
                    continue;

                p.Kill();
                needsQueueRebuild = true;
                BuffEntityIndexSystem.NonMaintainableBuffIds.Add(buffId);

                if (!logged)
                {
                    logged = true;
                    EmojLog.WarnFull(EmojLogChannel.Virtual,
                        $"virtual apply spawned projectile; blacklist buff {Lang.GetBuffName(buffId)} id={buffId}");
                }
            }
        }

        public static bool UsesVirtualEffect(int buffId) => UsesVirtualEffect(buffId, null);

        /// <summary>���������� scratch���� Buff �Ƿ����? scratch������ �� ͼ������ȣ���?</summary>
        public static bool WouldUseVirtualEffect(int buffId, BuffResearchPlayer modPlayer) =>
            QualifiesForVirtualEffect(buffId, modPlayer);

        public static bool UsesVirtualEffect(int buffId, BuffResearchPlayer modPlayer)
        {
            if (!OPJourneyConfig.UseVirtualScratchApply())
                return false;

            return QualifiesForVirtualEffect(buffId, modPlayer);
        }

        private static bool QualifiesForVirtualEffect(int buffId, BuffResearchPlayer modPlayer)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (modPlayer != null && modPlayer.PinnedPhysicalBuffs.Contains(buffId))
                return false;

            if (VanillaPhysicalOnlyBuffs.Contains(buffId))
                return false;

            if (VanillaBuffStatRegistry.IsSyntheticStatBuff(buffId))
                return false;

            if (IsEntityManagedBuff(buffId))
                return false;

            if (BuffCombatSummonClassifier.IsMinionBuff(buffId) || BuffCombatSummonClassifier.IsSentryBuff(buffId))
                return false;

            if (BuffEntityIndexSystem.IsSummonMinionBuff(buffId))
                return false;

            if (buffId >= BuffID.Count)
            {
                ModBuff modBuff = BuffLoader.GetBuff(buffId);
                if (BuffEntityIndexSystem.ModBuffNamespaceIndicatesEntitySpawn(modBuff))
                    return false;
            }

            if (!BuffVirtualEffectSafety.IsSafeForVirtualApply(buffId))
                return false;

            if (BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId))
                return false;

            string category = BuffPage.GetBuffCategoryStatic(buffId);
            return category == BuffCategories.Positive || BuffCategories.IsVirtualizablePositiveSubCategory(category);
        }

        public static bool IsEntityManagedBuff(int buffId)
        {
            if (BuffMountCategorySystem.IsMountBuff(buffId) || BuffMountCategorySystem.IsMinecartBuff(buffId))
                return true;

            if (BuffMountCategorySystem.TryResolveMountCategory(buffId, out _))
                return true;

            if (BuffMiscEquipIndexSystem.GetMiscEquipSlotIndex(BuffPage.GetBuffCategoryStatic(buffId)) >= 0)
                return true;

            return BuffEntityIndexSystem.IsEntitySpawningBuff(buffId);
        }

        private static int _lastLoggedQueueCount = -1;

        public static void RebuildVirtualQueue(BuffResearchPlayer mp, bool force = false)
        {
            if (!force && !mp.VirtualQueueDirty)
                return;

            int prevTotal = mp.VirtualEffectQueue.Count;
            mp.VirtualEffectQueue.Clear();
            mp.VirtualStatQueue.Clear();
            mp.VirtualCombatVisualQueue.Clear();

            bool balanced = OPJourneyConfig.UseBalancedVirtualQueues();

            foreach (int buffId in mp.ActiveBuffs)
            {
                if (mp.DisabledBuffs.Contains(buffId))
                    continue;

                if (!WouldUseVirtualEffect(buffId, mp))
                    continue;

                mp.VirtualEffectQueue.Add(buffId);

                if (!balanced || !OPJourneyConfig.UseVirtualScratchApply())
                    continue;

                if (BuffVirtualEffectClassifier.GetPhase(buffId) == BuffVirtualEffectPhase.CombatVisual)
                    mp.VirtualCombatVisualQueue.Add(buffId);
                else
                    mp.VirtualStatQueue.Add(buffId);
            }

            mp.OnVirtualQueueRebuilt();

            if (EmojLog.IsFullMode && mp.VirtualEffectQueue.Count != _lastLoggedQueueCount)
            {
                _lastLoggedQueueCount = mp.VirtualEffectQueue.Count;
                string mode = balanced ? "balanced" : "unified";
                EmojLog.InfoFull(EmojLogChannel.Virtual,
                    $"virtual queue rebuilt total={mp.VirtualEffectQueue.Count} stat={mp.VirtualStatQueue.Count} combat={mp.VirtualCombatVisualQueue.Count} (was {prevTotal}) mode={mode} aggregate={mp.UseAggregatedVirtualBarDisplay()}");
            }
        }

        /// <summary>ÿ֡���ִ��һ�Σ���? <see cref="BuffResearchPlayer.PreUpdateBuffs"/> ���á�</summary>
        public static void ApplyAllVirtualEffects(Player player, BuffResearchPlayer mp)
        {
            if (player == null || mp == null)
                return;

            if (_lastVirtualApplyFrame == Main.GameUpdateCount)
                return;

            if (mp.VirtualEffectQueue.Count == 0)
                return;

            _lastVirtualApplyFrame = Main.GameUpdateCount;

            if (!OPJourneyConfig.UseVirtualScratchApply())
                return;

            if (OPJourneyConfig.UseBalancedVirtualQueues())
            {
                ApplyVirtualQueue(player, mp, mp.VirtualStatQueue, OPJourneyConfig.GetStatUpdateSpreadFrames());

                if (ShouldApplyCombatVisualThisFrame(player))
                    ApplyVirtualQueue(player, mp, mp.VirtualCombatVisualQueue, 1);
            }
            else
            {
                ApplyVirtualQueue(player, mp, mp.VirtualEffectQueue, 1);
            }
        }

        private static bool ShouldApplyCombatVisualThisFrame(Player player)
        {
            int interval = OPJourneyConfig.GetCombatVisualIntervalFrames();
            int phase = player.whoAmI % interval;
            return Main.GameUpdateCount % interval == phase;
        }

        private static void ApplyVirtualQueue(Player player, BuffResearchPlayer mp, IReadOnlyList<int> queue, int spreadFrames)
        {
            int queueCount = queue.Count;
            if (queueCount == 0)
                return;

            int spread = spreadFrames < 1 ? 1 : spreadFrames;
            int spreadPhase = spread <= 1 ? 0 : (int)(Main.GameUpdateCount % spread);

            int slotCount = player.buffType.Length;
            if (slotCount <= 0)
                return;

            int batchSize = System.Math.Min(Player.MaxBuffs, slotCount);
            if (batchSize <= 0)
                return;

            int[] savedType = new int[slotCount];
            int[] savedTime = new int[slotCount];
            System.Array.Copy(player.buffType, savedType, slotCount);
            System.Array.Copy(player.buffTime, savedTime, slotCount);

            BuffVirtualEffectSummonGuard summonGuard = BuffVirtualEffectSummonGuard.Capture(player);
            mp.IsApplyingVirtualBuffEffects = true;
            bool needsQueueRebuild = false;

            try
            {
                for (int start = 0; start < queueCount; start += batchSize)
                {
                    int chunk = System.Math.Min(batchSize, queueCount - start);

                    for (int j = 0; j < slotCount; j++)
                    {
                        if (j < chunk)
                        {
                            int buffId = queue[start + j];
                            player.buffType[j] = buffId;
                            player.buffTime[j] = VirtualScratchDuration;
                        }
                        else
                        {
                            player.buffType[j] = 0;
                            player.buffTime[j] = 0;
                        }
                    }

                    for (int j = 0; j < chunk; j++)
                    {
                        int globalIndex = start + j;
                        if (spread > 1 && globalIndex % spread != spreadPhase)
                            continue;

                        int buffId = queue[globalIndex];
                        if (IsEntityManagedBuff(buffId))
                            continue;

                        ApplyVirtualBuffUpdate(player, buffId, j, ref needsQueueRebuild);
                    }
                }
            }
            finally
            {
                mp.IsApplyingVirtualBuffEffects = false;
                System.Array.Copy(savedType, player.buffType, slotCount);
                System.Array.Copy(savedTime, player.buffTime, slotCount);
                summonGuard.Restore(player);
                BuffVirtualEffectSummonGuard.Clamp(player);
                BuffEmoteGuardSystem.SuppressPlayerEmotes(player);

                if (needsQueueRebuild)
                    RebuildVirtualQueue(mp, force: true);
            }
        }

        private static void ApplyVirtualBuffUpdate(Player player, int buffId, int buffIndex, ref bool needsQueueRebuild)
        {
            ModBuff buff = BuffLoader.GetBuff(buffId);
            if (buff == null)
                return;

            try
            {
                if (NeedsProjectileGuard(buffId))
                {
                    SnapshotProjectiles();
                    buff.Update(player, ref buffIndex);
                    TryConsumeNewPlayerProjectile(player, buffId, ref needsQueueRebuild);
                }
                else
                {
                    buff.Update(player, ref buffIndex);
                }
            }
            catch
            {
            }
        }

        public static void ApplyBuffImmediately(Player player, BuffResearchPlayer mp, int buffId)
        {
            if (player == null || mp == null || mp.DisabledBuffs.Contains(buffId))
                return;

            if (BuffVirtualEffectSafety.PrefersContinuousPhysicalBar(buffId))
            {
                player.AddBuff(buffId, BuffResearchPlayer.ActiveBuffDurationFrames, quiet: true, foodHack: false);
                return;
            }

            if (!WouldUseVirtualEffect(buffId, mp))
                return;

            if (!OPJourneyConfig.UseVirtualScratchApply())
            {
                player.AddBuff(buffId, BuffResearchPlayer.ActiveBuffDurationFrames, quiet: true, foodHack: false);
                return;
            }

            RebuildVirtualQueue(mp, force: true);
            _lastVirtualApplyFrame = uint.MaxValue;
            ApplyAllVirtualEffects(player, mp);
        }
    }
}
