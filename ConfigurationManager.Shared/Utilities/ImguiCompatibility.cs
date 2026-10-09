using System;
using System.Globalization;
using UnityEngine;
#if IL2CPP
using GUILayout = ConfigurationManager.Utilities.ManagedLayout;
using GUILayoutUtility = ConfigurationManager.Utilities.ManagedLayout;
using GUILayoutOption = ConfigurationManager.Utilities.LayoutOption;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

namespace ConfigurationManager.Utilities
{
    // Keep replacements local to the manager; never patch the game's shared IMGUI assembly.
    internal static class ImguiCompatibility
    {
        public static GUILayoutOption Width(float value)
        {
#if IL2CPP
            return new LayoutOption(LayoutRule.Width, value);
#else
            return GUILayout.Width(value);
#endif
        }

        public static GUILayoutOption Height(float value)
        {
#if IL2CPP
            return new LayoutOption(LayoutRule.Height, value);
#else
            return GUILayout.Height(value);
#endif
        }

        public static GUILayoutOption MinWidth(float value)
        {
#if IL2CPP
            return new LayoutOption(LayoutRule.MinWidth, value);
#else
            return GUILayout.MinWidth(value);
#endif
        }

        public static GUILayoutOption MaxWidth(float value)
        {
#if IL2CPP
            return new LayoutOption(LayoutRule.MaxWidth, value);
#else
            return GUILayout.MaxWidth(value);
#endif
        }

        public static GUILayoutOption MinHeight(float value)
        {
#if IL2CPP
            return new LayoutOption(LayoutRule.MinHeight, value);
#else
            return GUILayout.MinHeight(value);
#endif
        }

        public static GUILayoutOption MaxHeight(float value)
        {
#if IL2CPP
            return new LayoutOption(LayoutRule.MaxHeight, value);
#else
            return GUILayout.MaxHeight(value);
#endif
        }

        public static GUILayoutOption ExpandWidth(bool value)
        {
#if IL2CPP
            return new LayoutOption(LayoutRule.ExpandWidth, value ? 1 : 0);
#else
            return GUILayout.ExpandWidth(value);
#endif
        }

        public static GUILayoutOption ExpandHeight(bool value)
        {
#if IL2CPP
            return new LayoutOption(LayoutRule.ExpandHeight, value ? 1 : 0);
#else
            return GUILayout.ExpandHeight(value);
#endif
        }

        public static void Space(float pixels)
        {
#if IL2CPP
            ManagedLayout.Space(pixels);
#else
            GUILayout.Space(pixels);
#endif
        }

        public static void FlexibleSpace()
        {
#if IL2CPP
            ManagedLayout.FlexibleSpace();
#else
            GUILayout.FlexibleSpace();
#endif
        }

        public static Rect GetLastRect()
        {
#if IL2CPP
            return ManagedLayout.GetLastRect();
#else
            return GUILayoutUtility.GetLastRect();
#endif
        }

        public static float HorizontalSlider(float value, float left, float right, params GUILayoutOption[] options)
        {
#if IL2CPP
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.horizontalSlider, options);
            var enabled = GUI.enabled;
            try { GUI.enabled = enabled && !ComboBox.IsOpen && ManagedLayout.Visible(rect); return Slider(rect, value, left, right, false); }
            finally { GUI.enabled = enabled; }
#else
            return GUILayout.HorizontalSlider(value, left, right, options);
#endif
        }

#if IL2CPP
        internal static Vector2 UpdateVerticalScroll(Rect rect, Vector2 scroll, float contentHeight, bool overlay = false)
        {
            var evt = Event.current;
            var wheel = 0f;
            if (GUI.enabled && (overlay || !ComboBox.IsOpen) && evt.type == EventType.ScrollWheel && rect.Contains(evt.mousePosition))
            {
                wheel = evt.delta.y * 28;
                evt.Use();
            }
            scroll.x = 0;
            scroll.y = ScrollMath.Update(scroll.y, wheel, contentHeight, rect.height, false);
            var rail = new Rect(rect.xMax - 12, rect.y, 10, rect.height);
            var enabled = GUI.enabled;
            try
            {
                GUI.enabled = enabled && (overlay || !ComboBox.IsOpen);
                scroll.y = Slider(rail, scroll.y, 0, Mathf.Max(0, contentHeight - rect.height), true);
            }
            finally { GUI.enabled = enabled; }
            return scroll;
        }

