# Pine

Typed, reactive native Unity UI in C#. Declare App.cs and return the tree from App.Mount. Pine starts it automatically; no scene owner, attribute, Start callback or mount variable is required. The canvas persists by default; destroying its root disposes the interface.

```csharp title="App.cs"
using Pine;
using UnityEngine;

public static class App
{
    public static RectTransform Mount()
    {
        var count = UI.Source(value: 0);

        return UI.Column(
            gap: 12,
            UI.Label(text: () => $"Count: {count.Value}"),
            UI.Button(text: "Increment", click: () => count.Value++),
            UI.Button(
                text: "Reset",
                click: () => count.Value = 0,
                UI.Enabled(enabled: () => count.Value > 0)
            )
        );
    }
}
```

## Component composition

Compose plain functions directly, or declare a public instance `Create(...)` on a `MonoBehaviour` for Unity callbacks. Pine generates typed `Components.Counter(title: "Count")` calls from the behaviour's signature. No render lambda, registration or base class is required. Components can call other components at any depth. Use `UI.Column(gap: 12, childA, childB)` for compact containers or typed properties for reactive layout. Import the four-file **Component composition** sample or read [components](https://pine-ui.com/docs/tutorials/components/).

## Install

Install with Unity Package Manager using https://github.com/pine-ui/package.git#v1.0.0, or install the matching com.kbenim.pine-1.0.0.tgz release artifact and verify its SHA256 checksum. The manifest declares Unity 6000.3, uGUI 2.0.0 and Input System 1.20.1; Unity resolves its Editor-compatible uGUI core package. See [compatibility](COMPATIBILITY.md) for the exact verified versions and scope.

The package bundles an accented-Latin font and missing-only TMP setup. It preserves existing TMP settings, compatible external EventSystems/action assets and parent canvases. Generated defaults retain project-owned resource copies for safe package removal. Desktop legacy-only projects enable Both with an Editor restart; legacy-only Android uses the compatible legacy UI fallback because Android does not support Both. When switching targets, Pine restores only its own original legacy backend choice; external Both configurations remain unchanged and receive a diagnostic. Setup diagnoses incompatible external configurations without rewriting gameplay.

## Features

- Compile-safe native properties, groups, events and custom composition.
- Retained components with sources, derived values, effects, batching and scoped context.
- Automatic App.cs startup; optional explicit `Mount` for early disposal.
- Exact Size, explicit Fill/Auto axes, reactive uniform Grid and explicit clipping.
- Frame/row/column/grid, label, image/raw image, button, toggle, slider, scrollbar, text field, dropdown, scroll view and progress.
- Read-only dynamic results/presence/indices, stable row identity and retained exits.
- Springs, custom spaces, reduced motion, native navigation/focus and safe areas.
- Overlay, camera and world-space canvases configured in code.
- Verbose XML summaries/examples for every public/protected API declaration.

[Documentation](https://pine-ui.com) · [source](https://github.com/pine-ui/package) · [compatibility](COMPATIBILITY.md).

## Source and licenses

Pine source is MIT. Bundled Liberation Sans is SIL OFL; Unity TMP shader/style/line-breaking resources use the Unity Companion License. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
