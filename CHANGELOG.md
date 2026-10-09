# Changelog

## 19.1.8

- Prepare the generic IL2CPP modal hook, skin and active language's system font on separate hidden repaint frames after startup, reducing work on the first-open path.
- Keep preparation on Unity's main thread without changing the active game skin or blocking input while the window is hidden.
- Preserve normal initialization when opening early and continue collecting current plugin settings on every opening.
- Log preparation-stage and initial-settings-collection durations; add regression checks for per-frame scheduling, passive input preparation and first-open hook reuse.
- Preparation distributes initialization cost; native JIT/font glyph work and game-side load still require in-game timing before claiming a specific latency improvement.

## 19.1.7

### Interface and editing

- Add English / Simplified Chinese selection with a saved preference and system font fallback.
- Refresh the private window skin with larger text, clearer selected states and consistent control dimensions.
- Fix title/background overlap, clipped English hints, localized layout overflow, tooltip lifetime and dropdown dismissal.
- Keep the IL2CPP window modal after dragging and allow F1 to open and close it once per press.
- Save IL2CPP text drafts on Enter, Tab/focus loss, disappearance or close; Escape discards the active draft. Search remains immediate.
- Improve caret painting, selection, clipboard shortcuts, word navigation, undo/redo and focus traversal.
- Keep Reset buttons readable and independent of numeric editor widths; center sliders within their rows.
- Track plugin expansion by GUID across filtering and reset it consistently with Expand All / Collapse All.
- Close dropdowns and cancel shortcut capture when their owning setting disappears. Show localized empty-result hints.

### Compatibility and correctness

- Avoid hard references to optional BepInEx IL2CPP shortcut/input types and ship the compatible shortcut DLL with the IL2CPP package.
- Replace native IL2CPP GUILayout paths with managed geometry and basic GUI primitives to avoid stripped methods, unsafe layout-option boxing and unbalanced clips.
- Preserve vertical scrolling and control order across repaint, filtering, expansion and language changes.
- Preserve EventSystem.current while suppressing Unity uGUI dispatch during the modal window. No game-specific input service is patched.
- Preserve exact numeric types and bounds when committing range text; retain scientific notation and reject nonfinite drafts.
- Correct property read-only metadata and isolate per-plugin IL2CPP settings collection failures.
- Separate appearance code, consolidate interaction cleanup and remove redundant category-order sorting.
- Add regression checks for compatibility, input backends, layout, scrolling, editing, settings and constructor/static-initializer failure paths.

### Installation and validation

- BepInEx 5 / Mono: use `BepInEx.ConfigurationManager_BepInEx5_v19.1.7.zip`.
- BepInEx 6 / IL2CPP: use `BepInEx.ConfigurationManager_IL2CPP_v19.1.7.zip`; update both plugin DLLs together.
- Release build and regression checks pass. Static JIT/reachable-method checks also pass against the supplied Unity 6000.3.8f1 / BepInEx 6.0.0-be.755 interop assemblies; these checks do not execute native rendering or detours.
- Native IME composition is unsupported by the managed IL2CPP editor. Custom native GUILayout drawers fall back to standard editors on IL2CPP.
- Generic UI modality does not universally block independent gameplay input polling. Game-side integration belongs in the game's mod.