        private static float Slider(Rect rect, float value, float start, float end, bool vertical)
        {
            var id = GUIUtility.GetControlID(0x434d534c, FocusType.Passive, rect);
            var evt = Event.current;
            var type = evt.GetTypeForControl(id);
            if (GUI.enabled && type == EventType.MouseDown && evt.button == 0 && ManagedLayout.HitTest(rect))
            {
                GUIUtility.hotControl = id;
                _ownedHotControl = id;
                _mouseTextKey = null;
                evt.Use();
            }
            if (GUIUtility.hotControl == id)
            {
                if (type == EventType.MouseDown || type == EventType.MouseDrag)
                {
                    var span = vertical ? rect.height : rect.width;
                    var offset = vertical ? evt.mousePosition.y - rect.y : evt.mousePosition.x - rect.x;
                    var result = Mathf.Lerp(start, end, Mathf.Clamp01(offset / Mathf.Max(1, span)));
                    if (value != result) { value = result; GUI.changed = true; }
                    if (type == EventType.MouseDrag) evt.Use();
                }
                if (type == EventType.MouseUp)
                {
                    GUIUtility.hotControl = 0;
                    _ownedHotControl = 0;
                    evt.Use();
                }
            }
            GUI.Box(rect, GUIContent.none, vertical ? GUI.skin.verticalSlider : GUI.skin.horizontalSlider);
            var fraction = start == end ? 0 : Mathf.InverseLerp(start, end, value);
            var thumb = vertical
                ? new Rect(rect.x, rect.y + fraction * Mathf.Max(0, rect.height - 18), rect.width, 18)
                : new Rect(rect.x + fraction * Mathf.Max(0, rect.width - 12), rect.y - 3, 12, Mathf.Max(12, rect.height + 6));
            GUI.Box(thumb, GUIContent.none, vertical ? GUI.skin.verticalSliderThumb : GUI.skin.horizontalSliderThumb);
            return value;
        }
#endif

        public static Vector2 BeginScrollView(Vector2 scroll, bool horizontal, bool vertical)
        {
#if IL2CPP
            return ManagedLayout.BeginScrollView(scroll);
#else
            return GUILayout.BeginScrollView(scroll, horizontal, vertical);
#endif
        }

        public static void EndScrollView()
        {
#if IL2CPP
            ManagedLayout.EndScrollView();
#else
            GUILayout.EndScrollView();
#endif
        }

        public static Vector2 BeginFixedScrollView(Rect rect, Vector2 scroll, Rect content, bool horizontal, bool vertical)
        {
#if IL2CPP
            scroll = UpdateVerticalScroll(rect, scroll, content.height, true);
            var viewport = new Rect(rect.x, rect.y, Mathf.Max(0, rect.width - 16), rect.height);
            GUI.BeginClip(viewport, new Vector2(0, -scroll.y - content.y), Vector2.zero, false);
            return scroll;
#else
            return GUI.BeginScrollView(rect, scroll, content, horizontal, vertical);
#endif
        }

        public static void EndFixedScrollView()
        {
#if IL2CPP
            GUI.EndClip();
#else
            GUI.EndScrollView(true);
#endif
        }

        public static void EditValue(string value, Action<string> commit, params GUILayoutOption[] options)
        {
#if IL2CPP
            TextFieldCore(value, commit, options);
#else
            var result = TextField(value, options);
            if (result != value) commit(result);
#endif
        }

