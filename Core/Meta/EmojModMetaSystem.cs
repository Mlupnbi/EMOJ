using System.Reflection;
using Terraria.Localization;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Core.Meta
{
    /// <summary>���ػ����غ�д��ģ����ʾ������ ImproveGame ͬ���������°� tML �� DisplayName Ϊֻ�����跴�䣩��</summary>
    public sealed class EmojModMetaSystem : ModSystem
    {
        public override void OnLocalizationsLoaded()
        {
            Mod mod = ModContent.GetInstance<EvenMoreOverpoweredJourney>();
            string name = Language.GetTextValue("Mods.EvenMoreOverpoweredJourney.DisplayName");
            if (string.IsNullOrWhiteSpace(name))
                return;

            TrySetModDisplayName(mod, name);
        }

        private static void TrySetModDisplayName(Mod mod, string displayName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo field = typeof(Mod).GetField("displayName", flags)
                ?? typeof(Mod).GetField("<DisplayName>k__BackingField", flags);
            if (field != null && field.FieldType == typeof(string))
            {
                field.SetValue(mod, displayName);
                typeof(Mod).GetField("displayNameClean", flags)?.SetValue(mod, null);
            }
        }
    }
}
