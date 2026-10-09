using ConfigurationManager.Utilities;
using UnityEngine;

var expanded = true;
var chinese = false;
var offset = Vector2.zero;
var text = "12";
Rect first = default, field = default;
var count = 80;

var collapse = new PluginCollapseState(true);
collapse.Set("plugin.one", false);
Assert(!collapse.Get("plugin.one") && collapse.Get("plugin.two"), "Plugins sharing a display name must have independent GUID states");
// Rebuilding a filtered list only queries the currently visible GUIDs; omitted entries keep their state.
Assert(collapse.Get("plugin.two") && !collapse.Get("plugin.one"), "Filtering discarded a hidden plugin's expansion state");
collapse.Reset(false);
Assert(!collapse.Get("plugin.one") && !collapse.Get("plugin.two") && !collapse.Get("new.plugin"), "Expand All failed to reset overrides/new plugins");
collapse.Set("plugin.two", true);
collapse.Reset(true);
Assert(collapse.Get("plugin.one") && collapse.Get("plugin.two"), "Collapse All retained stale overrides");
Console.WriteLine("PASS: plugin GUID expansion identity, filtering retention and Expand All / Collapse All resets.");

void Frame(EventType type, Vector2 mouse = default, Vector2 wheel = default, KeyCode key = KeyCode.None, char character = '\0', bool control = false, bool fail = false)
{
    Event.current = new Event { type = type, mousePosition = mouse, delta = wheel, keyCode = key, character = character, control = control };
    GUI.ControlCalls = 0;
    ManagedLayout.BeginFrame(new Rect(18, 48, 700, 450));
    try
    {
        ManagedLayout.BeginHorizontal();
        ManagedLayout.Label(chinese ? "插件设置" : "Plugin settings");
        text = ImguiCompatibility.TextField(text, ImguiCompatibility.ExpandWidth(true));
        field = ManagedLayout.GetLastRect();
        ManagedLayout.EndHorizontal();
        offset = ManagedLayout.BeginScrollView(offset);
        try
        {
            for (var i = 0; i < count; i++)
            {
                using (ManagedLayout.Identity("plugin:" + i))
                {
                    var depth = ManagedLayout.Depth;
                    try
                    {
                        ManagedLayout.BeginVertical(GUI.skin.box);
                        ManagedLayout.Label(new GUIContent(chinese ? "这是较长的中文插件名称" : "Long plugin name", "Description"));
                        if (i == 0) first = ManagedLayout.GetLastRect();
                        if (expanded)
                        {
                            ManagedLayout.BeginHorizontal();
                            ManagedLayout.Label("Setting name", ImguiCompatibility.Width(220));
                            ManagedLayout.Button("Value", ImguiCompatibility.ExpandWidth(true));
                            ManagedLayout.Button(chinese ? "重置" : "Reset", ImguiCompatibility.ExpandWidth(false));
                            ManagedLayout.EndHorizontal();
                        }
                        if (fail && i == 3) throw new InvalidOperationException("Injected drawer failure");
                        ManagedLayout.EndVertical();
                    }
                    catch (InvalidOperationException) when (fail) { }
                    finally { ManagedLayout.RestoreDepth(depth); }
                }
            }
        }
        finally { ManagedLayout.EndScrollView(); }
    }
    finally { ManagedLayout.EndFrame(); }
    Assert(GUI.ClipDepth == 0, "Clip leaked after event/failure");
    Assert(GUI.Origin.x == 0 && GUI.Origin.y == 0, "Coordinate origin leaked");
}

Frame(EventType.Layout);
var layoutControls = GUI.ControlCalls;
Frame(EventType.Repaint);
Assert(GUI.ControlCalls == layoutControls, "Layout/Repaint allocate different primitive control IDs");
var viewportY = field.yMax + 8;
Frame(EventType.ScrollWheel, new Vector2(200, viewportY + 50), new Vector2(0, 3));
Assert(offset.y == 84, "Wheel was not applied");
var scrolledY = first.y;
Frame(EventType.Layout);
Frame(EventType.Repaint);
Assert(offset.y == 84 && first.y == scrolledY, "Scroll position/content changed on layout or repaint");
Assert(GUI.ControlCalls == layoutControls, "Clipping changes primitive control order after scrolling");
chinese = true;
Frame(EventType.Layout);
Frame(EventType.Repaint);
Assert(first.x >= 0 && first.x + first.width <= 684, "Localized content overflows viewport horizontally");
Frame(EventType.Layout, fail: true);
Frame(EventType.Repaint, fail: true);
Frame(EventType.Repaint);

