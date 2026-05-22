using System;
using System.Reflection;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>构造原版 <see cref="UIBestiaryEntryInfoPage.FillInfoForEntry"/> 所需的 extra 参数。</summary>
    internal static class BestiaryVanillaDetailBridge
    {
        private static FieldInfo[] _extraFields;

        public static BestiaryUICollectionInfo GetBoostedCollectionInfo(BestiaryEntry entry, BestiaryFaceMode face)
        {
            BestiaryUICollectionInfo collection = entry.UIInfoProvider.GetEntryUICollectionInfo();
            collection.OwnerEntry = entry;
            return ApplyPreviewUnlockBoost(face, collection);
        }

        public static ExtraBestiaryInfoPageInformation CreateExtra(BestiaryEntry entry, BestiaryFaceMode face)
        {
            BestiaryUICollectionInfo collection = GetBoostedCollectionInfo(entry, face);
            var extra = new ExtraBestiaryInfoPageInformation();
            TryAssignCollectionInfo(extra, collection);
            return extra;
        }

        /// <summary>全部可见 / 仅未解锁：详情仍展示立绘、背景、星级与群系（抬高 UnlockState）。</summary>
        internal static BestiaryUICollectionInfo ApplyPreviewUnlockBoost(
            BestiaryFaceMode face,
            BestiaryUICollectionInfo collection)
        {
            if (face != BestiaryFaceMode.AllVisible && face != BestiaryFaceMode.UnlockedOnly)
                return collection;

            BestiaryEntryUnlockState target = GetDetailPreviewUnlockState();
            if (collection.UnlockState < target)
                collection.UnlockState = target;

            return collection;
        }

        /// <summary>与网格「全部可见」一致：至少能显示立绘/背景/星级/群系。</summary>
        private static BestiaryEntryUnlockState GetDetailPreviewUnlockState()
        {
            BestiaryEntryUnlockState best = BestiaryEntryUnlockState.CanShowDropsWithoutDropRates_3;
            int bestOrd = (int)best;
            foreach (BestiaryEntryUnlockState state in Enum.GetValues(typeof(BestiaryEntryUnlockState)))
            {
                int ord = (int)state;
                if (ord > bestOrd)
                {
                    bestOrd = ord;
                    best = state;
                }
            }

            return best;
        }

        private static void TryAssignCollectionInfo(ExtraBestiaryInfoPageInformation extra, BestiaryUICollectionInfo collection)
        {
            if (TrySetMember(extra, collection))
                return;

            _extraFields ??= typeof(ExtraBestiaryInfoPageInformation).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < _extraFields.Length; i++)
            {
                FieldInfo field = _extraFields[i];
                if (field.FieldType != typeof(BestiaryUICollectionInfo))
                    continue;

                try
                {
                    field.SetValue(extra, collection);
                    return;
                }
                catch
                {
                    // try next field name variant
                }
            }
        }

        private static bool TrySetMember(ExtraBestiaryInfoPageInformation extra, BestiaryUICollectionInfo collection)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var prop in typeof(ExtraBestiaryInfoPageInformation).GetProperties(flags))
            {
                if (prop.PropertyType != typeof(BestiaryUICollectionInfo) || !prop.CanWrite)
                    continue;

                try
                {
                    prop.SetValue(extra, collection);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }

        /// <summary>Fill 后写入 info 页内部集合信息（部分 tML 版本只读 extra 字段）。</summary>
        public static void TryPushCollectionToInfoPage(UIBestiaryEntryInfoPage page, BestiaryUICollectionInfo collection)
        {
            if (page == null)
                return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type pageType = page.GetType();

            foreach (FieldInfo field in pageType.GetFields(flags))
            {
                if (field.FieldType != typeof(BestiaryUICollectionInfo))
                    continue;

                try
                {
                    field.SetValue(page, collection);
                }
                catch
                {
                    // ignored
                }
            }

            foreach (var prop in pageType.GetProperties(flags))
            {
                if (prop.PropertyType != typeof(BestiaryUICollectionInfo) || !prop.CanWrite)
                    continue;

                try
                {
                    prop.SetValue(page, collection);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
