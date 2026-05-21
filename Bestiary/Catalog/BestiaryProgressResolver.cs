using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Config;
using EvenMoreOverpoweredJourney.Integration.ImproveGame;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    /// <summary>
    /// 渐进式解锁（脸②③④）信息披露档位：可选对齐原版击杀进度；
    /// ImproveGame 开启「图鉴快速解锁」时按单次击杀视为完全解锁。
    /// </summary>
    public static class BestiaryProgressResolver
    {
        public enum DisclosureTier
        {
            None = 0,
            SeenOnly = 1,
            Partial = 2,
            Full = 3
        }

        public static bool UseVanillaKillCountForProgressiveDisclosure()
        {
            var cfg = ModContent.GetInstance<OPJourneyConfig>();
            return cfg.BestiaryUseVanillaKillCountForProgressiveDisclosure;
        }

        public static bool ImproveGameQuickUnlockActive()
        {
            return ImproveGameIntegration.IsLoaded && ImproveGameIntegration.ImproveGameBestiaryQuickUnlock;
        }

        public static BestiaryUICollectionInfo GetCollectionInfo(BestiaryEntry entry)
        {
            if (entry?.UIInfoProvider == null)
                return default;

            return entry.UIInfoProvider.GetEntryUICollectionInfo();
        }

        public static BestiaryEntryUnlockState GetUnlockState(BestiaryEntry entry) =>
            GetCollectionInfo(entry).UnlockState;

        /// <summary>是否已在图鉴中「遇见」。</summary>
        public static bool WasEverFound(BestiaryEntry entry)
        {
            if (entry == null)
                return false;

            return GetUnlockState(entry) != BestiaryEntryUnlockState.NotKnownAtAll_0;
        }

        /// <summary>是否达到原版击杀进度下的「完整解锁」。</summary>
        public static bool IsFullyUnlockedInTracker(BestiaryEntry entry)
        {
            if (entry == null)
                return false;

            // 与原版一致：≥ CanShowStats（枚举序号因版本可能不同，用序数 3+）
            return (int)GetUnlockState(entry) >= 3;
        }

        public static DisclosureTier GetDisclosureTier(BestiaryEntry entry)
        {
            if (entry == null)
                return DisclosureTier.None;

            if (!WasEverFound(entry))
                return DisclosureTier.None;

            if (!UseVanillaKillCountForProgressiveDisclosure())
                return DisclosureTier.Partial;

            if (ImproveGameQuickUnlockActive())
                return DisclosureTier.Full;

            return IsFullyUnlockedInTracker(entry) ? DisclosureTier.Full : DisclosureTier.Partial;
        }

        public static bool CountsAsUnlockedForFace4(BestiaryEntry entry) =>
            !IsFullyUnlockedInTracker(entry);

        public static bool CountsAsHiddenForFace3(BestiaryEntry entry) =>
            !WasEverFound(entry);
    }
}
