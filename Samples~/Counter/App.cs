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
                sizeDelta: new Vector2(240, 160)
            )
            .With(
                P.Text(() => $"Count: {count.Value}", fontSize: 24)
                    .With(P.LayoutElement(preferredHeight: 40)),
                P.Button("Increment", onClick: () => count.Value++)
                    .With(P.LayoutElement(preferredHeight: 40)),
                P.Button(
                        "Reset",
                        onClick: () => count.Value = 0,
                        interactable: new Value<bool>(() => count.Value > 0)
                    )
                    .With(P.LayoutElement(preferredHeight: 40))
            );
    }
}
