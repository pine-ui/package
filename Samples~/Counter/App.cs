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
