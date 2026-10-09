using System.Globalization;
using ConfigurationManager;
using ConfigurationManager.Utilities;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

var errors = new List<string>();
void Check(bool condition, string message) { if (!condition) errors.Add(message); }

var model = new Model();
var writable = new PropertySettingEntry(model, typeof(Model).GetProperty(nameof(Model.Enabled)), null);
writable.Set(false);
Check(!model.Enabled, "Writable debug property is treated as read-only");
var readOnly = new PropertySettingEntry(model, typeof(Model).GetProperty(nameof(Model.ReadOnly)), null);
Check(readOnly.ReadOnly == true, "Getter-only property is not read-only");
var tagged = new PropertySettingEntry(model, typeof(Model).GetProperty(nameof(Model.Tagged)), null);
Check(tagged.ReadOnly == true, "Explicit ReadOnly attribute was overwritten");

foreach (var value in new[] { 1e-5f, 1e20f })
{
    var text = value.ToString("G", CultureInfo.InvariantCulture).AppendZeroIfFloat(typeof(float));
    Check(float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed == value,
        "Scientific notation was corrupted: " + text);
}

var drawer = new SettingFieldDrawer(new ConfigurationManager.ConfigurationManager());
var stored = 1;
var entry = new TestEntry(typeof(int), () => stored, value => stored = (int)value, 0, 20000000);
void Draw(EventType type, KeyCode key = KeyCode.None, char character = '\0', bool control = false)
{
    Event.current = new Event { type = type, mousePosition = new Vector2(320, 15), keyCode = key, character = character, control = control };
    GUI.ControlCalls = 0;
    ManagedLayout.BeginFrame(new Rect(0, 0, 400, 50));
    try { ManagedLayout.BeginHorizontal(); drawer.DrawSettingValue(entry); ManagedLayout.EndHorizontal(); }
    finally { ManagedLayout.EndFrame(); }
}
Draw(EventType.Layout); Draw(EventType.Repaint); Draw(EventType.MouseDown); Draw(EventType.MouseUp);
Draw(EventType.KeyDown, KeyCode.A, control: true);
foreach (var character in "16777217") Draw(EventType.KeyDown, character: character);
Draw(EventType.KeyDown, KeyCode.Return);
Check(stored == 16777217, "Production range editor changed 16777217 to " + stored);

Check((long)RangeValue.Parse("9007199254740993", typeof(long), long.MinValue, long.MaxValue) == 9007199254740993,
    "Int64 range editing lost precision");
Check((ulong)RangeValue.Parse("18446744073709551615", typeof(ulong), ulong.MinValue, ulong.MaxValue) == ulong.MaxValue,
    "UInt64 endpoint was rounded or overflowed");
Check((decimal)RangeValue.Parse("0.1234567890123456789012345678", typeof(decimal), 0m, 1m) == 0.1234567890123456789012345678m,
    "Decimal range editing lost precision");
Check((double)RangeValue.Parse("1e-100", typeof(double), 0d, 1d) == 1e-100, "Double exponent input lost precision");
Check((int)RangeValue.Parse("25", typeof(int), 0, 20) == 20, "Typed range was not clamped");
Check((int)RangeValue.FromSlider((float)int.MaxValue, typeof(int), 0, int.MaxValue) == int.MaxValue,
    "Float slider endpoint overflowed the integer configuration type");
foreach (var text in new[] { "NaN", "Infinity", "-Infinity" })
{
    try { RangeValue.Parse(text, typeof(double), 0d, 1d); errors.Add("Range accepted nonfinite input: " + text); }
    catch (FormatException) { }
}
Check("2".AppendZero() == "2.0" && "2.5".AppendZero() == "2.5" && "NaN".AppendZero() == "NaN" && ((string)null).AppendZero() == null,
    "Numeric formatting broke integers, special values or null");

var hotkey = new TestEntry(typeof(KeyboardShortcut), () => new KeyboardShortcut(), _ => { }, null, null);
void HotkeyFrame(EventType type, bool visible = true)
{
    Event.current = new Event { type = type, mousePosition = new Vector2(20, 15) }; GUI.ControlCalls = 0;
    ManagedLayout.BeginFrame(new Rect(0, 0, 400, 50)); SettingFieldDrawer.BeginFrame();
    try { if (visible) { ManagedLayout.BeginHorizontal(); drawer.DrawSettingValue(hotkey); ManagedLayout.EndHorizontal(); } }
    finally { SettingFieldDrawer.EndFrame(); ManagedLayout.EndFrame(); }
}
HotkeyFrame(EventType.Layout); HotkeyFrame(EventType.MouseUp);
Check(SettingFieldDrawer.SettingKeyboardShortcut, "Click failed to start production shortcut capture");
HotkeyFrame(EventType.Layout);
Check(SettingFieldDrawer.SettingKeyboardShortcut && GUIUtility.keyboardControl == -1, "Visible shortcut capture was lost");
HotkeyFrame(EventType.Repaint, visible: false);
Check(!SettingFieldDrawer.SettingKeyboardShortcut && GUIUtility.keyboardControl == 0, "Filtered/collapsed shortcut capture blocked F1");

