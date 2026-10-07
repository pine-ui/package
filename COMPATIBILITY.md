# Pine compatibility

## 1.2.0 UI Toolkit and renderer namespaces

Verified on 2026-10-08 on Windows. Import `Pine` together with `Pine.uGUI` or `Pine.UIToolkit`; there is no public root `Pine.P`. Both renderers use the same reactive types and scheduler.

The native metadata catalog supplies 504 factory overloads, all 88 writable `IStyle` properties, inherited current mutable properties/fields, typed callbacks, native constructor overloads and typed references. Native Editor APIs are excluded from players; the verified patch additions are gated at 6000.3.25f1. The generated [factory/style reference](Documentation~/ui-toolkit-reference.md) lists named inputs.

| Executed check | Result |
| --- | --- |
| Windows Editors 6000.3.0f1 and 6000.3.25f1 | 41 native UI Toolkit assertions per Editor: source write-back and clamping, styles, retained children, UXML clones/targets, callback ownership, binding conflicts, references and recycled list/tree/table scopes. |
| Native Editor binding in both Editors | 4 assertions per Editor: SerializedObject read/write, Undo and disposal cleanup. |
| UI Toolkit Play/render in both Editors | 21 assertions per Editor: generated startup, native pointer/focus/keyboard paths, owned MonoBehaviour activation/cleanup, themed text/buttons, tracked Painter2D repaint, document disable/enable and native collection callbacks. Captured render-target images were inspected. |
| Domain reload disabled | Repeated startup/runtime sessions passed in both Editors; the final collection fixture passed in Play mode with this setting. |
| Windows standalone, 6000.3.25f1 | IL2CPP with High managed stripping built successfully. The player passed the same 21 runtime/render assertions, including generic typed list/tree/table templates. |
| Existing uGUI Edit/authoring checks through the new import | 82 native and 48 authoring assertions passed in each Editor. |
| .NET / C# generator / formatting | 57 core assertions, 28 regressions, 39 component-generator checks; bundled Release compiler DLL matched its source build; CSharpier passed. |
| Documentation | Production build, sitemap, download and versioned assistant citation checks passed. |

Input is dispatched through pooled native events; collection recycling checks call native callbacks, including in the IL2CPP player. These results do not certify physical devices, every viewport/collection controller configuration, world-space/XR panels, mobile, WebGL, every generic specialization or future Unity versions. Native methods, custom controls, imported USS/UXML and native assets remain available through typed references and scoped configuration; Pine retains Unity's platform requirements. Historical uGUI platform limits below still apply to the uGUI checks.

## Source audit — 2026-10-07

The source-audit patch was executed against the committed uGUI implementation based on `eb502c4`, in separate Windows projects using Unity 6000.3.0f1 and 6000.3.25f1, uGUI 2.0.0 and Input System 1.20.1.

| Check | Result in each Editor |
| --- | --- |
| Native Edit / authoring / native Play | 82 / 48 / 12 assertions passed. |
| Safe-area geometry / runtime lifecycle | 31 synthetic geometry assertions / 12 Play assertions passed. |
| Interaction/render | 50 assertions passed; desktop render captures produced. |
| Compiler | One valid declaration and 9 expected compile-negative cases passed. |
| Startup | 13 assertions; 13 assertions in each of two sessions with domain reload disabled. |
| Runtime graph contract / setup | 8 graph checks; missing-only defaults and unrelated dirty asset preservation passed. |

The patch fixes Active property activation ordering, stable ordering when an assignment delegate is reused, never-active object cleanup, simultaneous cleanup/update error reporting, immediate spring position control, finite-number overflow handling and native component types named View in generated factories. It reuses scoped event handling and removes redundant cleanup scans, getter/delegate allocations and the grid-validation Transform enumerator. .NET checks passed 57 core assertions, 28 regressions and 30 generator checks; the Release compiler plugin matches its source.

`SafeAreaProperty` maps Player-window pixels into the actual parent rect and follows parent/canvas/camera geometry changes. It supports axis-aligned screen-space overlay and camera canvases, including partial-screen, translated or scaled parents. The follower rect must be unrotated, unscaled and in its parent's plane; its parent must be unrotated relative to the Canvas. World-space canvases and unsupported transforms are rejected. Disabling the helper restores the supplied anchors and offsets. Synthetic desktop cutouts and actual Play callbacks were checked; physical mobile cutouts were not.

A warmed .NET 8 Release benchmark (median of five samples) reduced disposal of 20,000 empty effects from 255.516 ms / 1,280,000 allocated bytes to 0.464 ms / 0 allocated bytes. This establishes the bulk-cleanup improvement; it is not a Unity frame-time benchmark. Standalone, mobile, WebGL and IL2CPP/AOT remain unverified. Concurrent UI Toolkit development is covered by its own verification.

## 1.1.1 declarative extensions (historical checks)

Verified on 2026-10-07 against Unity 6000.3.0f1 and 6000.3.25f1 on Windows, with uGUI 2.0.0 and Input System 1.20.1. The published 1.1.0 results below remain historical.

| Check | Result in each Editor |
| --- | --- |
| Native Edit / authoring / native Play | 82 / 32 / 8 assertions passed. |
| Compiler | One valid declaration and 9 expected compile-negative cases passed. |
| Startup | 13 assertions passed; another 13 passed in each of two sessions with domain reload disabled. |
| Interaction/render | 50 assertions passed; captured desktop renders inspected visually. |
| Runtime graph contract | 8 checks passed, including incompatible declared parts and reference cleanup/recovery after rejected construction. |
| Setup | Missing-only defaults and unrelated dirty asset preservation passed. |

