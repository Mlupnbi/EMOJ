using System;
using System.Collections.Generic;

namespace EvenMoreOverpoweredJourney.Shell.UI.Assets
{
    /// <summary>
    /// 登记所有「曾用泰拉原版 UI/槽位贴图」的模组路径与原版回退路径。
    /// 将 PNG 放到 <c>Assets/UI/{Tab}/</c> 即可覆盖；未放置时自动用原版。
    /// </summary>
    internal static class EojUiAssetCatalog
    {
        private static readonly Dictionary<string, string> VanillaToPrimaryModPath =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static EojUiAssetCatalog()
        {
            RegisterVanillaMapping(Common.Cursor2);
            RegisterVanillaMapping(Common.SearchCancel);
            RegisterVanillaMapping(Common.Divider);
            RegisterVanillaMapping(Common.InventoryBack);
            RegisterVanillaMapping(Buff.ButtonPlay);
            RegisterVanillaMapping(Buff.NpcHappiness);
            RegisterVanillaMapping(Bestiary.SlotBack);
            RegisterVanillaMapping(Bestiary.SlotFront);
            RegisterVanillaMapping(Bestiary.SlotOverlay);
            RegisterVanillaMapping(Bestiary.SlotSelection);
            RegisterVanillaMapping(Bestiary.IconLocked);
            RegisterVanillaMapping(Bestiary.IconTagsShadow);
            RegisterVanillaMapping(Bestiary.NpcHappiness);
        }

        private static void RegisterVanillaMapping(EojUiAssetEntry entry)
        {
            if (string.IsNullOrEmpty(entry.VanillaFallback) || entry.ModPaths.Length == 0)
                return;

            VanillaToPrimaryModPath[entry.VanillaFallback] = entry.ModPaths[0];
        }

        public static string TryGetPrimaryModPath(string vanillaAssetPath)
        {
            if (string.IsNullOrEmpty(vanillaAssetPath))
                return null;

            if (VanillaToPrimaryModPath.TryGetValue(vanillaAssetPath, out string mapped))
                return mapped;

            return InferModPathFromVanillaUi(vanillaAssetPath);
        }