        public static string TextField(string value, params GUILayoutOption[] options)
        {
#if IL2CPP
            return TextFieldCore(value, null, options);
#else
            return GUILayout.TextField(value, options);
#endif
        }

#if IL2CPP
        private static string TextFieldCore(string value, Action<string> commit, GUILayoutOption[] options)
        {
            var rect = GUILayoutUtility.GetRect(new GUIContent(value), GUI.skin.textField, options);
            var key = ManagedLayout.LastKey;
            if (_seenTextKeySet.Add(key)) _seenTextKeys.Add(key);
            var id = GUIUtility.GetControlID(key.GetHashCode(), FocusType.Keyboard, rect);
            if (!_textStates.TryGetValue(key, out var state))
                _textStates[key] = state = new TextEditState(value);
            if (commit != null) _finishEdits[key] = () => { if (state.Text != value) commit(state.Text); };
            else { _finishEdits.Remove(key); _dirtyEdits.Remove(key); }
            var screen = GUIUtility.GUIToScreenPoint(rect.position);
            _fieldScreens[key] = new Rect(screen.x, screen.y, rect.width, rect.height);
            var evt = Event.current;
            var editable = GUI.enabled && !ComboBox.IsOpen && ManagedLayout.Visible(rect);
            if (_focusedTextKey == key && (!editable || (evt.type == EventType.MouseDown && !ManagedLayout.HitTest(rect))))
            {
                _focusedTextKey = null;
                ReleaseKeyboardControl();
            }
            if (_focusedTextKey != key)
            {
                FinishEdit(key);
                state.Sync(value);
            }
            var before = state.Text;
            if (editable && evt.type == EventType.MouseDown && evt.button == 0 && ManagedLayout.HitTest(rect))
            {
                _focusedTextKey = key;
                GUIUtility.keyboardControl = id;
                _ownedKeyboardControl = id;
                GUIUtility.hotControl = id;
                _ownedHotControl = id;
                _mouseTextKey = key;
                state.Move(NearestCaret(state.Text, evt.mousePosition.x - rect.x - GUI.skin.textField.padding.left + state.ScrollX), evt.shift);
                if (evt.clickCount >= 2) state.SelectAll();
                _caretEpoch = Time.realtimeSinceStartup;
                evt.Use();
            }
            if (_focusNextTextField) { _focusedTextKey = key; GUIUtility.keyboardControl = id; _ownedKeyboardControl = id; _focusNextTextField = false; }
            var focused = _focusedTextKey == key;
            if (focused) { GUIUtility.keyboardControl = id; _ownedKeyboardControl = id; }
            if (focused && GUIUtility.hotControl == id && evt.type == EventType.MouseDrag)
            {
                state.Move(NearestCaret(state.Text, evt.mousePosition.x - rect.x - GUI.skin.textField.padding.left + state.ScrollX), true);
                _caretEpoch = Time.realtimeSinceStartup;
                evt.Use();
            }
            if (GUIUtility.hotControl == id && evt.type == EventType.MouseUp) { ReleaseMouseControl(); evt.Use(); }
            if (editable && focused && evt.type == EventType.KeyDown)
            {
                var modifier = evt.control || evt.command;
                if (modifier && evt.keyCode == KeyCode.A) state.SelectAll();
                else if (modifier && evt.keyCode == KeyCode.Z) { if (evt.shift) state.Redo(); else state.Undo(); }
                else if (modifier && evt.keyCode == KeyCode.Y) state.Redo();
                else if (modifier && evt.keyCode == KeyCode.C) GUIUtility.systemCopyBuffer = state.SelectedText;
                else if (modifier && evt.keyCode == KeyCode.X)
                {
                    GUIUtility.systemCopyBuffer = state.SelectedText;
                    state.Insert(string.Empty);
                }
                else if (modifier && evt.keyCode == KeyCode.V) state.Insert(GUIUtility.systemCopyBuffer ?? string.Empty);
                else if (evt.keyCode == KeyCode.Backspace) state.Backspace();
                else if (evt.keyCode == KeyCode.Delete) state.Delete();
                else if (evt.keyCode == KeyCode.LeftArrow) { if (modifier) state.WordLeft(evt.shift); else state.Left(evt.shift); }
                else if (evt.keyCode == KeyCode.RightArrow) { if (modifier) state.WordRight(evt.shift); else state.Right(evt.shift); }
                else if (evt.keyCode == KeyCode.Home) state.Move(0, evt.shift);
                else if (evt.keyCode == KeyCode.End) state.Move(state.Text.Length, evt.shift);
                else if (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    if (evt.keyCode == KeyCode.Escape) { _dirtyEdits.Remove(key); state.Sync(value); }
                    _focusedTextKey = null; ReleaseKeyboardControl();
                }
                else if (evt.keyCode == KeyCode.Tab)
                {
                    var index = _previousTextKeys.IndexOf(key);
                    if (_previousTextKeys.Count > 0)
                        _focusedTextKey = _previousTextKeys[(index + (evt.shift ? _previousTextKeys.Count - 1 : 1)) % _previousTextKeys.Count];
                }
                else if (!evt.control && !evt.command && !char.IsControl(evt.character))
                    state.Insert(evt.character.ToString());
                _caretEpoch = Time.realtimeSinceStartup;
                evt.Use();
            }
            if (state.Text != before && commit != null && _focusedTextKey == key) _dirtyEdits.Add(key);
            if (_focusedTextKey != key) FinishEdit(key);
            focused = _focusedTextKey == key;
            DrawTextField(rect, state, focused);
            if (state.Text != before) GUI.changed = true;
            return state.Text;
        }
        private static readonly System.Collections.Generic.Dictionary<string, Action> _finishEdits = new System.Collections.Generic.Dictionary<string, Action>();
        private static readonly System.Collections.Generic.Dictionary<string, Rect> _fieldScreens = new System.Collections.Generic.Dictionary<string, Rect>();
        private static readonly System.Collections.Generic.HashSet<string> _dirtyEdits = new System.Collections.Generic.HashSet<string>();
        private static void FinishEdit(string key)
        {
            if (!_dirtyEdits.Remove(key) || !_finishEdits.TryGetValue(key, out var finish)) return;
            try { finish(); }
            catch (Exception ex) { ConfigurationManager.Logger.LogError("Failed to save edited setting - " + ex); }
        }
        private static bool _focusNextTextField;
        private static string _focusedTextKey;
        private static string _mouseTextKey;
        private static int _ownedHotControl, _ownedKeyboardControl;
        private static void ReleaseMouseControl()
        {
            if (_ownedHotControl != 0 && GUIUtility.hotControl == _ownedHotControl) GUIUtility.hotControl = 0;
            _ownedHotControl = 0; _mouseTextKey = null;
        }
        private static void ReleaseKeyboardControl()
        {
            if (_ownedKeyboardControl != 0 && GUIUtility.keyboardControl == _ownedKeyboardControl) GUIUtility.keyboardControl = 0;
            _ownedKeyboardControl = 0;
        }
        private static readonly System.Collections.Generic.List<string> _seenTextKeys = new System.Collections.Generic.List<string>();
        private static readonly System.Collections.Generic.HashSet<string> _seenTextKeySet = new System.Collections.Generic.HashSet<string>();
        private static readonly System.Collections.Generic.List<string> _previousTextKeys = new System.Collections.Generic.List<string>();
        internal static void BeginInputFrame()
        {
            _seenTextKeys.Clear(); _seenTextKeySet.Clear();
            if (_focusedTextKey != null && Event.current.type == EventType.MouseDown &&
                _fieldScreens.TryGetValue(_focusedTextKey, out var rect) &&
                !rect.Contains(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)))
            {
                FinishEdit(_focusedTextKey); _focusedTextKey = null;
                ReleaseKeyboardControl();
            }
        }
        internal static void EndInputFrame()
        {
            if (_focusedTextKey != null && !_seenTextKeySet.Contains(_focusedTextKey))
            {
                _focusedTextKey = null; ReleaseKeyboardControl();
            }
            if (_mouseTextKey != null && !_seenTextKeySet.Contains(_mouseTextKey)) ReleaseMouseControl();
            _previousTextKeys.Clear(); _previousTextKeys.AddRange(_seenTextKeys);
            var removed = new System.Collections.Generic.List<string>();
            foreach (var key in _textStates.Keys) if (!_seenTextKeySet.Contains(key)) removed.Add(key);
            foreach (var key in removed)
            {
                FinishEdit(key); _textStates.Remove(key); _finishEdits.Remove(key); _fieldScreens.Remove(key);
            }
        }
        internal static void ClearInput()
        {
            foreach (var key in new System.Collections.Generic.List<string>(_dirtyEdits)) FinishEdit(key);
            ReleaseMouseControl(); ReleaseKeyboardControl();
            _focusedTextKey = null; _focusNextTextField = false;
            _textStates.Clear(); _previousTextKeys.Clear(); _seenTextKeys.Clear(); _seenTextKeySet.Clear();
            _finishEdits.Clear(); _fieldScreens.Clear(); _dirtyEdits.Clear();
        }
        private static float _caretEpoch;
        private static readonly System.Collections.Generic.Dictionary<string, TextEditState> _textStates =
            new System.Collections.Generic.Dictionary<string, TextEditState>();
        private static GUIStyle _inputTextStyle;
        private static float TextWidth(string text) => _inputTextStyle.CalcSize(new GUIContent(text)).x;
        private static void EnsureInputStyle()
        {
            if (_inputTextStyle != null) return;
            _inputTextStyle = GUI.skin.label.CreateCopy();
            _inputTextStyle.fontSize = GUI.skin.textField.fontSize;
            _inputTextStyle.padding = ModernSkin.Offset(0, 0, 0, 0);
            _inputTextStyle.wordWrap = false;
            _inputTextStyle.alignment = TextAnchor.MiddleLeft;
        }
        private static int NearestCaret(string text, float x)
        {
            EnsureInputStyle();
            var nearest = 0; var distance = float.MaxValue;
            for (var i = 0; i <= text.Length; i++)
            {
                if (i > 0 && i < text.Length && char.IsSurrogatePair(text, i - 1)) continue;
                var candidate = Mathf.Abs(x - TextWidth(text.Substring(0, i)));
                if (candidate < distance) { nearest = i; distance = candidate; }
            }
            return nearest;
        }
        private static void Paint(Rect rect, Color color)
        {
            var previous = GUI.color;
            try { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); }
            finally { GUI.color = previous; }
        }
        private static void DrawTextField(Rect rect, TextEditState state, bool focused)
        {
            EnsureInputStyle();
            GUI.Box(rect, GUIContent.none, GUI.skin.textField);
            if (Event.current.type != EventType.Repaint || !ManagedLayout.Visible(rect)) return;
            if (focused) Paint(new Rect(rect.x, rect.yMax - 2, rect.width, 2), ModernSkin.Accent);
            var inner = new Rect(rect.x + 10, rect.y + 3, Math.Max(0, rect.width - 20), Math.Max(0, rect.height - 6));
            var caretX = TextWidth(state.Text.Substring(0, state.Caret));
            if (focused) state.ScrollX = Math.Max(0, Math.Min(state.ScrollX, caretX));
            if (focused && caretX - state.ScrollX > inner.width - 2) state.ScrollX = Math.Max(0, caretX - inner.width + 2);
            state.ScrollX = focused ? Mathf.RoundToInt(state.ScrollX) : 0;
            GUI.BeginClip(inner, Vector2.zero, Vector2.zero, false);
            try
            {
                if (focused && state.SelectionLength > 0)
                {
                    var start = TextWidth(state.Text.Substring(0, state.SelectionStart));
                    var end = TextWidth(state.Text.Substring(0, state.SelectionStart + state.SelectionLength));
                    Paint(new Rect(start - state.ScrollX, 2, end - start, Math.Max(0, inner.height - 4)), new Color(0.12f, 0.43f, 0.49f));
                }
                GUI.Label(new Rect(-state.ScrollX, 0, Math.Max(inner.width, TextWidth(state.Text) + 2), inner.height), state.Text, _inputTextStyle);
                if (focused && (Time.realtimeSinceStartup - _caretEpoch) % 1 < 0.55f)
                    Paint(new Rect(Mathf.RoundToInt(caretX - state.ScrollX), 3, 2, Math.Max(0, inner.height - 6)), ModernSkin.Accent);
            }
            finally { GUI.EndClip(); }
        }
#endif
        public static void FocusNextTextField()
        {
#if IL2CPP
            _focusNextTextField = true;
#endif
        }

        public static string ColorToHex(Color color)
        {
#if IL2CPP
            return Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255).ToString("X2", CultureInfo.InvariantCulture) +
                   Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255).ToString("X2", CultureInfo.InvariantCulture) +
                   Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255).ToString("X2", CultureInfo.InvariantCulture) +
                   Mathf.RoundToInt(Mathf.Clamp01(color.a) * 255).ToString("X2", CultureInfo.InvariantCulture);
#else
            return ColorUtility.ToHtmlStringRGBA(color);
#endif
        }

        public static Font CreateSystemFont(string[] names, int size)
        {
#if IL2CPP
            var font = new Font();
            try
            {
                var factory = typeof(Font).GetMethod("Internal_CreateDynamicFont",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (factory == null) throw new NotSupportedException("Dynamic system font creation is unavailable.");
                factory.Invoke(null, new object[] { font, new Il2CppStringArray(names), size });
                return font;
            }
            catch { UnityEngine.Object.Destroy(font); throw; }
#else
            return Font.CreateDynamicFontFromOSFont(names, size);
#endif
        }
    }
}
