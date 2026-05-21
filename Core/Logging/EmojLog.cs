using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Core.Logging
{
    /// <summary>
    /// EMOJ ͳһ��־���ر� / �򻯣����ļ��¼������ɳ�פ��/ ��������ͨ�� + �����嵥����
    /// </summary>
    public static class EmojLog
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<EmojLogChannel, StreamWriter> Writers = new Dictionary<EmojLogChannel, StreamWriter>();
        private static StreamWriter _simpleWriter;

        private static string _sessionDirectory;
        private static bool _sessionStarted;
        private static bool _environmentManifestWritten;

        private static readonly HashSet<string> _onceKeys = new HashSet<string>();
        private static int _linesSinceFlush;
        private const int FlushEveryLines = 64;

        public static OPJourneyConfig.ModLogModeKind Mode { get; private set; } = OPJourneyConfig.ModLogModeKind.Off;

        public static bool IsActive => Mode != OPJourneyConfig.ModLogModeKind.Off;

        public static bool IsFullMode => Mode == OPJourneyConfig.ModLogModeKind.Full;

        public static bool WritesFiles => IsActive;

        public static string SessionDirectory => _sessionDirectory;

        public static void RefreshFromConfig()
        {
            OPJourneyConfig config = ModContent.GetInstance<OPJourneyConfig>();
            bool wasActive = IsActive;

            Mode = config?.ModLogMode ?? OPJourneyConfig.ModLogModeKind.Off;

            if (!IsActive)
            {
                if (wasActive || _sessionStarted)
                    EndSession();
                else
                    CloseAllWriters();
            }
        }

        public static void EnsureSession()
        {
            if (!WritesFiles || _sessionStarted)
                return;

            lock (Gate)
            {
                if (_sessionStarted)
                    return;

                string root = Path.Combine(Main.SavePath, "Logs", "EMOJ");
                string sessionId = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _sessionDirectory = Path.Combine(root, sessionId);
                Directory.CreateDirectory(_sessionDirectory);

                _sessionStarted = true;
                _environmentManifestWritten = false;

                WriteReadme();
                WriteBootstrapManifest();

                if (IsFullMode)
                    ModContent.GetInstance<EvenMoreOverpoweredJourney>().Logger.Info(
                        $"[INFO] [Core] Full log session: {_sessionDirectory}");
                else
                    WriteSimpleLine("INFO", "Core", "Simplified log session started.");

                WriteCoreFileLine("Session started.");
            }
        }

        public static void WriteEnvironmentManifest()
        {
            if (!IsFullMode || !_sessionStarted)
                return;

            lock (Gate)
            {
                if (_environmentManifestWritten)
                    return;

                WriteEnvironmentManifestCore();
                _environmentManifestWritten = true;
            }
        }

        public static void EndSession()
        {
            lock (Gate)
            {
                if (!_sessionStarted)
                {
                    CloseAllWriters();
                    return;
                }

                FlushAll();
                WriteSimpleLine("INFO", "Core", "Session ended.");
                WriteCoreFileLine("Session ended.");
                CloseAllWriters();
                _sessionStarted = false;
                _environmentManifestWritten = false;
                _sessionDirectory = null;
            }
        }

        public static void Info(EmojLogChannel channel, string message, EmojLogDetail detail = EmojLogDetail.Simplified) =>
            Write("INFO", channel, message, detail);

        public static void InfoFull(EmojLogChannel channel, string message) =>
            Write("INFO", channel, message, EmojLogDetail.Full);

        public static void Warn(EmojLogChannel channel, string message, EmojLogDetail detail = EmojLogDetail.Simplified) =>
            Write("WARN", channel, message, detail);

        public static void WarnFull(EmojLogChannel channel, string message) =>
            Write("WARN", channel, message, EmojLogDetail.Full);

        public static void Error(EmojLogChannel channel, string message, Exception ex = null)
        {
            Write("ERROR", channel, message, EmojLogDetail.Simplified);
            if (ex != null)
                Write("ERROR", channel, ex.ToString(), EmojLogDetail.Simplified);
        }

        public static void InfoOnce(EmojLogChannel channel, string dedupeKey, string message,
            EmojLogDetail detail = EmojLogDetail.Simplified)
        {
            if (!ShouldWrite(detail))
                return;

            if (_onceKeys.Contains(dedupeKey))
                return;

            lock (Gate)
            {
                if (!_onceKeys.Add(dedupeKey))
                    return;
            }

            Write("INFO", channel, message, detail);
        }

        public static void ResetDedupeKeys() => _onceKeys.Clear();

        private static bool ShouldWrite(EmojLogDetail detail) =>
            IsActive && (detail == EmojLogDetail.Simplified || IsFullMode);

        private static void Write(string level, EmojLogChannel channel, string message, EmojLogDetail detail)
        {
            if (!ShouldWrite(detail) || string.IsNullOrEmpty(message))
                return;

            try
            {
                EnsureSession();

                if (detail == EmojLogDetail.Simplified || Mode == OPJourneyConfig.ModLogModeKind.Simplified)
                    WriteSimpleLine(level, channel.ToString(), message);

                if (IsFullMode && detail == EmojLogDetail.Full)
                {
                    lock (Gate)
                    {
                        StreamWriter writer = GetChannelWriter(channel);
                        writer?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}");
                        if (++_linesSinceFlush >= FlushEveryLines)
                            FlushAll();
                    }
                }
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<EvenMoreOverpoweredJourney>().Logger.Warn(
                    $"EmojLog write failed ({channel}): {ex.Message}");
            }
        }

        private static void WriteSimpleLine(string level, string channel, string message)
        {
            lock (Gate)
            {
                StreamWriter writer = GetSimpleWriter();
                writer?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{channel}] {message}");
                if (++_linesSinceFlush >= FlushEveryLines)
                    FlushAll();
            }
        }

        private static StreamWriter GetSimpleWriter()
        {
            if (_simpleWriter != null)
                return _simpleWriter;

            string path = Path.Combine(_sessionDirectory, "simple.log");
            _simpleWriter = new StreamWriter(path, append: true, Encoding.UTF8);
            return _simpleWriter;
        }

        private static StreamWriter GetChannelWriter(EmojLogChannel channel)
        {
            if (Writers.TryGetValue(channel, out StreamWriter existing))
                return existing;

            string path = Path.Combine(_sessionDirectory, ChannelFileName(channel));
            var writer = new StreamWriter(path, append: true, Encoding.UTF8);
            Writers[channel] = writer;
            return writer;
        }

        private static void FlushAll()
        {
            _simpleWriter?.Flush();
            foreach (StreamWriter writer in Writers.Values)
                writer?.Flush();

            _linesSinceFlush = 0;
        }

        private static void WriteCoreFileLine(string message)
        {
            if (!IsFullMode || !_sessionStarted)
                return;

            try
            {
                StreamWriter writer = GetChannelWriter(EmojLogChannel.Core);
                writer?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [INFO] {message}");
                writer?.Flush();
            }
            catch
            {
                /* */
            }
        }

        private static string ChannelFileName(EmojLogChannel channel) => channel switch
        {
            EmojLogChannel.Core => "01_core.log",
            EmojLogChannel.Buff => "02_buff.log",
            EmojLogChannel.Virtual => "03_virtual.log",
            EmojLogChannel.SetBonus => "04_setbonus.log",
            EmojLogChannel.Summon => "05_summon.log",
            EmojLogChannel.Entity => "06_entity.log",
            EmojLogChannel.Ui => "07_ui.log",
            EmojLogChannel.ItemHub => "08_itemhub.log",
            EmojLogChannel.Research => "09_research.log",
            EmojLogChannel.Error => "99_error.log",
            _ => "00_misc.log"
        };

        private static void CloseAllWriters()
        {
            try
            {
                _simpleWriter?.Dispose();
            }
            catch
            {
                /* */
            }

            _simpleWriter = null;

            foreach (StreamWriter writer in Writers.Values)
            {
                try
                {
                    writer?.Dispose();
                }
                catch
                {
                    /* */
                }
            }

            Writers.Clear();
        }

        private static void WriteReadme()
        {
            string path = Path.Combine(_sessionDirectory, "00_README.txt");
            if (IsFullMode)
            {
                File.WriteAllText(path,
                    """
                    EMOJ (EvenMoreOverpoweredJourney) ����������־��
                    =============================================
                    �뽫�����ļ���ԭ�����������

                    simple.log         - ���¼������롸�򻯡�ģʽ��ͬ����Ϣ��
                    01_core.log ��      - ��ͨ����ϸ��־
                    manifest-full.txt  - ģ���б���������Ϣ���������

                    ����� tModLoader-Logs/client.log ���� EvenMoreOverpoweredJourney��
                    """,
                    Encoding.UTF8);
            }
            else
            {
                File.WriteAllText(path,
                    """
                    EMOJ ����־���ɳ�פ���Ϳ�����
                    ==============================
                    simple.log - ��/�����硢���� Buff������������UI��������¼�
                    manifest.txt - �ỰԪ���ݣ���������ģ���б��

                    ��Ҫ�������/��װ������ϸ��ʱ��������á�������־����
                    """,
                    Encoding.UTF8);
            }
        }

        private static void WriteBootstrapManifest()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"started={DateTime.Now:O}");
            sb.AppendLine($"logMode={Mode}");
            sb.AppendLine($"tML={BuildInfo.tMLVersion}");
            sb.AppendLine($"emoJ={ModContent.GetInstance<EvenMoreOverpoweredJourney>().DisplayName}");

            if (IsFullMode)
                sb.AppendLine("mods=(see manifest-full.txt after world load)");
            else
                sb.AppendLine("mods=(not collected in simplified mode)");

            if (Main.gameMenu)
                sb.AppendLine("phase=main-menu");

            File.WriteAllText(Path.Combine(_sessionDirectory, "manifest.txt"), sb.ToString(), Encoding.UTF8);
        }

        private static void WriteEnvironmentManifestCore()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"captured={DateTime.Now:O}");
            sb.AppendLine($"tML={BuildInfo.tMLVersion}");

            if (Main.gameMenu)
                sb.AppendLine("phase=main-menu");
            else if (Main.worldID != 0)
                sb.AppendLine($"world={Main.worldName} id={Main.worldID} drunk={Main.drunkWorld} remix={Main.remixWorld}");

            if (Main.LocalPlayer != null && Main.LocalPlayer.active)
                sb.AppendLine($"player={Main.LocalPlayer.name} whoAmI={Main.LocalPlayer.whoAmI}");

            sb.AppendLine("mods=");
            foreach (Mod mod in ModLoader.Mods)
            {
                if (mod.Name == "ModLoader")
                    continue;

                sb.AppendLine($"  {mod.Name} {mod.Version}");
            }

            File.WriteAllText(Path.Combine(_sessionDirectory, "manifest-full.txt"), sb.ToString(), Encoding.UTF8);
            WriteCoreFileLine("Environment manifest written (manifest-full.txt).");
            WriteSimpleLine("INFO", "Core", "manifest-full.txt written.");
        }
    }
}
