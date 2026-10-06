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
        return P.Vertical(spacing: 12, childControlWidth: true, childControlHeight: true,
            childForceExpandHeight: false, sizeDelta: new Vector2(240, 160)).With(
            P.Text(() => $"Count: {count.Value}", fontSize: 24).With(P.LayoutElement(preferredHeight: 40)),
            P.Button("Increment", onClick: () => count.Value++).With(P.LayoutElement(preferredHeight: 40)),
            P.Button("Reset", onClick: () => count.Value = 0,
                interactable: new Value<bool>(() => count.Value > 0)).With(P.LayoutElement(preferredHeight: 40))
        );
    }
}
```

`.With(...)` composes children and same-object modifiers. `P.Self(P.Image(...))` configures the containing object's Image; ordinary `P.Image(...)` creates a child. Button attaches Unity's native Button and wires its Image/caption. TMP text outlines use `outlineColor`/`outlineWidth`; uGUI Outline targets Image/LegacyText meshes.

The catalog has 41 native factories with native Inspector settings as typed named props, native grouped structs and owned event callbacks. Later uGUI/Unity members are version-gated. TMP Text/InputField/Dropdown are default; explicit Legacy factories are available. `reference` captures the native component and `configure` handles one-time native work. Imperative `P.Create<T>`/`P.Apply` remain available for integration.

Import the Counter or Component composition sample. For Unity callbacks, define `MonoBehaviour.Create(...)` returning View; generated `Components` factories preserve the signature. Reactive child lists use `.With(() => rows.Value)` with retained operators (`Show`, `Switch`, `Indexes`, `Values`).

Install `https://github.com/pine-ui/package.git#v1.0.0` with Package Manager, or use the [1.0.0 archive](https://pine-ui.com/packages/com.kbenim.pine-1.0.0.tgz). The 1.0.0 tag/archive were replaced on 2026-10-06; remove/reinstall earlier installs to refresh their cached revision. The manifest baseline remains Unity 6000.3/uGUI 2.0.0/Input System 1.20.1. [Compatibility](COMPATIBILITY.md) records the verification boundary.

Missing-only TMP/input setup preserves project resources and external ownership. Native transitions, navigation and structs keep their declared settings. The auto canvas persists by default; `CanvasOptions.Persistent=false` gives scene lifetime. Explicit Canvas roots retain their own settings.

[Native API guide](Documentation~/index.md) · [All props](https://pine-ui.com/docs/api/controls-reference/) · [MIT and bundled resource notices](THIRD_PARTY_NOTICES.md).
