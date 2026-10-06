# Pine compatibility

## 1.0.0 native API

Manifest baseline: Unity 6000.3, uGUI 2.0.0 and Input System 1.20.1. Unity resolves its compatible uGUI core package. This 1.0.0 tag replaces the earlier API; old installations must be refreshed.

| Executed environment | Result |
| --- | --- |
| Windows Unity 6000.7.0b2, uGUI 2.7.0, resolved Input System 6.7.0 | 82 native Edit assertions, 8 native Play assertions, 13 startup assertions; compiler accepts one valid example and rejects 8 invalid cases. |
| .NET, C# 9 | 57 core assertions and 21 regression checks; 27 source-generator checks. Native assembly and XML documentation compilation has zero warnings/errors. |

Checks cover all 41 factories, nesting and same-object effects, native defaults/structs, source write-back, retained child identity, scoped callbacks/context, generated startup and owned behaviour/native-object cleanup. Native callbacks are invoked programmatically. These results do not certify physical input, visual rendering, frame-time performance, Unity 6000.3 runtime, mobile/WebGL, or IL2CPP/AOT.

RaycastReceiver requires uGUI 2.5; SafeArea and LayoutElement.maxWidth/maxHeight require 2.6; TMP advanced text requires 2.7; Canvas.useReflectionProbes requires Unity 6000.4. Those members are conditionally compiled. The uGUI baseline conditional source profile compiles against the installed native assemblies; this is not execution on the minimum Editor.

TMP `regexValue` and input-module `sendPointerHoverToParent` use cached access to their native serialized fields, which have no public setter. Editor checks cover this path; IL2CPP/stripping has not been verified. TMP outlines use native outline/material settings; uGUI Outline/Shadow applies to standard uGUI meshes.

Use Pine on Unity's main thread. Additional glyph coverage requires code-configured fonts. Missing-only setup preserves project defaults and external input/UI ownership.

Previous macOS/player measurements concern the replaced API and are not claimed for this implementation.
