using System;
using UnityEngine;

namespace ConfigurationManager.Utilities
{
    internal sealed class ComboBox
    {
        private static ComboBox _active;
        private static bool _activeWasDrawn;
        internal static bool IsOpen => _active != null;
        private readonly GUIContent[] _items;
        private readonly GUIStyle _style;
        private Vector2 _scroll;
        public Rect Rect { get; set; }
        public GUIContent ButtonContent { get; set; }
        public Rect Bounds { get; set; }
        public static Action CurrentDropdownDrawer { get; set; }

        public ComboBox(Rect rect, GUIContent buttonContent, GUIContent[] items, GUIStyle style, float windowYmax)
        {
            Rect = rect; ButtonContent = buttonContent; _items = items; _style = style;
            Bounds = new Rect(0, 0, float.MaxValue, windowYmax);
        }
        internal static void Close() { _active = null; CurrentDropdownDrawer = null; }
        internal static void BeginFrame() { _activeWasDrawn = false; CurrentDropdownDrawer = null; }
        internal static void EndFrame() { if (!_activeWasDrawn) Close(); }

        public void Show(Action<int> select)
        {
            var enabled = GUI.enabled;
#if IL2CPP
            var visible = ManagedLayout.Visible(Rect);
#else
            var visible = Rect.width > 0 && Rect.height > 0;
#endif
            try
            {
                GUI.enabled = enabled && visible && (_active == null || _active == this);
                if (GUI.Button(Rect, ButtonContent, _style))
                {
                    if (_active == this) Close();
                    else { _active = this; _scroll = Vector2.zero; }
                }
            }
            finally { GUI.enabled = enabled; }
            if (_active != this) return;
            if (!enabled || !visible || _items.Length == 0) { Close(); return; }
            _activeWasDrawn = true;
            var anchor = GUIUtility.GUIToScreenPoint(new Vector2(Rect.x, Rect.yMax));
            var width = Rect.width;
            CurrentDropdownDrawer = () => DrawPopup(anchor, width, select);
        }

        private void DrawPopup(Vector2 anchor, float width, Action<int> select)
        {
            if (_active != this) return;
            const float rowHeight = 32;
            var height = Mathf.Min(Mathf.Min(256, _items.Length * rowHeight), Mathf.Max(0, Bounds.height - 64));
            var screenY = anchor.y + height > Bounds.yMax - 12 ? anchor.y - Rect.height - height : anchor.y;
            screenY = Mathf.Max(Bounds.y + 48, Mathf.Min(screenY, Bounds.yMax - height - 12));
            width = Mathf.Min(width, Mathf.Max(0, Bounds.width - 36));
            var screenX = Mathf.Max(Bounds.x + 18, Mathf.Min(anchor.x, Bounds.xMax - width - 18));
            var local = GUIUtility.ScreenToGUIPoint(new Vector2(screenX, screenY));
            var outer = new Rect(local.x, local.y, width, height);
            var evt = Event.current;
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape ||
                evt.type == EventType.MouseDown && !outer.Contains(evt.mousePosition) &&
                !new Rect(GUIUtility.ScreenToGUIPoint(anchor).x, GUIUtility.ScreenToGUIPoint(anchor).y - Rect.height, Rect.width, Rect.height).Contains(evt.mousePosition))
            {
                Close(); evt.Use(); return;
            }
            var enabled = GUI.enabled; var color = GUI.color;
            try
            {
                GUI.enabled = true; GUI.color = Color.white;
                GUI.Box(outer, GUIContent.none, GUI.skin.box);
                _scroll = ImguiCompatibility.BeginFixedScrollView(outer, _scroll,
                    new Rect(0, 0, Mathf.Max(0, outer.width - 16), _items.Length * rowHeight), false, false);
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                    {
                        if (GUI.Button(new Rect(0, i * rowHeight, Mathf.Max(0, outer.width - 16), rowHeight), _items[i], _style))
                        {
                            Close(); select(i); break;
                        }
                    }
                }
                finally { ImguiCompatibility.EndFixedScrollView(); }
            }
            finally { GUI.enabled = enabled; GUI.color = color; }
        }
    }
}
