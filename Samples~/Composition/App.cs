using Pine;
using UnityEngine;

namespace PineComposition.Examples
{
    public static class App
    {
        public static RectTransform Mount()
        {
            var shared = UI.Source(value: 0);
            return UI.Column(
                UI.Name(name: "Pine Composition"),
                UI.Size(width: 420, height: 640),
                UI.Vertical(spacing: 12),
                UI.Children(
                    UI.Label(
                        text: "Component composition",
                        UI.Size(width: 420, height: 40)
                    ),
                    Card.Create(
                        title: "Independent counters",
                        Components.Counter(title: "First"),
                        Components.Counter(title: "Second")
                    ),
                    Card.Create(
                        title: "Shared state",
                        Components.Counter(title: "Shared A", count: shared),
                        Components.Counter(title: "Shared B", count: shared)
                    ),
                    Actions.Save(
                        canSave: UI.Derive(compute: () => shared.Value > 0),
                        save: () => shared.Value = 0
                    )
                )
            );
        }
    }
}
