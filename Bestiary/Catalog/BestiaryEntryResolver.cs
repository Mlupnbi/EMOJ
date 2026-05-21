using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Bestiary.Catalog
{
    internal static class BestiaryEntryResolver
    {
        private static readonly string[] PortraitIdMemberNames =
        {
            "_npcNetId",
            "NPCNetId",
            "NpcNetId",
            "NetID",
            "NetId",
            "netId",
            "npcNetId",
            "_npcType",
            "NpcType",
            "Type"
        };

        public static bool TryGetNpcNetId(BestiaryEntry entry, out int netId)
        {
            netId = 0;
            if (entry == null)
                return false;

            if (entry.Icon != null && TryReadNpcIdFromObject(entry.Icon, out netId) && netId > 0)
                return true;

            if (entry.Info == null)
                return false;

            foreach (IBestiaryInfoElement el in entry.Info)
            {
                if (TryReadNpcIdFromObject(el, out netId) && netId > 0)
                    return true;
            }

            return false;
        }

        public static string ResolveDisplayName(BestiaryEntry entry, int netId, int catalogIndex)
        {
            if (netId <= 0 && entry != null)
                TryGetNpcNetId(entry, out netId);

            if (netId > 0 && netId < NPCLoader.NPCCount && NPCLoader.GetNPC(netId) != null)
            {
                string name = Lang.GetNPCNameValue(netId);
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }

            if (entry?.Icon != null && TryReadNpcIdFromObject(entry.Icon, out int fromIcon) && fromIcon > 0)
            {
                string name = Lang.GetNPCNameValue(fromIcon);
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }

            return catalogIndex >= 0 ? $"#{catalogIndex}" : "?";
        }

        private static bool TryReadNpcIdFromObject(object obj, out int netId)
        {
            netId = 0;
            if (obj == null)
                return false;

            Type t = obj.GetType();

            if (TryReadIntMember(t, obj, "_npcNetId", out netId) && netId > 0)
                return true;

            foreach (string name in PortraitIdMemberNames)
            {
                if (name == "_npcNetId")
                    continue;

                if (TryReadIntMember(t, obj, name, out netId) && netId > 0)
                    return true;
            }

            if (TryReadNullableIntMember(t, obj, "_npcNetId", out netId) && netId > 0)
                return true;

            return false;
        }

        private static bool TryReadIntMember(Type t, object obj, string name, out int value)
        {
            value = 0;
            FieldInfo field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field?.FieldType == typeof(int))
            {
                value = (int)field.GetValue(obj);
                return true;
            }

            PropertyInfo prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop?.PropertyType == typeof(int) && prop.CanRead)
            {
                value = (int)prop.GetValue(obj);
                return true;
            }

            return false;
        }

        private static bool TryReadNullableIntMember(Type t, object obj, string name, out int value)
        {
            value = 0;
            FieldInfo field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field?.FieldType == typeof(int?))
            {
                if (field.GetValue(obj) is int v)
                {
                    value = v;
                    return true;
                }
            }

            return false;
        }
    }
}
