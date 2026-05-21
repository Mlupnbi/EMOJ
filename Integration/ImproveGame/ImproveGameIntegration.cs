using System;
using System.Reflection;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Integration.ImproveGame
{
    /// <summary>̽�⡸���õ����项(ImproveGame) �Ķ��� Buff ���������������棬������ EMOJ �ظ����ӡ�</summary>
    public static class ImproveGameIntegration
    {
        public const string ModSlug = "ImproveGame";

        private static bool _loaded;
        private static int _extraPlayerBuffSlots;
        private static bool _dontDeleteBuff;
        private static bool _bestiaryQuickUnlock;
        private static string _lastProbeError;

        public static bool IsLoaded => _loaded;

        /// <summary>ImproveGame �����ö��� Buff ����&gt;0��ʱ���ɶԷ��ṩ��λ��EMOJ ���ٹ��� <see cref="Mod.ExtraPlayerBuffSlots"/>��</summary>
        public static bool DelegatesExtraBuffSlots => _loaded && _extraPlayerBuffSlots > 0;

        /// <summary>ImproveGame �ѿ���������������桹ʱ���ɶԷ�? IL ���������� Buff��</summary>
        public static bool DelegatesDeathBuffPreserve => _loaded && _dontDeleteBuff;

        public static int ImproveGameExtraBuffSlots => _extraPlayerBuffSlots;

        public static bool ImproveGameDontDeleteBuff => _dontDeleteBuff;

        /// <summary>ImproveGame?????????????????????</summary>
        public static bool ImproveGameBestiaryQuickUnlock => _loaded && _bestiaryQuickUnlock;

        public static string LastProbeError => _lastProbeError;

        public static void Refresh()
        {
            _loaded = false;
            _extraPlayerBuffSlots = 0;
            _dontDeleteBuff = false;
            _bestiaryQuickUnlock = false;
            _lastProbeError = null;

            if (!ModLoader.TryGetMod(ModSlug, out Mod mod) || mod.Code == null)
                return;

            _loaded = true;

            if (!TryReadImproveConfigs(mod, out int extraSlots, out bool dontDelete, out bool bestiaryQuick, out string error))
            {
                _lastProbeError = error;
                return;
            }

            _extraPlayerBuffSlots = Math.Max(0, extraSlots);
            _dontDeleteBuff = dontDelete;
            _bestiaryQuickUnlock = bestiaryQuick;
        }

        private static bool TryReadImproveConfigs(Mod mod, out int extraSlots, out bool dontDelete, out bool bestiaryQuickUnlock, out string error)
        {
            extraSlots = 0;
            dontDelete = false;
            bestiaryQuickUnlock = false;
            error = null;

            Type configType = mod.Code.GetType("ImproveGame.Common.Configs.ImproveConfigs");
            if (configType == null)
            {
                error = "ImproveConfigs type not found";
                return false;
            }

            PropertyInfo instanceProp = configType.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static);

            if (instanceProp == null)
            {
                error = "ImproveConfigs.Instance not found";
                return false;
            }

            object instance = instanceProp.GetValue(null);
            if (instance == null)
            {
                error = "ImproveConfigs.Instance is null";
                return false;
            }

            FieldInfo extraField = configType.GetField(
                "ExtraPlayerBuffSlots",
                BindingFlags.Public | BindingFlags.Instance);

            FieldInfo dontDeleteField = configType.GetField(
                "DontDeleteBuff",
                BindingFlags.Public | BindingFlags.Instance);

            FieldInfo bestiaryQuickField = configType.GetField(
                "BestiaryQuickUnlock",
                BindingFlags.Public | BindingFlags.Instance);

            if (extraField == null || dontDeleteField == null)
            {
                error = "ImproveConfigs buff fields not found";
                return false;
            }

            try
            {
                extraSlots = Convert.ToInt32(extraField.GetValue(instance));
                dontDelete = Convert.ToBoolean(dontDeleteField.GetValue(instance));
                if (bestiaryQuickField != null)
                    bestiaryQuickUnlock = Convert.ToBoolean(bestiaryQuickField.GetValue(instance));
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
