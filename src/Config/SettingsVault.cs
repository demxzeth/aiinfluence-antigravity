using System;
using System.IO;
using Newtonsoft.Json;
using AIInfluenceAntigravity.Infra;

namespace AIInfluenceAntigravity.Config
{
    public static class SettingsVault
    {
        private static readonly object Gate = new object();
        private static AntigravitySettings _current;
        private const string ConfigFileName = "antigravity_settings.json";

        public static AntigravitySettings Current
        {
            get
            {
                if (_current == null)
                {
                    lock (Gate)
                    {
                        if (_current == null)
                        {
                            _current = LoadOrCreate();
                        }
                    }
                }
                return _current;
            }
        }

        public static void Reset()
        {
            lock (Gate)
            {
                _current = null;
            }
        }

        public static void Save()
        {
            lock (Gate)
            {
                if (_current == null) return;
                try
                {
                    string path = GetConfigFilePath();
                    if (string.IsNullOrEmpty(path)) return;

                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string json = JsonConvert.SerializeObject(_current, Formatting.Indented);
                    File.WriteAllText(path, json);
                }
                catch (Exception ex)
                {
                    LogSink.Error("settings: save failed: " + ex.Message, ex);
                }
            }
        }

        private static AntigravitySettings LoadOrCreate()
        {
            try
            {
                string path = GetConfigFilePath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    AntigravitySettings loaded = JsonConvert.DeserializeObject<AntigravitySettings>(json);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                LogSink.Warn("settings: load failed, using defaults: " + ex.Message);
            }

            var created = new AntigravitySettings();
            try
            {
                string path = GetConfigFilePath();
                if (!string.IsNullOrEmpty(path))
                {
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    string json = JsonConvert.SerializeObject(created, Formatting.Indented);
                    File.WriteAllText(path, json);
                }
            }
            catch { }

            return created;
        }

        public static string GetConfigFilePath()
        {
            string root = ModuleLocator.Root;
            if (string.IsNullOrEmpty(root))
            {
                return Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);
            }
            return Path.Combine(root, "Config", ConfigFileName);
        }
    }
}
