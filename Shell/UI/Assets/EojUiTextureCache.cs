using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Shell.UI.Assets
{
    /// <summary>按 <see cref="EojUiAssetCatalog"/> 加载并缓存 UI 贴图；模组 PNG 优先，否则回退原版。</summary>
    internal static class EojUiTextureCache
    {
        private static Mod _mod;
        private static readonly Dictionary<EojUiAssetEntry, Texture2D> ByEntry = new();
        private static readonly Dictionary<string, Texture2D> ByVanillaPath = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<EojUiTab> WarmedTabs = new();

        public static void Initialize(Mod mod)
        {
            Unload();
            _mod = mod;
            // 不在 Load 阶段预热：旧版 tmod 可能尚无子目录贴图，预热会触发大量 AssetLoadException 日志。
        }

        public static void Unload()
        {
            _mod = null;
            ByEntry.Clear();
            ByVanillaPath.Clear();
            WarmedTabs.Clear();
        }

        public static void WarmTab(EojUiTab tab)
        {
            if (Main.dedServ || WarmedTabs.Contains(tab))
                return;

            WarmedTabs.Add(tab);
            switch (tab)
            {
                case EojUiTab.Common:
                    _ = Get(EojUiAssetCatalog.Common.Cursor2);
                    _ = Get(EojUiAssetCatalog.Common.SearchCancel);
                    _ = Get(EojUiAssetCatalog.Common.Divider);
                    _ = Get(EojUiAssetCatalog.Common.InventoryBack);
                    break;
                case EojUiTab.Shell:
                    _ = Get(EojUiAssetCatalog.Shell.ResizeHandle);
                    _ = Get(EojUiAssetCatalog.Shell.TabResearch);
                    _ = Get(EojUiAssetCatalog.Shell.TabBuff);
                    _ = Get(EojUiAssetCatalog.Shell.TabStorage);
                    _ = Get(EojUiAssetCatalog.Shell.TabBestiary);
                    break;
                case EojUiTab.Buff:
                    _ = Get(EojUiAssetCatalog.Buff.ButtonPlay);
                    _ = Get(EojUiAssetCatalog.Buff.NpcHappiness);
                    break;
                case EojUiTab.ItemHub:
                    _ = Get(EojUiAssetCatalog.ItemHub.FilterButton);
                    _ = Get(EojUiAssetCatalog.ItemHub.SortAsc);
                    _ = Get(EojUiAssetCatalog.ItemHub.SortDesc);
                    _ = Get(EojUiAssetCatalog.ItemHub.ViewCard);
                    _ = Get(EojUiAssetCatalog.ItemHub.ViewList);
                    break;
                case EojUiTab.Bestiary:
                    _ = Get(EojUiAssetCatalog.Bestiary.SlotBack);
                    _ = Get(EojUiAssetCatalog.Bestiary.SlotFront);
                    _ = Get(EojUiAssetCatalog.Bestiary.SlotOverlay);
                    _ = Get(EojUiAssetCatalog.Bestiary.SlotSelection);
                    _ = Get(EojUiAssetCatalog.Bestiary.IconLocked);
                    _ = Get(EojUiAssetCatalog.Bestiary.IconTagsShadow);
                    _ = Get(EojUiAssetCatalog.Bestiary.NpcHappiness);
                    break;
            }
        }

        public static Texture2D Get(EojUiAssetEntry entry)
        {
            if (Main.dedServ)
                return null;

            if (ByEntry.TryGetValue(entry, out Texture2D cached) && cached != null)
                return cached;

            Texture2D loaded = LoadEntry(entry);
            ByEntry[entry] = loaded;
            if (!string.IsNullOrEmpty(entry.VanillaFallback) && loaded != null)
                ByVanillaPath[entry.VanillaFallback] = loaded;

            return loaded;
        }

        /// <summary>动态原版路径（如图鉴生态 Background_*）：先查模组镜像路径，再回退 <see cref="Main.Assets"/>。</summary>
        public static Texture2D ResolveVanillaPath(string vanillaAssetPath)
        {
            if (Main.dedServ || string.IsNullOrEmpty(vanillaAssetPath))
                return null;

            if (ByVanillaPath.TryGetValue(vanillaAssetPath, out Texture2D cached) && cached != null)
                return cached;

            Texture2D loaded = null;
            string modPath = EojUiAssetCatalog.TryGetPrimaryModPath(vanillaAssetPath);
            if (!string.IsNullOrEmpty(modPath))
                loaded = TryLoadModAsset(modPath);

            if (loaded == null)
                loaded = TryLoadVanilla(vanillaAssetPath);

            ByVanillaPath[vanillaAssetPath] = loaded;
            return loaded;
        }

        public static Texture2D TryLoadFirst(params string[] modPaths)
        {
            if (Main.dedServ || modPaths == null)
                return null;

            foreach (string path in modPaths)
            {
                Texture2D tex = TryLoadModAsset(path);
                if (tex != null)
                    return tex;
            }

            return null;
        }

        private static Texture2D LoadEntry(EojUiAssetEntry entry)
        {
            Texture2D fromMod = TryLoadFirst(entry.ModPaths);
            if (fromMod != null)
                return fromMod;

            if (entry.VanillaFallback == "Images/Inventory_Back")
                return TextureAssets.InventoryBack.Value;

            return TryLoadVanilla(entry.VanillaFallback);
        }

        private static Texture2D TryLoadModAsset(string path)
        {
            if (_mod == null || string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                if (!_mod.HasAsset(path))
                    return null;

                return _mod.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                return null;
            }
        }

        private static Texture2D TryLoadVanilla(string assetPath)
        {
            if (Main.dedServ || string.IsNullOrEmpty(assetPath))
                return null;

            try
            {
                return Main.Assets.Request<Texture2D>(assetPath, AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