offset.y = 100000;
expanded = false; count = 1;
Frame(EventType.Layout);
Frame(EventType.Layout);
Frame(EventType.Repaint);
Assert(offset.y == 0, "Collapse/filter did not clamp scroll");

var point = new Vector2(field.x + 20, field.y + 10);
Frame(EventType.MouseDown, point);
Frame(EventType.MouseUp, point);
Frame(EventType.KeyDown, key: KeyCode.A, control: true);
Frame(EventType.KeyDown, character: '-');
Assert(text == "-", "Replace selected field text");
text = "12"; // Config converter rejects a partial numeric draft.
Frame(EventType.Layout);
Assert(text == "-", "Partial numeric draft discarded during measurement");
Frame(EventType.Repaint);
Assert(GUI.PaintedText.Contains("-") && !GUI.PaintedText.Contains("-|"), "Caret glyph inserted into text");
Assert(GUI.PaintedRects.Any(item => item.Rect.width == 2), "Caret was not painted separately");
Frame(EventType.KeyDown, key: KeyCode.Z, control: true);
Assert(text == "12", "Undo did not restore draft");
Frame(EventType.KeyDown, key: KeyCode.Y, control: true);
Assert(text == "-", "Redo did not restore draft");
Frame(EventType.MouseDown, new Vector2(5, 5));
text = "12";
Frame(EventType.Repaint);
Assert(text == "12", "Blur did not restore accepted config value");

var hover = new HoverState();
hover.Update("Description", false, 0);
Assert(!hover.Visible(0.1f) && hover.Visible(0.5f), "Tooltip delay");
hover.Update(null, false, 0.6f);
Assert(!hover.Visible(10), "Tooltip persists after pointer leaves");
hover.Update("Description", false, 11);
hover.Update("Description", true, 12);
Assert(!hover.Visible(20), "Popup fails to suppress tooltip");

foreach (var width in new[] { 320, 480, 700 })
{
    var root = new LayoutNode(LayoutKind.Scroll, "scroll");
    for (var i = 0; i < 50; i++)
    {
        var row = new LayoutNode(LayoutKind.Row, "row:" + i);
        row.Children.Add(new LayoutNode(LayoutKind.Leaf, "name:" + i) { Width = 300, PreferredHeight = 30 });
        row.Children.Add(new LayoutNode(LayoutKind.Leaf, "value:" + i) { ExpandWidth = true, MinWidth = 120, PreferredHeight = 30 });
        row.Children.Add(new LayoutNode(LayoutKind.Leaf, "reset:" + i) { PreferredWidth = chinese ? 64 : 100, PreferredHeight = 30 });
        root.Children.Add(row);
    }
    LayoutTree.Arrange(root, new LayoutRect(0, 0, width, 400));
    foreach (var row in root.Children)
        foreach (var child in row.Children)
            Assert(child.Rect.X >= 0 && child.Rect.X + child.Rect.Width <= width - 16 + 0.001f, "Narrow/localized row exceeds vertical viewport");
}

Event.current = new Event { type = EventType.Repaint };
ManagedLayout.BeginFrame(new Rect(0, 0, 400, 300));
try
{
    ManagedLayout.BeginScrollView(Vector2.zero);
    ManagedLayout.BeginVertical();
    throw new InvalidOperationException("Injected frame abort");
}
catch (InvalidOperationException ex) when (ex.Message == "Injected frame abort") { }
finally { ManagedLayout.EndFrame(); }
Assert(GUI.ClipDepth == 0, "Frame abort leaked a clip when EndScrollView was skipped");

var popup = new ComboBox(new Rect(100, 100, 200, 30), new GUIContent("Choice"),
    Enumerable.Range(0, 40).Select(i => new GUIContent("Option " + i)).ToArray(), GUI.skin.button, 600);
