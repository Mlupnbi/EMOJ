using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.Chat.Commands;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.SuperAdmin
{
    /// <summary>超级管理员调试指令的会话状态；可改写玩家进度与存档调试�?</summary>
    public static class SuperAdminSession
    {
        public static bool DebugLetTheLightIn;
        public static bool DebugUnlockAllBuffs;

        /// <summary>结束 DEBUG_UNLOCKALLBUFFS：先还原解锁快照并清栏，再清除会话标志�?</summary>
        public static void EndDebugUnlockAllBuffs()
        {
            if (!DebugUnlockAllBuffs)
                return;

            BuffDebugUnlockSupport.OnDebugUnlockAllBuffsDisabled();
            DebugUnlockAllBuffs = false;
        }

        public static void ResetAll()
        {
            EndDebugUnlockAllBuffs();
            DebugLetTheLightIn = false;
        }
    }

    /// <summary>聊天框执行超级管理员指令；同一逻辑�? IChatCommand 拦截器互为兜底�?</summary>
    public static class SuperAdminChatHandler
    {
        private static int _lastHandledGameUpdate = int.MinValue;

        private static void PlayDebugAcceptSfx()
        {
            try
            {
                SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Custom/menu_accept"));
                return;
            }
            catch
            {
                /* */
            }

            try
            {
                SoundEngine.PlaySound(new SoundStyle("Sounds/Custom/menu_accept"));
            }
            catch
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        private static void PlayCancelSfx()
        {
            try
            {
                SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Custom/abigail_cry_1"));
                return;
            }
            catch
            {
                /* */
            }

            try
            {
                SoundEngine.PlaySound(new SoundStyle("Sounds/Custom/abigail_cry_1"));
            }
            catch
            {
                SoundEngine.PlaySound(SoundID.NPCDeath1);
            }
        }

        /// <summary>若已消费指令则返�? true（同帧去重）�?</summary>
        public static bool TryHandleLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;
            int tick = (int)Main.GameUpdateCount;
            if (tick == _lastHandledGameUpdate)
                return false;

            string t = line.Trim();
            if (t.Length == 0)
                return false;

            if (t.StartsWith("CANCEL_", StringComparison.Ordinal))
            {
                if (t == "CANCEL_ALL")
                {
                    _lastHandledGameUpdate = tick;
                    PlayCancelSfx();
                    SuperAdminSession.ResetAll();
                    Main.NewText("当前调试效果已取�?", new Color(255, 150, 150));
                    return true;
                }

                if (t == "CANCEL_DEBUG_UNLOCKALLBUFFS")
                {
                    _lastHandledGameUpdate = tick;
                    PlayCancelSfx();
                    SuperAdminSession.EndDebugUnlockAllBuffs();
                    Main.NewText("DEBUG_UNLOCKALLBUFFS 效果已取�?", new Color(255, 200, 120));
                    return true;
                }

                if (t == "CANCEL_DEBUG_LETTHELIGHTIN")
                {
                    _lastHandledGameUpdate = tick;
                    PlayCancelSfx();
                    SuperAdminSession.DebugLetTheLightIn = false;
                    Main.NewText("DEBUG_LETTHELIGHTIN 效果已取�?", new Color(255, 200, 120));
                    return true;
                }

                return false;
            }

            if (t.StartsWith("DEBUG_", StringComparison.Ordinal))
            {
                EmojLog.Info(EmojLogChannel.Core, $"superadmin command {t}");
                if (t == "DEBUG_LETTHELIGHTIN")
                {
                    _lastHandledGameUpdate = tick;
                    PlayDebugAcceptSfx();
                    SuperAdminSession.DebugLetTheLightIn = true;
                    Main.NewText("物品研究限制已被忽略", new Color(120, 255, 180));
                    return true;
                }

                if (t == "DEBUG_OUTPUTBACKPACK")
                {
                    _lastHandledGameUpdate = tick;
                    PlayDebugAcceptSfx();
                    SessionBackpackExporter.TryExportToClipboard();
                    return true;
                }

                if (t == "DEBUG_COMPAREDRAGONLENS")
                {
                    _lastHandledGameUpdate = tick;
                    PlayDebugAcceptSfx();
                    SessionBrowserCatalogCompare.TryCompareToClipboard();
                    return true;
                }

                if (t == "DEBUG_UNLOCKALLBUFFS")
                {
                    _lastHandledGameUpdate = tick;
                    PlayDebugAcceptSfx();
                    SuperAdminSession.DebugUnlockAllBuffs = true;
                    BuffDebugUnlockSupport.OnDebugUnlockAllBuffsEnabled();
                    Main.NewText("你暂时获得了所�? buff 的力量！", new Color(255, 220, 80));
                    return true;
                }

                if (t == "DEBUG_OUTPUTBUFFS")
                {
                    _lastHandledGameUpdate = tick;
                    PlayDebugAcceptSfx();
                    SessionActiveBuffExporter.TryExportToClipboard();
                    return true;
                }

                if (t == "DEBUG_EXPORTVANILLACATALOG")
                {
                    _lastHandledGameUpdate = tick;
                    PlayDebugAcceptSfx();
                    VanillaBuffCatalogExporter.TryExportToClipboard();
                    return true;
                }

                return false;
            }

            return false;
        }
    }

    /// <summary>出站聊天拦截：严格匹配指令文本（大小写敏感），消费消息�?</summary>
    public sealed class SuperAdminChatInterceptor : IChatCommand
    {
        public void ProcessIncomingMessage(string text, byte clientId)
        {
        }

        public void ProcessOutgoingMessage(ChatMessage message)
        {
            if (message == null || message.IsConsumed)
                return;
            string t = message.Text;
            if (string.IsNullOrEmpty(t))
                return;
            if (!SuperAdminChatHandler.TryHandleLine(t))
                return;
            message.Consume();
        }
    }

    public sealed class SuperAdminCommandsSystem : ModSystem
    {
        private static bool _defaultCommandHookRegistered;
        private int _registerAttempts;
        private bool _chatWindowWasOpen;
        private string _lastChatTextWhileOpen = "";

        public override void OnWorldLoad()
        {
            if (Main.dedServ)
                return;
            _registerAttempts = 0;
            _chatWindowWasOpen = false;
            _lastChatTextWhileOpen = "";
            TryRegisterDefaultChatCommandOnce();
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
                return;

            if (!_defaultCommandHookRegistered && !Main.gameMenu)
            {
                if (_registerAttempts++ <= 600)
                    TryRegisterDefaultChatCommandOnce();
            }

            if (!Main.gameMenu)
                PollChatWindowSubmitFallback();
        }

        /// <summary>
        /// �? IChatCommand 注册失败时：在聊天窗口从打开变为关闭的帧，用上一帧记录的文本尝试匹配指令�?
        /// </summary>
        private void PollChatWindowSubmitFallback()
        {
            if (Main.drawingPlayerChat)
                _lastChatTextWhileOpen = Main.chatText ?? "";
            else if (_chatWindowWasOpen && !Main.drawingPlayerChat)
            {
                if (!Main.keyState.IsKeyDown(Keys.Escape))
                    SuperAdminChatHandler.TryHandleLine(_lastChatTextWhileOpen);
            }

            _chatWindowWasOpen = Main.drawingPlayerChat;
        }

        public override void OnWorldUnload()
        {
            SuperAdminSession.ResetAll();
            _chatWindowWasOpen = false;
            _lastChatTextWhileOpen = "";
        }

        public override void Unload()
        {
            _defaultCommandHookRegistered = false;
        }

        private static void TryRegisterDefaultChatCommandOnce()
        {
            if (_defaultCommandHookRegistered)
                return;

            ChatCommandProcessor processor = FindChatCommandProcessor();
            if (processor == null)
                return;

            MethodInfo add = typeof(ChatCommandProcessor).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "AddDefaultCommand" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            if (add == null)
                return;

            add.MakeGenericMethod(typeof(SuperAdminChatInterceptor)).Invoke(processor, null);
            _defaultCommandHookRegistered = true;
        }

        private static ChatCommandProcessor FindChatCommandProcessor()
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            foreach (FieldInfo fi in typeof(Main).GetFields(bf))
            {
                if (fi.FieldType != typeof(ChatCommandProcessor))
                    continue;
                try
                {
                    object v = fi.IsStatic ? fi.GetValue(null) : (Main.instance != null ? fi.GetValue(Main.instance) : null);
                    if (v is ChatCommandProcessor p)
                        return p;
                }
                catch
                {
                    /* */
                }
            }

            if (Main.instance == null)
                return null;

            foreach (FieldInfo fi in typeof(Main).GetFields(bf))
            {
                if (!fi.IsStatic && fi.FieldType == typeof(ChatCommandProcessor))
                {
                    try
                    {
                        if (fi.GetValue(Main.instance) is ChatCommandProcessor p)
                            return p;
                    }
                    catch
                    {
                        /* */
                    }
                }
            }

            foreach (FieldInfo fi in typeof(Main).GetFields(bf))
            {
                if (fi.IsStatic || !fi.FieldType.IsClass || fi.FieldType == typeof(string))
                    continue;
                object nested;
                try
                {
                    nested = fi.GetValue(Main.instance);
                }
                catch
                {
                    continue;
                }

                if (nested == null)
                    continue;

                foreach (FieldInfo fi2 in fi.FieldType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (fi2.FieldType != typeof(ChatCommandProcessor))
                        continue;
                    try
                    {
                        if (fi2.GetValue(nested) is ChatCommandProcessor p2)
                            return p2;
                    }
                    catch
                    {
                        /* */
                    }
                }
            }

            return null;
        }
    }
}
