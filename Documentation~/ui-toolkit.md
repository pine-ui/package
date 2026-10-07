# UI Toolkit

This API is available in Pine 1.2.0. Use both imports:

```csharp
using Pine;
using Pine.UIToolkit;

public static class App
{
    public static View Mount()
    {
        var count = P.Source(0);
        return P.VisualElement(
            style: new Style { paddingLeft = 24, paddingTop = 24, width = 360 },
            children: new[]
            {
                P.Label(text: P.Value(() => $"Count: {count.Value}")),
                P.Button(text: "Increment", onClicked: () => count.Value++)
            });
    }
}
```

The compiler mounts `App.Mount()` once in a native UIDocument. Pine supplies an owned PanelSettings object, theme and bundled font. A second App entry is rejected. The shared `Source<T>`, `Value<T>`, `Scope`, derived values, effects, contexts, retained operators and springs are the same types used by uGUI. A source can feed either renderer.

## Native declarations

Factories retain native names: `P.Label`, `P.TextField`, `P.ListView`, `P.MultiColumnTreeView`, and the native Editor controls. Mutable properties/fields become optional named `Value<T>` inputs, so literals, sources and `P.Value(() => ...)` calculations update the existing native control. A writable field's `value` source receives native user changes; model writes use `SetValueWithoutNotify`. Bubbling child-field events do not overwrite a parent's source. Native normalization is reconciled back into writable sources.

```csharp
var name = P.Source("Ada");
var field = P.Ref<UnityEngine.UIElements.TextField>();
var view = P.TextField(
    label: "Name", value: name, reference: field,
    textEdition: P.TextEdition(maxLength: 64, placeholder: "Your name"));
```

Public native constructors also have explicit overloads. Their constructor arguments are native literals; use the ordinary optional property inputs for ongoing reactive changes. Constructor settings participate in writer diagnostics. Delegate properties accept native lambdas, with an additional `nameValue` input for reactive delegate replacement. Native C# events use `onEventName` inputs; generic events use `events: new[] { P.On<ClickEvent>(handler) }`, including native trickle behavior and typed user data.

See the generated [native factory and style reference](ui-toolkit-reference.md) for every named input. When targeting an existing UXML element, constructor inputs that match mutable properties are applied to that element. Constructor-only inputs require a newly constructed control; use property inputs or explicit native configuration for an existing target.

`reference` accepts a typed callback or `P.Ref<T>()`. References publish before tracked properties run, supporting forward links. Native methods, queries, focus, pointer capture, scheduling, drawing and custom controls remain accessible through the native reference. `P.Element(() => new MyElement(), ...)` declares any custom VisualElement; abstract native factories accept a typed `create` callback. `configure` remains available for native operations that are outside ordinary property declarations.

## Components and ownership

```csharp
public static class Card
{
    public static View Create(Value<string> title, View[] children = null)
    {
        var expanded = P.Source(true);
        return P.VisualElement(children: new[]
        {
            P.Button(text: title, onClicked: () => expanded.Value = !expanded.Value),
            P.VisualElement(enabled: expanded, children: children)
        });
    }
}
```

The compiler creates `Components.Card(...)` with an owned setup scope. Use `P.Component(() => Card.Create(...))` explicitly when desired. A MonoBehaviour with public `Create(...)` receives a generated factory too; its native properties initialize before activation. Setup scopes survive reactive property updates and end when the component is removed. For reactive child sets, use `children: () => views.Value`; the shared retained operators preserve declaration identity across reorderings.

```csharp
var mounted = P.Mount(() => Components.Card(title: "Settings"), parent: rootVisualElement);
// Dispose in the host's lifetime callback, such as EditorWindow.OnDisable.
mounted.Dispose();
```

An external root and its existing children survive disposal. Temporary native detachment/reparenting is not disposal. A Pine-owned runtime document remounts when UIDocument replaces its root after disable/enable or native root replacement. Owned panel settings and behaviour hosts are destroyed with the mount; supplied settings and assets remain external. Use `App.Options` or explicit `P.Mount(..., options: ...)` for owned panels:

```csharp
public static PanelOptions Options => new PanelOptions
{
    Persistent = true,
    Panel = P.PanelSettings(referenceResolution: new UnityEngine.Vector2Int(1920, 1080), scale: 1f),
    Document = P.UIDocument(sortingOrder: 10f)
};
```

