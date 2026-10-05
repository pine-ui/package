using System;
using System.Linq;
using UnityEngine;

namespace Pine.Samples
{
    using Pine;

    public static class PineExample
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (
                Environment.GetEnvironmentVariable("PINE_EXAMPLE") == "1"
                || Environment.GetCommandLineArgs().Contains("-pine-example")
            )
                UI.Mount(Build);
        }

        public static Component Build()
        {
            var count = UI.Source(0);
            var show = UI.Source(true);
            var history = UI.Source(new[] { 0 });
            var offset = UI.Spring(() => count.Value % 2 == 0 ? -180f : 180f, period: 0.5);
            var rows = UI.Show(
                () => show.Value,
                present =>
                {
                    var items = UI.Values(
                        () => history.Value,
                        (value, index) =>
                            UI.Label(
                                () => $"{index.Value + 1}. Count was {value}",
                                UI.Size(620, 30)
                            )
                    );
                    var fade = UI.Spring(() => present.Value ? 1f : 0f, period: 0.18);
                    var list = UI.Column(
                        UI.Name("History"),
                        UI.Vertical(4),
                        UI.Size(620, 170),
                        UI.Opacity(fade),
                        UI.Children(() => items.Value)
                    );
                    return new Branch<Component>(list, 0.35);
                }
            );
            return UI.Column(
                UI.Name("Pine Example"),
                UI.Size(620, 570),
                UI.Vertical(12),
                UI.Children(
                    UI.Label("pine", UI.FontSize(48), UI.Size(620, 70)),
                    UI.Label(
                        "Reactive uGUI. Created entirely in C#.",
                        UI.FontSize(20),
                        UI.Size(620, 40)
                    ),
                    UI.Label(
                        () => $"Count: {count.Value}",
                        UI.Name("Counter"),
                        UI.FontSize(32),
                        UI.Size(620, 60)
                    ),
                    UI.Button(
                        "Increment",
                        () =>
                        {
                            count.Value++;
                            history.Value = history
                                .Peek()
                                .Append(count.Peek())
                                .TakeLast(5)
                                .ToArray();
                        },
                        UI.Name("Increment"),
                        UI.Size(620, 48)
                    ),
                    UI.Button(
                        "Show / hide history",
                        () => show.Value = !show.Peek(),
                        UI.Size(620, 48)
                    ),
                    UI.Column(UI.Vertical(0), UI.Size(620, 170), UI.Children(() => rows.Value)),
                    UI.Frame(
                        UI.Size(620, 32),
                        UI.Children(
                            UI.Image(
                                UI.Name("Spring marker"),
                                UI.Tint(new Color(0.3f, 0.8f, 0.5f)),
                                UI.Size(18, 18),
                                UI.Position(() => new Vector2(offset.Value, 0))
                            )
                        )
                    )
                )
            );
        }
    }
}
