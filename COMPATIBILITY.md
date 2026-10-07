# Pine compatibility

## Unreleased declarative extensions

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
