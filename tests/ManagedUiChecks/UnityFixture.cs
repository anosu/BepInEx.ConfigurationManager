// Deterministic GUI primitive fixture. It models coordinates/events, not Unity's native runtime.
namespace UnityEngine
{
    public enum EventType { Layout, Repaint, Used, MouseDown, MouseUp, MouseDrag, ScrollWheel, KeyDown }
    public enum FocusType { Passive, Keyboard }
    public enum TextAnchor { MiddleLeft, UpperLeft, MiddleCenter }
    public enum FontStyle { Normal }
    public enum TextClipping { Clip }
    public enum TextureFormat { RGBA32, ARGB32 }
    public enum KeyCode { None, A, C, X, V, Y, Z, Backspace, Delete, LeftArrow, RightArrow, Home, End, Escape, Return, KeypadEnter, Tab, Mouse0 }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2();
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
        public float xMax => x + width;
        public float yMax => y + height;
        public Vector2 position => new Vector2(x, y);
        public bool Contains(Vector2 point) => point.x >= x && point.x < xMax && point.y >= y && point.y < yMax;
    }
    public class Event
    {
        public static Event current = new Event();
        public EventType type;
        public Vector2 mousePosition, delta;
        public int button, clickCount;
        public bool shift, control, command;
        public char character;
        public KeyCode keyCode;
        public EventType GetTypeForControl(int id) => type;
        public void Use() { type = EventType.Used; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color Lerp(Color a, Color b, float t) => new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        public static bool operator ==(Color a, Color b) => a.Equals(b);
        public static bool operator !=(Color a, Color b) => !a.Equals(b);
        public override bool Equals(object obj) => obj is Color c && r == c.r && g == c.g && b == c.b && a == c.a;
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
    }
    public class Object
    {
        public static void Destroy(Object value) { }
        public static Object Instantiate(Object value) => new GUISkin();
        public T Cast<T>() where T : Object => (T)this;
        public static T[] FindObjectsOfType<T>() => Array.Empty<T>();
    }
    public class Texture : Object { }
    public class Texture2D : Texture
    {
        public static Texture2D whiteTexture = new Texture2D();
        public Color Pixel;
        public int width = 100, height = 20;
        public Texture2D() { }
        public Texture2D(int width, int height, TextureFormat format, bool mipmaps) { }
        public void SetPixel(int x, int y, Color color) { Pixel = color; }
        public void Apply(bool updateMipmaps = true) { }
        public Color GetPixel(int x, int y) => Pixel;
    }
    public class Font : Object { }
    public class RectOffset { public int left, right, top, bottom; }
    public class GUIContent
    {
        public string text, tooltip;
        public Texture image;
        public GUIContent() { }
        public GUIContent(string text) { this.text = text; }
        public GUIContent(string text, string tooltip) { this.text = text; this.tooltip = tooltip; }
        public GUIContent(string text, Texture image, string tooltip) { this.text = text; this.image = image; this.tooltip = tooltip; }
        public static GUIContent none => new GUIContent("");
    }
    public class GUIStyle
    {
        public IntPtr m_Ptr;
        public static IntPtr Internal_Copy(GUIStyle target, GUIStyle source)
        {
            target.padding = source.padding; target.fixedHeight = source.fixedHeight; target.fixedWidth = source.fixedWidth;
            target.wordWrap = source.wordWrap; target.stretchWidth = source.stretchWidth; target.fontSize = source.fontSize;
            return IntPtr.Zero;
        }
        public static GUIStyle none = new GUIStyle();
        public RectOffset padding = new RectOffset();
        public RectOffset margin, border, overflow;
        public int fontSize;
        public Font font;
        public FontStyle fontStyle;
        public TextClipping clipping;
        public Vector2 contentOffset;
        public GUIStyleState normal = new GUIStyleState(), hover = new GUIStyleState(), active = new GUIStyleState(), focused = new GUIStyleState(),
            onNormal = new GUIStyleState(), onHover = new GUIStyleState(), onActive = new GUIStyleState(), onFocused = new GUIStyleState();
        public float fixedHeight, fixedWidth;
        public bool stretchWidth, stretchHeight, wordWrap;
        public TextAnchor alignment;
        public Vector2 CalcSize(GUIContent content) => new Vector2(fixedWidth > 0 ? fixedWidth : (content.text?.Length ?? 0) * 8 + padding.left + padding.right, fixedHeight > 0 ? fixedHeight : 24);
        public float CalcHeight(GUIContent content, float width) => wordWrap ? Math.Max(24, (float)Math.Ceiling(CalcSize(content).x / Math.Max(1, width)) * 24) : 24;
    }
    public class GUIStyleState { public Texture2D background; public Color textColor; }
    public class GUISkin : Object
    {
        public GUIStyle window = new GUIStyle(), scrollView = new GUIStyle();
        public GUIStyle label = new GUIStyle { wordWrap = true }, box = new GUIStyle(), button = new GUIStyle { fixedHeight = 30 },
            toggle = new GUIStyle { fixedHeight = 30 }, textField = new GUIStyle { fixedHeight = 30, stretchWidth = true, padding = new RectOffset { left = 10, right = 10 } },
            horizontalSlider = new GUIStyle { fixedHeight = 8 }, horizontalSliderThumb = new GUIStyle(), verticalSlider = new GUIStyle(), verticalSliderThumb = new GUIStyle();
    }
    public static class GUI
    {
        public static bool enabled = true, changed;
        public static Color color = Color.white;
        public static GUISkin skin = new GUISkin();
        public static readonly List<string> PaintedText = new List<string>();
        public static readonly List<(Rect Rect, Color Color)> PaintedRects = new List<(Rect, Color)>();
        private static readonly Stack<Vector2> Origins = new Stack<Vector2>();
        public static Vector2 Origin;
        public static int ClipDepth => Origins.Count;
        public static int ControlCalls;
        public static void BeginGroup(Rect rect)
        {
            GUIUtility.GetControlID(101, FocusType.Passive, rect);
            BeginClip(rect, Vector2.zero, Vector2.zero, false);
        }
        public static void BeginClip(Rect rect, Vector2 offset, Vector2 renderOffset, bool reset)
        {
            Origins.Push(Origin);
            var dx = rect.x + offset.x; var dy = rect.y + offset.y;
            Origin = new Vector2(Origin.x + dx, Origin.y + dy);
            Event.current.mousePosition = new Vector2(Event.current.mousePosition.x - dx, Event.current.mousePosition.y - dy);
        }
        public static void EndGroup() => EndClip();
        public static void EndClip()
        {
            var previous = Origins.Pop();
            Event.current.mousePosition = new Vector2(Event.current.mousePosition.x + Origin.x - previous.x, Event.current.mousePosition.y + Origin.y - previous.y);
            Origin = previous;
        }
        public static void Box(Rect rect, GUIContent content, GUIStyle style) { GUIUtility.GetControlID(102, FocusType.Passive, rect); }
        public static void Label(Rect rect, GUIContent content, GUIStyle style) { if (Event.current.type == EventType.Repaint) PaintedText.Add(content.text ?? ""); }
        public static void Label(Rect rect, string text, GUIStyle style) => Label(rect, new GUIContent(text), style);
        public static bool Button(Rect rect, GUIContent content, GUIStyle style)
        {
            GUIUtility.GetControlID(103, FocusType.Passive, rect);
            if (enabled && Event.current.type == EventType.MouseUp && rect.Contains(Event.current.mousePosition)) { Event.current.Use(); return true; }
            return false;
        }
        public static bool Toggle(Rect rect, bool value, string text, GUIStyle style) => Button(rect, new GUIContent(text), style) ? !value : value;
        public static void DrawTexture(Rect rect, Texture texture) => PaintedRects.Add((rect, color));
    }
    public static class GUIUtility
    {
        public static int hotControl, keyboardControl;
        public static string systemCopyBuffer;
        public static int GetControlID(int hint, FocusType focus, Rect rect) => HashCode.Combine(hint, GUI.ControlCalls++);
        public static Vector2 GUIToScreenPoint(Vector2 point) => new Vector2(point.x + GUI.Origin.x, point.y + GUI.Origin.y);
        public static Vector2 ScreenToGUIPoint(Vector2 point) => new Vector2(point.x - GUI.Origin.x, point.y - GUI.Origin.y);
    }
    public static class Time { public static float realtimeSinceStartup; }
    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Abs(float value) => Math.Abs(value);
        public static float Clamp01(float value) => Math.Clamp(value, 0, 1);
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static float InverseLerp(float a, float b, float value) => Clamp01((value - a) / (b - a));
        public static int RoundToInt(float value) => (int)Math.Round(value);
        public static float Round(float value) => (float)Math.Round(value);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
    }
}
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    public class Il2CppStringArray { public Il2CppStringArray(string[] names) { } }
}
namespace ConfigurationManager.Utilities
{
#if !SETTING_CHECKS
    internal static class FixtureExtensions
    {
        internal static UnityEngine.GUIStyle CreateCopy(this UnityEngine.GUIStyle style) => new UnityEngine.GUIStyle
        { padding = style.padding, fixedHeight = style.fixedHeight, fixedWidth = style.fixedWidth, wordWrap = style.wordWrap, stretchWidth = style.stretchWidth, fontSize = style.fontSize };
    }
#endif
}
namespace ConfigurationManager
{
#if !SETTING_CHECKS
    internal class ConfigurationManager
    {
        internal static FixtureLog Logger = new FixtureLog();
        internal class FixtureLog
        {
            internal void LogError(object value) { }
            internal void LogWarning(object value) { }
            internal void LogInfo(object value) { }
        }
    }
#endif
}
namespace UnityEngine.EventSystems
{
    public class EventSystem : UnityEngine.Object
    {
        public static readonly List<EventSystem> Systems = new List<EventSystem>();
        public static EventSystem current => Systems.FirstOrDefault(system => system.enabled);
        public bool enabled;
        public int ProcessCount;
        protected void Update() { ProcessCount++; }
        public EventSystem(bool enabled) { this.enabled = enabled; Systems.Add(this); }
    }
}
