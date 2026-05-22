using System;
using System.Collections.Generic;
using System.Text;
using Terraria.GameContent.Bestiary;
using Terraria.Localization;
using EvenMoreOverpoweredJourney.Bestiary.Catalog;
using EvenMoreOverpoweredJourney.Core.Localization;

namespace EvenMoreOverpoweredJourney.Bestiary.Detail
{
    internal readonly struct BestiaryDetailSection
    {
        public readonly string Title;
        public readonly string Body;

        public BestiaryDetailSection(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }

    internal static class BestiaryDetailInfoComposer
    {
        public static List<BestiaryDetailSection> Compose(BestiaryNpcMeta meta, BestiaryFaceMode face)
        {
            var sections = new List<BestiaryDetailSection>();
            if (meta == null)
                return sections;

            bool wasFound = meta.Entry != null && BestiaryProgressResolver.WasEverFound(meta.Entry);
            bool fullyUnlocked = meta.Entry != null && BestiaryProgressResolver.IsFullyUnlockedInTracker(meta.Entry);

            if (BestiaryDetailFieldMatrix.ShouldShow(face, BestiaryDetailFieldId.DisplayName, wasFound, fullyUnlocked))
            {
                sections.Add(new BestiaryDetailSection(
                    EOPJText.UI("BestiaryDetailField_Name"),
                    meta.DisplayName ?? "?"));
            }

            if (BestiaryDetailFieldMatrix.ShouldShow(face, BestiaryDetailFieldId.ModSource, wasFound, fullyUnlocked))
            {
                string modLabel = meta.ModKey == "Terraria"
                    ? EOPJText.UI("BestiaryModTipVanilla")
                    : meta.ModKey ?? "?";
                sections.Add(new BestiaryDetailSection(
                    EOPJText.UI("BestiaryDetailField_Mod"),
                    modLabel));
            }

            if (meta.Entry == null)
            {
                if (BestiaryDetailFieldMatrix.ShouldShow(face, BestiaryDetailFieldId.UnlockState, wasFound, fullyUnlocked))
                {
                    sections.Add(new BestiaryDetailSection(
                        EOPJText.UI("BestiaryDetailField_Status"),
                        EOPJText.UI("BestiaryDetailUnregistered")));
                }

                return sections;
            }

            if (BestiaryDetailFieldMatrix.ShouldShow(face, BestiaryDetailFieldId.UnlockState, wasFound, fullyUnlocked))
            {
                sections.Add(new BestiaryDetailSection(
                    EOPJText.UI("BestiaryDetailField_Status"),
                    EOPJText.UIFormat("BestiaryDetailUnlockFmt", BestiaryProgressResolver.GetUnlockState(meta.Entry))));
            }

            AppendInfoElementSections(meta.Entry, face, wasFound, fullyUnlocked, sections);

            if (BestiaryDetailFieldMatrix.ShouldShow(face, BestiaryDetailFieldId.NetIdDebug, wasFound, fullyUnlocked))
            {
                sections.Add(new BestiaryDetailSection(
                    EOPJText.UI("BestiaryDetailField_Debug"),
                    EOPJText.UIFormat("BestiaryDetailNetIdFmt", meta.NetId)));
            }

            return sections;
        }

        private static void AppendInfoElementSections(
            BestiaryEntry entry,
            BestiaryFaceMode face,
            bool wasFound,
            bool fullyUnlocked,
            List<BestiaryDetailSection> sections)
        {
            if (entry?.Info == null)
                return;

            for (int i = 0; i < entry.Info.Count; i++)
            {
                IBestiaryInfoElement el = entry.Info[i];
                if (el == null)
                    continue;

                if (!TryMapField(el, out BestiaryDetailFieldId fieldId))
                    continue;

                if (!BestiaryDetailFieldMatrix.ShouldShow(face, fieldId, wasFound, fullyUnlocked))
                    continue;

                BestiaryDetailFieldMatrix.FieldVisibility vis =
                    BestiaryDetailFieldMatrix.GetVisibility(face, fieldId, wasFound, fullyUnlocked);
                string body = TryFormatElement(el, fieldId, vis);
                if (string.IsNullOrWhiteSpace(body))
                    continue;

                sections.Add(new BestiaryDetailSection(GetFieldTitle(fieldId), body));
            }
        }

        private static bool TryMapField(IBestiaryInfoElement el, out BestiaryDetailFieldId fieldId)
        {
            fieldId = default;
            string name = el.GetType().Name;

            // 原版 tML（ilspy 反编译 tModLoader.dll）常见类型名
            switch (name)
            {
                case "FlavorTextBestiaryInfoElement":
                    fieldId = BestiaryDetailFieldId.FlavorText;
                    return true;
                case "NPCKillCounterInfoElement":
                    fieldId = BestiaryDetailFieldId.KillCount;
                    return true;
                case "ItemDropBestiaryInfoElement":
                case "ItemFromCatchingNPCBestiaryInfoElement":
                    fieldId = BestiaryDetailFieldId.Drops;
                    return true;
                case "NPCStatsReportInfoElement":
                    fieldId = BestiaryDetailFieldId.Stats;
                    return true;
                case "NPCPortraitInfoElement":
                case "NamePlateInfoElement":
                case "NPCNetIdBestiaryInfoElement":
                    return false;
            }

            if (name.Contains("Flavor", StringComparison.OrdinalIgnoreCase))
            {
                fieldId = BestiaryDetailFieldId.FlavorText;
                return true;
            }

            if (name.Contains("Kill", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Counter", StringComparison.OrdinalIgnoreCase))
            {
                fieldId = BestiaryDetailFieldId.KillCount;
                return true;
            }

            if (name.Contains("Drop", StringComparison.OrdinalIgnoreCase))
            {
                fieldId = BestiaryDetailFieldId.Drops;
                return true;
            }

            if (name.Contains("Stat", StringComparison.OrdinalIgnoreCase))
            {
                fieldId = BestiaryDetailFieldId.Stats;
                return true;
            }

            if (name.Contains("Spawn", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Biome", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("ModBiome", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("ModSource", StringComparison.OrdinalIgnoreCase))
            {
                fieldId = BestiaryDetailFieldId.SpawnConditions;
                return true;
            }

            return false;
        }

        private static string GetFieldTitle(BestiaryDetailFieldId id) => id switch
        {
            BestiaryDetailFieldId.FlavorText => EOPJText.UI("BestiaryDetailField_Flavor"),
            BestiaryDetailFieldId.KillCount => EOPJText.UI("BestiaryDetailField_Kills"),
            BestiaryDetailFieldId.Drops => EOPJText.UI("BestiaryDetailField_Drops"),
            BestiaryDetailFieldId.Stats => EOPJText.UI("BestiaryDetailField_Stats"),
            BestiaryDetailFieldId.SpawnConditions => EOPJText.UI("BestiaryDetailField_Spawn"),
            _ => id.ToString()
        };

        private static string TryFormatElement(
            IBestiaryInfoElement el,
            BestiaryDetailFieldId fieldId,
            BestiaryDetailFieldMatrix.FieldVisibility visibility)
        {
            if (visibility == BestiaryDetailFieldMatrix.FieldVisibility.VisibleWithoutNumbers &&
                (fieldId == BestiaryDetailFieldId.Drops || fieldId == BestiaryDetailFieldId.Stats))
            {
                return EOPJText.UI("BestiaryDetailField_NoNumbers");
            }

            try
            {
                string key = null;
                var t = el.GetType();
                var getKey = t.GetMethod("GetDisplayNameKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (getKey?.Invoke(el, null) is string k && !string.IsNullOrWhiteSpace(k))
                    key = k;

                if (!string.IsNullOrWhiteSpace(key))
                {
                    string txt = Language.GetTextValue(key);
                    if (!string.IsNullOrWhiteSpace(txt))
                        return txt;
                }
            }
            catch
            {
                // ignored
            }

            return el.GetType().Name;
        }
    }
}
