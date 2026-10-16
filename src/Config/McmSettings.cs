using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using AIInfluenceAntigravity.Infra;
using AIInfluenceAntigravity.Transport;

namespace AIInfluenceAntigravity.Config
{
    public sealed class McmSettings : AttributeGlobalSettings<McmSettings>
    {
        public override string Id => "AIInfluenceAntigravity_v1";
        public override string DisplayName => "AIInfluence Antigravity";
        public override string FolderName => "AIInfluenceAntigravity";
        public override string FormatType => "json";

        // ─── Support ──────────────────────────────────────────

        private Action _openNexus;
        private Action _showDiscord;

        [SettingPropertyGroup("Support", GroupOrder = 1)]
        [SettingPropertyButton("Nexus Page", Order = 1, Content = "Nexus Page", RequireRestart = false,
            HintText = "Opens the Nexus Mods page of AIInfluence Antigravity in your browser.")]
        public Action OpenNexus
        {
            get => _openNexus ?? (_openNexus = () => OpenUrl(ModInfo.NexusUrl));
            set => _openNexus = value;
        }

        [SettingPropertyGroup("Support", GroupOrder = 1)]
        [SettingPropertyButton("Contact Me on Discord", Order = 2, Content = "@demxzeth", RequireRestart = false,
            HintText = "Shows developer Discord contact in the chat.")]
        public Action ShowDiscord
        {
            get => _showDiscord ?? (_showDiscord = () => ChatNotices.Show("Discord: " + ModInfo.DiscordTag));
            set => _showDiscord = value;
        }

