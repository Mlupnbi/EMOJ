using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Spawning
{
    /// <summary>
    /// ����/�� Buff �������� <see cref="Mount.mounts"/> �� <see cref="BuffID.Sets.BasicMountData"/> Ϊ����
    /// ������Ʒ <c>mountType</c> / <c>buffType</c> ��ȫ������ Gemini ���飬�����������ڵ� IsMount API����
    /// </summary>
    public sealed class BuffMountCategorySystem : ModSystem
    {
        public static HashSet<int> MountBuffIds { get; } = new HashSet<int>();
        public static HashSet<int> MinecartBuffIds { get; } = new HashSet<int>();

        public static bool IsMountBuff(int buffId) => buffId > 0 && MountBuffIds.Contains(buffId);

        public static bool IsMinecartBuff(int buffId) => buffId > 0 && MinecartBuffIds.Contains(buffId);

        public static bool TryResolveMountCategory(int buffId, out bool isMinecart)
        {
            isMinecart = false;
            if (buffId <= 0)
                return false;

            if (IsMinecartBuff(buffId))
            {
                isMinecart = true;
                return true;
            }

            if (IsMountBuff(buffId))
                return true;

            if (!TryGetMountIdFromBuff(buffId, out int mountId))
                return false;

            isMinecart = IsCartMount(mountId);
            RegisterMountBuff(buffId, mountId);
            return true;
        }

        public static void RebuildIndexes()
        {
            MountBuffIds.Clear();
            MinecartBuffIds.Clear();

            IndexFromMountTable();
            IndexFromBasicMountData();
            IndexFromMountItems();
        }

        /// <summary>ԭ��������ÿ�� mountId ��ӦΨһ��� Buff��</summary>
        private static void IndexFromMountTable()
        {
            int count = MountLoader.MountCount;
            if (count <= 0 || Mount.mounts == null)
                return;

            int limit = System.Math.Min(count, Mount.mounts.Length);
            for (int mountId = 0; mountId < limit; mountId++)
            {
                int buffId = Mount.mounts[mountId].buff;
                if (buffId > 0)
                    RegisterMountBuff(buffId, mountId);
            }
        }

        private static void IndexFromBasicMountData()
        {
            var mountData = BuffID.Sets.BasicMountData;
            if (mountData == null)
                return;

            int limit = System.Math.Min(BuffLoader.BuffCount, mountData.Length);
            for (int buffId = 1; buffId < limit; buffId++)
            {
                if (mountData[buffId] == null)
                    continue;

                RegisterMountBuff(buffId, mountData[buffId].mountID);
            }
        }

        private static void IndexFromMountItems()
        {
            var mountIdToBuff = new Dictionary<int, int>();
            foreach (int id in MountBuffIds)
            {
                if (TryGetMountIdFromBuff(id, out int mid) && !mountIdToBuff.ContainsKey(mid))
                    mountIdToBuff[mid] = id;
            }
            foreach (int id in MinecartBuffIds)
            {
                if (TryGetMountIdFromBuff(id, out int mid) && !mountIdToBuff.ContainsKey(mid))
                    mountIdToBuff[mid] = id;
            }

            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                if (!ContentSamples.ItemsByType.TryGetValue(i, out Item item) || item == null || item.IsAir)
                    continue;

                if (item.mountType == -1)
                    continue;

                int buffId = item.buffType;
                if (buffId <= 0 && mountIdToBuff.TryGetValue(item.mountType, out int inferred))
                    buffId = inferred;

                if (buffId > 0)
                    RegisterMountBuff(buffId, item.mountType);
            }
        }

        private static bool TryGetMountIdFromBuff(int buffId, out int mountId)
        {
            mountId = -1;
            if (buffId <= 0)
                return false;

            var mountData = BuffID.Sets.BasicMountData;
            if (mountData == null || buffId >= mountData.Length || mountData[buffId] == null)
                return false;

            mountId = mountData[buffId].mountID;
            return true;
        }

        private static bool IsCartMount(int mountId) =>
            mountId >= 0 && mountId < MountLoader.MountCount && MountID.Sets.Cart[mountId];

        private static void RegisterMountBuff(int buffId, int mountId)
        {
            if (buffId <= 0)
                return;

            if (IsCartMount(mountId))
                MinecartBuffIds.Add(buffId);
            else
                MountBuffIds.Add(buffId);
        }
    }
}
