// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using ConfigurationManager.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
#if IL2CPP
using UnityInput = BepInEx.Configuration.CompatibleUnityInput;
using GUILayout = ConfigurationManager.Utilities.ManagedLayout;
using GUILayoutUtility = ConfigurationManager.Utilities.ManagedLayout;
using GUILayoutOption = ConfigurationManager.Utilities.LayoutOption;
#endif

namespace ConfigurationManager
{
    internal class SettingFieldDrawer
    {
        private static IEnumerable<KeyCode> _keysToCheck;
        public static Dictionary<Type, Action<SettingEntryBase>> SettingDrawHandlers { get; }

        private static readonly Dictionary<SettingEntryBase, ComboBox> _comboBoxCache = new Dictionary<SettingEntryBase, ComboBox>();
        private static readonly Dictionary<SettingEntryBase, ColorCacheEntry> _colorCache = new Dictionary<SettingEntryBase, ColorCacheEntry>();

        private static ConfigurationManager _instance;

        private static SettingEntryBase _currentKeyboardShortcutToSet;
        private static bool _keyboardShortcutWasDrawn;
        public static bool SettingKeyboardShortcut => _currentKeyboardShortcutToSet != null;

        internal static void BeginFrame()
        {
            _keyboardShortcutWasDrawn = false;
            ComboBox.BeginFrame();
        }
        internal static void EndFrame()
        {
            if (!_keyboardShortcutWasDrawn) CancelKeyboardShortcut();
            ComboBox.EndFrame();
        }
        private static void CancelKeyboardShortcut()
        {
            if (_currentKeyboardShortcutToSet == null) return;
            _currentKeyboardShortcutToSet = null;
            if (GUIUtility.keyboardControl == -1) GUIUtility.keyboardControl = 0;
        }

        static SettingFieldDrawer()
        {
            SettingDrawHandlers = new Dictionary<Type, Action<SettingEntryBase>>
            {
                { typeof(bool), DrawBoolField },
                { typeof(KeyboardShortcut), DrawKeyboardShortcut},
                { typeof(KeyCode), DrawKeyCode },
                { typeof(Color), DrawColor },
                { typeof(Vector2), DrawVector2 },
                { typeof(Vector3), DrawVector3 },
                { typeof(Vector4), DrawVector4 },
                { typeof(Quaternion), DrawQuaternion },
            };
        }

        public SettingFieldDrawer(ConfigurationManager instance)
        {
            _instance = instance;
        }

        public void DrawSettingValue(SettingEntryBase setting)
        {
#if !IL2CPP
            if (setting.CustomDrawer != null)
                setting.CustomDrawer(setting is ConfigSettingEntry newSetting ? newSetting.Entry : null);
            else if (setting.CustomHotkeyDrawer != null)
            {
                var isBeingSet = _currentKeyboardShortcutToSet == setting;
                var isBeingSetOriginal = isBeingSet;

                setting.CustomHotkeyDrawer(setting is ConfigSettingEntry newSetting ? newSetting.Entry : null, ref isBeingSet);

                if (isBeingSet != isBeingSetOriginal)
                {
                    if (isBeingSet) _currentKeyboardShortcutToSet = setting;
                    else CancelKeyboardShortcut();
                }
                if (isBeingSet) _keyboardShortcutWasDrawn = true;
            }
            else
#endif
            if (setting.ShowRangeAsPercent != null && setting.AcceptableValueRange.Key != null)
                DrawRangeField(setting);
            else if (setting.AcceptableValues != null)
                DrawListField(setting);
            else if (DrawFieldBasedOnValueType(setting))
                return;
            else if (setting.SettingType.IsEnum)
                DrawEnumField(setting);
            else
                DrawUnknownField(setting, _instance.RightColumnWidth);

        }

        public static void ClearCache()
        {
            _comboBoxCache.Clear();
            CancelKeyboardShortcut();
            ComboBox.Close();

            foreach (var tex in _colorCache)
                UnityEngine.Object.Destroy(tex.Value.Tex);
            _colorCache.Clear();
        }