## Styles, USS and UXML

`Style` exposes all 88 current native IStyle setters. Values remain native: lengths, percentages, colors, enums, lists, transforms, material definitions and USS reset keywords. Each property can independently observe sources or typed computed values. Pine adds no stylesheet or layout engine.

```csharp
var width = P.Source(240);
var color = P.Source(UnityEngine.Color.white);
var view = P.Label(text: "Status", style: new Style
{
    width = width,
    color = color,
    opacity = P.Value(() => width.Value > 200 ? 1f : .5f),
    marginLeft = UnityEngine.UIElements.StyleKeyword.Null
}, classes: new[] { "status" }, styleSheets: new[] { sheet });
```

Native USS controls selectors, pseudo states, transitions and custom properties. Classes and stylesheet arrays may also be reactive. Supply imported native assets directly. UXML is cloned and its named elements can receive typed declarations:

```csharp
var view = P.Template(template, targets: new[]
{
    P.Target("status", P.Label(text: status)),
    P.Target("accept", P.Button(onClicked: Accept))
});
```

Each target query is scoped to its own clone; duplicate names in separate instances do not collide. Missing or incompatible targets fail clearly. Cloned elements are owned by their mount; the template and USS assets survive cleanup.

## Native virtualized lists, trees and tables

```csharp
var list = P.ListView(
    fixedItemHeight: 32,
    rows: P.Rows(() => model.Items, (item, index) => P.Label(text: item.Title)));

var tree = P.TreeView(rows: P.TreeRows(() => model.Roots,
    (item, index) => P.Label(text: item.Title)));

var table = P.MultiColumnListView(
    rows: P.Rows(() => model.Items),
    columns: new[]
    {
        P.Column(name: "title", title: "Title", cell: P.Cell<Item>((item, index) => P.Label(text: item.Title))),
        P.Column(name: "enabled", title: "Enabled", cell: P.Cell<Item>(item => P.Toggle(value: item.Enabled)))
    });
```

Rows use native virtualization and tree IDs. Every bind creates a row/cell scope; unbind, rebinding, destruction and collection disposal release the old scope, callbacks and bindings. Persistent item state belongs to the model or a component outside recycled rows. Native raw callbacks remain available for custom controllers; declare either a Pine template or competing native row callbacks, once. Native columns, sorting, selection and expansion APIs remain accessible through typed references.

## Native bindings and Editor UI

`bindings: new[] { P.Bind("tooltip", nativeDataBinding) }` installs native runtime bindings. `P.DataBinding(...)` exposes native binding settings. Pine clears only its own installed binding; an external replacement survives disposal. Declared source/property writers and native binding writers on the same property are rejected.

Under `UNITY_EDITOR`, `P.SerializedBinding(serializedObject)` binds the declaration's isolated subtree using native SerializedObject behavior. Use field `bindingPath` inputs. Native inspectors/property drawers that Unity automatically binds should rely on that binding. Competing declared serialized/source writers are rejected; arbitrary later imperative native mutations cannot be diagnosed universally.

Editor controls are gated from players, including inspectors, property fields, GraphView, toolbars and IMGUIContainer. Mount into the EditorWindow's explicit root and dispose in OnDisable. Shared springs tick from EditorApplication.update while outside Play mode.

## Drawing and native extensions

`events: new[] { P.Draw(context => ...) }` uses native MeshGenerationContext/Painter2D and tracks source reads to request repaint. `generateVisualContent` exposes the raw native callback too. Add native manipulators with `manipulators: new[] { manipulator }`; installed manipulators and callbacks are removed by their owning scope, preserving unrelated handlers and external replacements.

## Compatibility

The floor is Unity 6000.3.0f1. Newer public members such as `PanelSettings.renderMode`, input `hideSoftKeyboard` and CallbackOptions overloads are enabled from the verified 6000.3.25f1 gate. Native Editor APIs remain Editor-only. Older patches can consume supplied settings assets for capabilities whose setters are not public. Native world-space panels, render targets and material/shader APIs retain Unity's own requirements and limitations. See [compatibility evidence](../COMPATIBILITY.md) for executed checks and platform limits.