Event.current = new Event { type = EventType.MouseUp, mousePosition = new Vector2(120, 110) };
popup.Show(_ => throw new InvalidOperationException("Injected selection failure"));
Assert(ComboBox.IsOpen && GUI.enabled, "Popup did not open or leaked disabled state");
Event.current = new Event { type = EventType.MouseUp, mousePosition = new Vector2(120, 140) };
GUI.enabled = false; GUI.color = new Color(0.3f, 0.4f, 0.5f);
try { ComboBox.CurrentDropdownDrawer!(); throw new Exception("Selection failure was not injected"); }
catch (InvalidOperationException ex) when (ex.Message == "Injected selection failure") { }
Assert(!GUI.enabled && GUI.color.r == 0.3f && GUI.ClipDepth == 0, "Popup failed to restore state/clip after callback exception");
Assert(!ComboBox.IsOpen, "Selected dropdown stayed open");
GUI.enabled = true; GUI.color = Color.white;
Event.current = new Event { type = EventType.MouseUp, mousePosition = new Vector2(120, 110) };
popup.Show(_ => { });
Event.current = new Event { type = EventType.MouseDown, mousePosition = new Vector2(20, 20) };
ComboBox.CurrentDropdownDrawer!();
Assert(!ComboBox.IsOpen && Event.current.type == EventType.Used, "Outside click did not dismiss dropdown and consume event");
Console.WriteLine("PASS: complete layout, localized bounds, wheel stability, collapse/filter, injected exceptions and clip cleanup.");
Console.WriteLine("PASS: focus, numeric drafts, selection, undo/redo, blur, separate caret painting and tooltip lifecycle.");
Console.WriteLine("PASS: narrow window bounds and dropdown state/clip restoration on selection failure and outside dismissal.");

// A config entry autosaves on Set. Typing must not call its commit delegate until confirmation.
ImguiCompatibility.ClearInput();
var stored = "1";
var saves = 0;
Rect editRect = default;
void EditFrame(EventType type, Vector2 mouse = default, KeyCode key = KeyCode.None, char character = '\0', bool control = false)
{
    Event.current = new Event { type = type, mousePosition = mouse, keyCode = key, character = character, control = control };
    GUI.ControlCalls = 0;
    ManagedLayout.BeginFrame(new Rect(0, 0, 400, 50));
    try
    {
        ImguiCompatibility.EditValue(stored, value => { if (int.TryParse(value, out _)) { stored = value; saves++; } }, ImguiCompatibility.ExpandWidth(true));
        editRect = ManagedLayout.GetLastRect();
    }
    finally { ManagedLayout.EndFrame(); }
}
EditFrame(EventType.Layout);
EditFrame(EventType.MouseDown, new Vector2(20, 15));
EditFrame(EventType.MouseUp, new Vector2(20, 15));
EditFrame(EventType.KeyDown, key: KeyCode.End);
EditFrame(EventType.KeyDown, character: '2');
EditFrame(EventType.KeyDown, character: '3');
Assert(saves == 0 && stored == "1", "Typing saved configuration before commit");
EditFrame(EventType.KeyDown, key: KeyCode.Return);
Assert(saves == 1 && stored == "123", "Enter must save the completed edit exactly once");
EditFrame(EventType.Layout);
EditFrame(EventType.Repaint);
Assert(saves == 1, "Measurement/repaint repeated a save");
EditFrame(EventType.MouseDown, new Vector2(20, 15));
EditFrame(EventType.MouseUp, new Vector2(20, 15));
EditFrame(EventType.KeyDown, key: KeyCode.End);
EditFrame(EventType.KeyDown, character: '4');
EditFrame(EventType.MouseDown, new Vector2(500, 100));
Assert(saves == 2 && stored == "1234", "Blur must commit once");
EditFrame(EventType.MouseDown, new Vector2(20, 15));
EditFrame(EventType.MouseUp, new Vector2(20, 15));
EditFrame(EventType.KeyDown, key: KeyCode.End);
EditFrame(EventType.KeyDown, character: '5');
EditFrame(EventType.KeyDown, key: KeyCode.Escape);
Assert(saves == 2 && stored == "1234", "Escape must discard the draft");
EditFrame(EventType.MouseDown, new Vector2(20, 15));
EditFrame(EventType.MouseUp, new Vector2(20, 15));
EditFrame(EventType.KeyDown, key: KeyCode.End);
EditFrame(EventType.KeyDown, character: '6');
ImguiCompatibility.ClearInput();
Assert(saves == 3 && stored == "12346", "Closing the window must commit the active draft once");
EditFrame(EventType.Layout);
EditFrame(EventType.MouseDown, new Vector2(20, 15));
EditFrame(EventType.MouseUp, new Vector2(20, 15));
EditFrame(EventType.KeyDown, key: KeyCode.A, control: true);
EditFrame(EventType.KeyDown, character: '-');
EditFrame(EventType.KeyDown, key: KeyCode.Return);
EditFrame(EventType.Repaint);
Assert(saves == 3 && stored == "12346", "Invalid numeric draft must not overwrite accepted configuration");

