using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Bestiary;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;

namespace EvenMoreOverpoweredJourney.Bestiary.UI
{
    /// <summary>
    /// 脸① / 脸④ 详情预览：临时替换 <see cref="BestiaryEntry.UIInfoProvider"/> 并抬高击杀数，
    /// 使原版 <see cref="Terraria.GameContent.UI.Elements.UIBestiaryEntryInfoPage"/> 显示立绘/背景/星级/群系。
    /// </summary>
    internal sealed class BestiaryDetailPreviewScope : IDisposable
    {
        private readonly BestiaryEntry _entry;
        private readonly IBestiaryUICollectionInfoProvider _originalProvider;
        private readonly BestiaryDetailKillBoostScope _killBoost;
        private readonly bool _providerSwapped;

        private BestiaryDetailPreviewScope(
            BestiaryEntry entry,
            IBestiaryUICollectionInfoProvider originalProvider,
            BestiaryDetailKillBoostScope killBoost,
            bool providerSwapped)
        {
            _entry = entry;
            _originalProvider = originalProvider;
            _killBoost = killBoost;
            _providerSwapped = providerSwapped;
        }

        public static BestiaryDetailPreviewScope TryEnter(BestiaryEntry entry, BestiaryFaceMode face)
        {
            if (entry == null)
                return null;

            if (face != BestiaryFaceMode.AllVisible && face != BestiaryFaceMode.UnlockedOnly)
                return null;

            IBestiaryUICollectionInfoProvider original = entry.UIInfoProvider;
            var boosted = new BoostedBestiaryUiInfoProvider(original, entry, face);
            bool swapped = TrySetEntryProvider(entry, boosted);
            BestiaryDetailKillBoostScope killBoost = BestiaryDetailKillBoostScope.TryEnter(entry, face);

            if (!swapped && killBoost == null)
                return null;

            return new BestiaryDetailPreviewScope(entry, swapped ? original : null, killBoost, swapped);
        }

        public void Dispose()
        {
            _killBoost?.Dispose();
            if (_providerSwapped && _entry != null && _originalProvider != null)
                TrySetEntryProvider(_entry, _originalProvider);
        }

        private static bool TrySetEntryProvider(BestiaryEntry entry, IBestiaryUICollectionInfoProvider provider)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type t = typeof(BestiaryEntry);

            PropertyInfo prop = t.GetProperty("UIInfoProvider", flags);
            if (prop?.CanWrite == true)
            {
                try
                {
                    prop.SetValue(entry, provider);
                    return true;
                }
                catch
                {
                    // fall through
                }
            }

            foreach (FieldInfo field in t.GetFields(flags))
            {
                if (field.FieldType != typeof(IBestiaryUICollectionInfoProvider))
                    continue;

                try
                {
                    field.SetValue(entry, provider);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }

        private sealed class BoostedBestiaryUiInfoProvider : IBestiaryUICollectionInfoProvider
        {
            private readonly IBestiaryUICollectionInfoProvider _inner;
            private readonly BestiaryEntry _entry;
            private readonly BestiaryFaceMode _face;

            public BoostedBestiaryUiInfoProvider(
                IBestiaryUICollectionInfoProvider inner,
                BestiaryEntry entry,
                BestiaryFaceMode face)
            {
                _inner = inner;
                _entry = entry;
                _face = face;
            }

            public BestiaryUICollectionInfo GetEntryUICollectionInfo()
            {
                BestiaryUICollectionInfo info = _inner != null
                    ? _inner.GetEntryUICollectionInfo()
                    : default;

                info.OwnerEntry = _entry;
                return BestiaryVanillaDetailBridge.ApplyPreviewUnlockBoost(_face, info);
            }
        }
    }
}
