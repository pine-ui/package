using Pine;
using Pine.UIToolkit;

public static class App
{
    public static View Mount()
    {
        var count = P.Source(0);
        return P.VisualElement(
            style: new Style
            {
                paddingTop = 24,
                paddingLeft = 24,
                width = 360,
            },
            children: new[]
            {
                P.Label(
                    text: P.Value(() => $"Count: {count.Value}"),
                    name: "Count",
                    style: new Style { fontSize = 24 }
                ),
                P.Button(text: "Increment", name: "Increment", onClicked: () => count.Value++),
                P.Button(text: "Reset", name: "Reset", onClicked: () => count.Value = 0),
            }
        );
    }
}