var rangeRow = new LayoutNode(LayoutKind.Row, "range");
rangeRow.Children.Add(new LayoutNode(LayoutKind.Leaf, "slider") { ExpandWidth = true, PreferredHeight = 8 });
rangeRow.Children.Add(new LayoutNode(LayoutKind.Leaf, "number") { Width = 88, PreferredHeight = 36 });
rangeRow.Children.Add(new LayoutNode(LayoutKind.Leaf, "reset") { Width = 84, PreferredHeight = 36 });
LayoutTree.Arrange(rangeRow, new LayoutRect(0, 0, 450, 36));
Assert(rangeRow.Children[0].Rect.Y + 4 == 18, "Slider is not vertically centered with the input/reset row");
ImguiCompatibility.ClearInput();
foreach (var width in new[] { 350, 700 })
{
    Rect inputRect = default, resetRect = default;
    void SettingRow(EventType type)
    {
        Event.current = new Event { type = type };
        ManagedLayout.BeginFrame(new Rect(0, 0, width, 100));
        try
        {
            ManagedLayout.BeginHorizontal();
            ManagedLayout.Label("A setting", ImguiCompatibility.Width(width / 2.8f));
            ManagedLayout.BeginHorizontal(ImguiCompatibility.ExpandWidth(true));
            ImguiCompatibility.HorizontalSlider(0.5f, 0, 1, ImguiCompatibility.ExpandWidth(true));
            ImguiCompatibility.EditValue("0.5", _ => { }, ImguiCompatibility.Width(88));
            inputRect = ManagedLayout.GetLastRect();
            ManagedLayout.EndHorizontal();
            ImguiCompatibility.Space(5);
            ManagedLayout.Button("Reset", ImguiCompatibility.Width(84));
            resetRect = ManagedLayout.GetLastRect();
            ManagedLayout.EndHorizontal();
        }
        finally { ManagedLayout.EndFrame(); }
    }
    SettingRow(EventType.Layout); SettingRow(EventType.Repaint);
    Assert(resetRect.width == 84 && inputRect.xMax < resetRect.x, "Numeric input shrinks/overlaps the standard Reset button");
}
Console.WriteLine("PASS: configuration commits once on Enter and range slider is centered.");

var gate = new HotkeyGate();
var open = false;
if (gate.Press(100)) open = !open; // Update observes F1.
if (gate.Press(100)) open = !open; // IMGUI observes the same F1.
if (gate.Press(101)) open = !open; // Repeated KeyDown while held.
Assert(open, "F1 opened and then immediately closed the window");
gate.Release();
if (gate.Press(110)) open = !open;
Assert(!open, "Second F1 press did not close the window");
var gameUi = new UnityEngine.EventSystems.EventSystem(true);
var secondaryUi = new UnityEngine.EventSystems.EventSystem(true);
var alreadyDisabled = new UnityEngine.EventSystems.EventSystem(false);
var preparedStages = new List<string>();
var warmup = new WindowWarmup(
    () => { ModalInput.Prepare(); preparedStages.Add("input"); },
    () => preparedStages.Add("skin"),
    () => preparedStages.Add("font"));
warmup.Advance(10, true);
Assert(preparedStages.Count == 0, "Warmup added preparation work to a visible window frame");
warmup.Advance(11, false); warmup.Advance(11, false);
Assert(preparedStages.SequenceEqual(new[] { "input" }), "Warmup ran multiple expensive stages in one frame");
Assert(ModalInput.AllowGameInput(), "Preparing input hooks activated modality while the window was hidden");
var uiUpdateBeforeOpening = typeof(UnityEngine.EventSystems.EventSystem).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
HarmonyLib.Harmony.Invoke(uiUpdateBeforeOpening, gameUi);
Assert(gameUi.ProcessCount == 1, "Passive warmup blocked game input");
gameUi.ProcessCount = 0;
warmup.Advance(12, false); warmup.Advance(13, false); warmup.Advance(14, false);
Assert(preparedStages.SequenceEqual(new[] { "input", "skin", "font" }), "Warmup skipped or repeated a stage");
var failedWarmup = new WindowWarmup(() => throw new InvalidOperationException("Injected warmup failure"), () => preparedStages.Add("recovered"));
try { failedWarmup.Advance(20, false); }
catch (InvalidOperationException ex) when (ex.Message == "Injected warmup failure") { }
failedWarmup.Advance(20, false); failedWarmup.Advance(21, false);
Assert(preparedStages.Last() == "recovered", "Failed warmup blocked subsequent preparation");
var coldPatchCount = HarmonyLib.Harmony.PatchCount;
ModalInput.SetActive(true);
Assert(HarmonyLib.Harmony.PatchCount == coldPatchCount, "First modal activation installed input hooks on the opening path instead of preparing them while hidden");
Assert(UnityEngine.EventSystems.EventSystem.current == gameUi,
    "Modal opening removed EventSystem.current used by the game's touch queries");
