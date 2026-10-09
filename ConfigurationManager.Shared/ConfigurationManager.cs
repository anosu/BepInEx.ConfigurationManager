// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ConfigurationManager.Utilities;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if IL2CPP
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using BaseUnityPlugin = BepInEx.Unity.IL2CPP.BasePlugin;
using UnityInput = BepInEx.Configuration.CompatibleUnityInput;
using GUILayout = ConfigurationManager.Utilities.ManagedLayout;
#endif

namespace ConfigurationManager
{
    /// <summary>
    /// An easy way to let user configure how a plugin behaves without the need to make your own GUI. The user can change any of the settings you expose, even keyboard shortcuts.
    /// https://github.com/ManlyMarco/BepInEx.ConfigurationManager
    /// </summary>
    [BepInPlugin(GUID, "Configuration Manager", Constants.Version)]
    [Browsable(false)]
    public partial class ConfigurationManager : BaseUnityPlugin
    {
        /// <summary>
        /// GUID of this plugin
        /// </summary>
        public const string GUID = "com.bepis.bepinex.configurationmanager";

        /// <summary>
        /// Version constant
        /// </summary>
        public const string Version = Constants.Version;

#if IL2CPP
        internal static ManualLogSource Logger;
#else
        internal new static ManualLogSource Logger;
#endif
        private static SettingFieldDrawer _fieldDrawer;

        private static readonly Color _advancedSettingColor = new Color(1f, 0.95f, 0.67f, 1f);
        private const int WindowId = -68;

        private const string SearchBoxName = "searchBox";
        private bool _focusSearchBox;
        private string _searchString = string.Empty;

        /// <summary>
        /// Event fired every time the manager window is shown or hidden.
        /// </summary>
        public event EventHandler<ValueChangedEventArgs<bool>> DisplayingWindowChanged;

        /// <summary>
        /// Disable the hotkey check used by config manager. If enabled you have to set <see cref="DisplayingWindow"/> to show the manager.
        /// </summary>
        public bool OverrideHotkey;

        private bool _displayingWindow;
        private bool _obsoleteCursor;

        private string _modsWithoutSettings;

        private List<SettingEntryBase> _allSettings;
        private List<PluginSettingsData> _filteredSettings = new List<PluginSettingsData>();

        internal Rect SettingWindowRect { get; private set; }

        /// <summary>
        /// Window is visible and blocks interaction with the game, including after dragging.
        /// </summary>
        public bool IsWindowFullscreen => DisplayingWindow;

        private bool _tipsPluginHeaderWasClicked, _tipsWindowWasMoved;

        private Vector2 _settingWindowScrollPos;

        private PropertyInfo _curLockState;
        private PropertyInfo _curVisible;
        private int _previousCursorLockState;
        private bool _previousCursorVisible;

        internal int LeftColumnWidth { get; private set; }
        internal int RightColumnWidth { get; private set; }

        private readonly ConfigEntry<bool> _showAdvanced;
        private readonly ConfigEntry<bool> _showKeybinds;
        private readonly ConfigEntry<bool> _showSettings;
        private readonly ConfigEntry<KeyboardShortcut> _keybind;
        private readonly ConfigEntry<bool> _hideSingleSection;
        private readonly ConfigEntry<bool> _pluginConfigCollapsedDefault;
        private readonly PluginCollapseState _pluginCollapseState;
        private readonly ConfigEntry<string> _language;
        private bool _showDebug;
        private readonly HotkeyGate _hotkeyGate = new HotkeyGate();

