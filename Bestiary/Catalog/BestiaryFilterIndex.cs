using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.Localization;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    public sealed class BestiaryFilterDef
    {
        public string Id;
        public string DisplayName;
        public IBestiaryEntryFilter Filter;
        public Microsoft.Xna.Framework.Point IconFrame;
    }

    public sealed class BestiaryFilterIndex : ModSystem
    {
        public static bool Ready { get; private set; }
        public static IReadOnlyList<string> ModKeys => _modKeys;
        public static IReadOnlyList<BestiaryFilterDef> VanillaFilters => _vanillaFilters;

        private static readonly List<string> _modKeys = new List<string>();
        private static readonly List<BestiaryFilterDef> _vanillaFilters = new List<BestiaryFilterDef>();

        public override void PostSetupContent()
        {
            BestiaryListCatalog.EnsureFresh();
            Rebuild();
        }

        public override void OnWorldLoad()
        {
            BestiaryListCatalog.EnsureFresh();
            Rebuild();
        }

        public static void Rebuild()
        {
            _modKeys.Clear();
            _vanillaFilters.Clear();
            Ready = false;

            if (Main.dedServ)
                return;

            var modSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BestiaryNpcMeta meta in BestiaryListCatalog.All)
            {
                if (!string.IsNullOrEmpty(meta.ModKey))
                    modSet.Add(meta.ModKey);
            }

            _modKeys.AddRange(modSet);
            _modKeys.Sort(StringComparer.OrdinalIgnoreCase);
            PinTerrariaModFirst(_modKeys);

            RegisterVanillaFiltersFromBestiaryDatabase();
            Ready = true;
            EmojLog.Info(EmojLogChannel.Core, $"BestiaryFilterIndex mods={_modKeys.Count} vanillaFilters={_vanillaFilters.Count}");
        }

        /// <summary>与原版 UI 一致：筛选项来自 <see cref="BestiaryDatabase.Filters"/>，而非反射 Filters 静态字段。</summary>
        private static void RegisterVanillaFiltersFromBestiaryDatabase()
        {
            IReadOnlyList<IBestiaryEntryFilter> filters = Main.BestiaryDB?.Filters;
            if (filters == null)
                return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < filters.Count; i++)
            {
                IBestiaryEntryFilter filter = filters[i];
                if (filter == null)
                    continue;

                string id = filter.GetType().Name;
                if (!seen.Add(id))
                    id = id + "_" + i;

                string name = TryGetFilterDisplayName(filter, id);
                BestiaryFilterIconResolver.TryGetIconFrame(filter, out Microsoft.Xna.Framework.Point frame);
                _vanillaFilters.Add(new BestiaryFilterDef
                {
                    Id = id,
                    DisplayName = name,
                    Filter = filter,
                    IconFrame = frame
                });
            }
        }

        private static void PinTerrariaModFirst(List<string> modKeys)
        {
            const string vanilla = "Terraria";
            int idx = modKeys.FindIndex(k => string.Equals(k, vanilla, StringComparison.OrdinalIgnoreCase));
            if (idx > 0)
            {
                string key = modKeys[idx];
                modKeys.RemoveAt(idx);
                modKeys.Insert(0, key);
            }
            else if (idx < 0)
            {
                modKeys.Insert(0, vanilla);
            }
        }

        private static string TryGetFilterDisplayName(IBestiaryEntryFilter filter, string fallbackId)
        {
            try
            {
                if (filter is IEntryFilter<BestiaryEntry> entryFilter)
                {
                    string key = entryFilter.GetDisplayNameKey();
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        string txt = Language.GetTextValue(key);
                        if (!string.IsNullOrWhiteSpace(txt))
                            return txt;
                    }
                }

                MethodInfo getName = filter.GetType().GetMethod("GetDisplayNameKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (getName?.Invoke(filter, null) is string legacyKey && !string.IsNullOrWhiteSpace(legacyKey))
                {
                    string txt = Language.GetTextValue(legacyKey);
                    if (!string.IsNullOrWhiteSpace(txt))
                        return txt;
                }
            }
            catch
            {
                // ignored
            }

            return fallbackId;
        }

        public static bool EntryMatchesVanillaFilter(BestiaryEntry entry, IBestiaryEntryFilter filter)
        {
            if (entry == null || filter == null)
                return false;

            try
            {
                if (filter is IEntryFilter<BestiaryEntry> gen)
                    return gen.FitsFilter(entry);
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