Assert(gameUi.enabled && secondaryUi.enabled && !alreadyDisabled.enabled, "Modal opening changed EventSystem component states");
var uiUpdate = typeof(UnityEngine.EventSystems.EventSystem).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
var gameInput = new Project.InputService();
var inputUpdate = typeof(Project.InputService).GetMethod("OnUpdate")!;
HarmonyLib.Harmony.Invoke(uiUpdate, gameUi);
HarmonyLib.Harmony.Invoke(uiUpdate, secondaryUi);
HarmonyLib.Harmony.Invoke(inputUpdate, gameInput, 0.016f);
Assert(gameInput.DispatchCount == 1, "General-purpose manager intercepted a game-specific InputService");
Assert(HarmonyLib.Harmony.Prefixes.Count == 1 && HarmonyLib.Harmony.Prefixes.ContainsKey(uiUpdate),
    "Modal guard patched a target outside Unity EventSystem.Update");
Assert(gameUi.ProcessCount == 0 && secondaryUi.ProcessCount == 0,
    "Modal window let uGUI dispatch clicks");
Assert(ModalInput.AllowGameInput() == false, "Modal dispatch guard became inactive");
var installed = HarmonyLib.Harmony.PatchCount;
ModalInput.SetActive(true); // Dragging/repainting keeps the same modal state.
Assert(installed == HarmonyLib.Harmony.PatchCount, "Repaint duplicated native input hooks");
var sceneUi = new UnityEngine.EventSystems.EventSystem(true);
HarmonyLib.Harmony.Invoke(uiUpdate, sceneUi);
Assert(sceneUi.enabled && sceneUi.ProcessCount == 0, "Scene-created EventSystem escaped the modal guard");
ModalInput.SetActive(false);
HarmonyLib.Harmony.Invoke(uiUpdate, gameUi);
HarmonyLib.Harmony.Invoke(inputUpdate, gameInput, 0.016f);
Assert(gameUi.ProcessCount == 1 && gameInput.DispatchCount == 2, "Closing did not resume uGUI or changed independent input dispatch");
Assert(gameUi.enabled && secondaryUi.enabled && !alreadyDisabled.enabled && sceneUi.enabled, "Closing changed game UI enable states");
ModalInput.SetActive(true); ModalInput.SetActive(false);
Assert(installed == HarmonyLib.Harmony.PatchCount && ModalInput.AllowGameInput(), "Reopening/destruction left stale or duplicate input guards");
Console.WriteLine("PASS: hidden warmup runs one stage per frame, preserves input and removes hook installation from first modal activation.");

var original = new GUISkin();
original.button.fixedWidth = 20;
original.button.contentOffset = new Vector2(80, 0);
var skin = ModernSkin.Create(original);
Assert(skin.toggle.CalcSize(new GUIContent("Advanced")).x >= 88 && skin.toggle.contentOffset.x == 0,
    "Inherited icon-button sizing/offset clips the compact English header");
double Luminance(Color color)
{
    double Linear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    return 0.2126 * Linear(color.r) + 0.7152 * Linear(color.g) + 0.0722 * Linear(color.b);
}
foreach (var state in new[] { skin.toggle.onNormal, skin.toggle.onHover, skin.toggle.onActive, skin.toggle.onFocused })
{
    var contrast = (Luminance(state.textColor) + 0.05) / (Luminance(state.background.Pixel) + 0.05);
    Assert(contrast >= 7, "Enabled toggle text/background contrast is too low");
}
Console.WriteLine("PASS: F1 press/repeat/release, persistent modality, game UI restoration and selected-state contrast.");

