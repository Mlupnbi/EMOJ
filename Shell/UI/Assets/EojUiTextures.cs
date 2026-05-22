using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace EvenMoreOverpoweredJourney.Shell.UI.Assets
{
    /// <summary>各页签 UI 贴图统一入口；优先模组 <c>Assets/UI</c>，缺失时回退原版。</summary>
    public static class EojUiTextures
    {
        public static class Common
        {
            public static Texture2D Cursor2 => EojUiTextureCache.Get(EojUiAssetCatalog.Common.Cursor2);

            public static Texture2D SearchCancel => EojUiTextureCache.Get(EojUiAssetCatalog.Common.SearchCancel);

            public static Texture2D Divider => EojUiTextureCache.Get(EojUiAssetCatalog.Common.Divider);

            public static Texture2D InventoryBack =>
                EojUiTextureCache.Get(EojUiAssetCatalog.Common.InventoryBack) ??
                TextureAssets.InventoryBack.Value;
        }

        public static class Shell
        {
            public static Texture2D ResizeHandle => EojUiTextureCache.Get(EojUiAssetCatalog.Shell.ResizeHandle);

            public static Texture2D TabResearch => EojUiTextureCache.Get(EojUiAssetCatalog.Shell.TabResearch);

            public static Texture2D TabBuff => EojUiTextureCache.Get(EojUiAssetCatalog.Shell.TabBuff);

            public static Texture2D TabStorage => EojUiTextureCache.Get(EojUiAssetCatalog.Shell.TabStorage);

            public static Texture2D TabBestiary => EojUiTextureCache.Get(EojUiAssetCatalog.Shell.TabBestiary);
        }

        public static class Buff
        {
            public static Texture2D ButtonPlay => EojUiTextureCache.Get(EojUiAssetCatalog.Buff.ButtonPlay);

            public static Texture2D NpcHappiness => EojUiTextureCache.Get(EojUiAssetCatalog.Buff.NpcHappiness);
        }

        public static class ItemHub
        {
            public static Texture2D ModBrandVanilla => EojUiTextureCache.Get(EojUiAssetCatalog.ItemHub.ModBrandVanilla);

            public static Texture2D ModBrandTModLoader => EojUiTextureCache.Get(EojUiAssetCatalog.ItemHub.ModBrandTModLoader);

            public static Texture2D FilterButton => EojUiTextureCache.Get(EojUiAssetCatalog.ItemHub.FilterButton);

            public static Texture2D SortAsc => EojUiTextureCache.Get(EojUiAssetCatalog.ItemHub.SortAsc);

            public static Texture2D SortDesc => EojUiTextureCache.Get(EojUiAssetCatalog.ItemHub.SortDesc);

            public static Texture2D ViewCard => EojUiTextureCache.Get(EojUiAssetCatalog.ItemHub.ViewCard);

            public static Texture2D ViewList => EojUiTextureCache.Get(EojUiAssetCatalog.ItemHub.ViewList);
        }

        public static class Bestiary
        {
            public static Texture2D SlotBack => EojUiTextureCache.Get(EojUiAssetCatalog.Bestiary.SlotBack);

            public static Texture2D SlotFront => EojUiTextureCache.Get(EojUiAssetCatalog.Bestiary.SlotFront);

            public static Texture2D SlotOverlay => EojUiTextureCache.Get(EojUiAssetCatalog.Bestiary.SlotOverlay);

            public static Texture2D SlotSelection => EojUiTextureCache.Get(EojUiAssetCatalog.Bestiary.SlotSelection);

            public static Texture2D IconLocked => EojUiTextureCache.Get(EojUiAssetCatalog.Bestiary.IconLocked);

            public static Texture2D IconTagsShadow => EojUiTextureCache.Get(EojUiAssetCatalog.Bestiary.IconTagsShadow);

            public static Texture2D NpcHappiness => EojUiTextureCache.Get(EojUiAssetCatalog.Bestiary.NpcHappiness);
        }

        public static Texture2D ResolveVanillaUiPath(string vanillaAssetPath) =>
            EojUiTextureCache.ResolveVanillaPath(vanillaAssetPath);
    }
}
