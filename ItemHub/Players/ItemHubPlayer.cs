using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.ItemHub.Players
{
    /// <summary>��Ʒ���ࣺ����ù�������/����г��ֹ�������Ʒ�ſ���ȡ����;�¿ɵ��ӡ��о����ȡ��ж�������ɫ�浵��</summary>
    public class ItemHubPlayer : ModPlayer
    {
        public HashSet<int> KnownItemTypes = new HashSet<int>();
        /// <summary>�״η�����Ʒ��˳�򣨴浵���ݣ��������Ƴ�����</summary>
        public List<int> AcquireOrder = new List<int>();

        private ulong _lastInvSignature = ulong.MaxValue;
        private bool _wasPlayerInventory;
        private int _wasChest = int.MinValue;
        private int _hubStatsCachedFrame = -1;
        private int _hubStatsUnlocked;
        private int _hubStatsTotal;
        private int _hubStatsDiscoveryVer = int.MinValue;
        private int _hubStatsFilterHash = int.MinValue;

        /// <summary><see cref="Player.chest"/> Ϊ������-2 �����-3 �����䡢-4 ������¯��-5 ��մ���</summary>
        private static bool IsPersonalPortableStorageChest(int chest) =>
            chest is -2 or -3 or -4 or -5;

        /// <summary>��Ʒ�����б����ʧЧ�ã�ÿ���·��ֵ�����</summary>
        public int DiscoveryVersion;

        public override void OnEnterWorld()
        {
            if (Player.whoAmI == Main.myPlayer && RecipeAnalyzer.IsJourneyWorld)
                SyncJourneyFullyResearchedIntoDiscovered();
            SyncItemHubInventoryScanEdgeState();
        }

        private void SyncItemHubInventoryScanEdgeState()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            _wasPlayerInventory = Main.playerInventory;
            _wasChest = Player.chest;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["KnownItemTypes"] = KnownItemTypes.ToList();
            tag["AcquireOrder"] = AcquireOrder.ToList();
        }

        private static bool IsValidLoadedItemType(int itemType) =>
            itemType > ItemID.None && itemType < ItemLoader.ItemCount;

        public override void LoadData(TagCompound tag)
        {
            KnownItemTypes.Clear();
            AcquireOrder.Clear();
            if (tag.ContainsKey("KnownItemTypes"))
            {
                foreach (int t in tag.GetList<int>("KnownItemTypes"))
                {
                    if (IsValidLoadedItemType(t))
                        KnownItemTypes.Add(t);
                }
            }
            if (tag.ContainsKey("AcquireOrder"))
            {
                var ordered = new HashSet<int>();
                foreach (int t in tag.GetList<int>("AcquireOrder"))
                {
                    if (IsValidLoadedItemType(t) && KnownItemTypes.Contains(t) && ordered.Add(t))
                        AcquireOrder.Add(t);
                }

                foreach (int t in KnownItemTypes)
                {
                    if (ordered.Add(t))
                        AcquireOrder.Add(t);
                }
            }
            else
            {
                foreach (int t in KnownItemTypes)
                    AcquireOrder.Add(t);
            }
            InvalidateHubProgressCache();
            SyncItemHubInventoryScanEdgeState();
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            bool invOpen = Main.playerInventory;
            int chestNow = Player.chest;
            bool invJustOpened = invOpen && !_wasPlayerInventory;
            bool chestChangedWhileInv = invOpen && _wasChest != int.MinValue && chestNow != _wasChest;

            if (invJustOpened || chestChangedWhileInv)
                ScanInventoryAndVisiblePersonalStorageForDiscoveries();

            _wasPlayerInventory = invOpen;
            _wasChest = chestNow;

            if (Main.GameUpdateCount % 15 == 0)
            {
                ulong sig = ComputeInventorySignature();
                if (sig != _lastInvSignature)
                {
                    _lastInvSignature = sig;
                    ApplyInventorySignatureDiscoveries();
                }
            }

            if (RecipeAnalyzer.IsJourneyWorld && Main.GameUpdateCount % 300 == 0)
                SyncJourneyFullyResearchedIntoDiscovered();

            if (RecipeAnalyzer.IsJourneyWorld && Main.GameUpdateCount % 180 == 0)
                InvalidateHubProgressCache();
        }

        /// <summary>�򿪱���/�л��������ʱɨ�裬����Ʒ���� UI ��ʱˢ�·����б��</summary>
        private void ScanInventoryAndVisiblePersonalStorageForDiscoveries()
        {
            int invLen = Player.inventory.Length;
            for (int i = 0; i < invLen; i++)
            {
                Item it = Player.inventory[i];
                if (!it.IsAir && it.type > ItemID.None)
                    NoteDiscovered(it.type);
            }

            if (!Main.mouseItem.IsAir && Main.mouseItem.type > ItemID.None)
                NoteDiscovered(Main.mouseItem.type);

            Item[] bankItems = Player.chest switch
            {
                -2 => Player.bank?.item,
                -3 => Player.bank2?.item,
                -4 => Player.bank3?.item,
                -5 => Player.bank4?.item,
                _ => null
            };
            if (bankItems != null)
            {
                foreach (Item it in bankItems)
                {
                    if (it != null && !it.IsAir && it.type > ItemID.None)
                        NoteDiscovered(it.type);
                }
            }
        }

        private void ApplyInventorySignatureDiscoveries()
        {
            int invLen = Player.inventory.Length;
            for (int i = 0; i < invLen; i++)
            {
                Item it = Player.inventory[i];
                if (!it.IsAir && it.type > ItemID.None)
                    NoteDiscovered(it.type);
            }

            if (!Main.mouseItem.IsAir && Main.mouseItem.type > ItemID.None)
                NoteDiscovered(Main.mouseItem.type);

            if (IsPersonalPortableStorageChest(Player.chest))
            {
                Item[] bankItems = Player.chest switch
                {
                    -2 => Player.bank?.item,
                    -3 => Player.bank2?.item,
                    -4 => Player.bank3?.item,
                    -5 => Player.bank4?.item,
                    _ => null
                };
                if (bankItems != null)
                {
                    foreach (Item it in bankItems)
                    {
                        if (it != null && !it.IsAir && it.type > ItemID.None)
                            NoteDiscovered(it.type);
                    }
                }
            }
        }

        private void SyncJourneyFullyResearchedIntoDiscovered()
        {
            HubRegistry.EnsureBuilt();
            foreach (int t in HubRegistry.AllTypes)
            {
                if (RecipeAnalyzer.IsFullyResearched(t))
                    NoteDiscovered(t);
            }
        }

        private ulong ComputeInventorySignature()
        {
            const ulong fnv = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong h = fnv;
            int invLen = Player.inventory.Length;
            for (int i = 0; i < invLen; i++)
            {
                Item it = Player.inventory[i];
                h ^= (uint)it.type;
                h *= prime;
                h ^= (uint)it.stack;
                h *= prime;
            }
            h ^= (uint)Main.mouseItem.type;
            h *= prime;
            h ^= (uint)Main.mouseItem.stack;
            h *= prime;

            if (IsPersonalPortableStorageChest(Player.chest))
            {
                Item[] bankItems = Player.chest switch
                {
                    -2 => Player.bank?.item,
                    -3 => Player.bank2?.item,
                    -4 => Player.bank3?.item,
                    -5 => Player.bank4?.item,
                    _ => null
                };
                if (bankItems != null)
                {
                    foreach (Item it in bankItems)
                    {
                        if (it == null)
                            continue;
                        h ^= (uint)it.type;
                        h *= prime;
                        h ^= (uint)it.stack;
                        h *= prime;
                    }
                }
            }

            return h;
        }

        public void NoteDiscovered(int itemType)
        {
            if (!IsValidLoadedItemType(itemType))
                return;
            if (KnownItemTypes.Add(itemType))
            {
                AcquireOrder.Add(itemType);
                DiscoveryVersion++;
                InvalidateHubProgressCache();
            }
        }

        public void MarkKnown(int itemType) => NoteDiscovered(itemType);

        public int AcquireRank(int itemType)
        {
            for (int i = 0; i < AcquireOrder.Count; i++)
            {
                if (AcquireOrder[i] == itemType)
                    return i;
            }
            return int.MaxValue;
        }

        public void InvalidateHubProgressCache()
        {
            _hubStatsCachedFrame = -1;
            _hubStatsFilterHash = int.MinValue;
        }

        /// <summary>���ڸ����⣺�ǵ�����Ʒ���ѽ����� / ��ͳ������??</summary>
        public (int unlocked, int total) GetHubUnlockProgressCached()
        {
            HubRegistry.EnsureBuilt();
            int frame = (int)Main.GameUpdateCount;
            int filterH = OPJourneyUI.Instance?.ItemHubSecondary?.ComputeHash() ?? 0;
            if (_hubStatsCachedFrame == frame && _hubStatsDiscoveryVer == DiscoveryVersion && _hubStatsFilterHash == filterH)
                return (_hubStatsUnlocked, _hubStatsTotal);

            _hubStatsCachedFrame = frame;
            _hubStatsDiscoveryVer = DiscoveryVersion;
            _hubStatsFilterHash = filterH;
            _hubStatsTotal = 0;
            OPJourneyConfig.ItemHubUnlockRequirementKind cfg =
                ModContent.GetInstance<OPJourneyConfig>().ItemHubUnlockRequirement;
            bool jw = RecipeAnalyzer.IsJourneyWorld;
            int u = 0;
            foreach (int t in HubRegistry.AllTypes)
            {
                if (HubRegistry.IsDebugItem(t))
                    continue;
                _hubStatsTotal++;
                if (IsUnlockedForHub(t, cfg, jw))
                    u++;
            }
            _hubStatsUnlocked = u;
            return (_hubStatsUnlocked, _hubStatsTotal);
        }

        /// <summary>������Ʒʼ����Ϊ���ѽ�������ʾ����������ȡ??</summary>
        public bool CanClaimFromHub(int itemType)
        {
            if (HubRegistry.IsDebugItem(itemType))
                return false;
            return IsUnlockedForHub(itemType);
        }

        /// <summary>�Ƿ���Ϊ�ѽ�������������������Ʒ��Ϊ true??</summary>
        public bool IsUnlockedForHub(int itemType) =>
            IsUnlockedForHub(itemType,
                ModContent.GetInstance<OPJourneyConfig>().ItemHubUnlockRequirement,
                RecipeAnalyzer.IsJourneyWorld);

        internal bool IsUnlockedForHub(int itemType, OPJourneyConfig.ItemHubUnlockRequirementKind cfg, bool journeyWorld)
        {
            if (itemType <= ItemID.None)
                return false;
            if (HubRegistry.IsDebugItem(itemType))
                return true;

            bool bag = KnownItemTypes.Contains(itemType);
            if (!journeyWorld)
                return bag;

            bool inResearch = RecipeAnalyzer.GetSacrificesRemaining(itemType).HasValue;

            if (!inResearch)
                return bag;

            switch (cfg)
            {
                case OPJourneyConfig.ItemHubUnlockRequirementKind.Once:
                    return bag;
                case OPJourneyConfig.ItemHubUnlockRequirementKind.Five:
                    return bag;
                case OPJourneyConfig.ItemHubUnlockRequirementKind.JourneyHalf:
                    return bag || RecipeAnalyzer.IsJourneyResearchProgressAtLeastHalf(itemType);
                case OPJourneyConfig.ItemHubUnlockRequirementKind.JourneyFull:
                    return bag || RecipeAnalyzer.IsFullyResearched(itemType);
                default:
                    return bag;
            }
        }
    }
}
