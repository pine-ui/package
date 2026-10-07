using Pine;
using UnityEngine;

namespace PineComposition.Examples
{
    public static class App
    {
        public static View Mount()
        {
            var shared = P.Source(0);
            return P.Vertical(
                name: "Pine Composition",
                sizeDelta: new Vector2(420, 640),
                spacing: 12,
                childControlWidth: true,
                childControlHeight: true,
                childForceExpandHeight: false,
                children: new[]
                {
                    P.Text(
                        "Component composition",
                        children: new[] { P.LayoutElement(preferredHeight: 40) }
                    ),
                    Card.Create(
                        "Independent counters",
                        Components.Counter(title: "First"),
                        Components.Counter(title: "Second")
                    ),
                    Card.Create(
                        "Shared state",
                        Components.Counter(title: "Shared A", count: shared),
                        Components.Counter(title: "Shared B", count: shared)
                    ),
                    Actions.Save(P.Derive(() => shared.Value > 0), () => shared.Value = 0),
                }
            );
        }
    }
}
