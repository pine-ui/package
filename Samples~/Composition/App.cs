using Pine;
using UnityEngine;

namespace PineComposition.Examples
{
    public static class App
    {
        public static RectTransform Create()
        {
            var shared = UI.Source(0);
            var panel = UI.Column(
                12,
                UI.Label("Component composition", UI.Size(420, 40)),
                Card.Create(
                    "Independent counters",
                    Counter.Create("First"),
                    Counter.Create("Second")
                ),
                Card.Create(
                    "Shared state",
                    Counter.Create("Shared A", shared),
                    Counter.Create("Shared B", shared)
                ),
                Actions.Save(
                    UI.Derive(() => shared.Value > 0),
                    () => shared.Value = 0
                )
            );

            UI.Apply(panel, UI.Name("Pine Composition"), UI.Size(420, 640));
            return panel;
        }
    }
}