        public static void DrawCenteredLabel(string text, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            ImguiCompatibility.FlexibleSpace();
            GUILayout.Label(text);
            ImguiCompatibility.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static GUIStyle _categoryHeaderSkin;
        public static void DrawCategoryHeader(string text)
        {
            if (_categoryHeaderSkin == null)
            {
                _categoryHeaderSkin = GUI.skin.label.CreateCopy();
                _categoryHeaderSkin.alignment = TextAnchor.MiddleLeft;
                _categoryHeaderSkin.wordWrap = true;
                _categoryHeaderSkin.stretchWidth = true;
                _categoryHeaderSkin.fontSize = 16;
                _categoryHeaderSkin.normal.textColor = ModernSkin.Accent;
            }

            GUILayout.Label(text, _categoryHeaderSkin);
        }

        private static GUIStyle _pluginHeaderSkin;
        public static bool DrawPluginHeader(GUIContent content, bool isCollapsed)
        {
            if (_pluginHeaderSkin == null)
            {
                _pluginHeaderSkin = GUI.skin.label.CreateCopy();
                _pluginHeaderSkin.alignment = TextAnchor.MiddleLeft;
                _pluginHeaderSkin.wordWrap = true;
                _pluginHeaderSkin.stretchWidth = true;
                _pluginHeaderSkin.fontSize = 18;
                _pluginHeaderSkin.padding = ModernSkin.Offset(4, 4, 8, 8);
            }

            content.text = (isCollapsed ? "+  " : "−  ") + content.text;
            return GUILayout.Button(content, _pluginHeaderSkin, ImguiCompatibility.ExpandWidth(true));
        }

        public static bool DrawCurrentDropdown()
        {
            var draw = ComboBox.CurrentDropdownDrawer;
            ComboBox.CurrentDropdownDrawer = null;
            if (draw == null) return false;
            draw();
            return true;
        }

        private static void DrawListField(SettingEntryBase setting)
        {
            var acceptableValues = setting.AcceptableValues;
            if (acceptableValues.Length == 0)
                throw new ArgumentException("AcceptableValueListAttribute returned an empty list of acceptable values. You need to supply at least 1 option.");

            if (!setting.SettingType.IsInstanceOfType(acceptableValues.FirstOrDefault(x => x != null)))
                throw new ArgumentException("AcceptableValueListAttribute returned a list with items of type other than the settng type itself.");

            if (setting.SettingType == typeof(KeyCode))
                DrawKeyCode(setting);
            else
                DrawComboboxField(setting, acceptableValues, _instance.SettingWindowRect.yMax);
        }

        private static bool DrawFieldBasedOnValueType(SettingEntryBase setting)
        {
            if (SettingDrawHandlers.TryGetValue(setting.SettingType, out var drawMethod))
            {
#if IL2CPP
                // External GUILayout drawers cannot share the managed layout tree safely.
                if (drawMethod.Method.DeclaringType != typeof(SettingFieldDrawer)) return false;
#endif
                drawMethod(setting);
                return true;
            }
            if (IsKeyboardShortcut(setting.SettingType))
            {
                DrawKeyboardShortcut(setting);
                return true;
            }
            return false;
        }

        internal static bool IsKeyboardShortcut(Type type)
        {
            return type == typeof(KeyboardShortcut) ||
                   (type.FullName == "BepInEx.Unity.IL2CPP.Configuration.KeyboardShortcut" &&
                    type.GetConstructor(new[] { typeof(KeyCode), typeof(KeyCode[]) }) != null);
        }

        private static void DrawBoolField(SettingEntryBase setting)
        {
            var boolVal = (bool)setting.Get();
            var result = GUILayout.Toggle(boolVal, boolVal ? Localization.Text("Enabled") : Localization.Text("Disabled"), ImguiCompatibility.ExpandWidth(true));
            if (result != boolVal)
                setting.Set(result);
        }

        private static void DrawEnumField(SettingEntryBase setting)
        {
            if (setting.SettingType.GetCustomAttributes(typeof(FlagsAttribute), false).Any())
                DrawFlagsField(setting, Enum.GetValues(setting.SettingType), _instance.RightColumnWidth);
            else
                DrawComboboxField(setting, Enum.GetValues(setting.SettingType), _instance.SettingWindowRect.yMax);
        }

        private static void DrawFlagsField(SettingEntryBase setting, IList enumValues, int maxWidth)
        {
            var currentValue = Convert.ToInt64(setting.Get());
            var allValues = enumValues.Cast<Enum>().Select(x => new { name = x.ToString(), val = Convert.ToInt64(x) }).ToArray();

            // Vertically stack Horizontal groups of the options to deal with the options taking more width than is available in the window
            GUILayout.BeginVertical(ImguiCompatibility.MaxWidth(maxWidth));
            {
                for (var index = 0; index < allValues.Length;)
                {
                    GUILayout.BeginHorizontal();
                    {
                        var currentWidth = 0;
                        for (; index < allValues.Length; index++)
                        {
                            var value = allValues[index];

                            // Skip the 0 / none enum value, just uncheck everything to get 0
                            if (value.val != 0)
                            {
                                // Make sure this horizontal group doesn't extend over window width, if it does then start a new horiz group below
                                var textDimension = (int)GUI.skin.toggle.CalcSize(new GUIContent(value.name)).x;
                                currentWidth += textDimension;
                                if (currentWidth > maxWidth && currentWidth != textDimension)
                                    break;

                                GUI.changed = false;
                                var newVal = GUILayout.Toggle((currentValue & value.val) == value.val, value.name,
                                    ImguiCompatibility.ExpandWidth(false));
                                if (GUI.changed)
                                {
                                    var newValue = newVal ? currentValue | value.val : currentValue & ~value.val;
                                    setting.Set(Enum.ToObject(setting.SettingType, newValue));
                                }
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                GUI.changed = false;
            }
            GUILayout.EndVertical();
            // Make sure the reset button is properly spaced
            ImguiCompatibility.FlexibleSpace();
        }

        private static void DrawComboboxField(SettingEntryBase setting, IList list, float windowYmax)
        {
            var buttonText = ObjectToGuiContent(setting.Get());
            var dispRect = GUILayoutUtility.GetRect(buttonText, GUI.skin.button, ImguiCompatibility.ExpandWidth(true));

            if (!_comboBoxCache.TryGetValue(setting, out var box))
            {
                box = new ComboBox(dispRect, buttonText, list.Cast<object>().Select(ObjectToGuiContent).ToArray(), GUI.skin.button, windowYmax);
                _comboBoxCache[setting] = box;
            }
            else
            {
                box.Rect = dispRect;
                box.ButtonContent = buttonText;
            }

            box.Bounds = _instance.SettingWindowRect;
            box.Show(id =>
            {
                if (id >= 0 && id < list.Count)
                    setting.Set(list[id]);
            });
        }

        private static GUIContent ObjectToGuiContent(object x)
        {
            if (x is Enum)
            {
                var enumType = x.GetType();
                var enumMember = enumType.GetMember(x.ToString()).FirstOrDefault();
                var attr = enumMember?.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault();
                if (attr != null)
                    return new GUIContent(attr.Description);
                return new GUIContent(x.ToString().ToProperCase());
            }
            return new GUIContent(x.ToString());
        }

        private static void DrawRangeField(SettingEntryBase setting)
        {
            var value = setting.Get();
            var converted = (float)Convert.ToDouble(value, CultureInfo.InvariantCulture);
            var leftValue = (float)Convert.ToDouble(setting.AcceptableValueRange.Key, CultureInfo.InvariantCulture);
            var rightValue = (float)Convert.ToDouble(setting.AcceptableValueRange.Value, CultureInfo.InvariantCulture);

            var result = ImguiCompatibility.HorizontalSlider(converted, leftValue, rightValue, ImguiCompatibility.ExpandWidth(true));
            if (Math.Abs(result - converted) > Mathf.Abs(rightValue - leftValue) / 1000)
            {
                var newValue = RangeValue.FromSlider(result, setting.SettingType, setting.AcceptableValueRange.Key, setting.AcceptableValueRange.Value);
                setting.Set(newValue);
            }

            if (setting.ShowRangeAsPercent == true)
            {
                DrawCenteredLabel(
                    Mathf.Round(100 * Mathf.Abs(result - leftValue) / Mathf.Abs(rightValue - leftValue)) + "%",
                    ImguiCompatibility.Width(72));
            }
            else
            {
                var strVal = Convert.ToString(value, CultureInfo.InvariantCulture).AppendZeroIfFloat(setting.SettingType);
                ImguiCompatibility.EditValue(strVal, strResult =>
                {
                    try
                    {
                        setting.Set(RangeValue.Parse(strResult, setting.SettingType, setting.AcceptableValueRange.Key, setting.AcceptableValueRange.Value));
                    }
                    catch (FormatException)
                    {
                        // Ignore user typing in bad data
                    }
                    catch (OverflowException) { }
                }, ImguiCompatibility.Width(88));
            }
        }

        private void DrawUnknownField(SettingEntryBase setting, int rightColumnWidth)
        {
            // Try to use user-supplied converters
            if (setting.ObjToStr != null && setting.StrToObj != null)
            {
                var text = setting.ObjToStr(setting.Get()).AppendZeroIfFloat(setting.SettingType);
                ImguiCompatibility.EditValue(text, result =>
                {
                    try { setting.Set(setting.StrToObj(result)); }
                    catch (FormatException) { }
                    catch (OverflowException) { }
                    catch (ArgumentException) { }
                }, ImguiCompatibility.ExpandWidth(true));
            }
            else
            {
                // Fall back to slow/less reliable method
                var rawValue = setting.Get();
                var value = rawValue == null ? "NULL" : Convert.ToString(rawValue, CultureInfo.InvariantCulture).AppendZeroIfFloat(setting.SettingType);
                if (CanCovert(value, setting.SettingType))
                {
                    ImguiCompatibility.EditValue(value, result =>
                    {
                        try { setting.Set(Convert.ChangeType(result, setting.SettingType, CultureInfo.InvariantCulture)); }
                        catch (FormatException) { }
                        catch (OverflowException) { }
                    }, ImguiCompatibility.ExpandWidth(true));
                }
                else
                {
                    GUILayout.Label(value, ImguiCompatibility.ExpandWidth(true));
                }
            }

        }

        private readonly Dictionary<Type, bool> _canCovertCache = new Dictionary<Type, bool>();
        private bool CanCovert(string value, Type type)
        {
            if (_canCovertCache.ContainsKey(type))
                return _canCovertCache[type];

            try
            {
                var _ = Convert.ChangeType(value, type);
                _canCovertCache[type] = true;
                return true;
            }
            catch
            {
                _canCovertCache[type] = false;
                return false;
            }
        }

        private static void DrawKeyCode(SettingEntryBase setting)
        {
            if (ReferenceEquals(_currentKeyboardShortcutToSet, setting))
            {
                GUILayout.Label(Localization.Text("Press any key"), ImguiCompatibility.ExpandWidth(true));
                GUIUtility.keyboardControl = -1;

                var input = UnityInput.Current;
                if (_keysToCheck == null) _keysToCheck = input.SupportedKeyCodes.Except(new[] { KeyCode.Mouse0, KeyCode.None }).ToArray();
                foreach (var key in _keysToCheck)
                {
                    if (input.GetKeyUp(key))
                    {
                        setting.Set(key);
                        CancelKeyboardShortcut();
                        break;
                    }
                }
                if (GUILayout.Button(Localization.Text("Cancel"), ImguiCompatibility.ExpandWidth(false)))
                    CancelKeyboardShortcut();
            }
            else
            {
                var acceptableValues = setting.AcceptableValues?.Length > 1 ? setting.AcceptableValues : Enum.GetValues(setting.SettingType);
                DrawComboboxField(setting, acceptableValues, _instance.SettingWindowRect.yMax);

                if (GUILayout.Button(new GUIContent(Localization.Text("Set..."), null, Localization.Text("Set the key by pressing any key on your keyboard.")), ImguiCompatibility.ExpandWidth(false)))
                    _currentKeyboardShortcutToSet = setting;
            }
            if (ReferenceEquals(_currentKeyboardShortcutToSet, setting)) _keyboardShortcutWasDrawn = true;
        }

        private static void DrawKeyboardShortcut(SettingEntryBase setting)
        {
            if (ReferenceEquals(_currentKeyboardShortcutToSet, setting))
            {
                GUILayout.Label(Localization.Text("Press any key combination"), ImguiCompatibility.ExpandWidth(true));
                GUIUtility.keyboardControl = -1;

                var input = UnityInput.Current;
                if (_keysToCheck == null) _keysToCheck = input.SupportedKeyCodes.Except(new[] { KeyCode.Mouse0, KeyCode.None }).ToArray();
                foreach (var key in _keysToCheck)
                {
                    if (input.GetKeyUp(key))
                    {
                        setting.Set(Activator.CreateInstance(setting.SettingType, new object[] { key, _keysToCheck.Where(input.GetKey).ToArray() }));
                        CancelKeyboardShortcut();
                        break;
                    }
                }
                if (GUILayout.Button(Localization.Text("Cancel"), ImguiCompatibility.ExpandWidth(false)))
                    CancelKeyboardShortcut();
            }
            else
            {
                if (GUILayout.Button(Localization.Text(setting.Get().ToString()), ImguiCompatibility.ExpandWidth(true)))
                    _currentKeyboardShortcutToSet = setting;

                if (GUILayout.Button(Localization.Text("Clear"), ImguiCompatibility.ExpandWidth(false)))
                {
                    setting.Set(Activator.CreateInstance(setting.SettingType));
                    CancelKeyboardShortcut();
                }
            }
            if (ReferenceEquals(_currentKeyboardShortcutToSet, setting)) _keyboardShortcutWasDrawn = true;
        }


        private static void DrawVector2(SettingEntryBase obj)
        {
            var setting = (Vector2)obj.Get();
            DrawSingleVectorSlider(setting.x, "X", value => { var current = (Vector2)obj.Get(); current.x = value; obj.Set(current); });
            DrawSingleVectorSlider(setting.y, "Y", value => { var current = (Vector2)obj.Get(); current.y = value; obj.Set(current); });
        }

        private static void DrawVector3(SettingEntryBase obj)
        {
            var setting = (Vector3)obj.Get();
            DrawSingleVectorSlider(setting.x, "X", value => { var current = (Vector3)obj.Get(); current.x = value; obj.Set(current); });
            DrawSingleVectorSlider(setting.y, "Y", value => { var current = (Vector3)obj.Get(); current.y = value; obj.Set(current); });
            DrawSingleVectorSlider(setting.z, "Z", value => { var current = (Vector3)obj.Get(); current.z = value; obj.Set(current); });
        }

        private static void DrawVector4(SettingEntryBase obj)
        {
            var setting = (Vector4)obj.Get();
            DrawSingleVectorSlider(setting.x, "X", value => { var current = (Vector4)obj.Get(); current.x = value; obj.Set(current); });
            DrawSingleVectorSlider(setting.y, "Y", value => { var current = (Vector4)obj.Get(); current.y = value; obj.Set(current); });
            DrawSingleVectorSlider(setting.z, "Z", value => { var current = (Vector4)obj.Get(); current.z = value; obj.Set(current); });
            DrawSingleVectorSlider(setting.w, "W", value => { var current = (Vector4)obj.Get(); current.w = value; obj.Set(current); });
        }

        private static void DrawQuaternion(SettingEntryBase obj)
        {
            var setting = (Quaternion)obj.Get();
            DrawSingleVectorSlider(setting.x, "X", value => { var current = (Quaternion)obj.Get(); current.x = value; obj.Set(current); });
            DrawSingleVectorSlider(setting.y, "Y", value => { var current = (Quaternion)obj.Get(); current.y = value; obj.Set(current); });
            DrawSingleVectorSlider(setting.z, "Z", value => { var current = (Quaternion)obj.Get(); current.z = value; obj.Set(current); });
            DrawSingleVectorSlider(setting.w, "W", value => { var current = (Quaternion)obj.Get(); current.w = value; obj.Set(current); });
        }

        private static void DrawSingleVectorSlider(float setting, string label, Action<float> commit)
        {
            GUILayout.Label(label, ImguiCompatibility.ExpandWidth(false));
            ImguiCompatibility.EditValue(setting.ToString("G", CultureInfo.InvariantCulture), text =>
            {
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) commit(value);
            }, ImguiCompatibility.ExpandWidth(true));
        }

        private static bool _drawColorHex;
        private static void DrawColor(SettingEntryBase obj)
        {
            var colorValue = (Color)obj.Get();

            GUI.changed = false;

            if (!_colorCache.TryGetValue(obj, out var cacheEntry))
            {
                var tex = new Texture2D(100, 20, TextureFormat.ARGB32, false);
                cacheEntry = new ColorCacheEntry { Tex = tex, Last = colorValue };
                FillTex(colorValue, tex);
                _colorCache[obj] = cacheEntry;
            }

            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(cacheEntry.Tex, ImguiCompatibility.ExpandWidth(false));

                    var colorStr = _drawColorHex ? "#" + ImguiCompatibility.ColorToHex(colorValue) : $"{colorValue.r:F2} {colorValue.g:F2} {colorValue.b:F2} {colorValue.a:F2}";
                    var hexMode = _drawColorHex;
                    ImguiCompatibility.EditValue(colorStr, newColorStr =>
                    {
                        if (hexMode)
                        {
                            if (ColorUtility.TryParseHtmlString(newColorStr, out var parsedColor))
                            { colorValue = parsedColor; obj.Set(colorValue); }
                        }
                        else
                        {
                            var split = newColorStr.Split(' ');
                            if (split.Length == 4 && float.TryParse(split[0], out var r) && float.TryParse(split[1], out var g) && float.TryParse(split[2], out var b) && float.TryParse(split[3], out var a))
                            { colorValue = new Color(r, g, b, a); obj.Set(colorValue); }
                        }
                    }, ImguiCompatibility.ExpandWidth(true));

                    _drawColorHex = GUILayout.Toggle(_drawColorHex, Localization.Text("Hex"), ImguiCompatibility.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("R", ImguiCompatibility.ExpandWidth(false));
                    colorValue.r = ImguiCompatibility.HorizontalSlider(colorValue.r, 0f, 1f, ImguiCompatibility.ExpandWidth(true));
                    GUILayout.Label("G", ImguiCompatibility.ExpandWidth(false));
                    colorValue.g = ImguiCompatibility.HorizontalSlider(colorValue.g, 0f, 1f, ImguiCompatibility.ExpandWidth(true));
                    GUILayout.Label("B", ImguiCompatibility.ExpandWidth(false));
                    colorValue.b = ImguiCompatibility.HorizontalSlider(colorValue.b, 0f, 1f, ImguiCompatibility.ExpandWidth(true));
                    GUILayout.Label("A", ImguiCompatibility.ExpandWidth(false));
                    colorValue.a = ImguiCompatibility.HorizontalSlider(colorValue.a, 0f, 1f, ImguiCompatibility.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            if (colorValue != cacheEntry.Last)
            {
                if (colorValue != (Color)obj.Get()) obj.Set(colorValue);
                FillTex(colorValue, cacheEntry.Tex);
                cacheEntry.Last = colorValue;
            }

            void FillTex(Color color, Texture2D tex)
            {
                if (color.a < 1f) tex.FillTextureCheckerboard();
                tex.FillTexture(color);
            }
        }

        private sealed class ColorCacheEntry
        {
            public Color Last;
            public Texture2D Tex;
        }
    }
}