        // ─── General Settings ─────────────────────────────────

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyBool("Enable Antigravity Interception", Order = 1, RequireRestart = false,
            HintText = "When off, this mod passes all calls through to AIInfluence's original backend.")]
        public bool Enabled
        {
            get => SettingsVault.Current.Enabled;
            set { if (SettingsVault.Current.Enabled != value) { SettingsVault.Current.Enabled = value; SettingsVault.Save(); } }
        }

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyBool("Response Timer in Chat", Order = 2, RequireRestart = false,
            HintText = "Shows real-time request progress and elapsed time in game chat with prefix [Antigravity] - Timer **s.")]
        public bool TimerInChat
        {
            get => SettingsVault.Current.TimerInChat;
            set { if (SettingsVault.Current.TimerInChat != value) { SettingsVault.Current.TimerInChat = value; SettingsVault.Save(); } }
        }

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyText("Antigravity CLI Path", Order = 3, RequireRestart = false,
            HintText = "Path to agy.exe. Leave empty to auto-detect from %LOCALAPPDATA%\\agy\\bin\\agy.exe or PATH.")]
        public string CliPath
        {
            get => SettingsVault.Current.CliPath;
            set { string v = (value ?? string.Empty).Trim(); if (SettingsVault.Current.CliPath != v) { SettingsVault.Current.CliPath = v; SettingsVault.Save(); } }
        }

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyText("Model", Order = 4, RequireRestart = false,
            HintText = "Model name for Antigravity CLI (e.g. gemini-3.8-flash-low or gemini-3.7-flash-high). Empty = default.")]
        public string Model
        {
            get => SettingsVault.Current.Model;
            set
            {
                string v = (value ?? string.Empty).Trim();
                if (SettingsVault.Current.Model != v)
                {
                    SettingsVault.Current.Model = v;
                    SettingsVault.Save();
                    SyncModelDropdown(v);
                }
            }
        }

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyDropdown("Select Model", Order = 5, RequireRestart = false,
            HintText = "Select an available model fetched from the Antigravity CLI endpoint.")]
        public Dropdown<string> ModelDropdown
        {
            get => GetModelDropdown();
            set => SetModelDropdown(value);
        }

        private Action _fetchModels;

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyButton("Refresh Models from Endpoint", Order = 6, Content = "Fetch", RequireRestart = false,
            HintText = "Queries agy.exe models to retrieve all available models.")]
        public Action FetchModels
        {
            get => _fetchModels ?? (_fetchModels = () => FetchModelsFromCli());
            set => _fetchModels = value;
        }

        private Action _clearModels;

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyButton("Clear Fetched Models", Order = 7, Content = "Clear", RequireRestart = false,
            HintText = "Clears the list of fetched models, empties the Model field, and resets the model dropdown.")]
        public Action ClearModels
        {
            get => _clearModels ?? (_clearModels = () => ClearModelsList());
            set => _clearModels = value;
        }

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyInteger("Timeout (Seconds)", 60, 600, "{0}", Order = 8, RequireRestart = false,
            HintText = "Single request timeout in seconds.")]
        public int TimeoutSeconds
        {
            get => SettingsVault.Current.TimeoutSeconds;
            set { if (SettingsVault.Current.TimeoutSeconds != value) { SettingsVault.Current.TimeoutSeconds = value; SettingsVault.Save(); } }
        }

        private Action _testCli;

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyButton("Test CLI Connection", Order = 9, Content = "Test CLI", RequireRestart = false,
            HintText = "Checks if agy.exe is executable and reports version in chat.")]
        public Action TestCli
        {
            get => _testCli ?? (_testCli = () => Task.Run(() => Pipeline.ProbeCli()));
            set => _testCli = value;
        }

        private Action _testModel;

        [SettingPropertyGroup("General", GroupOrder = 2)]
        [SettingPropertyButton("Test Connection to Model", Order = 10, Content = "Test Model", RequireRestart = false,
            HintText = "Sends a small request to the model itself and reports its reply in the chat.")]
        public Action TestModel
        {
            get => _testModel ?? (_testModel = () => Task.Run(() => Pipeline.ProbeModel()));
            set => _testModel = value;
        }

        // ─── Diagnostics ──────────────────────────────────────

        [SettingPropertyGroup("Diagnostics", GroupOrder = 3)]
        [SettingPropertyBool("Enable File Logging", Order = 1, RequireRestart = false,
            HintText = "Logs requests and errors to logs/antigravity_log.txt.")]
        public bool LogEnabled
        {
            get => SettingsVault.Current.LogEnabled;
            set
            {
                if (SettingsVault.Current.LogEnabled != value)
                {
                    SettingsVault.Current.LogEnabled = value;
                    LogSink.Enabled = value;
                    SettingsVault.Save();
                }
            }
        }

        private Action _openLogFolder;

        [SettingPropertyGroup("Diagnostics", GroupOrder = 3)]
        [SettingPropertyButton("Open Log Folder", Order = 2, Content = "Open", RequireRestart = false,
            HintText = "Opens the logs/ directory in Windows Explorer.")]
        public Action OpenLogFolder
        {
            get => _openLogFolder ?? (_openLogFolder = () => OpenLogDirectory());
            set => _openLogFolder = value;
        }

        [SettingPropertyGroup("Diagnostics", GroupOrder = 3)]
        [SettingPropertyBool("Auto-repair JSON", Order = 3, RequireRestart = false,
            HintText = "Fixes malformed or cut-off JSON output automatically before returning to AIInfluence.")]
        public bool JsonRepair
        {
            get => SettingsVault.Current.JsonRepair;
            set { if (SettingsVault.Current.JsonRepair != value) { SettingsVault.Current.JsonRepair = value; SettingsVault.Save(); } }
        }

        [SettingPropertyGroup("Diagnostics", GroupOrder = 3)]
        [SettingPropertyBool("Show Errors in Chat", Order = 4, RequireRestart = false,
            HintText = "Displays error notices in game chat when requests fail.")]
        public bool ErrorNotices
        {
            get => SettingsVault.Current.ErrorNotices;
            set { if (SettingsVault.Current.ErrorNotices != value) { SettingsVault.Current.ErrorNotices = value; SettingsVault.Save(); } }
        }

        // ─── Dropdown & MCM Sync ──────────────────────────────

        private static bool _suppressDropdownSync = false;
        private static Dropdown<string> _modelDropdown;

        private static Dropdown<string> GetModelDropdown()
        {
            if (_modelDropdown == null)
            {
                List<string> list = BuildInitialModelList();
                string current = SettingsVault.Current.Model;
                int selectedIndex = 0;
                if (!string.IsNullOrEmpty(current))
                {
                    int idx = list.IndexOf(current);
                    if (idx >= 0) selectedIndex = idx;
                }
                _modelDropdown = new Dropdown<string>(list, selectedIndex);
                _modelDropdown.PropertyChanged += (s, e) => SetModelDropdown((Dropdown<string>)s);
            }
            else
            {
                string current = SettingsVault.Current.Model;
                if (!string.IsNullOrEmpty(current))
                {
                    int idx = _modelDropdown.IndexOf(current);
                    if (idx >= 0 && _modelDropdown.SelectedIndex != idx)
                    {
                        _suppressDropdownSync = true;
                        try { _modelDropdown.SelectedIndex = idx; }
                        finally { _suppressDropdownSync = false; }
                    }
                }
                else
                {
                    if (_modelDropdown.Count > 0 && _modelDropdown.SelectedIndex != 0)
                    {
                        _suppressDropdownSync = true;
                        try { _modelDropdown.SelectedIndex = 0; }
                        finally { _suppressDropdownSync = false; }
                    }
                }
            }
            return _modelDropdown;
        }

        private static void SetModelDropdown(Dropdown<string> dd)
        {
            if (_suppressDropdownSync) return;
            if (dd != null && dd.SelectedIndex >= 0 && dd.SelectedIndex < dd.Count)
            {
                string sel = dd.SelectedValue;
                if (!string.IsNullOrEmpty(sel) && !sel.StartsWith("(", StringComparison.Ordinal))
                {
                    if (SettingsVault.Current.Model != sel)
                    {
                        SettingsVault.Current.Model = sel;
                        SettingsVault.Save();
                        NotifyModelProperties();
                    }
                }
            }
        }

        private static void SyncModelDropdown(string newModel)
        {
            if (_modelDropdown == null) return;
            if (string.IsNullOrEmpty(newModel))
            {
                if (_modelDropdown.Count > 0 && _modelDropdown.SelectedIndex != 0)
                {
                    _suppressDropdownSync = true;
                    try { _modelDropdown.SelectedIndex = 0; }
                    finally { _suppressDropdownSync = false; }
                }
                return;
            }

            _suppressDropdownSync = true;
            try
            {
                int idx = _modelDropdown.IndexOf(newModel);
                if (idx >= 0 && _modelDropdown.SelectedIndex != idx)
                {
                    _modelDropdown.SelectedIndex = idx;
                }
            }
            finally
            {
                _suppressDropdownSync = false;
            }
        }

        private static List<string> BuildInitialModelList()
        {
            var list = new List<string>();
            var available = SettingsVault.Current.AvailableModels;
            if (available != null && available.Count > 0)
            {
                list.AddRange(available);
            }

            string current = SettingsVault.Current.Model;
            if (list.Count > 0)
            {
                if (!string.IsNullOrEmpty(current) && !list.Contains(current))
                {
                    list.Insert(0, current);
                }
                else if (string.IsNullOrEmpty(current))
                {
                    list.Insert(0, "(Select Model)");
                }
            }
            else
            {
                list.Add("(Click Fetch to load)");
            }

            return list;
        }

        private static void FetchModelsFromCli()
        {
            Task.Run(async () =>
            {
                try
                {
                    ChatNotices.Show("- Fetching available models...");
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    {
                        List<string> models = await AntigravityTransport.FetchModelsAsync(
                            SettingsVault.Current.CliPath, cts.Token).ConfigureAwait(false);

                        if (models != null && models.Count > 0)
                        {
                            SettingsVault.Current.AvailableModels = new List<string>(models);
                            SettingsVault.Save();

                            List<string> items = new List<string>();
                            string cur = SettingsVault.Current.Model;
                            int selIdx = 0;
                            if (!string.IsNullOrEmpty(cur))
                            {
                                items.AddRange(models);
                                if (!items.Contains(cur)) items.Insert(0, cur);
                                selIdx = items.IndexOf(cur);
                            }
                            else
                            {
                                items.Add("(Select Model)");
                                items.AddRange(models);
                                selIdx = 0;
                            }

                            _suppressDropdownSync = true;
                            try
                            {
                                if (_modelDropdown != null)
                                {
                                    _modelDropdown.Clear();
                                    foreach (string m in items) _modelDropdown.Add(m);
                                    _modelDropdown.SelectedIndex = (selIdx >= 0 && selIdx < items.Count) ? selIdx : 0;
                                }
                                else
                                {
                                    _modelDropdown = new Dropdown<string>(items, (selIdx >= 0 && selIdx < items.Count) ? selIdx : 0);
                                    _modelDropdown.PropertyChanged += (s, e) => SetModelDropdown((Dropdown<string>)s);
                                }
                            }
                            finally
                            {
                                _suppressDropdownSync = false;
                            }

                            NotifyModelProperties(items, selIdx);

                            string preview = string.Join(", ", models.Take(4)) + (models.Count > 4 ? "..." : string.Empty);
                            ChatNotices.Show("Fetched " + models.Count + " models: " + preview);
                        }
                        else
                        {
                            ChatNotices.Show("No models returned by CLI.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ChatNotices.Error("Fetch models error: " + ex.Message);
                }
            });
        }

        private static void ClearModelsList()
        {
            try
            {
                SettingsVault.Current.AvailableModels.Clear();
                SettingsVault.Current.Model = string.Empty;
                SettingsVault.Save();

                List<string> items = new List<string> { "(Click Fetch to load)" };

                _suppressDropdownSync = true;
                try
                {
                    if (_modelDropdown != null)
                    {
                        _modelDropdown.Clear();
                        foreach (string m in items) _modelDropdown.Add(m);
                        _modelDropdown.SelectedIndex = 0;
                    }
                    else
                    {
                        _modelDropdown = new Dropdown<string>(items, 0);
                        _modelDropdown.PropertyChanged += (s, e) => SetModelDropdown((Dropdown<string>)s);
                    }
                }
                finally
                {
                    _suppressDropdownSync = false;
                }

                NotifyModelProperties(items, 0);
                ChatNotices.Show("- Fetched models cleared.");
            }
            catch (Exception ex)
            {
                LogSink.Error("clear models failed: " + ex.Message, ex);
            }
        }

        private static void NotifyModelProperties(List<string> items = null, int selectedIndex = -1)
        {
            try
            {
                if (Instance != null)
                {
                    Instance.OnPropertyChanged(nameof(ModelDropdown));
                    Instance.OnPropertyChanged(nameof(Model));
                }

                TryRefreshMcmUi(nameof(ModelDropdown), items, selectedIndex, nameof(Model), SettingsVault.Current.Model ?? string.Empty);
            }
            catch (Exception ex)
            {
                LogSink.Warn("mcm: notify model properties failed: " + ex.Message);
            }
        }

        private static void TryRefreshMcmUi(string dropdownPropertyName, List<string> items = null, int selectedIndex = -1, string textPropertyName = null, string textValue = null)
        {
            try
            {
                object topScreen = TaleWorlds.ScreenSystem.ScreenManager.TopScreen;
                if (topScreen == null) return;
                Type topType = topScreen.GetType();
                if (!topType.Name.Contains("ModOptions")) return;
                var dsField = topType.GetField("_dataSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dsField == null) return;
                object modOptionsVM = dsField.GetValue(topScreen);
                if (modOptionsVM == null) return;
                var selectedModProp = modOptionsVM.GetType().GetProperty("SelectedMod");
                if (selectedModProp == null) return;
                object settingsVM = selectedModProp.GetValue(modOptionsVM);
                if (settingsVM == null) return;

                var groupsProp = settingsVM.GetType().GetProperty("SettingPropertyGroups");
                if (groupsProp == null) return;
                var groups = groupsProp.GetValue(settingsVM) as System.Collections.IEnumerable;
                if (groups == null) return;

                bool matchedDropdown = false;
                bool matchedText = false;

                foreach (object group in groups)
                {
                    if (group == null) continue;
                    var propsProp = group.GetType().GetProperty("SettingProperties");
                    if (propsProp == null) continue;
                    var props = propsProp.GetValue(group) as System.Collections.IEnumerable;
                    if (props == null) continue;

                    foreach (object spVM in props)
                    {
                        if (spVM == null) continue;
                        var defProp = spVM.GetType().GetProperty("SettingPropertyDefinition");
                        if (defProp == null) continue;
                        object def = defProp.GetValue(spVM);
                        if (def == null) continue;
                        var idProp = def.GetType().GetProperty("Id");
                        string propId = idProp != null ? idProp.GetValue(def) as string : null;

                        if (!matchedDropdown && string.Equals(propId, dropdownPropertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            var wrapperField = spVM.GetType().GetField("_selectorVMWrapper", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (wrapperField != null)
                            {
                                object wrapper = wrapperField.GetValue(spVM);
                                if (wrapper != null && items != null && items.Count > 0)
                                {
                                    var itemListProp = wrapper.GetType().GetProperty("ItemList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    var itemList = itemListProp != null ? itemListProp.GetValue(wrapper) as System.Collections.IList : null;
                                    if (itemList != null)
                                    {
                                        Type itemType = itemList.GetType().IsGenericType ? itemList.GetType().GetGenericArguments()[0] : null;
                                        itemList.Clear();
                                        foreach (string m in items)
                                        {
                                            object newItem = CreateSelectorItem(itemType, m);
                                            if (newItem != null) itemList.Add(newItem);
                                        }

                                        var selIdxProp = wrapper.GetType().GetProperty("SelectedIndex", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        if (selIdxProp != null && selIdxProp.CanWrite && selectedIndex >= 0)
                                        {
                                            selIdxProp.SetValue(wrapper, selectedIndex);
                                        }

                                        var refreshMethod = wrapper.GetType().GetMethod("Refresh", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null);
                                        if (refreshMethod != null)
                                        {
                                            refreshMethod.Invoke(wrapper, null);
                                        }
                                    }
                                }
                            }

                            var onPropChangedMethod = spVM.GetType().GetMethod("OnPropertyChanged", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(string) }, null);
                            if (onPropChangedMethod != null)
                            {
                                onPropChangedMethod.Invoke(spVM, new object[] { "DropdownValue" });
                                onPropChangedMethod.Invoke(spVM, new object[] { "Selector" });
                            }
                            matchedDropdown = true;
                        }
                        else if (!matchedText && !string.IsNullOrEmpty(textPropertyName) && string.Equals(propId, textPropertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            string[] valPropNames = new[] { "Value", "StringValue", "ValueString" };
                            foreach (string vp in valPropNames)
                            {
                                var p = spVM.GetType().GetProperty(vp, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                                {
                                    try { p.SetValue(spVM, textValue ?? string.Empty); } catch { }
                                }
                            }
                            var onPropChangedMethod = spVM.GetType().GetMethod("OnPropertyChanged", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(string) }, null);
                            if (onPropChangedMethod != null)
                            {
                                onPropChangedMethod.Invoke(spVM, new object[] { "Value" });
                                onPropChangedMethod.Invoke(spVM, new object[] { "StringValue" });
                                onPropChangedMethod.Invoke(spVM, new object[] { "ValueString" });
                            }
                            matchedText = true;
                        }

                        if (matchedDropdown && (string.IsNullOrEmpty(textPropertyName) || matchedText))
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogSink.Warn("mcm: try refresh ui error: " + ex.Message);
            }
        }

        private static object CreateSelectorItem(Type itemType, string text)
        {
            if (itemType == null) return null;
            var to = new TaleWorlds.Localization.TextObject(text ?? string.Empty);
            foreach (var ctor in itemType.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
            {
                var pars = ctor.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(TaleWorlds.Localization.TextObject))
                {
                    return ctor.Invoke(new object[] { to });
                }
                if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
                {
                    return ctor.Invoke(new object[] { text });
                }
                if (pars.Length == 2 && pars[0].ParameterType == typeof(TaleWorlds.Localization.TextObject))
                {
                    object arg2 = pars[1].ParameterType == typeof(bool) ? (object)true : null;
                    return ctor.Invoke(new object[] { to, arg2 });
                }
            }
            try
            {
                return Activator.CreateInstance(itemType);
            }
            catch
            {
                return null;
            }
        }

        private static void OpenUrl(string url)
        {
            try
            {
                if (!string.IsNullOrEmpty(url)) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogSink.Warn("open url failed: " + ex.Message);
            }
        }

        private static void OpenLogDirectory()
        {
            try
            {
                string path = LogSink.LogFile;
                string dir = string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    dir = Path.Combine(ModuleLocator.Root, "logs");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                }
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogSink.Warn("open log dir failed: " + ex.Message);
            }
        }
    }
}