ImguiCompatibility.ClearInput();
EditFrame(EventType.Layout); EditFrame(EventType.Repaint);
EditFrame(EventType.MouseDown, new Vector2(20, 15));
Assert(GUIUtility.hotControl != 0 && GUIUtility.keyboardControl != 0, "Editor did not acquire input controls");
ImguiCompatibility.ClearInput();
Assert(GUIUtility.hotControl == 0 && GUIUtility.keyboardControl == 0, "Close leaked editor mouse/keyboard capture");
EditFrame(EventType.Layout); EditFrame(EventType.Repaint);
EditFrame(EventType.MouseDown, new Vector2(20, 15));
GUIUtility.hotControl = 12345; GUIUtility.keyboardControl = 54321;
ImguiCompatibility.ClearInput();
Assert(GUIUtility.hotControl == 12345 && GUIUtility.keyboardControl == 54321, "Cleanup released another GUI owner's controls");
GUIUtility.hotControl = GUIUtility.keyboardControl = 0;
Console.WriteLine("PASS: closing releases only editor-owned mouse and keyboard controls.");

// Exercise production appearance preparation, not substitute warmup callbacks.
var appearance = new ConfigurationManager.ConfigurationManager();
var gameSkin = GUI.skin;
gameSkin.font = new Font();
var gameFont = gameSkin.font;
object AppearanceCall(ConfigurationManager.ConfigurationManager instance, string name, params object[] args) =>
    typeof(ConfigurationManager.ConfigurationManager).GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(instance, args);
var initialSkinCopies = UnityEngine.Object.InstantiateCalls;
var initialFontCreations = Font.CreateCalls;
try
{
    ConfigurationManager.Localization.Language = ConfigurationManager.Localization.English;
    AppearanceCall(appearance, "PrepareWindowSkin", gameSkin);
    var englishFont = (Font)AppearanceCall(appearance, "PrepareWindowFont")!;
    AppearanceCall(appearance, "PrepareWindowSkin", gameSkin);
    Assert(ReferenceEquals(englishFont, AppearanceCall(appearance, "PrepareWindowFont")), "Repeated preparation recreated the English font");
    Assert(ReferenceEquals(GUI.skin, gameSkin) && ReferenceEquals(gameSkin.font, gameFont), "Hidden preparation modified the active game's GUI skin/font");
    Assert(UnityEngine.Object.InstantiateCalls == initialSkinCopies + 1 && Font.CreateCalls == initialFontCreations + 1,
        "Repeated preparation allocated duplicate skin/font resources");
    AppearanceCall(appearance, "ApplyWindowAppearance", gameSkin);
    var preparedSkin = GUI.skin;
    Assert(!ReferenceEquals(preparedSkin, gameSkin) && ReferenceEquals(preparedSkin.font, englishFont), "Opening did not apply prepared appearance resources");
    ConfigurationManager.Localization.Language = ConfigurationManager.Localization.SimplifiedChinese;
    var chineseFont = (Font)AppearanceCall(appearance, "PrepareWindowFont")!;
    Assert(chineseFont.Size == 17 && chineseFont.Names.Contains("Microsoft YaHei"), "Chinese font preparation used the wrong language/size");
    AppearanceCall(appearance, "ApplyWindowAppearance", gameSkin);
    Assert(ReferenceEquals(GUI.skin, preparedSkin) && ReferenceEquals(preparedSkin.font, chineseFont), "Language switching replaced the skin or omitted the prepared font");
    ConfigurationManager.Localization.Language = ConfigurationManager.Localization.English;
    AppearanceCall(appearance, "ApplyWindowAppearance", gameSkin);
    Assert(Font.CreateCalls == initialFontCreations + 2 && ReferenceEquals(preparedSkin.font, englishFont), "Switching back recreated a cached font");
    var earlyAppearance = new ConfigurationManager.ConfigurationManager();
    AppearanceCall(earlyAppearance, "ApplyWindowAppearance", gameSkin);
    var earlySkinCopies = UnityEngine.Object.InstantiateCalls; var earlyFontCreations = Font.CreateCalls;
    GUI.skin = gameSkin;
    AppearanceCall(earlyAppearance, "PrepareWindowSkin", gameSkin);
    AppearanceCall(earlyAppearance, "PrepareWindowFont");
    Assert(UnityEngine.Object.InstantiateCalls == earlySkinCopies && Font.CreateCalls == earlyFontCreations,
        "Preparation after early opening recreated existing resources");
    Assert(ReferenceEquals(GUI.skin, gameSkin) && ReferenceEquals(gameSkin.font, gameFont), "Preparation after early opening leaked GUI state");
}
finally { GUI.skin = gameSkin; ConfigurationManager.Localization.Language = ConfigurationManager.Localization.English; }
Console.WriteLine("PASS: production appearance preparation preserves the game skin and reuses skin/fonts across opening, language changes and early fallback.");

void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
