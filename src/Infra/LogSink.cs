using System;
using System.IO;

namespace AIInfluenceAntigravity.Infra
{
    public static class LogSink
    {
        private static readonly object Gate = new object();
        private static StreamWriter _writer;
        private static string _path;

        public static void Initialize(string moduleRoot, bool verbose = true)
        {
            lock (Gate)
            {
                CloseNoLock();

                try
                {
                    if (string.IsNullOrEmpty(moduleRoot))
                    {
                        return;
                    }

                    _path = Path.Combine(moduleRoot, "logs", "antigravity_log.txt");
                    string folder = Path.GetDirectoryName(_path);
                    if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    _writer = new StreamWriter(_path, append: true) { AutoFlush = true };
                }
                catch
                {
                    _writer = null;
                }

                Verbose = verbose;
            }
        }

        public static bool Enabled { get; set; } = true;
        public static bool Verbose { get; private set; }
        public static string LogFile => _path ?? string.Empty;

        public static void Always(string message) => Write("INFO ", message);
        public static void Info(string message) { if (Verbose) Write("info ", message); }
        public static void Warn(string message) => Write("WARN ", message);
        public static void Error(string message, Exception ex = null) =>
            Write("ERROR", message + (ex == null ? string.Empty : Environment.NewLine + ex));

        public static void Flush()
        {
            lock (Gate)
            {
                if (_writer != null)
                {
                    try { _writer.Flush(); } catch { }
                }
            }
        }

        public static void Shutdown()
        {
            lock (Gate) { CloseNoLock(); }
        }

        private static void CloseNoLock()
        {
            if (_writer != null)
            {
                try { _writer.Dispose(); } catch { }
                _writer = null;
            }
        }

        private static void Write(string level, string message)
        {
            if (!Enabled && !string.Equals(level, "INFO ", StringComparison.Ordinal))
            {
                return;
            }

            lock (Gate)
            {
                if (_writer == null) return;

                try
                {
                    string clean = StripMarkup(message);
                    string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    _writer.WriteLine(stamp + " [" + level + "] " + clean);
                }
                catch { }
            }
        }

        public static string StripMarkup(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (!text.Contains("<") && !text.Contains("!")) return text;

            var sb = new System.Text.StringBuilder(text.Length);
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<') { inTag = true; continue; }
                if (c == '>') { inTag = false; continue; }
                if (!inTag) sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
