using System;
using System.Collections.Generic;
using System.Linq;
using EvenMoreOverpoweredJourney.Integration.ImproveGame;
using EvenMoreOverpoweredJourney.Integration.Session;
using EvenMoreOverpoweredJourney.Integration.Browser;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using EvenMoreOverpoweredJourney.Buffs.Content;
using EvenMoreOverpoweredJourney.Buffs.Systems.Catalog;
using EvenMoreOverpoweredJourney.Buffs.Systems.Virtual;
using EvenMoreOverpoweredJourney.Buffs.Systems.Managed;
using EvenMoreOverpoweredJourney.Buffs.Systems.Combat;
using EvenMoreOverpoweredJourney.Buffs.Systems.Spawning;
using EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus;
using EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport;
using EvenMoreOverpoweredJourney.Buffs.Systems.FedState;
using EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics;
using EvenMoreOverpoweredJourney.Buffs.Systems.Display;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.Players
{
    public class BuffResearchPlayer : ModPlayer
    {
        private const int ActiveBuffDuration = 3600;

        public const int ActiveBuffDurationFrames = ActiveBuffDuration;
        private const int MiscEquipSlotCount = 4;

        /// <summary>����ҳ���� Buff �ﵽ����������������/����ȣ�ʱ����? Alpha �ۺ�ͼ�ꡣ</summary>
        public const int BarDisplayAggregateThreshold = 20;

        public HashSet<int> UnlockedBuffs = new HashSet<int>();

        /// <summary>���� DEBUG_UNLOCKALLBUFFS ǰ����ʵ�������գ������ڼ䲻д <see cref="UnlockedBuffs"/>��</summary>
        public HashSet<int> DebugUnlockBaseline;

        public HashSet<int> ActiveBuffs = new HashSet<int>();
        public HashSet<int> DisabledBuffs = new HashSet<int>();

        /// <summary>����м��̶���ǿ�ƹ���ԭ��״̬������������ʩ�ӡ�?</summary>
        public HashSet<int> PinnedPhysicalBuffs = new HashSet<int>();

        /// <summary>������������۵�������������� Buff ������</summary>
        public bool NeedsBuffUiCollapseForNewWorld;

        public int TrackedMinionBuffId;
        public int TrackedSentryBuffId;

        public Item[] SavedMiscEquips = new Item[MiscEquipSlotCount];
        public bool[] HasSavedMisc = new bool[MiscEquipSlotCount];

        /// <summary>��һ֡��������Ƿ���ڸ� Buff�����ڼ��ԭ���Ҽ�ȡ��? misc �� Buff��</summary>
        public bool[] HadBuffLastFrame;

        /// <summary>ȫ������ Buff������ + ��Ч�������ڼ���/����/ͳһģʽ��</summary>
        public readonly List<int> VirtualEffectQueue = new List<int>();

        /// <summary>ƽ��ģʽ��ÿ֡ PreUpdate ȫ��ʩ�ӡ�</summary>
        public readonly List<int> VirtualStatQueue = new List<int>();

        /// <summary>ƽ��ģʽ����Ƶʩ�ӣ������� CombatVisualUpdateInterval����</summary>
        public readonly List<int> VirtualCombatVisualQueue = new List<int>();

        /// <summary>����ʩ���У����� UpdateBuffs Hook ��ÿ֡����ɨ�衣</summary>
        public bool IsApplyingVirtualBuffEffects;

        public bool VirtualQueueDirty = true;

        /// <summary>��������������ڲ�? AddBuff/���滻 misc �ۣ���������ʵ����ˡ�?</summary>
        public int WorldTransitionGraceFrames;

        private bool _barDisplayDirty = true;

        private int _cachedVirtualBarCount = -1;
        private readonly HashSet<int> _cachedVirtualBuffIds = new HashSet<int>();

        public void NotifyBuffRuntimeStateChanged()
        {
            VirtualQueueDirty = true;
            _barDisplayDirty = true;
            _cachedVirtualBarCount = -1;
        }

        public bool IsInWorldTransitionGrace => WorldTransitionGraceFrames > 0;

        public void BeginWorldTransitionGrace() => WorldTransitionGraceFrames = 120;

        public void OnVirtualQueueRebuilt()
        {
            _cachedVirtualBarCount = VirtualEffectQueue.Count;
            VirtualQueueDirty = false;

            _cachedVirtualBuffIds.Clear();
            foreach (int buffId in VirtualEffectQueue)
                _cachedVirtualBuffIds.Add(buffId);
        }

        public int VirtualStatQueueCount => VirtualStatQueue.Count;

        public int VirtualCombatVisualQueueCount => VirtualCombatVisualQueue.Count;

        public bool IsCachedVirtualBuff(int buffId)
        {
            if (VirtualQueueDirty)
                return BuffVirtualEffectSystem.WouldUseVirtualEffect(buffId, this);

            return _cachedVirtualBuffIds.Contains(buffId);
        }

        public override void Initialize()
        {
            for (int i = 0; i < MiscEquipSlotCount; i++)
                SavedMiscEquips[i] = new Item();
        }

        public override void SaveData(TagCompound tag)
        {
            tag["UnlockedBuffKeys"] = KeysForPersistence(UnlockedBuffs, forActive: false);
            tag["ActiveBuffKeys"] = KeysForPersistence(ActiveBuffs, forActive: true);
            tag["DisabledBuffKeys"] = KeysForPersistence(DisabledBuffs, forActive: false);
            tag["PinnedPhysicalBuffKeys"] = KeysForPersistence(PinnedPhysicalBuffs, forActive: false);

            tag["UnlockedBuffs"] = FilterLegacyInts(UnlockedBuffs, forActive: false);
            tag["ActiveBuffs"] = FilterLegacyInts(ActiveBuffs, forActive: true);
            tag["DisabledBuffs"] = FilterLegacyInts(DisabledBuffs, forActive: false);
            tag["PinnedPhysicalBuffs"] = FilterLegacyInts(PinnedPhysicalBuffs, forActive: false);
        }

        private static List<string> KeysForPersistence(HashSet<int> ids, bool forActive)
        {
            var keys = new List<string>();
            foreach (int id in ids)
            {
                if (!ShouldPersistBuffId(id, forActive))
                    continue;

                string key = BuffStableKey.ToKey(id);
                if (!string.IsNullOrEmpty(key) && !keys.Contains(key))
                    keys.Add(key);
            }

            return keys;
        }

        private static List<int> FilterLegacyInts(HashSet<int> ids, bool forActive)
        {
            var list = new List<int>();
            foreach (int id in ids)
            {
                if (ShouldPersistBuffId(id, forActive))
                    list.Add(id);
            }

            return list;
        }

        private static bool ShouldPersistBuffId(int id, bool forActive)
        {
            if (!IsValidPersistentBuffId(id))
                return false;

            if (forActive && BuffEntityIndexSystem.RequiresManualEntityManagement(id))
                return false;

            return true;
        }

        private static void ImportBuffKeys(TagCompound tag, string keyName, HashSet<int> target)
        {
            if (!tag.ContainsKey(keyName))
                return;

            foreach (string key in tag.GetList<string>(keyName))
            {
                if (BuffStableKey.TryResolve(key, out int buffId) && IsValidPersistentBuffId(buffId))
                    target.Add(buffId);
            }
        }

        private static void ImportLegacyInts(TagCompound tag, string keyName, HashSet<int> target)
        {
            if (!tag.ContainsKey(keyName))
                return;

            foreach (int id in tag.GetList<int>(keyName))
            {
                if (IsValidPersistentBuffId(id))
                    target.Add(id);
            }
        }

        /// <summary>UI/�����Ƿ���Ϊ�ѽ������� DEBUG ��ʱȫ������</summary>
        public bool IsBuffUnlocked(int buffId) =>
            buffId > 0 && (SuperAdminSession.DebugUnlockAllBuffs || HasPermanentUnlock(buffId));

        /// <summary>�浵�е����ý��������� DEBUG Ӱ�졣</summary>
        public bool HasPermanentUnlock(int buffId) =>
            buffId > 0 && UnlockedBuffs.Contains(buffId);

        /// <summary>�����ʵ���Ƴ�? Buff������ԭ <see cref="Main.buffNoTimeDisplay"/>��</summary>
        public static void ClearManagedBuff(Player player, int buffId)
        {
            if (buffId <= 0)
                return;

            player.ClearBuff(buffId);
            if (buffId < Main.buffNoTimeDisplay.Length)
                Main.buffNoTimeDisplay[buffId] = false;
        }

        public override void LoadData(TagCompound tag)
        {
            UnlockedBuffs = new HashSet<int>();
            ActiveBuffs = new HashSet<int>();
            DisabledBuffs = new HashSet<int>();
            PinnedPhysicalBuffs = new HashSet<int>();

            bool hasStableKeys = tag.ContainsKey("UnlockedBuffKeys") ||
                                 tag.ContainsKey("ActiveBuffKeys") ||
                                 tag.ContainsKey("DisabledBuffKeys");

            if (hasStableKeys)
            {
                ImportBuffKeys(tag, "UnlockedBuffKeys", UnlockedBuffs);
                ImportBuffKeys(tag, "ActiveBuffKeys", ActiveBuffs);
                ImportBuffKeys(tag, "DisabledBuffKeys", DisabledBuffs);
                ImportBuffKeys(tag, "PinnedPhysicalBuffKeys", PinnedPhysicalBuffs);
            }
            else
            {
                ImportLegacyInts(tag, "UnlockedBuffs", UnlockedBuffs);
                ImportLegacyInts(tag, "ActiveBuffs", ActiveBuffs);
                ImportLegacyInts(tag, "DisabledBuffs", DisabledBuffs);
            }

            ImportLegacyInts(tag, "PinnedPhysicalBuffs", PinnedPhysicalBuffs);

            SanitizePersistentSets();
            NotifyBuffRuntimeStateChanged();

            for (int i = 0; i < MiscEquipSlotCount; i++)
                HasSavedMisc[i] = false;
        }

        public override void OnRespawn()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (BuffInfrastructureSettings.ShouldReapplyBuffsOnRespawn())
                NotifyBuffRuntimeStateChanged();
        }

        public override void OnEnterWorld()
        {
            ImproveGameIntegration.Refresh();
            SanitizePersistentSets();

            if (!SuperAdminSession.DebugUnlockAllBuffs)
            {
                SyncUnlocksFromPlayerBar();
                EnforcePermanentUnlockConsistency(purgePlayerBar: true);
            }
            else if (DebugUnlockBaseline == null)
                CaptureDebugUnlockBaseline();

            if (BuffInfrastructureSettings.ShouldReapplyBuffsOnWorldEnter())
                NotifyBuffRuntimeStateChanged();

            NeedsBuffUiCollapseForNewWorld = true;
            SetBonusHookSystem.ResetRuntimeState();
            BuffFedStateCompat.ResetSessionDiagnostics();
            if (Player.whoAmI == Main.myPlayer)
            {
                BuffWorldTransitionCleanup.OnPlayerEnterWorld(Player, this);
                BuffVirtualEffectSummonGuard.Clamp(Player);
                BuffFedStateCompat.ApplySatietyAfterBuffPipeline(Player, this);
                BuffEmoteGuardSystem.SuppressPlayerEmotes(Player);
            }
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (WorldTransitionGraceFrames > 0)
                WorldTransitionGraceFrames--;

            BuffFedStateCompat.ApplySatietyAfterBuffPipeline(Player, this);
        }

        public void SetTrackedCombatSummonBuff(string category, int buffId)
        {
            if (category == BuffCategories.Minion)
                TrackedMinionBuffId = buffId;
            else if (category == BuffCategories.Sentry)
                TrackedSentryBuffId = buffId;
        }

        public int GetTrackedCombatSummonBuff(string category)
        {
            if (category == BuffCategories.Minion)
                return TrackedMinionBuffId;

            if (category == BuffCategories.Sentry)
                return TrackedSentryBuffId;

            return 0;
        }

        public static bool PlayerHasBuff(Player player, int buffType)
        {
            if (player == null || buffType <= 0)
                return false;

            int buffSlotCount = Math.Min(Player.MaxBuffs, Math.Min(player.buffType.Length, player.buffTime.Length));
            for (int buffSlot = 0; buffSlot < buffSlotCount; buffSlot++)
            {
                if (player.buffType[buffSlot] == buffType && player.buffTime[buffSlot] > 0)
                    return true;
            }

            return false;
        }

        /// <summary>���ý�������׷�ӡ�����������DEBUG ģʽ��д�浵��</summary>
        public void TryGrantPermanentUnlock(int buffId)
        {
            if (SuperAdminSession.DebugUnlockAllBuffs)
                return;

            if (!IsDiscoverableBuffId(buffId))
                return;

            UnlockedBuffs.Add(buffId);
        }

        /// <summary>???????????? <see cref="IsValidPersistentBuffId"/> ????</summary>
        public static bool IsDiscoverableBuffId(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (IsEmojInternalBuff(buffId))
                return false;

            if (BuffFedStateCompat.IsHungerDebuff(buffId))
                return false;

            return BuffPlayerApplicability.IsMeantForPlayer(buffId);
        }

        private bool IsManagedPlayerBarBuff(int buffId) =>
            ActiveBuffs.Contains(buffId) || DisabledBuffs.Contains(buffId);

        /// <summary>���� DEBUG_UNLOCKALLBUFFS��������ʵ�����������й��б���״̬������д����Ҵ浵��?</summary>
        public void OnDebugUnlockAllBuffsEnabled()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            CaptureDebugUnlockBaseline();
            EnforcePermanentUnlockConsistency(purgePlayerBar: true);
            EmojLog.Info(EmojLogChannel.Buff,
                $"debug unlock all: baseline saved count={DebugUnlockBaseline?.Count ?? 0}");
        }

        /// <summary>�ر� DEBUG_UNLOCKALLBUFFS���ָ����������Ƴ�δ���ý������й�����״̬�� Buff������ SyncUnlocks �������?</summary>
        public void OnDebugUnlockAllBuffsDisabled()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (DebugUnlockBaseline != null)
            {
                UnlockedBuffs.Clear();
                foreach (int id in DebugUnlockBaseline)
                    UnlockedBuffs.Add(id);

                DebugUnlockBaseline = null;
            }

            EnforcePermanentUnlockConsistency(purgePlayerBar: true);
            EmojLog.Info(EmojLogChannel.Buff, "debug unlock all: disabled, restored baseline unlocks");
        }

        private void CaptureDebugUnlockBaseline() =>
            DebugUnlockBaseline = new HashSet<int>(UnlockedBuffs);

        /// <summary>�й��б���״̬�������������ѽ������ֹ DEBUG/ȫ�� ��������������</summary>
        public void EnforcePermanentUnlockConsistency(bool purgePlayerBar)
        {
            ActiveBuffs.RemoveWhere(id => !HasPermanentUnlock(id));
            DisabledBuffs.RemoveWhere(id => !HasPermanentUnlock(id));
            PinnedPhysicalBuffs.RemoveWhere(id => !HasPermanentUnlock(id));

            if (purgePlayerBar && Player.whoAmI == Main.myPlayer)
                PurgePlayerBarBuffsNotPermanentlyUnlocked();

            NotifyBuffRuntimeStateChanged();
        }

        /// <summary>���״̬���ϱ�ģ����¼��δ���ý�����? Buff������ <see cref="SyncUnlocksFromPlayerBar"/> �������?</summary>
        public void PurgePlayerBarBuffsNotPermanentlyUnlocked()
        {
            int buffSlotCount = Math.Min(Player.MaxBuffs, Math.Min(Player.buffType.Length, Player.buffTime.Length));
            for (int i = 0; i < buffSlotCount; i++)
            {
                int type = Player.buffType[i];
                if (type <= 0 || Player.buffTime[i] <= 0)
                    continue;

                if (IsEmojInternalBuff(type))
                    continue;

                if (BuffFedStateCompat.IsHungerDebuff(type))
                    continue;

                if (!IsManagedPlayerBarBuff(type))
                    continue;

                if (!BuffListCatalog.IsListable(type))
                    continue;

                if (HasPermanentUnlock(type))
                    continue;

                ClearManagedBuff(Player, type);
            }
        }

        private static bool IsValidPersistentBuffId(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (IsEmojInternalBuff(buffId))
                return false;

            if (BuffPlayerApplicability.ShouldBlockManagedApplication(buffId))
                return false;

            return BuffListCatalog.IsListable(buffId);
        }

        private static bool IsEmojInternalBuff(int buffId)
        {
            int alphaId = ModContent.BuffType<EMOJAlphaBuff>();
            int omegaId = ModContent.BuffType<EMOJOmegaBuff>();
            return buffId == alphaId || buffId == omegaId;
        }

        private void SanitizePersistentSets()
        {
            UnlockedBuffs.RemoveWhere(id => !IsDiscoverableBuffId(id));
            ActiveBuffs.RemoveWhere(id => !IsValidPersistentBuffId(id) || BuffEntityIndexSystem.RequiresManualEntityManagement(id));
            DisabledBuffs.RemoveWhere(id => !IsValidPersistentBuffId(id));
            PinnedPhysicalBuffs.RemoveWhere(id => !IsValidPersistentBuffId(id));
        }

        /// <summary>�м��л����̶���״̬�������� Buff ������������ˢ����λ��</summary>
        public void TogglePinnedPhysicalBuff(int buffId)
        {
            if (!IsValidPersistentBuffId(buffId))
                return;

            if (PinnedPhysicalBuffs.Contains(buffId))
                PinnedPhysicalBuffs.Remove(buffId);
            else
                PinnedPhysicalBuffs.Add(buffId);

            NotifyBuffRuntimeStateChanged();

            if (Player.whoAmI != Main.myPlayer)
                return;

            if (ActiveBuffs.Contains(buffId))
                ApplyMiscEquipBuffsFromUi();
        }

        private void SyncUnlocksFromPlayerBar()
        {
            if (SuperAdminSession.DebugUnlockAllBuffs)
                return;

            for (int i = 0; i < Player.buffType.Length; i++)
            {
                int type = Player.buffType[i];
                if (type > 0 && Player.buffTime[i] > 0)
                    TryGrantPermanentUnlock(type);
            }

            foreach (int buffId in ActiveBuffs)
                TryGrantPermanentUnlock(buffId);
        }

        public void EnforceDisabledBuffImmunityBeforeUpdate()
        {
            foreach (int buffId in DisabledBuffs)
            {
                if (buffId <= 0 || buffId >= Player.buffImmune.Length)
                    continue;

                Player.buffImmune[buffId] = true;
                Player.ClearBuff(buffId);
            }
        }

        public override void PreUpdateBuffs()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            EnforceDisabledBuffImmunityBeforeUpdate();

            if (OPJourneyConfig.UseVirtualScratchApply() && GetVirtualBarBuffCount() > 0)
            {
                BuffVirtualEffectSystem.RebuildVirtualQueue(this);
                BuffVirtualEffectSystem.ApplyAllVirtualEffects(Player, this);
            }
        }

        public override void PreUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (IsInWorldTransitionGrace)
                return;

            int[] targetBuffs = new int[MiscEquipSlotCount];

            foreach (string miscCategory in new[] { BuffCategories.Pet, BuffCategories.LightPet, BuffCategories.Mount, BuffCategories.Minecart })
            {
                int chosen = GetFirstActiveBuffInCategory(miscCategory);
                if (chosen <= 0)
                    continue;

                int slot = BuffMiscEquipIndexSystem.GetMiscEquipSlotIndex(miscCategory);
                if (slot >= 0)
                    targetBuffs[slot] = chosen;
            }

            for (int slot = 0; slot < MiscEquipSlotCount; slot++)
            {
                int activeBuffId = targetBuffs[slot];
                if (activeBuffId > 0 &&
                    BuffMiscEquipIndexSystem.BuffToItemType.TryGetValue(activeBuffId, out int itemId) &&
                    itemId > 0)
                {
                    if (!HasSavedMisc[slot])
                    {
                        SavedMiscEquips[slot] = Player.miscEquips[slot].Clone();
                        HasSavedMisc[slot] = true;
                    }

                    if (Player.miscEquips[slot].type != itemId)
                        Player.miscEquips[slot].SetDefaults(itemId);
                }
                else if (HasSavedMisc[slot])
                {
                    Player.miscEquips[slot] = SavedMiscEquips[slot].Clone();
                    HasSavedMisc[slot] = false;
                }
            }
        }

        public override void PostUpdateEquips()
        {
            if (Player.whoAmI == Main.myPlayer && !IsInWorldTransitionGrace)
                VanillaBuffStatRegistry.ApplyActiveSyntheticStats(Player, this);
        }

        public override void PostUpdateBuffs()
        {
            EnsureHadBuffFrameBuffer();

            if (!SuperAdminSession.DebugUnlockAllBuffs)
                SyncUnlocksFromPlayerBar();

            var manualClears = new List<int>();
            foreach (int buffId in ActiveBuffs)
            {
                if (BuffVirtualEffectSystem.UsesVirtualEffect(buffId, this) ||
                    (OPJourneyConfig.UseVirtualScratchApply() && BuffVirtualEffectSystem.WouldUseVirtualEffect(buffId, this)))
                    continue;

                string category = BuffPage.GetBuffCategory(buffId);
                if (BuffPage.IsExclusiveCombatSummonCategory(category))
                    continue;

                bool hasBuffNow = Player.FindBuffIndex(buffId) != -1;
                bool clearedByForeignImmunity = buffId > 0 &&
                    buffId < Player.buffImmune.Length &&
                    Player.buffImmune[buffId];
                if (HadBuffLastFrame[buffId] && !hasBuffNow && !clearedByForeignImmunity)
                    manualClears.Add(buffId);
            }

            if (manualClears.Count > 0)
            {
                foreach (int buffId in manualClears)
                    ActiveBuffs.Remove(buffId);

                NotifyBuffRuntimeStateChanged();
            }

            if (!SuperAdminSession.DebugUnlockAllBuffs)
            {
                PurgePlayerBarBuffsNotPermanentlyUnlocked();
                ActiveBuffs.RemoveWhere(id => !HasPermanentUnlock(id));
                DisabledBuffs.RemoveWhere(id => !HasPermanentUnlock(id));
                PinnedPhysicalBuffs.RemoveWhere(id => !HasPermanentUnlock(id));
            }

            foreach (int buffId in ActiveBuffs)
            {
                if (DisabledBuffs.Contains(buffId))
                    continue;

                if (buffId > 0 && buffId < Player.buffImmune.Length)
                    Player.buffImmune[buffId] = false;
            }

            PurgeInvalidManagedBuffsFromPlayer();

            bool grace = IsInWorldTransitionGrace;
            if (!grace && _barDisplayDirty)
                RefreshBarBuffDisplay();

            BuffManagedReapplySystem.ApplyMissing(Player, this, allowNewAddBuff: !grace);
            ApplyMiscEquipBuffs(allowNewAddBuff: !grace);
            BuffCombatSummonSystem.Maintain(Player, this);
            BuffFedStateCompat.ApplySatietyAfterBuffPipeline(Player, this);

            for (int i = 0; i < HadBuffLastFrame.Length; i++)
                HadBuffLastFrame[i] = false;

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int type = Player.buffType[i];
                if (type > 0 && type < HadBuffLastFrame.Length && Player.buffTime[i] > 0)
                    HadBuffLastFrame[type] = true;
            }
        }

        private void PurgeInvalidManagedBuffsFromPlayer()
        {
            var remove = new List<int>();
            foreach (int buffId in ActiveBuffs)
            {
                if (BuffPlayerApplicability.ShouldBlockManagedApplication(buffId))
                    remove.Add(buffId);
            }

            foreach (int buffId in remove)
            {
                ActiveBuffs.Remove(buffId);
                ClearManagedBuff(Player, buffId);
            }
        }

        private void EnsureHadBuffFrameBuffer()
        {
            int count = BuffLoader.BuffCount;
            if (HadBuffLastFrame == null || HadBuffLastFrame.Length < count)
                HadBuffLastFrame = new bool[count];
        }

        /// <summary>����/����/��/����/�ٻ�������ʵ�� Buff����������������Ч��</summary>
        public static bool ShouldSkipTimedRenewal(int buffId)
        {
            if (buffId <= 0)
                return false;

            if (BuffCategoryIndexSystem.IsCombatSummonCategory(BuffPage.GetBuffCategoryStatic(buffId)))
                return false;

            if (BuffVirtualEffectSystem.UsesVirtualEffect(buffId))
                return true; // �������� ApplyAllVirtualEffects���������� BuffManagedReapplySystem

            if (BuffMountCategorySystem.IsMountBuff(buffId) || BuffMountCategorySystem.IsMinecartBuff(buffId))
                return true;

            if (BuffMountCategorySystem.TryResolveMountCategory(buffId, out _))
                return true;

            if (BuffMiscEquipIndexSystem.GetMiscEquipSlotIndex(BuffPage.GetBuffCategoryStatic(buffId)) >= 0)
                return true;

            return BuffEntityIndexSystem.IsEntitySpawningBuff(buffId);
        }

        public bool UseAggregatedVirtualBarDisplay() =>
            GetVirtualBarBuffCount() >= BarDisplayAggregateThreshold;

        public int GetVirtualBarBuffCount()
        {
            if (VirtualQueueDirty)
                BuffVirtualEffectSystem.RebuildVirtualQueue(this);

            if (_cachedVirtualBarCount >= 0)
                return _cachedVirtualBarCount;

            _cachedVirtualBarCount = CountVirtualBarBuffsUncached();
            return _cachedVirtualBarCount;
        }

        private int CountVirtualBarBuffsUncached()
        {
            int n = 0;
            foreach (int buffId in ActiveBuffs)
            {
                if (DisabledBuffs.Contains(buffId))
                    continue;

                if (!BuffVirtualEffectSystem.WouldUseVirtualEffect(buffId, this))
                    continue;

                n++;
            }

            return n;
        }

        /// <summary>״̬�������ȹ̶�/ʵ���ࣻ��20 �������������� Alpha ͼ�꣨��ʵ��λģʽ��������λ������Ƶ������</summary>
        private void RefreshBarBuffDisplay()
        {
            _barDisplayDirty = false;

            int alphaId = ModContent.BuffType<EMOJAlphaBuff>();
            ClearManagedBuff(Player, ModContent.BuffType<EMOJOmegaBuff>());

            bool aggregate = UseAggregatedVirtualBarDisplay();
            var keepPhysical = new HashSet<int>();

            int maxSafePhysicalSlots = Player.MaxBuffs - 2;
            int currentPhysicalCount = 0;
            for (int i = 0; i < Player.buffType.Length; i++)
            {
                int type = Player.buffType[i];
                if (type > 0 && Player.buffTime[i] > 0 && !ActiveBuffs.Contains(type))
                    currentPhysicalCount++;
            }

            foreach (int buffId in ActiveBuffs)
            {
                if (DisabledBuffs.Contains(buffId))
                    continue;

                if (BuffPlayerApplicability.ShouldBlockManagedApplication(buffId))
                    continue;

                if (BuffVirtualEffectSafety.PrefersContinuousPhysicalBar(buffId) &&
                    currentPhysicalCount < maxSafePhysicalSlots)
                {
                    keepPhysical.Add(buffId);
                    currentPhysicalCount++;
                }
            }

            foreach (int buffId in ActiveBuffs)
            {
                if (DisabledBuffs.Contains(buffId))
                    continue;

                if (BuffPlayerApplicability.ShouldBlockManagedApplication(buffId))
                    continue;

                if (keepPhysical.Contains(buffId))
                    continue;

                if (BuffFedStateCompat.ShouldForcePhysical(buffId) && currentPhysicalCount < maxSafePhysicalSlots)
                {
                    keepPhysical.Add(buffId);
                    currentPhysicalCount++;
                }
            }

            foreach (int buffId in ActiveBuffs)
            {
                if (DisabledBuffs.Contains(buffId))
                    continue;

                if (BuffPlayerApplicability.ShouldBlockManagedApplication(buffId))
                    continue;

                if (keepPhysical.Contains(buffId))
                    continue;

                bool isMisc = IsMiscEquipBuff(buffId);
                bool entityOnBar = BuffEntityIndexSystem.IsEntitySpawningBuff(buffId) || isMisc;

                if (entityOnBar && currentPhysicalCount < maxSafePhysicalSlots)
                {
                    keepPhysical.Add(buffId);
                    currentPhysicalCount++;
                }
            }

            foreach (int buffId in ActiveBuffs)
            {
                if (DisabledBuffs.Contains(buffId) || keepPhysical.Contains(buffId))
                    continue;

                bool isVirtual = OPJourneyConfig.UseVirtualScratchApply() && IsCachedVirtualBuff(buffId);
                bool keepOnBar = !isVirtual || !aggregate;

                if (keepOnBar && currentPhysicalCount < maxSafePhysicalSlots)
                {
                    keepPhysical.Add(buffId);
                    currentPhysicalCount++;
                }
            }

            foreach (int buffId in keepPhysical)
            {
                if (ShouldDeferAddBuffToMiscEquip(buffId))
                {
                    RenewBuffTimeIfPresent(buffId);
                    continue;
                }

                int idx = Player.FindBuffIndex(buffId);
                if (idx == -1)
                    Player.AddBuff(buffId, ActiveBuffDuration);
                else if (Player.buffTime[idx] < ActiveBuffDuration)
                    Player.buffTime[idx] = ActiveBuffDuration;

                if (buffId > 0 && buffId < Main.buffNoTimeDisplay.Length)
                    Main.buffNoTimeDisplay[buffId] = true;
            }

            for (int i = Player.buffType.Length - 1; i >= 0; i--)
            {
                int type = Player.buffType[i];
                if (type <= 0 || type == alphaId)
                    continue;

                if (ActiveBuffs.Contains(type) && !keepPhysical.Contains(type))
                    Player.DelBuff(i);
            }

            if (aggregate && GetVirtualBarBuffCount() > 0)
            {
                int idx = Player.FindBuffIndex(alphaId);
                if (idx == -1)
                    Player.AddBuff(alphaId, ActiveBuffDuration);
                else if (Player.buffTime[idx] < ActiveBuffDuration)
                    Player.buffTime[idx] = ActiveBuffDuration;

                if (alphaId < Main.buffNoTimeDisplay.Length)
                    Main.buffNoTimeDisplay[alphaId] = true;
            }
            else
                ClearManagedBuff(Player, alphaId);
        }

        /// <summary>UI �������غ�����ˢ�� misc �� Buff��</summary>
        public void ApplyMiscEquipBuffsFromUi()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            _barDisplayDirty = true;
            RefreshBarBuffDisplay();
            ApplyMiscEquipBuffs(allowNewAddBuff: true);
        }

        private void ApplyMiscEquipBuffs(bool allowNewAddBuff = true)
        {
            foreach (int buffId in ActiveBuffs)
            {
                if (DisabledBuffs.Contains(buffId) || !IsMiscEquipBuff(buffId))
                    continue;

                if (ShouldDeferAddBuffToMiscEquip(buffId))
                {
                    RenewBuffTimeIfPresent(buffId);
                    continue;
                }

                int idx = Player.FindBuffIndex(buffId);
                if (idx == -1)
                {
                    if (!allowNewAddBuff)
                        continue;

                    Player.AddBuff(buffId, ActiveBuffDuration);
                }
                else if (Player.buffTime[idx] < ActiveBuffDuration)
                    Player.buffTime[idx] = ActiveBuffDuration;

                if (buffId > 0 && buffId < Main.buffNoTimeDisplay.Length)
                    Main.buffNoTimeDisplay[buffId] = true;
            }
        }

        /// <summary>����/�������misc ��Ʒ���Ѿ�λʱ��ԭ���? Buff������ÿ֡ AddBuff ���� Spooky ���ظ�����ʵ�塣</summary>
        private bool ShouldDeferAddBuffToMiscEquip(int buffId)
        {
            string category = BuffPage.GetBuffCategoryStatic(buffId);
            if (category != BuffCategories.Pet && category != BuffCategories.LightPet)
                return false;

            return TryGetMiscEquipItemForBuff(buffId, out _, out int itemId) && itemId > 0 &&
                   HasMiscEquipItem(buffId, itemId);
        }

        private bool TryGetMiscEquipItemForBuff(int buffId, out int miscSlot, out int itemType)
        {
            miscSlot = BuffMiscEquipIndexSystem.GetMiscEquipSlotIndex(BuffPage.GetBuffCategoryStatic(buffId));
            itemType = 0;
            if (miscSlot < 0)
                return false;

            return BuffMiscEquipIndexSystem.BuffToItemType.TryGetValue(buffId, out itemType) && itemType > 0;
        }

        private bool HasMiscEquipItem(int buffId, int itemType)
        {
            if (!TryGetMiscEquipItemForBuff(buffId, out int miscSlot, out int expected) || expected != itemType)
                return false;

            return Player.miscEquips[miscSlot].type == itemType;
        }

        private void RenewBuffTimeIfPresent(int buffId)
        {
            int idx = Player.FindBuffIndex(buffId);
            if (idx == -1)
                return;

            if (Player.buffTime[idx] < ActiveBuffDuration)
                Player.buffTime[idx] = ActiveBuffDuration;

            if (buffId > 0 && buffId < Main.buffNoTimeDisplay.Length)
                Main.buffNoTimeDisplay[buffId] = true;
        }

        public static bool IsMiscEquipBuff(int buffId) =>
            buffId > 0 && BuffMiscEquipIndexSystem.GetMiscEquipSlotIndex(BuffPage.GetBuffCategoryStatic(buffId)) >= 0;

        private int GetFirstActiveBuffInCategory(string category)
        {
            foreach (int buffId in ActiveBuffs)
            {
                if (DisabledBuffs.Contains(buffId))
                    continue;

                if (BuffPage.GetBuffCategoryStatic(buffId) == category)
                    return buffId;
            }

            return 0;
        }

        public bool HasAnyKnownSetBonusBuffActive()
        {
            foreach (int buffId in ActiveBuffs)
            {
                if (DisabledBuffs.Contains(buffId))
                    continue;

                if (BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId))
                    return true;
            }

            return false;
        }

        public bool HasAnyActiveBuffByName(params string[] buffNames)
        {
            if (buffNames == null || buffNames.Length == 0)
                return false;

            foreach (int id in ActiveBuffs)
            {
                string name = id < BuffID.Count ? BuffID.Search.GetName(id) : ModContent.GetModBuff(id)?.Name;
                if (string.IsNullOrEmpty(name))
                    continue;

                foreach (string candidate in buffNames)
                {
                    if (string.Equals(name, candidate, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }
    }
}
