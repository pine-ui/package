using Pine;
using UnityEngine;

namespace PineComposition.Examples
{
    public static class Counter
    {
        public static RectTransform Create(
            string title = "Counter",
            Source<int> count = null
        )
        {
            count ??= UI.Source(0);
            return UI.Column(
                8,
                UI.Label(() => $"{title}: {count.Value}", UI.Size(420, 32)),
                UI.Button("Increment", () => count.Value++, UI.Size(420, 40))
            );
        }
    }
}
