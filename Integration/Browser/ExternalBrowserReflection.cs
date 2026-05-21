using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.UI;

namespace EvenMoreOverpoweredJourney.Integration.Browser
{
    /// <summary>�����ȡ�Ѵ򿪵��ⲿ��Ʒ����� ItemBrowser �����е�ǰ�ɼ���δɸѡ���أ�����Ʒ type��</summary>
    internal static class ExternalBrowserReflection
    {
        private static MethodInfo _cachedGetUiGeneric;
        private static Type _cachedItemBrowserType;
        private static Type _cachedUiLoaderType;
        private static Type _cachedItemButtonType;
        private static FieldInfo _cachedFilteredField;
        private static FieldInfo _cachedItemField;
        private static Assembly _cachedAssembly;

        public static bool TryGetVisibleItemTypes(out HashSet<int> types, out string status)
        {
            types = null;
            status = null;

            if (!ModLoader.TryGetMod("DragonLens", out Mod dlMod))
            {
                status = "δ�����ⲿ��Ʒ�����ģ��?";
                return false;
            }

            Assembly asm = dlMod.Code;
            if (!TryResolveBrowserTypes(asm, out Type itemBrowserType, out Type uiLoaderType, out Type itemButtonType, out string resolveError))
            {
                status = resolveError;
                return false;
            }

            MethodInfo getUiGeneric = _cachedGetUiGeneric;
            if (getUiGeneric == null)
            {
                status = "�Ҳ��� UILoader.GetUIState";
                return false;
            }

            object browserState;
            try
            {
                browserState = getUiGeneric.MakeGenericMethod(itemBrowserType).Invoke(null, null);
            }
            catch (Exception ex)
            {
                status = "GetUIState ʧ��: " + ex.Message;
                return false;
            }

            if (browserState == null)
            {
                status = "��Ʒ�����? UI δ��ʼ��";
                return false;
            }

            if (!IsBrowserVisible(browserState))
            {
                status = "���ȴ��ⲿ��Ʒ���������Ʒ����������?";
                return false;
            }

            FieldInfo optionsField = FindInstanceField(browserState, "options");
            if (optionsField?.GetValue(browserState) is not Shell.UI.UIGrid grid)
            {
                status = "�޷���ȡ ItemBrowser.options ����";
                return false;
            }

            if (!TryGetGridItems(grid, out IList elements))
            {
                status = "�޷���ȡ UIGrid ��Ԫ���б�";
                return false;
            }

            FieldInfo filteredField = _cachedFilteredField;
            FieldInfo itemField = _cachedItemField;

            types = new HashSet<int>();
            foreach (object el in elements)
            {
                if (el == null || !itemButtonType.IsInstanceOfType(el))
                    continue;

                if (filteredField != null && filteredField.GetValue(el) is true)
                    continue;

                if (el is UIElement ui && (ui.Width.Pixels <= 0.01f && ui.Width.Precent == 0f))
                    continue;

                if (itemField?.GetValue(el) is Terraria.Item item && item.type > Terraria.ID.ItemID.None)
                    types.Add(item.type);
            }

            if (types.Count == 0)
            {
                status = "�����Ѵ򿪵��޿ɼ���Ʒ�����ɸ�?/������";
                return false;
            }

            status = $"�Ѵ��ⲿ��Ʒ��������ڶ��? {types.Count} ���ɼ���Ʒ";
            return true;
        }

        private static bool TryResolveBrowserTypes(
            Assembly asm,
            out Type itemBrowserType,
            out Type uiLoaderType,
            out Type itemButtonType,
            out string error)
        {
            itemBrowserType = null;
            uiLoaderType = null;
            itemButtonType = null;
            error = null;

            if (_cachedAssembly == asm && _cachedItemBrowserType != null && _cachedUiLoaderType != null && _cachedItemButtonType != null)
            {
                itemBrowserType = _cachedItemBrowserType;
                uiLoaderType = _cachedUiLoaderType;
                itemButtonType = _cachedItemButtonType;
                return true;
            }

            itemBrowserType = asm.GetType("DragonLens.Content.Tools.Spawners.ItemBrowser", throwOnError: false);
            uiLoaderType = asm.GetType("DragonLens.Core.Loaders.UILoading.UILoader", throwOnError: false);
            itemButtonType = asm.GetType("DragonLens.Content.Tools.Spawners.ItemButton", throwOnError: false);
            if (itemBrowserType == null || uiLoaderType == null || itemButtonType == null)
            {
                error = "�ⲿ��Ʒ������汾�뷴��·��������?";
                return false;
            }

            MethodInfo getUiGeneric = null;
            foreach (MethodInfo m in uiLoaderType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name == "GetUIState" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
                {
                    getUiGeneric = m;
                    break;
                }
            }

            if (getUiGeneric == null)
            {
                error = "�Ҳ��� UILoader.GetUIState";
                return false;
            }

            _cachedAssembly = asm;
            _cachedItemBrowserType = itemBrowserType;
            _cachedUiLoaderType = uiLoaderType;
            _cachedItemButtonType = itemButtonType;
            _cachedGetUiGeneric = getUiGeneric;
            _cachedFilteredField = asm.GetType("DragonLens.Content.GUI.BrowserButton", throwOnError: false)
                ?.GetField("filtered", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            _cachedItemField = itemButtonType.GetField("item",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return true;
        }

        private static bool TryGetGridItems(Shell.UI.UIGrid grid, out IList list)
        {
            list = null;
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            foreach (string name in new[] { "_items", "Items", "items" })
            {
                FieldInfo fi = typeof(Shell.UI.UIGrid).GetField(name, bf);
                if (fi?.GetValue(grid) is IList il)
                {
                    list = il;
                    return true;
                }
            }

            PropertyInfo pi = typeof(Shell.UI.UIGrid).GetProperty("Items", bf);
            if (pi?.GetValue(grid) is IList pl)
            {
                list = pl;
                return true;
            }

            return false;
        }

        private static bool IsBrowserVisible(object browserState)
        {
            for (Type t = browserState.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField("visible",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f?.GetValue(browserState) is bool vis)
                    return vis;
            }

            return true;
        }

        private static FieldInfo FindInstanceField(object target, string name)
        {
            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                    return f;
            }

            return null;
        }
    }
}