        /// <inheritdoc />
        public ConfigurationManager()
        {
#if IL2CPP
            Logger = Log;
#else
            Logger = base.Logger;
#endif
            _fieldDrawer = new SettingFieldDrawer(this);

            _showAdvanced = Config.Bind("Filtering", "Show advanced", false);
            _showKeybinds = Config.Bind("Filtering", "Show keybinds", true);
            _showSettings = Config.Bind("Filtering", "Show settings", true);
            _keybind = Config.Bind("General", "Show config manager", new KeyboardShortcut(KeyCode.F1),
                new ConfigDescription("The shortcut used to toggle the config manager window on and off.\n" +
                                      "The key can be overridden by a game-specific plugin if necessary, in that case this setting is ignored."));
            _hideSingleSection = Config.Bind("General", "Hide single sections", false, new ConfigDescription("Show section title for plugins with only one section"));
            _pluginConfigCollapsedDefault = Config.Bind("General", "Plugin collapsed default", true, new ConfigDescription("If set to true plugins will be collapsed when opening the configuration manager window"));
            _pluginCollapseState = new PluginCollapseState(_pluginConfigCollapsedDefault.Value);
            _pluginConfigCollapsedDefault.SettingChanged += (sender, args) =>
            {
                _pluginCollapseState.Reset(_pluginConfigCollapsedDefault.Value);
                foreach (var plugin in _filteredSettings) plugin.Collapsed = _pluginConfigCollapsedDefault.Value;
            };
            _language = Config.Bind("General", "Language", Localization.English,
                new ConfigDescription("Interface language / 界面语言", new AcceptableValueList<string>(Localization.English, Localization.SimplifiedChinese)));
            Localization.Language = _language.Value;
            _language.SettingChanged += (sender, args) =>
            {
                Localization.Language = _language.Value;
                ClearWindowInteraction();
            };
        }

#if IL2CPP
        /// <inheritdoc/>
        public override void Load()
        {
            ConfigurationManagerBehaviour.Plugin = this;
            AddComponent<ConfigurationManagerBehaviour>();
        }
        private class ConfigurationManagerBehaviour : MonoBehaviour
        {
            internal static ConfigurationManager Plugin;
            private void Start() => Plugin.Start();
            private void Update() => Plugin.Update();
            private void LateUpdate() => Plugin.LateUpdate();
            private void OnGUI() => Plugin.OnGUI();
            private void OnDestroy() { if (Plugin != null) Plugin.DisplayingWindow = false; }
        }
#endif

        /// <summary>
        /// Is the config manager main window displayed on screen
        /// </summary>
        public bool DisplayingWindow
        {
            get => _displayingWindow;
            set
            {
                if (_displayingWindow == value) return;
                _displayingWindow = value;

                ClearWindowInteraction();
#if IL2CPP
                ModalInput.SetActive(value);
#endif

                if (_displayingWindow)
                {
                    CalculateWindowRect();

                    BuildSettingList();

                    _focusSearchBox = true;

                    // Do through reflection for unity 4 compat
                    if (_curLockState != null)
                    {
                        _previousCursorLockState = _obsoleteCursor ? Convert.ToInt32((bool)_curLockState.GetValue(null, null)) : (int)_curLockState.GetValue(null, null);
                        _previousCursorVisible = (bool)_curVisible.GetValue(null, null);
                    }
                }
                else
                {
                    if (!_previousCursorVisible || _previousCursorLockState != 0) // 0 = CursorLockMode.None
                        SetUnlockCursor(_previousCursorLockState, _previousCursorVisible);
                }

                DisplayingWindowChanged?.Invoke(this, new ValueChangedEventArgs<bool>(value));
            }
        }

        private static void ClearWindowInteraction()
        {
            TooltipState.Update(null, true, 0);
#if IL2CPP
            ImguiCompatibility.ClearInput();
#endif
            SettingFieldDrawer.ClearCache();
        }

        /// <summary>
        /// Register a custom setting drawer for a given type. The action is ran in OnGui in a single setting slot.
        /// Do not use any Begin / End layout methods, and avoid raising height from standard.
        /// </summary>
        public static void RegisterCustomSettingDrawer(Type settingType, Action<SettingEntryBase> onGuiDrawer)
        {
            if (settingType == null) throw new ArgumentNullException(nameof(settingType));
            if (onGuiDrawer == null) throw new ArgumentNullException(nameof(onGuiDrawer));

            if (SettingFieldDrawer.SettingDrawHandlers.ContainsKey(settingType))
                Logger.LogWarning("Tried to add a setting drawer for type " + settingType.FullName + " while one already exists.");
            else
                SettingFieldDrawer.SettingDrawHandlers[settingType] = onGuiDrawer;
        }

        /// <summary>
        /// Rebuild the setting list. Use to update the config manager window if config settings were removed or added while it was open.
        /// </summary>
        public void BuildSettingList()
        {
            SettingSearcher.CollectSettings(out var results, out var modsWithoutSettings, _showDebug);

            _modsWithoutSettings = string.Join(", ", modsWithoutSettings.Select(x => x.TrimStart('!')).OrderBy(x => x).ToArray());
            _allSettings = results.ToList();

            BuildFilteredSettingList();
        }

