using Pine;
using Pine.uGUI;
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