`P.Declare<T>` accepts existing typed reactive properties in both overloads, snapshots property arrays, and applies properties before optional configuration and activation. Mount checks cover updates, forward references, dependent-link clearing and disposal cleanup. A compiler-negative case rejects properties for the wrong component type.

The expanded interaction fixture exercises linked horizontal/vertical scrollbars and all visibility modes, world-space pointer input, TMP/legacy text/image/mixed dropdowns, multiline TMP input with a declared viewport/caret/scrollbar, changing ToggleGroup membership, and visible native Normal/Highlighted/Pressed/Selected/Disabled animation states. Input uses simulated devices and queued Game View text events. This also exposed and fixed generated-part layout errors in scroll viewports, toggle captions/checkmarks and dropdown images/captions.

.NET checks passed 57 core assertions, 21 regressions and 28 generator checks; the Release generator build had zero warnings/errors and matched the bundled DLL by SHA-256. The pinned C# formatter passed. No new standalone, hardware, multiplayer routing, mobile, WebGL, IL2CPP/AOT or performance verification was added. Typed settings/events/references plus runtime graph validation is the verified contract; these results do not prove every possible native/custom Inspector graph or future Unity API is declaratively covered.

## 1.1.0 declarative authoring

Manifest baseline: Unity 6000.3, uGUI 2.0.0 and Input System 1.20.1, with Unity's built-in animation module. The catalog contains 46 factories; newer native types/members remain version-gated.

| Executed environment | Result |
| --- | --- |
| Windows Unity 6000.3.0f1, uGUI 2.0.0, Input System 1.20.1 | 82 native Edit assertions, 26 authoring assertions, 8 native Play assertions, 13 startup assertions; one compiler-positive and 8 negative cases; missing-only setup and unrelated dirty asset preservation. |
| Windows Unity 6000.3.25f1, uGUI 2.0.0, Input System 1.20.1 | The same native, authoring, Play, startup, compiler and setup checks. |
| Both stable Windows Editors | 17 interaction/render assertions: Input System mouse click/toggle/slider drag/dropdown selection/scroll, native TMP text editing, keyboard submit, simulated gamepad navigation, supplied camera rendering and generated sample startup. Captured images were inspected visually. Startup also passed twice with domain reload disabled, 13 assertions per session. |
| .NET, C# 9 | 57 core assertions plus 21 regressions; 28 component-generator checks and bundled generator/source equality. Runtime/XML and downloadable example compilation: zero warnings/errors. |

Authoring checks cover declared composite parts, missing-only defaults, externally owned dropdown hierarchies and input assets, independent multiplayer defaults, forward/cyclic links and removal cleanup, retained children, static attachments beside reactive getters, supplied Animator controllers, PlayerInput module wiring and owned EventTrigger callbacks. Composition and wiring use supplied assets; Pine does not replace font/sprite/material/controller/input-asset authoring.

Interaction checks run in Editor Play mode with simulated Input System devices and queued Game View text events. They exercise native event/raycast/control paths and desktop rendering, but do not certify physical mouse/keyboard/gamepad, split-screen player routing, XR/tracked hardware, virtual-mouse hardware interaction, standalone player builds, mobile, WebGL, IL2CPP/AOT or performance. Later version-gated APIs were compiled against 6000.7.0b2 native metadata; their runtime behavior was not tested in this stable matrix.

`View.With` is replaced by factory `children` arrays/getters in 1.1.0. Use `components` for static modifiers/Self entries beside a reactive getter. Typed refs expose native methods through `Value`; call them at the required Unity lifecycle point (for example TMP Dropdown.Show after Start).

## 1.0.0 native API

Manifest baseline: Unity 6000.3, uGUI 2.0.0 and Input System 1.20.1. Unity resolves its compatible uGUI core package.

| Executed environment | Result |
| --- | --- |
| Windows Unity 6000.7.0b2, uGUI 2.7.0, resolved Input System 6.7.0 | 82 native Edit assertions, 8 native Play assertions, 13 startup assertions; compiler accepts one valid example and rejects 8 invalid cases. |
| .NET, C# 9 | 57 core assertions and 21 regression checks; 27 source-generator checks. Native assembly and XML documentation compilation has zero warnings/errors. |

Checks cover all 41 factories, nesting and same-object effects, native defaults/structs, source write-back, retained child identity, scoped callbacks/context, generated startup and owned behaviour/native-object cleanup. Native callbacks are invoked programmatically. These results do not certify physical input, visual rendering, frame-time performance, Unity 6000.3 runtime, mobile/WebGL, or IL2CPP/AOT.

RaycastReceiver requires uGUI 2.5; SafeArea and LayoutElement.maxWidth/maxHeight require 2.6; TMP advanced text requires 2.7; Canvas.useReflectionProbes requires Unity 6000.4. Those members are conditionally compiled. The uGUI baseline conditional source profile compiles against the installed native assemblies; this is not execution on the minimum Editor.

TMP `regexValue` and input-module `sendPointerHoverToParent` use cached access to their native serialized fields, which have no public setter. Editor checks cover this path; IL2CPP/stripping has not been verified. TMP outlines use native outline/material settings; uGUI Outline/Shadow applies to standard uGUI meshes.

Use Pine on Unity's main thread. Additional glyph coverage requires code-configured fonts. Missing-only setup preserves project defaults and external input/UI ownership.
