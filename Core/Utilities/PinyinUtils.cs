using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace EvenMoreOverpoweredJourney.Core.Utilities
{
    /// <summary>������ƴ������ĸ��Ƕ�� TinyPinyin���������ⲿ DLL����ʧ��ʱ������ ASCII ��ĸ��������ƥ�䡣</summary>
    public static class PinyinUtils
    {
        private const string EmbeddedResourceName = "TinyPinyinSatellite.dll";
        private static readonly MethodInfo GetPinyinInitialsMethod;

        static PinyinUtils()
        {
            try
            {
                Assembly host = typeof(PinyinUtils).Assembly;
                using Stream stream = host.GetManifestResourceStream(EmbeddedResourceName);
                if (stream == null)
                    return;
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                Assembly tiny = Assembly.Load(ms.ToArray());
                Type helper = tiny.GetType("TinyPinyin.PinyinHelper", throwOnError: false)
                    ?? tiny.GetType("PinyinHelper", throwOnError: false);
                if (helper == null)
                {
                    Type[] types = Type.EmptyTypes;
                    try
                    {
                        types = tiny.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        types = ex.Types ?? Type.EmptyTypes;
                    }

                    foreach (Type t in types)
                    {
                        if (t != null && t.Name == "PinyinHelper")
                        {
                            helper = t;
                            break;
                        }
                    }
                }

                if (helper != null)
                {
                    GetPinyinInitialsMethod = helper.GetMethod(
                        "GetPinyinInitials",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(string), typeof(string) },
                        null);
                }
            }
            catch
            {
                // ���� GetPinyinInitialsMethod Ϊ null���߻����߼�
            }
        }

        public static string GetPinyinInitials(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (GetPinyinInitialsMethod != null)
            {
                try
                {
                    object r = GetPinyinInitialsMethod.Invoke(null, new object[] { text, string.Empty });
                    if (r is string s && !string.IsNullOrEmpty(s))
                        return s.ToLowerInvariant();
                }
                catch
                {
                    // fall through
                }
            }

            return FallbackAsciiInitials(text);
        }

        private static string FallbackAsciiInitials(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char ch in text)
            {
                if (ch < 128 && char.IsLetter(ch))
                    sb.Append(char.ToLowerInvariant(ch));
                else if (ch < 128 && char.IsDigit(ch))
                    sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
