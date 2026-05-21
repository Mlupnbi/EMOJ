using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace EvenMoreOverpoweredJourney.Core.Utilities
{
    /// <summary>���ı�д��ϵͳ�����壨���� SDL2������ Win32����</summary>
    internal static class GameClipboard
    {
        public static bool TrySetText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            if (TrySdlClipboard(text))
                return true;

            if (OperatingSystem.IsWindows())
                return TryWin32Clipboard(text);

            return false;
        }

        private static bool TrySdlClipboard(string text)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = asm.GetName().Name ?? "";
                if (!name.Contains("SDL2", StringComparison.OrdinalIgnoreCase))
                    continue;

                Type sdl = asm.GetType("SDL2.SDL", throwOnError: false);
                MethodInfo set = sdl?.GetMethod("SDL_SetClipboardText",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(string) },
                    modifiers: null);
                if (set == null)
                    continue;

                try
                {
                    set.Invoke(null, new object[] { text });
                    return true;
                }
                catch
                {
                    /* */
                }
            }

            return false;
        }

        private static bool TryWin32Clipboard(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
                return false;

            try
            {
                EmptyClipboard();
                byte[] bytes = Encoding.Unicode.GetBytes(text + '\0');
                IntPtr hGlobal = Marshal.AllocHGlobal(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, hGlobal, bytes.Length);
                    if (SetClipboardData(CfUnicode, hGlobal) == IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(hGlobal);
                        return false;
                    }

                    hGlobal = IntPtr.Zero;
                    return true;
                }
                finally
                {
                    if (hGlobal != IntPtr.Zero)
                        Marshal.FreeHGlobal(hGlobal);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        private const uint CfUnicode = 13;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    }
}
