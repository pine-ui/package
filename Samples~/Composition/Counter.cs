using Pine;
using UnityEngine;

namespace PineComposition.Examples
{
    public sealed class Counter : MonoBehaviour
    {
        public RectTransform Create(
            Value<string> title,
            Source<int> count = null
        )
        {
            count ??= UI.Source(value: 0);
            return UI.Column(
                gap: 8,
                UI.Label(
                    text: () => $"{title.Read()}: {count.Value}",
                    UI.Size(width: 420, height: 32)
                ),
                UI.Button(
                    text: "Increment",
                    click: () => count.Value++,
                    UI.Size(width: 420, height: 40)
                )
            );
        }
    }
}
