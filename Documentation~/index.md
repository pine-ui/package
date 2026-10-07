# Native Pine API

Import `using Pine;` and `using Pine.uGUI;` for the uGUI examples below. For the other renderer use `using Pine.UIToolkit;` with [the UI Toolkit API](ui-toolkit.md). `P` lives in the selected renderer; reactive types stay in `Pine`. This is the Pine 1.2.0 namespace contract.

Plain C# functions return reusable `View` declarations. `children: new[] { ... }` nests declarations inside the factory without creating native objects. Child arrays are copied. Visual/control/layout entries create children. Modifiers attach to the containing GameObject. `P.Self(...)` explicitly places a visual component on that same object.

```csharp
View Button(Vector2 position, Value<string> text, Action onClick)
{
    return P.Button(
        anchoredPosition: position,
        sizeDelta: new Vector2(200, 150),
        onClick: onClick,
        children: new[]
        {
            P.Self(P.Image(color: new Color(50f / 255, 50f / 255, 50f / 255))),
            P.Outline(effectColor: Color.black),
            P.Text(
                text,
                color: Color.white,
                outlineColor: new Color32(0, 0, 0, 255),
                outlineWidth: .2f
            ),
        }
    );
}
```

The button object has RectTransform, Image, Button and Outline. Its text child has RectTransform, CanvasRenderer and TextMeshProUGUI. `P.Image()` without Self creates a child image. Nested factory calls apply the same placement rules at every level.

Unity permits one Graphic per GameObject. Pine rejects conflicting Graphics and component multiplicity forbidden by Unity; allowed repetitions such as multiple Outline components remain independent. A modifier/Self entry requires a containing visual view. Named `children` arrays keep the syntax valid in C# 9 alongside optional named settings. Use `components: new[] { ... }` for static modifiers or Self entries when `children` is a reactive getter.

**TMP correction:** the uGUI Outline/Shadow components affect standard uGUI meshes such as Image and LegacyText. For TMP text use its native `outlineColor`/`outlineWidth` or a font material preset. Attaching an Outline does not turn it into a TMP shader effect. [TMP outline API](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/api/TMPro.TMP_Text.html#TMPro_TMP_Text_outlineWidth).

For Unity callbacks, declare a concrete MonoBehaviour with a public `View Create(...)` method. Pine generates `Components.Counter(...)` with the same named parameters. Create runs once before Awake/OnEnable; hiding retains the instance, and destroying its UI disposes its behaviour and reactive scope. `using Pine;` opts a file into component generation. See the Composition sample for independent/shared state and multi-level nesting.

See [all named props](https://pine-ui.com/docs/api/controls-reference/).

Custom components can use the existing typed property API in either `P.Declare<T>` overload. Property arrays are copied into the declaration; getters and Sources remain reactive, and bindings end with the owning scope. Properties apply before the optional one-time `configure` callback and native activation. Forward references can be read through `P.Ref<T>()`.

```csharp
public sealed class Gauge : MonoBehaviour
{
    public float Amount;
    public TMP_Text Label;
}

public static class GaugeUi
{
    public static View Create(Source<float> amount)
    {
        var label = P.Ref<TMP_Text>();
        return P.Declare<Gauge>(
            properties: new IProperty<Gauge>[]
            {
                P.Set<Gauge, float>("Amount", (gauge, value) => gauge.Amount = value, amount),
                P.Set<Gauge, TMP_Text>("Label", (gauge, value) => gauge.Label = value, label)
            },
            children: new[] { P.Text("Gauge", reference: label) }
        );
    }
}
```

The setter expresses the custom field mapping; Pine owns the reactive binding. This avoids a required one-time `configure` callback without reflecting custom fields or generating another API. Settings/events/reference values are typed; composed part compatibility and hierarchy/lifetime rules are validated at runtime.

Named control parts accept declarations or externally owned targets: `P.Slider(fill: P.Image(), handle: P.Image())` and `P.ScrollRect(content: P.Vertical(children: new[] { P.Text("Row") }), viewport: P.Frame())`. `P.Ref<T>()` passed to `reference` publishes a native target before wiring and clears it when removed. Pass refs to other part arguments, or read them in tracked `Value<Navigation>` structs for forward/cyclic navigation. Assets come from the project. [Parts and typed links](https://pine-ui.com/docs/api/creation/).
