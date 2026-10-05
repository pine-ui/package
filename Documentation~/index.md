# Pine UI

Return your application tree from App.Mount in App.cs. Components compose directly and do not mount themselves.

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
            UI.Button(text: "Reset", click: () => count.Value = 0,
                UI.Enabled(enabled: () => count.Value > 0))
        );
    }
}
```

Use public MonoBehaviour.Create methods for Unity callbacks; Pine generates typed Components factories automatically. Plain functions are sufficient for UI-only components. The owned canvas persists by default; CanvasOptions.Persistent=false selects scene lifetime.

See the [complete documentation](https://pine-ui.com/docs/tutorials/installation/) and [component composition](https://pine-ui.com/docs/tutorials/components/).
