using Pine;
using UnityEngine;

namespace PineComposition.Examples
{
    public sealed class Counter : MonoBehaviour
    {
        public View Create(Value<string> title, Source<int> count = null)
        {
            count ??= P.Source(0);
            return P.Vertical(spacing: 8, childControlWidth: true, childControlHeight: true,
                childForceExpandHeight: false).With(
                P.Text(() => $"{title.Read()}: {count.Value}").With(P.LayoutElement(preferredHeight: 32)),
                P.Button("Increment", onClick: () => count.Value++).With(P.LayoutElement(preferredHeight: 40))
            );
        }
    }
}