// The actual drawer frame lifecycle must dismiss a dropdown whose owner disappears.
var popup = new ComboBox(new Rect(10, 10, 180, 30), new GUIContent("Choice"),
    new[] { new GUIContent("Option") }, GUI.skin.button, 600);
Event.current = new Event { type = EventType.MouseUp, mousePosition = new Vector2(20, 20) };
SettingFieldDrawer.BeginFrame(); popup.Show(_ => { }); SettingFieldDrawer.EndFrame();
Check(ComboBox.IsOpen, "Visible dropdown was closed by the drawer frame lifecycle");
Event.current = new Event { type = EventType.Repaint };
SettingFieldDrawer.BeginFrame(); SettingFieldDrawer.EndFrame();
Check(!ComboBox.IsOpen && ComboBox.CurrentDropdownDrawer == null,
    "Filtered/collapsed dropdown owner left a stale overlay blocking all controls");
Event.current = new Event { type = EventType.MouseUp, mousePosition = new Vector2(20, 20) };
SettingFieldDrawer.BeginFrame(); popup.Show(_ => { }); SettingFieldDrawer.EndFrame();
Check(ComboBox.IsOpen, "Dropdown could not reopen after its owner disappeared");
Event.current = new Event { type = EventType.Repaint };
GUI.enabled = false;
try { SettingFieldDrawer.BeginFrame(); popup.Show(_ => { }); SettingFieldDrawer.EndFrame(); }
finally { GUI.enabled = true; }
Check(!ComboBox.IsOpen, "Disabled dropdown owner remained interactive");
ComboBox.CurrentDropdownDrawer = () => throw new InvalidOperationException("Injected deferred draw failure");
try { SettingFieldDrawer.DrawCurrentDropdown(); errors.Add("Deferred draw failure was not injected"); }
catch (InvalidOperationException ex) when (ex.Message == "Injected deferred draw failure") { }
Check(ComboBox.CurrentDropdownDrawer == null, "Throwing dropdown retained its deferred callback");
ComboBox.Close();

var broken = new BasePlugin();
var config = new ConfigEntryBase { SettingType = typeof(int), BoxedValue = 1, DefaultValue = 1 };
config.Description.AcceptableValues = new BrokenValues();
broken.Config.Add(config.Definition, config);
IL2CPPChainloader.Instance.Plugins["broken"] = new PluginInfo { Instance = broken, Metadata = new BepInPlugin("broken", "Broken", "1.0") };
var good = new BasePlugin();
var goodConfig = new ConfigEntryBase { SettingType = typeof(bool), BoxedValue = true, DefaultValue = true };
good.Config.Add(goodConfig.Definition, goodConfig);
IL2CPPChainloader.Instance.Plugins["good"] = new PluginInfo { Instance = good, Metadata = new BepInPlugin("good", "Good", "1.0") };
try
{
    SettingSearcher.CollectSettings(out var found, out _, false);
    Check(found.Count() == 1 && found.Single().PluginInfo.GUID == "good", "Healthy plugin was lost after collection failure");
}
catch (Exception) { errors.Add("One plugin exception aborted IL2CPP setting collection"); }

if (errors.Count > 0) throw new Exception(string.Join("\n", errors));
Console.WriteLine("PASS: production range editing, exact numeric types/bounds, scientific notation, read-only metadata, hidden shortcut/dropdown cancellation and isolated IL2CPP collection.");

class Model
{
    public bool Enabled { get; set; } = true;
    public bool ReadOnly => true;
    [System.ComponentModel.ReadOnly(true)] public bool Tagged { get; set; }
}
class BrokenValues : AcceptableValueBase { public int MinValue => throw new InvalidOperationException("Injected metadata failure"); public int MaxValue => 10; }
class TestEntry : SettingEntryBase
{
    readonly Type _type; readonly Func<object> _get; readonly Action<object> _set;
    public TestEntry(Type type, Func<object> get, Action<object> set, object min, object max)
    { _type = type; _get = get; _set = set; AcceptableValueRange = new KeyValuePair<object, object>(min, max); ShowRangeAsPercent = false; }
    public override Type SettingType => _type;
    public override object Get() => _get();
    protected override void SetValue(object value) => _set(value);
}