        private void BuildFilteredSettingList()
        {
            IEnumerable<SettingEntryBase> results = _allSettings;

            var searchStrings = SearchString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (searchStrings.Length > 0)
            {
                results = results.Where(x => ContainsSearchString(x, searchStrings));
            }
            else
            {
                if (!_showAdvanced.Value)
                    results = results.Where(x => x.IsAdvanced != true);
                if (!_showKeybinds.Value)
                    results = results.Where(x => !IsKeyboardShortcut(x));
                if (!_showSettings.Value)
                    results = results.Where(x => x.IsAdvanced == true || IsKeyboardShortcut(x));
            }

            _filteredSettings = results
                .GroupBy(x => x.PluginInfo)
                .Select(pluginSettings =>
                {
                    // GroupBy preserves the order of each category's first appearance.
                    var categories = pluginSettings
                        .GroupBy(x => x.Category)
                        .Select(x => new PluginSettingsData.PluginSettingsGroupData { Name = x.Key, Settings = x.OrderByDescending(set => set.Order).ThenBy(set => set.DispName).ToList() });

                    var website = Utils.GetWebsite(pluginSettings.First().PluginInstance);

                    return new PluginSettingsData
                    {
                        Info = pluginSettings.Key,
                        Categories = categories.ToList(),
                        Collapsed = _pluginCollapseState.Get(pluginSettings.Key.GUID),
                        Website = website
                    };
                })
                .OrderBy(x => x.Info.Name)
                .ToList();
        }

        private static bool IsKeyboardShortcut(SettingEntryBase x)
        {
            return SettingFieldDrawer.IsKeyboardShortcut(x.SettingType) || x.SettingType == typeof(KeyCode);
        }

