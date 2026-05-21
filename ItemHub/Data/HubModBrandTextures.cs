using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.ItemHub.Data
{
    /// <summary>
    /// ԭ�� / tModLoader ģ�鰴ťСͼ�꣺�� external item browser <c>AssetModFilter</c> ��ͬ˼·����ʹ�ñ�ģ���ڴ���� PNG��
    /// �������� Terraria / ModLoader �� <c>icon</c> ��Դ·�������ڼ���ʱ�������
    /// </summary>
    public static class HubModBrandTextures
    {
        private static Texture2D _vanillaBrand;
        private static Texture2D _tmlBrand;

        public static Texture2D TryGetVanillaBrandIcon()
        {
            if (_vanillaBrand != null)
                return _vanillaBrand;

            Mod self = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            try
            {
                if (self != null && self.HasAsset(global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ModBrandVanilla))
                    _vanillaBrand = self.Assets.Request<Texture2D>(global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ModBrandVanilla, AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                _vanillaBrand = null;
            }

            if (_vanillaBrand == null && TextureAssets.Logo != null)
            {
                try
                {
                    _vanillaBrand = TextureAssets.Logo.Value;
                }
                catch
                {
                    _vanillaBrand = null;
                }
            }

            return _vanillaBrand;
        }

        public static Texture2D TryGetTModBrandIcon()
        {
            if (_tmlBrand != null)
                return _tmlBrand;

            Mod self = ModContent.GetInstance<global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney>();
            try
            {
                if (self != null && self.HasAsset(global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ModBrandTModLoader))
                    _tmlBrand = self.Assets.Request<Texture2D>(global::EvenMoreOverpoweredJourney.EvenMoreOverpoweredJourney.ModBrandTModLoader, AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                _tmlBrand = null;
            }

            if (_tmlBrand == null)
            {
                foreach (string name in new[] { "ModLoader", "tModLoader" })
                {
                    if (!ModLoader.TryGetMod(name, out Mod ml))
                        continue;
                    try
                    {
                        if (ml.HasAsset("icon"))
                            _tmlBrand = ml.Assets.Request<Texture2D>("icon", AssetRequestMode.ImmediateLoad).Value;
                    }
                    catch
                    {
                        _tmlBrand = null;
                    }

                    if (_tmlBrand != null)
                        break;
                }
            }

            return _tmlBrand;
        }
    }
}
