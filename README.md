# Pine

Native uGUI declarations and reactive props in C#. Import `using Pine;`, declare one `App.cs`, and return a `View` from `App.Mount()`. Generated startup builds it once. Sources/getters update the same native components.

```csharp
using Pine;
using UnityEngine;

public static class App
{
    public static View Mount()
    {
        var count = P.Source(0);
        return P.Vertical(
            spacing: 12,
            childControlWidth: true,
            childControlHeight: true,
            childForceExpandHeight: false,
            sizeDelta: new Vector2(240, 160),
            children: new[]
            {
                P.Text(
                    () => $"Count: {count.Value}",
                    fontSize: 24,
                    children: new[] { P.LayoutElement(preferredHeight: 40) }
                ),
                P.Button(
                    "Increment",
                    onClick: () => count.Value++,
                    children: new[] { P.LayoutElement(preferredHeight: 40) }
                ),
                P.Button(
                    "Reset",
                    onClick: () => count.Value = 0,
                    interactable: new Value<bool>(() => count.Value > 0),
                    children: new[] { P.LayoutElement(preferredHeight: 40) }
                ),
            }
        );
    }
}
```

`children: new[] { ... }` composes children and same-object modifiers inside the factory call. `P.Self(P.Image(...))` configures the containing object's Image; ordinary `P.Image(...)` creates a child. Button attaches Unity's native Button and wires its Image/caption. TMP text outlines use `outlineColor`/`outlineWidth`; uGUI Outline targets Image/LegacyText meshes.

The catalog has 46 native factories with native Inspector settings as typed named props, native grouped structs and owned event callbacks. Later uGUI/Unity members are version-gated. TMP Text/InputField/Dropdown are default; explicit Legacy factories are available. `reference` captures the native component and `configure` handles one-time native work. Imperative `P.Create<T>`/`P.Apply` remain available for integration.

Import the Counter or Component composition sample. For Unity callbacks, define `MonoBehaviour.Create(...)` returning View; generated `Components` factories preserve the signature. Reactive child lists use `children: () => rows.Value` with retained operators (`Show`, `Switch`, `Indexes`, `Values`).

Custom `P.Declare<T>` views accept `properties: new IProperty<T>[] { ... }` for the existing typed `P.Set`, `P.Group` and event bindings. Sources/getters remain reactive; property arrays are copied and bindings end with the declaration's lifetime. The [native API guide](Documentation~/index.md) shows custom field and forward-reference mappings without a required `configure` callback.

Install `https://github.com/pine-ui/package.git#v1.1.1` with Package Manager, or use the [1.1.1 archive](https://github.com/pine-ui/package/releases/download/v1.1.1/com.kbenim.pine-1.1.1.tgz). The manifest baseline remains Unity 6000.3/uGUI 2.0.0/Input System 1.20.1. [Compatibility](COMPATIBILITY.md) records the verification boundary.

Missing-only TMP/input setup preserves project resources and external ownership. Native transitions, navigation and structs keep their declared settings. The auto canvas persists by default; `CanvasOptions.Persistent=false` gives scene lifetime. Explicit Canvas roots retain their own settings.

[Native API guide](Documentation~/index.md) · [All props](https://pine-ui.com/docs/api/controls-reference/) · [MIT and bundled resource notices](THIRD_PARTY_NOTICES.md).
