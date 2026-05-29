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
using EvenMoreOverpoweredJourney.FurnitureBlueprint;

namespace EvenMoreOverpoweredJourney.SuperAdmin
{
    /// <summary>超级管理员调试指令的会话状态；可改写玩家进度与存档调试。</summary>
    public static class SuperAdminSession
    {
        public static bool DebugLetTheLightIn;
        public static bool DebugUnlockAllBuffs;
        public static bool DebugFillTheBlueprint;

        /// <summary>结束 DEBUG_UNLOCKALLBUFFS：先还原解锁快照并清栏，再清除会话标志。</summary>
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
            DebugFillTheBlueprint = false;
            if (FurnitureBlueprintBatchTest.IsRunning)
                FurnitureBlueprintBatchTest.Cancel();
        }
    }

    /// <summary>聊天框执行超级管理员指令；同一逻辑与 IChatCommand 拦截器互为兜底。</summary>
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

        /// <summary>若已消费指令则返回 true（同帧去重）。</summary>
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
                    Main.NewText("\u5f53\u524d\u8c03\u8bd5\u6548\u679c\u5df2\u53d6\u6d88", new Color(255, 150, 150));
                    return true;
                }

                if (t == "CANCEL_DEBUG_UNLOCKALLBUFFS")
                {
                    _lastHandledGameUpdate = tick;
                    PlayCancelSfx();
                    SuperAdminSession.EndDebugUnlockAllBuffs();
                    Main.NewText("DEBUG_UNLOCKALLBUFFS \u6548\u679c\u5df2\u53d6\u6d88", new Color(255, 200, 120));
                    return true;
                }

                if (t == "CANCEL_DEBUG_LETTHELIGHTIN")
                {
                    _lastHandledGameUpdate = tick;
                    PlayCancelSfx();
                    SuperAdminSession.DebugLetTheLightIn = false;
                    Main.NewText("DEBUG_LETTHELIGHTIN \u6548\u679c\u5df2\u53d6\u6d88", new Color(255, 200, 120));
                    return true;
                }

                if (t == "CANCEL_DEBUG_FILLTHEBLUEPRINT")
                {
                    _lastHandledGameUpdate = tick;
                    PlayCancelSfx();
                    SuperAdminSession.DebugFillTheBlueprint = false;
                    Main.NewText("\u84dd\u56fe\u514d\u6750\u6599\u653e\u7f6e\u5df2\u5173\u95ed", new Color(255, 200, 120));
                    return true;
                }

                if (t == "CANCEL_TEST_BLUEPRINT")
                {
                    _lastHandledGameUpdate = tick;
                    PlayCancelSfx();
                    if (FurnitureBlueprintBatchTest.IsRunning)
                        FurnitureBlueprintBatchTest.Cancel();
                    else
                        Main.NewText("\u5f53\u524d\u6ca1\u6709\u84dd\u56fe\u6d4b\u8bd5\u5728\u8fd0\u884c", new Color(200, 200, 200));
                    return true;
                }

                return false;
            }

            if (t.StartsWith("TEST_BLUEPRINT", StringComparison.Ordinal))
            {
                _lastHandledGameUpdate = tick;
                if (FurnitureBlueprintBatchTest.IsRunning)
                    return true;

                var mode = FurnitureBlueprintBatchTest.RunMode.Sets;
                if (t == "TEST_BLUEPRINT QUICK" || t == "TEST_BLUEPRINT_QUICK")
                    mode = FurnitureBlueprintBatchTest.RunMode.Quick;
                else if (t == "TEST_BLUEPRINT FULL" || t == "TEST_BLUEPRINT_FULL")
                    mode = FurnitureBlueprintBatchTest.RunMode.Full;
                else if (t.StartsWith("TEST_BLUEPRINT SEED=", StringComparison.OrdinalIgnoreCase))
                {
                    string num = t.Substring("TEST_BLUEPRINT SEED=".Length).Trim();
                    if (int.TryParse(num, out int seedType)
                        && FurnitureBlueprintBatchTest.TryStartSingleSeed(seedType))
                    {
                        return true;
                    }

                    Main.NewText($"\u65e0\u6548\u79cd\u5b50 ID\uff1a{num}", new Color(255, 150, 150));
                    return true;
                }

                if (FurnitureBlueprintBatchTest.TryStart(mode))
                {
                    return true;
                }

                if (FurnitureBlueprintBatchTest.IsRunning)
                    Main.NewText("\u84dd\u56fe\u6d4b\u8bd5\u5df2\u5728\u8fd0\u884c\uff0c\u53ef\u7528 CANCEL_TEST_BLUEPRINT \u53d6\u6d88", new Color(255, 200, 120));
                else
                    Main.NewText("\u65e0\u6cd5\u542f\u52a8\u84dd\u56fe\u6d4b\u8bd5\uff08\u8bf7\u786e\u8ba4\u5df2\u8fdb\u5165\u4e16\u754c\uff09", new Color(255, 150, 150));
                return true;
            }

            if (t.StartsWith("DEBUG_", StringComparison.Ordinal))
            {
                EmojLog.Info(EmojLogChannel.Core, $"superadmin command {t}");
                if (t == "DEBUG_LETTHELIGHTIN")
                {
                    _lastHandledGameUpdate = tick;
                    PlayDebugAcceptSfx();
                    SuperAdminSession.DebugLetTheLightIn = true;
                    Main.NewText("\u7269\u54c1\u7814\u7a76\u9650\u5236\u5df2\u88ab\u5ffd\u7565", new Color(120, 255, 180));
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
                    Main.NewText("\u4f60\u6682\u65f6\u83b7\u5f97\u4e86\u6240\u6709 buff \u7684\u529b\u91cf\uff01", new Color(255, 220, 80));
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

                if (t == "DEBUG_FILLTHEBLUEPRINT")
                {
                    _lastHandledGameUpdate = tick;
                    PlayDebugAcceptSfx();
                    SuperAdminSession.DebugFillTheBlueprint = true;
                    Main.NewText("\u84dd\u56fe\u514d\u6750\u6599\u653e\u7f6e\u5df2\u5f00\u542f\uff08CANCEL_DEBUG_FILLTHEBLUEPRINT \u5173\u95ed\uff09", new Color(120, 255, 180));
                    return true;
                }

                return false;
            }

            return false;
        }
    }

    /// <summary>出站聊天拦截：严格匹配指令文本（大小写敏感），消费消息。</summary>
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
        private bool _pendingChatSubmit;

        public override void OnWorldLoad()
        {
            if (Main.dedServ)
                return;
            _registerAttempts = 0;
            _chatWindowWasOpen = false;
            _lastChatTextWhileOpen = "";
            _pendingChatSubmit = false;
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
        /// 当 IChatCommand 注册失败时：在聊天窗口从打开变为关闭的帧，用上一帧记录的文本尝试匹配指令。
        /// </summary>
        private void PollChatWindowSubmitFallback()
        {
            if (FurnitureBlueprintBatchTest.IsRunning)
                return;

            if (Main.drawingPlayerChat)
            {
                _chatWindowWasOpen = true;
                _pendingChatSubmit = true;
                _lastChatTextWhileOpen = Main.chatText ?? string.Empty;
                return;
            }

            if (!_chatWindowWasOpen || !_pendingChatSubmit)
                return;

            _chatWindowWasOpen = false;
            _pendingChatSubmit = false;

            if (Main.keyState.IsKeyDown(Keys.Escape))
                return;

            SuperAdminChatHandler.TryHandleLine(_lastChatTextWhileOpen);
        }

        public override void OnWorldUnload()
        {
            SuperAdminSession.ResetAll();
            _chatWindowWasOpen = false;
            _lastChatTextWhileOpen = "";
            _pendingChatSubmit = false;
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
