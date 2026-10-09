#if IL2CPP
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConfigurationManager.Utilities
{
    // Own only geometry; Unity GUI supplies the primitive drawing and button interaction.
    internal static class ManagedLayout
    {
        private static readonly Dictionary<string, LayoutNode> Geometry = new Dictionary<string, LayoutNode>();
        private static readonly Stack<LayoutNode> Groups = new Stack<LayoutNode>();
        private static LayoutNode _root;
        private static Rect _bounds, _last;
        private static Vector2 _clipOrigin;
        private static bool _clipped;
        private static string _identity = "window";
        private static int _sequence;
        internal static string LastKey { get; private set; }
        internal static string Tooltip { get; private set; }
        internal static bool Interactive => !ComboBox.IsOpen && Event.current.type != EventType.Layout;

        internal static void BeginFrame(Rect bounds)
        {
            _bounds = bounds;
            _root = new LayoutNode(LayoutKind.Column, "root") { Gap = 8 };
            Groups.Clear(); Groups.Push(_root);
            _identity = "window"; _sequence = 0; Tooltip = null;
            ImguiCompatibility.BeginInputFrame();
        }

        internal static void EndFrame()
        {
            try
            {
                LayoutTree.Arrange(_root, new LayoutRect(_bounds.x, _bounds.y, _bounds.width, _bounds.height));
                Geometry.Clear(); Save(_root);
            }
            finally
            {
                if (_clipped) { GUI.EndGroup(); _clipped = false; }
                _clipOrigin = Vector2.zero; Groups.Clear();
                ImguiCompatibility.EndInputFrame();
            }
        }

        private static void Save(LayoutNode node)
        {
            Geometry[node.Key] = node;
            foreach (var child in node.Children) Save(child);
        }

        internal static IDisposable Identity(string key)
        {
            var previous = _identity; var sequence = _sequence;
            _identity = key; _sequence = 0;
            return new Scope(() => { _identity = previous; _sequence = sequence; });
        }
        private sealed class Scope : IDisposable
        {
            private readonly Action _end;
            internal Scope(Action end) { _end = end; }
            public void Dispose() { _end(); }
        }
        internal static int Depth => Groups.Count;
        internal static void RestoreDepth(int depth) { while (Groups.Count > depth) Groups.Pop(); }

        private static LayoutNode Add(LayoutKind kind, GUIContent content, GUIStyle style, LayoutOption[] options)
        {
            var key = _identity + "/" + _sequence++;
            LastKey = key;
            var node = new LayoutNode(kind, key);
            node.Apply(options);
            if (kind == LayoutKind.Leaf)
            {
                node.PreferredWidth = style.CalcSize(content).x;
                node.PreferredHeight = style.fixedHeight > 0 ? style.fixedHeight : 30;
                node.Measure = width => style.fixedHeight > 0 ? style.fixedHeight : Math.Max(24, style.CalcHeight(content, Math.Max(1, width)));
                if (node.Width < 0 && style.stretchWidth) node.ExpandWidth = true;
            }
            else if (style != GUIStyle.none)
            {
                node.Left = style.padding.left; node.Right = style.padding.right;
                node.Top = style.padding.top; node.Bottom = style.padding.bottom;
            }
            Groups.Peek().Children.Add(node);
            return node;
        }

        private static Rect Previous(LayoutNode node)
        {
            if (!Geometry.TryGetValue(node.Key, out var old)) return new Rect();
            var r = old.Rect;
            var x = Mathf.RoundToInt(r.X - _clipOrigin.x);
            var y = Mathf.RoundToInt(r.Y - _clipOrigin.y);
            return new Rect(x, y, Mathf.Max(0, Mathf.RoundToInt(r.X + r.Width - _clipOrigin.x) - x),
                Mathf.Max(0, Mathf.RoundToInt(r.Y + r.Height - _clipOrigin.y) - y));
        }
        internal static bool Visible(Rect rect)
        {
            if (rect.width <= 0 || rect.height <= 0) return false;
            return !_clipped || (rect.yMax > 0 && rect.y < _viewport.height && rect.xMax > 0 && rect.x < _viewport.width);
        }
        internal static void Hover(Rect rect, GUIContent content)
        {
            if (Interactive && HitTest(rect))
                Tooltip = content.tooltip;
        }
        internal static bool HitTest(Rect rect) => Visible(rect) && rect.Contains(Event.current.mousePosition) &&
            (!_clipped || new Rect(0, 0, _viewport.width, _viewport.height).Contains(Event.current.mousePosition));

        public static Rect GetRect(GUIContent content, GUIStyle style, params LayoutOption[] options)
        {
            _last = Previous(Add(LayoutKind.Leaf, content, style, options));
            Hover(_last, content);
            return _last;
        }
        public static Rect GetLastRect() => _last;
        public static void Label(string text, params LayoutOption[] options) => Label(new GUIContent(text), GUI.skin.label, options);
        public static void Label(string text, GUIStyle style, params LayoutOption[] options) => Label(new GUIContent(text), style, options);
        public static void Label(GUIContent content, params LayoutOption[] options) => Label(content, GUI.skin.label, options);
        public static void Label(Texture texture, params LayoutOption[] options) => Label(new GUIContent { image = texture }, GUI.skin.label, options);
        public static void Label(GUIContent content, GUIStyle style, params LayoutOption[] options)
        {
            var rect = GetRect(content, style, options);
            if (Visible(rect)) GUI.Label(rect, content, style);
        }
        public static bool Button(string text, params LayoutOption[] options) => Button(new GUIContent(text), GUI.skin.button, options);
        public static bool Button(GUIContent content, params LayoutOption[] options) => Button(content, GUI.skin.button, options);
        public static bool Button(GUIContent content, GUIStyle style, params LayoutOption[] options)
        {
            var rect = GetRect(content, style, options);
            var enabled = GUI.enabled;
            try { GUI.enabled = enabled && Interactive && Visible(rect); return GUI.Button(rect, content, style); }
            finally { GUI.enabled = enabled; }
        }
        public static bool Toggle(bool value, string text, params LayoutOption[] options)
        {
            var rect = GetRect(new GUIContent(text), GUI.skin.toggle, options);
            var enabled = GUI.enabled;
            try { GUI.enabled = enabled && Interactive && Visible(rect); return GUI.Toggle(rect, value, text, GUI.skin.toggle); }
            finally { GUI.enabled = enabled; }
        }
        private static void Begin(LayoutKind kind, GUIStyle style, LayoutOption[] options)
        {
            var node = Add(kind, GUIContent.none, style, options);
            var rect = Previous(node);
            // GUI.Box allocates a passive control ID even when it only paints on Repaint.
            // Call it on every event, including clipped cards, to preserve subsequent button IDs.
            if (style != GUIStyle.none) GUI.Box(rect, GUIContent.none, style);
            Groups.Push(node);
        }
        public static void BeginVertical(params LayoutOption[] options) => Begin(LayoutKind.Column, GUIStyle.none, options);
        public static void BeginVertical(GUIStyle style, params LayoutOption[] options) => Begin(LayoutKind.Column, style, options);
        public static void BeginHorizontal(params LayoutOption[] options) => Begin(LayoutKind.Row, GUIStyle.none, options);
        public static void BeginHorizontal(GUIStyle style, params LayoutOption[] options) => Begin(LayoutKind.Row, style, options);
        public static void EndVertical() => EndHorizontal();
        public static void EndHorizontal()
        {
            if (Groups.Count <= 1) throw new InvalidOperationException("Unbalanced managed layout group.");
            _last = Previous(Groups.Pop());
        }
        public static void Space(float size)
        {
            var row = Groups.Peek().Kind == LayoutKind.Row;
            Add(LayoutKind.Leaf, GUIContent.none, GUIStyle.none, new[] { new LayoutOption(row ? LayoutRule.Width : LayoutRule.Height, size) });
        }
        public static void FlexibleSpace()
        {
            var node = Add(LayoutKind.Leaf, GUIContent.none, GUIStyle.none, null);
            node.PreferredWidth = node.PreferredHeight = 0; node.Measure = null;
            if (Groups.Peek().Kind == LayoutKind.Row) node.ExpandWidth = true; else node.ExpandHeight = true;
        }

        private static Rect _viewport;
        internal static Vector2 BeginScrollView(Vector2 scroll)
        {
            var node = Add(LayoutKind.Scroll, GUIContent.none, GUIStyle.none, null);
            node.ExpandHeight = true;
            var rect = Previous(node);
            var contentHeight = Geometry.TryGetValue(node.Key, out var old) ? old.ContentHeight : 0;
            scroll = ImguiCompatibility.UpdateVerticalScroll(rect, scroll, contentHeight);
            node.ScrollY = scroll.y;
            // The cached geometry must follow the wheel immediately, before the next measurement event.
            if (old != null) Translate(old, old.ScrollY - scroll.y);
            Groups.Push(node);
            _viewport = new Rect(rect.x, rect.y, Math.Max(0, rect.width - 16), rect.height);
            GUI.BeginGroup(_viewport);
            _clipOrigin = _viewport.position; _clipped = true;
            return scroll;
        }
        private static void Translate(LayoutNode node, float dy)
        {
            foreach (var child in node.Children) { child.Rect.Y += dy; Translate(child, dy); }
            if (node.Kind == LayoutKind.Scroll) node.ScrollY -= dy;
        }
        internal static void EndScrollView()
        {
            try { EndVertical(); }
            finally { if (_clipped) GUI.EndGroup(); _clipped = false; _clipOrigin = Vector2.zero; }
        }
    }
}
#endif
