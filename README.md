# Pine

Typed, reactive native Unity UI in C#. Import `Pine` and use `UI`; mount the entire tree once from `Start()`. Pine owns the Canvas, native control wiring and scoped bindings. Scene unload or root destruction removes the interface.

```csharp
using Pine;
using UnityEngine;

public sealed class Counter : MonoBehaviour
{
    private readonly Source<int> _count = UI.Source(0);

    [RuntimeInitializeOnLoadMethod]
    private static void StartUI() =>
        new GameObject("Counter owner").AddComponent<Counter>();

    private void Start() => UI.Mount(Build);

    private Component Build() =>
        UI.Column(
            UI.Size(360, 120),
            UI.Children(
                UI.Label(() => $"Count: {_count.Value}", UI.Size(360, 48)),
                UI.Button("Increment", () => _count.Value++, UI.Size(360, 48))
            )
        );
}
```

## Component composition

Mount `App.Create` once from a root startup file. Component functions in separate files return native components and call other components; they inherit the active mount or dynamic branch scope. Use `UI.Column(12, childA, childB)` or `UI.Row(8, childA, childB)` for fixed-spacing containers. Ordinary typed parameters support local/shared state, callbacks and caller-provided children. Import the **Component composition** sample for the complete five-file example, or read [component composition](https://pine-ui.com/docs/tutorials/components/).

## Install

Install with Unity Package Manager using https://github.com/pine-ui/package.git#v0.2.0, or install the matching com.kbenim.pine-0.2.0.tgz release artifact and verify its SHA256 checksum. The manifest declares Unity 6000.3, uGUI 2.0.0 and Input System 1.20.1; Unity resolves its Editor-compatible uGUI core package. See [compatibility](COMPATIBILITY.md) for the exact verified versions and scope.

The package bundles an accented-Latin font and missing-only TMP setup. It preserves existing TMP settings, compatible external EventSystems/action assets and parent canvases. Generated defaults retain project-owned resource copies for safe package removal. Desktop legacy-only projects enable Both with an Editor restart; legacy-only Android uses the compatible legacy UI fallback because Android does not support Both. When switching targets, Pine restores only its own original legacy backend choice; external Both configurations remain unchanged and receive a diagnostic. Setup diagnoses incompatible external configurations without rewriting gameplay.

## Features

- Compile-safe native properties, groups, events and custom composition.
- Retained components with sources, derived values, effects, batching and scoped context.
- Mount the whole tree once; optional `Mount` for explicit early disposal.
- Exact Size, explicit Fill/Auto axes, reactive uniform Grid and explicit clipping.
- Frame/row/column/grid, label, image/raw image, button, toggle, slider, scrollbar, text field, dropdown, scroll view and progress.
- Read-only dynamic results/presence/indices, stable row identity and retained exits.
- Springs, custom spaces, reduced motion, native navigation/focus and safe areas.
- Overlay, camera and world-space canvases configured in code.
- Verbose XML summaries/examples for every public/protected API declaration.

[Documentation](https://pine-ui.com) · [source](https://github.com/pine-ui/package) · [compatibility](COMPATIBILITY.md).

## Source and licenses

Pine source is MIT. Bundled Liberation Sans is SIL OFL; Unity TMP shader/style/line-breaking resources use the Unity Companion License. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