        private static bool ContainsSearchString(SettingEntryBase setting, string[] searchStrings)
        {
            var combinedSearchTarget = setting.PluginInfo.Name + "\n" +
                                       setting.PluginInfo.GUID + "\n" +
                                       setting.DispName + "\n" +
                                       setting.Category + "\n" +
                                       setting.Description + "\n" +
                                       setting.DefaultValue + "\n" +
                                       setting.Get();

            return searchStrings.All(s => combinedSearchTarget.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }

        private void CalculateWindowRect()
        {
            var width = Mathf.Min(Screen.width, ModernSkin.WindowWidth);
            var height = Screen.height < 560 ? Screen.height : Screen.height - 100;
            var offsetX = Mathf.RoundToInt((Screen.width - width) / 2f);
            var offsetY = Mathf.RoundToInt((Screen.height - height) / 2f);
            SettingWindowRect = new Rect(offsetX, offsetY, width, height);

            LeftColumnWidth = Mathf.RoundToInt(SettingWindowRect.width / 2.8f);
            RightColumnWidth = Mathf.Max(80, (int)SettingWindowRect.width - LeftColumnWidth - 155);

        }

        private void OnGUI()
        {
            var evt = Event.current;
            HandleShortcutEvent(evt);
            if (DisplayingWindow)
            {
                SetUnlockCursor(0, true);

                var originalSkin = GUI.skin;
                var originalColor = GUI.color;
                var originalContentColor = GUI.contentColor;
                var originalBackgroundColor = GUI.backgroundColor;
                var originalMatrix = GUI.matrix;
                var originalEnabled = GUI.enabled;
                var originalFont = _modernSkin != null ? _modernSkin.font : originalSkin.font;
                Rect newRect;
                try
                {
                    GUI.color = GUI.contentColor = GUI.backgroundColor = Color.white;
                    GUI.matrix = Matrix4x4.identity;
                    GUI.enabled = true;
                    ApplyWindowAppearance(originalSkin);
#if IL2CPP
                    GUI.color = new Color(0, 0, 0, 0.55f);
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    newRect = GUI.ModalWindow(WindowId, SettingWindowRect, (GUI.WindowFunction)SettingsWindow, string.Empty);
#else
                    newRect = GUILayout.Window(WindowId, SettingWindowRect, (GUI.WindowFunction)SettingsWindow, string.Empty);
#endif
                }
                finally
                {
                    if (_modernSkin != null) _modernSkin.font = originalFont;
                    GUI.skin = originalSkin;
                    GUI.color = originalColor;
                    GUI.contentColor = originalContentColor;
                    GUI.backgroundColor = originalBackgroundColor;
                    GUI.matrix = originalMatrix;
                    GUI.enabled = originalEnabled;
                }

                if (newRect != SettingWindowRect)
                {
                    SettingWindowRect = newRect;

                    _tipsWindowWasMoved = true;
                }

                if (evt.type == EventType.MouseDown || evt.type == EventType.MouseUp || evt.type == EventType.ScrollWheel || evt.type == EventType.KeyDown)
                    evt.Use();
            }
        }

        private static readonly HoverState TooltipState = new HoverState();
        private static GUIStyle _tooltipStyle;
        private static void DrawTooltip(Rect area)
        {
            if (Event.current.type != EventType.Repaint) return;
#if IL2CPP
            var tooltip = ManagedLayout.Tooltip;
#else
            var tooltip = GUI.tooltip;
#endif
            TooltipState.Update(tooltip, ComboBox.IsOpen, Time.realtimeSinceStartup);
            if (TooltipState.Visible(Time.realtimeSinceStartup))
            {
                if (_tooltipStyle == null)
                {
                    _tooltipStyle = GUI.skin.box.CreateCopy();
                    _tooltipStyle.wordWrap = true;
                    _tooltipStyle.alignment = TextAnchor.MiddleLeft;
                    _tooltipStyle.normal.background = Texture2D.whiteTexture;
                    _tooltipStyle.normal.textColor = new Color(0.07f, 0.10f, 0.14f);
                    _tooltipStyle.padding = ModernSkin.Offset(12, 12, 10, 10);
                }
                var content = new GUIContent(TooltipState.Text);
                var width = Mathf.Min(400, area.width - 36);
                var height = _tooltipStyle.CalcHeight(content, width);
                var mousePosition = Event.current.mousePosition;
                var x = Mathf.Clamp(mousePosition.x + 12, 18, Mathf.Max(18, area.width - width - 18));
                var y = Mathf.Clamp(mousePosition.y + 22 + height > area.height ? mousePosition.y - height - 8 : mousePosition.y + 22,
                    8, Mathf.Max(8, area.height - height - 8));
                var previous = GUI.color;
                try { GUI.color = Color.white; GUI.Box(new Rect(x, y, width, height), content, _tooltipStyle); }
                finally { GUI.color = previous; }
            }
        }

        private void SettingsWindow(int id)
        {
            if (HandleShortcutEvent(Event.current) || !DisplayingWindow) return;
            if (_windowTitleStyle == null)
            {
                _windowTitleStyle = GUI.skin.label.CreateCopy();
                _windowTitleStyle.fontSize = 20;
                _windowTitleStyle.alignment = TextAnchor.MiddleLeft;
                _windowTitleStyle.padding = ModernSkin.Offset(0, 0, 0, 0);
                _windowTitleStyle.wordWrap = false;
            }
            GUI.Label(new Rect(18, 8, Mathf.Max(0, SettingWindowRect.width - 36), 28),
                Localization.Text("Plugin / mod settings"), _windowTitleStyle);

#if IL2CPP
            ManagedLayout.BeginFrame(new Rect(18, 48, Mathf.Max(0, SettingWindowRect.width - 36), Mathf.Max(0, SettingWindowRect.height - 64)));
#endif
            var enabled = GUI.enabled;
            var color = GUI.color;
            SettingFieldDrawer.BeginFrame();
            try
            {
                DrawWindowHeader();
                _settingWindowScrollPos = ImguiCompatibility.BeginScrollView(_settingWindowScrollPos, false, true);
                try
                {
                    GUILayout.BeginVertical();
                    try
                    {
                        if (string.IsNullOrEmpty(SearchString)) DrawTips();
                        if (_filteredSettings.Count == 0)
                            GUILayout.Label(Localization.Text(string.IsNullOrEmpty(SearchString) ? "No settings to display. Check the filters above." : "No matching settings. Try another search or clear it."));
                        // Build every plugin on every event. Cached-height virtualization changes control order.
                        foreach (var plugin in _filteredSettings.ToArray())
                        {
#if IL2CPP
                            using (ManagedLayout.Identity("plugin:" + plugin.Info.GUID))
                            {
                                var depth = ManagedLayout.Depth;
                                try { DrawSinglePlugin(plugin); }
                                catch (Exception ex) { ReportDrawError(plugin.Info.GUID, ex); }
                                finally { ManagedLayout.RestoreDepth(depth); GUI.enabled = enabled; GUI.color = color; }
                            }
#else
                            DrawSinglePlugin(plugin);
#endif
                        }
                        if (_showDebug) GUILayout.Label(Localization.Text("Plugins with no options available: ") + _modsWithoutSettings);
                        ImguiCompatibility.Space(12);
                    }
                    finally { GUILayout.EndVertical(); }
                }
                finally { ImguiCompatibility.EndScrollView(); }
            }
            finally
            {
                try
                {
                    try { SettingFieldDrawer.EndFrame(); }
                    finally
                    {
#if IL2CPP
                        ManagedLayout.EndFrame();
#endif
                    }
                }
                finally { GUI.enabled = enabled; GUI.color = color; }
            }
            if (!ComboBox.IsOpen) GUI.DragWindow(new Rect(0, 0, SettingWindowRect.width, 38));
            SettingFieldDrawer.DrawCurrentDropdown();
            DrawTooltip(SettingWindowRect);
        }

        private void DrawWindowHeader()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            {
                GUI.enabled = SearchString == string.Empty;

                DrawFilter(_showSettings, "Normal");
                DrawFilter(_showKeybinds, "Keys");
                DrawFilter(_showAdvanced, "Advanced");

                GUI.enabled = true;

                ImguiCompatibility.Space(8);

                var newVal = GUILayout.Toggle(_showDebug, Localization.Text("Debug"));
                if (_showDebug != newVal)
                {
                    _showDebug = newVal;
                    BuildSettingList();
                }

                if (GUILayout.Button(Localization.Text("Log")))
                {
                    try { Utils.OpenLog(); }
                    catch (SystemException ex) { Logger.Log(LogLevel.Message | LogLevel.Error, ex.Message); }
                }

                ImguiCompatibility.Space(8);

                if (GUILayout.Button(Localization.Text("Close")))
                {
                    DisplayingWindow = false;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUI.skin.box);
            {
                GUILayout.Label(Localization.Text("Search: "), ImguiCompatibility.ExpandWidth(false));

#if IL2CPP
                if (_focusSearchBox) ImguiCompatibility.FocusNextTextField();
#else
                GUI.SetNextControlName(SearchBoxName);
#endif
                SearchString = ImguiCompatibility.TextField(SearchString, ImguiCompatibility.ExpandWidth(true));

                if (_focusSearchBox)
                {
                    GUI.FocusWindow(WindowId);
#if !IL2CPP
                    GUI.FocusControl(SearchBoxName);
#endif
                    _focusSearchBox = false;
                }

                if (GUILayout.Button(Localization.Text("Clear"), ImguiCompatibility.ExpandWidth(false)))
                    SearchString = string.Empty;

                if (GUILayout.Button(Localization.Text("Language") + ": " + _language.Value, ImguiCompatibility.ExpandWidth(false)))
                    _language.Value = _language.Value == Localization.English ? Localization.SimplifiedChinese : Localization.English;

                ImguiCompatibility.Space(8);

                if (GUILayout.Button(_pluginConfigCollapsedDefault.Value ? Localization.Text("Expand All") : Localization.Text("Collapse All"), ImguiCompatibility.ExpandWidth(false)))
                {
                    var newValue = !_pluginConfigCollapsedDefault.Value;
                    _pluginConfigCollapsedDefault.Value = newValue;
                    _tipsPluginHeaderWasClicked = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFilter(ConfigEntry<bool> filter, string label)
        {
            var value = GUILayout.Toggle(filter.Value, Localization.Text(label));
            if (filter.Value == value) return;
            filter.Value = value;
            BuildFilteredSettingList();
        }

        /// <summary>
        /// String currently entered into the search box
        /// </summary>
        public string SearchString
        {
            get => _searchString;
            private set
            {
                if (value == null)
                    value = string.Empty;

                if (_searchString == value)
                    return;

                _searchString = value;

                BuildFilteredSettingList();
            }
        }

        private void DrawSinglePlugin(PluginSettingsData plugin)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            var categoryHeader = _showDebug ?
                new GUIContent($"{plugin.Info.Name.TrimStart('!')} {plugin.Info.Version}", null, "GUID: " + plugin.Info.GUID) :
                new GUIContent($"{plugin.Info.Name.TrimStart('!')} {plugin.Info.Version}");

            var isSearching = !string.IsNullOrEmpty(SearchString);

            {
                var hasWebsite = plugin.Website != null;
                if (hasWebsite)
                {
                    GUILayout.BeginHorizontal();
                }

                if (SettingFieldDrawer.DrawPluginHeader(categoryHeader, plugin.Collapsed && !isSearching) && !isSearching)
                {
                    _tipsPluginHeaderWasClicked = true;
                    plugin.Collapsed = !plugin.Collapsed;
                    _pluginCollapseState.Set(plugin.Info.GUID, plugin.Collapsed);
                }

                if (hasWebsite)
                {
                    var origColor = GUI.color;
                    GUI.color = Color.gray;
                    if (GUILayout.Button(new GUIContent("URL", null, plugin.Website), GUI.skin.label, ImguiCompatibility.ExpandWidth(false)))
                        Utils.OpenWebsite(plugin.Website);
                    GUI.color = origColor;
                    GUILayout.EndHorizontal();
                }
            }

            if (isSearching || !plugin.Collapsed)
            {
                foreach (var category in plugin.Categories)
                {
                    if (!string.IsNullOrEmpty(category.Name))
                    {
                        if (plugin.Categories.Count > 1 || !_hideSingleSection.Value)
                            SettingFieldDrawer.DrawCategoryHeader(category.Name);
                    }

                    foreach (var setting in category.Settings)
                    {
                        DrawSingleSetting(setting);
                        ImguiCompatibility.Space(6);
                    }
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawSingleSetting(SettingEntryBase setting)
        {
#if IL2CPP
            using (ManagedLayout.Identity("setting:" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(setting)))
            {
#endif
            GUILayout.BeginHorizontal();
            var enabled = GUI.enabled;
            var color = GUI.color;
#if IL2CPP
            var depth = ManagedLayout.Depth;
#endif
            try
            {
                try
                {
                    DrawSettingName(setting);
                    GUILayout.BeginHorizontal(ImguiCompatibility.ExpandWidth(true));
                    try { _fieldDrawer.DrawSettingValue(setting); }
                    finally { GUILayout.EndHorizontal(); }
                    DrawDefaultButton(setting);
                }
                catch (Exception ex)
                {
                    ReportDrawError(setting.PluginInfo.GUID + "/" + setting.Category + "/" + setting.DispName, ex);
#if IL2CPP
                    ManagedLayout.RestoreDepth(depth);
#endif
                    GUILayout.Label(Localization.Text("Failed to draw this field, check log for details."));
                }
            }
            finally
            {
#if IL2CPP
                ManagedLayout.RestoreDepth(depth);
#endif
                GUI.enabled = enabled; GUI.color = color;
                GUILayout.EndHorizontal();
            }
#if IL2CPP
            }
#endif
        }

        private static readonly HashSet<string> DrawErrors = new HashSet<string>();
        private static void ReportDrawError(string key, Exception ex)
        {
            if (DrawErrors.Add(key + ":" + ex.GetType().FullName + ":" + ex.Message))
                Logger.LogError("Failed to draw " + key + " - " + ex);
        }

        private void DrawSettingName(SettingEntryBase setting)
        {
            if (setting.HideSettingName) return;

            var origColor = GUI.color;
            if (setting.IsAdvanced == true)
                GUI.color = _advancedSettingColor;

            GUILayout.Label(new GUIContent(setting.DispName.TrimStart('!'), null, setting.Description),
                ImguiCompatibility.Width(LeftColumnWidth), ImguiCompatibility.MaxWidth(LeftColumnWidth));

            GUI.color = origColor;
        }

        private static void DrawDefaultButton(SettingEntryBase setting)
        {
            if (setting.HideDefaultButton) return;

            object defaultValue = setting.DefaultValue;
            if (defaultValue != null || setting.SettingType.IsClass)
            {
                ImguiCompatibility.Space(5);
                if (GUILayout.Button(Localization.Text("Reset"), ImguiCompatibility.Width(ModernSkin.ResetButtonWidth)))
                    setting.Set(defaultValue);
            }
        }

        private void Start()
        {
            // Use reflection to keep compatibility with unity 4.x since it doesn't have Cursor
            var tCursor = typeof(Cursor);
            _curLockState = tCursor.GetProperty("lockState", BindingFlags.Static | BindingFlags.Public);
            _curVisible = tCursor.GetProperty("visible", BindingFlags.Static | BindingFlags.Public);

            if (_curLockState == null && _curVisible == null)
            {
                _obsoleteCursor = true;

                _curLockState = typeof(Screen).GetProperty("lockCursor", BindingFlags.Static | BindingFlags.Public);
                _curVisible = typeof(Screen).GetProperty("showCursor", BindingFlags.Static | BindingFlags.Public);
            }

            // Check if user has permissions to write config files to disk
            try { Config.Save(); }
            catch (IOException ex) { Logger.Log(LogLevel.Message | LogLevel.Warning, "WARNING: Failed to write to config directory, expect issues!\nError message:" + ex.Message); }
            catch (UnauthorizedAccessException ex) { Logger.Log(LogLevel.Message | LogLevel.Warning, "WARNING: Permission denied to write to config directory, expect issues!\nError message:" + ex.Message); }
        }

        private void Update()
        {
            if (DisplayingWindow) SetUnlockCursor(0, true);

            if (_keybind.Value.MainKey != KeyCode.None && UnityInput.Current.GetKeyUp(_keybind.Value.MainKey)) _hotkeyGate.Release();
            if (OverrideHotkey || SettingFieldDrawer.SettingKeyboardShortcut) return;
            if (_keybind.Value.IsDown() && _hotkeyGate.Press(Time.frameCount)) DisplayingWindow = !DisplayingWindow;
        }

        private void LateUpdate()
        {
            if (DisplayingWindow)
            {
                SetUnlockCursor(0, true);
#if IL2CPP
                ModalInput.SetActive(true);
#endif
                if (!SettingFieldDrawer.SettingKeyboardShortcut) UnityInput.Current.ResetInputAxes();
            }
        }

        private bool HandleShortcutEvent(Event evt)
        {
            if (evt.type == EventType.KeyUp && evt.keyCode == _keybind.Value.MainKey) _hotkeyGate.Release();
            if (OverrideHotkey || SettingFieldDrawer.SettingKeyboardShortcut || evt.type != EventType.KeyDown || !MatchesShortcutEvent(evt)) return false;
            if (_hotkeyGate.Press(Time.frameCount)) DisplayingWindow = !DisplayingWindow;
            evt.Use();
            return !DisplayingWindow;
        }

        private bool MatchesShortcutEvent(Event evt)
        {
            var shortcut = _keybind.Value;
            if (shortcut.MainKey == KeyCode.None || evt.keyCode != shortcut.MainKey) return false;
            var keys = shortcut.Modifiers.Concat(new[] { shortcut.MainKey }).ToArray();
            var control = keys.Contains(KeyCode.LeftControl) || keys.Contains(KeyCode.RightControl);
            var shift = keys.Contains(KeyCode.LeftShift) || keys.Contains(KeyCode.RightShift);
            var alt = keys.Contains(KeyCode.LeftAlt) || keys.Contains(KeyCode.RightAlt);
            var command = keys.Contains(KeyCode.LeftCommand) || keys.Contains(KeyCode.RightCommand);
            if (evt.control != control || evt.shift != shift || evt.alt != alt || evt.command != command) return false;
            return shortcut.Modifiers.All(key =>
                key == KeyCode.LeftControl || key == KeyCode.RightControl ? evt.control :
                key == KeyCode.LeftShift || key == KeyCode.RightShift ? evt.shift :
                key == KeyCode.LeftAlt || key == KeyCode.RightAlt ? evt.alt :
                key == KeyCode.LeftCommand || key == KeyCode.RightCommand ? evt.command : UnityInput.Current.GetKey(key));
        }

        private void SetUnlockCursor(int lockState, bool cursorVisible)
        {
            if (_curLockState != null)
            {
                // Do through reflection for unity 4 compat
                //Cursor.lockState = CursorLockMode.None;
                //Cursor.visible = true;
                if (_obsoleteCursor)
                    _curLockState.SetValue(null, Convert.ToBoolean(lockState), null);
                else
                    _curLockState.SetValue(null, lockState, null);

                _curVisible.SetValue(null, cursorVisible, null);
            }
        }

        private sealed class PluginSettingsData
        {
            public BepInPlugin Info;
            public List<PluginSettingsGroupData> Categories;
            public string Website;
            public bool Collapsed { get; set; }

            public sealed class PluginSettingsGroupData
            {
                public string Name;
                public List<SettingEntryBase> Settings;
            }
        }
    }
}
