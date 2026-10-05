using Pine;
using UnityEngine;

namespace Pine.Samples
{
    public sealed class PineCounter : MonoBehaviour
    {
        private readonly Source<int> _count = UI.Source(0);

        [RuntimeInitializeOnLoadMethod]
        private static void StartUI() => new GameObject("Pine counter").AddComponent<PineCounter>();

        private void Start() => UI.Mount(Build);

        private Component Build() =>
            UI.Column(
                UI.Size(360, 180),
                UI.Children(
                    UI.Label(() => $"Count: {_count.Value}", UI.Size(360, 48)),
                    UI.Button("Increment", () => _count.Value++, UI.Size(360, 48)),
                    UI.Button(
                        "Reset",
                        () => _count.Value = 0,
                        UI.Enabled(() => _count.Value > 0),
                        UI.Size(360, 48)
                    )
                )
            );
    }
}
