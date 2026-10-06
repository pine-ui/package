# Native Pine API

Plain C# functions return reusable `View` declarations. `.With(...)` appends entries without creating native objects or mutating the original. Visual/control/layout entries create children. Modifiers attach to the containing GameObject. `P.Self(...)` explicitly places a visual component on that same object.

```csharp
View Button(Vector2 position, Value<string> text, Action onClick)
{
    return P.Button(anchoredPosition: position, sizeDelta: new Vector2(200, 150), onClick: onClick)
        .With(
            P.Self(P.Image(color: new Color(50f / 255, 50f / 255, 50f / 255))),
            P.Outline(effectColor: Color.black),
            P.Text(
                text,
                color: Color.white,
                outlineColor: new Color32(0, 0, 0, 255),
                outlineWidth: .2f
            )
        );
}
```

The button object has RectTransform, Image, Button and Outline. Its text child has RectTransform, CanvasRenderer and TextMeshProUGUI. `P.Image()` without Self creates a child image. Arbitrarily nested `.With(...)` applies the same placement rules at every level.

Unity permits one Graphic per GameObject. Pine rejects conflicting same-object Graphics and repeated explicit declarations of the same native type. A modifier/Self entry requires a containing visual view. C# does not support arbitrary named optional settings followed by skipped positional params; `.With` keeps every native setting available as a named prop.

**TMP correction:** the uGUI Outline/Shadow components affect standard uGUI meshes such as Image and LegacyText. For TMP text use its native `outlineColor`/`outlineWidth` or a font material preset. Attaching an Outline does not turn it into a TMP shader effect. [TMP outline API](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/api/TMPro.TMP_Text.html#TMPro_TMP_Text_outlineWidth).

For Unity callbacks, declare a concrete MonoBehaviour with a public `View Create(...)` method. Pine generates `Components.Counter(...)` with the same named parameters. Create runs once before Awake/OnEnable; hiding retains the instance, and destroying its UI disposes its behaviour and reactive scope. `using Pine;` opts a file into component generation. See the Composition sample for independent/shared state and multi-level nesting.

See [all named props](https://pine-ui.com/docs/api/controls-reference/).