        /// <summary>将 <c>Images/UI/...</c> 映射到 <c>Assets/UI/{Tab}/...</c>（图鉴生态底图等动态路径）。</summary>
        public static string InferModPathFromVanillaUi(string vanillaAssetPath)
        {
            const string prefix = "Images/UI/";
            if (!vanillaAssetPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            string rest = vanillaAssetPath.Substring(prefix.Length);
            if (rest.StartsWith("Bestiary/", StringComparison.OrdinalIgnoreCase))
                return "Assets/UI/Bestiary/" + rest.Substring("Bestiary/".Length);

            if (rest.IndexOf('/') < 0)
                return "Assets/UI/Common/" + rest;

            return "Assets/UI/Vanilla/" + rest.Replace('/', '_');
        }

        public static class Common
        {
            public static readonly EojUiAssetEntry Cursor2 = new(
                EojUiTab.Common,
                "Images/UI/Cursor_2",
                "Assets/UI/Common/Cursor_2");

            public static readonly EojUiAssetEntry SearchCancel = new(
                EojUiTab.Common,
                "Images/UI/SearchCancel",
                "Assets/UI/Common/SearchCancel");

            public static readonly EojUiAssetEntry Divider = new(
                EojUiTab.Common,
                "Images/UI/Divider",
                "Assets/UI/Common/Divider");

            /// <summary>物品槽底（原版 <c>Images/Inventory_Back</c>，非 Images/UI）。</summary>
            public static readonly EojUiAssetEntry InventoryBack = new(
                EojUiTab.Common,
                "Images/Inventory_Back",
                "Assets/UI/Common/Inventory_Back");
        }

        public static class Shell
        {
            public static readonly EojUiAssetEntry ResizeHandle = new(
                EojUiTab.Shell,
                null,
                "Assets/UI/Shell/Handle",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ResizeHandleCursor,
                "Assets/UI/Handle");

            public static readonly EojUiAssetEntry TabResearch = new(
                EojUiTab.Shell,
                null,
                "Assets/UI/Shell/TabResearch",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.TabIconResearch,
                "Assets/UI/TabResearch");

            public static readonly EojUiAssetEntry TabBuff = new(
                EojUiTab.Shell,
                null,
                "Assets/UI/Shell/TabBuff",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.TabIconBuff,
                "Assets/UI/TabBuff");

            public static readonly EojUiAssetEntry TabStorage = new(
                EojUiTab.Shell,
                null,
                "Assets/UI/Shell/TabStorage",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.TabIconStorage,
                "Assets/UI/TabStorage");

            public static readonly EojUiAssetEntry TabBestiary = new(
                EojUiTab.Shell,
                null,
                "Assets/UI/Shell/TabBestiary",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.TabIconBestiary,
                "Assets/UI/TabBestiary");

            public static readonly EojUiAssetEntry TabBlueprint = new(
                EojUiTab.Shell,
                null,
                "Assets/UI/Shell/TabBlueprint",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.TabIconBlueprint,
                "Assets/UI/TabBlueprint");
        }

        public static class Research
        {
            // 研究页暂无独立 Images/UI 贴图；槽位使用 Common.InventoryBack。
        }

        public static class Buff
        {
            public static readonly EojUiAssetEntry ButtonPlay = new(
                EojUiTab.Buff,
                "Images/UI/ButtonPlay",
                "Assets/UI/Buff/ButtonPlay");

            public static readonly EojUiAssetEntry NpcHappiness = new(
                EojUiTab.Buff,
                "Images/UI/NPCHappiness",
                "Assets/UI/Buff/NPCHappiness",
                "Assets/UI/Common/NPCHappiness");
        }

        public static class ItemHub
        {
            public static readonly EojUiAssetEntry ModBrandVanilla = new(
                EojUiTab.ItemHub,
                null,
                "Assets/UI/ItemHub/ModBrandVanilla",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ModBrandVanilla,
                "Assets/UI/ModBrandVanilla");

            public static readonly EojUiAssetEntry ModBrandTModLoader = new(
                EojUiTab.ItemHub,
                null,
                "Assets/UI/ItemHub/ModBrandTModLoader",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ModBrandTModLoader,
                "Assets/UI/ModBrandTModLoader");

            public static readonly EojUiAssetEntry FilterButton = new(
                EojUiTab.ItemHub,
                null,
                "Assets/UI/ItemHub/FilterButton",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubFilterButton,
                "Assets/UI/ItemHubFilterButton");

            public static readonly EojUiAssetEntry SortAsc = new(
                EojUiTab.ItemHub,
                null,
                "Assets/UI/ItemHub/SortOrderAsc",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubSortOrderAsc,
                "Assets/UI/ItemHubSortOrderASC",
                "Assets/UI/ItemHubSortAsc",
                "Assets/UI/SortOrderAsc");

            public static readonly EojUiAssetEntry SortDesc = new(
                EojUiTab.ItemHub,
                null,
                "Assets/UI/ItemHub/SortOrderDesc",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubSortOrderDesc,
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubSortOrderDescAlt,
                "Assets/UI/ItemHubSortDesc",
                "Assets/UI/SortOrderDesc");

            public static readonly EojUiAssetEntry ViewCard = new(
                EojUiTab.ItemHub,
                null,
                "Assets/UI/ItemHub/ViewCard",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubViewCard,
                "Assets/UI/ItemHubViewCard");

            public static readonly EojUiAssetEntry ViewList = new(
                EojUiTab.ItemHub,
                null,
                "Assets/UI/ItemHub/ViewList",
                global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ItemHubViewList,
                "Assets/UI/ItemHubViewList");
        }

        public static class Bestiary
        {
            public static readonly EojUiAssetEntry SlotBack = new(
                EojUiTab.Bestiary,
                BestiaryVanillaPaths.SlotBack,
                "Assets/UI/Bestiary/Slot_Back");

            public static readonly EojUiAssetEntry SlotFront = new(
                EojUiTab.Bestiary,
                BestiaryVanillaPaths.SlotFront,
                "Assets/UI/Bestiary/Slot_Front");

            public static readonly EojUiAssetEntry SlotOverlay = new(
                EojUiTab.Bestiary,
                BestiaryVanillaPaths.SlotOverlay,
                "Assets/UI/Bestiary/Slot_Overlay");

            public static readonly EojUiAssetEntry SlotSelection = new(
                EojUiTab.Bestiary,
                BestiaryVanillaPaths.SlotSelection,
                "Assets/UI/Bestiary/Slot_Selection");

            public static readonly EojUiAssetEntry IconLocked = new(
                EojUiTab.Bestiary,
                BestiaryVanillaPaths.IconLocked,
                "Assets/UI/Bestiary/Icon_Locked");

            public static readonly EojUiAssetEntry IconTagsShadow = new(
                EojUiTab.Bestiary,
                "Images/UI/Bestiary/Icon_Tags_Shadow",
                "Assets/UI/Bestiary/Icon_Tags_Shadow");

            public static readonly EojUiAssetEntry NpcHappiness = new(
                EojUiTab.Bestiary,
                "Images/UI/NPCHappiness",
                "Assets/UI/Bestiary/NPCHappiness",
                "Assets/UI/Buff/NPCHappiness",
                "Assets/UI/Common/NPCHappiness");
        }

        /// <summary>图鉴格子等仍用原版资源名字符串比对时的路径常量。</summary>
        public static class BestiaryVanillaPaths
        {
            public const string SlotBack = "Images/UI/Bestiary/Slot_Back";
            public const string SlotFront = "Images/UI/Bestiary/Slot_Front";
            public const string SlotOverlay = "Images/UI/Bestiary/Slot_Overlay";
            public const string SlotSelection = "Images/UI/Bestiary/Slot_Selection";
            public const string IconLocked = "Images/UI/Bestiary/Icon_Locked";
        }
    }
}
