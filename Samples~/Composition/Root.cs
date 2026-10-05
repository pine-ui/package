using Pine;
using UnityEngine;

namespace PineComposition.Examples
{
    public static class Root
    {
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Start() => UI.Mount(App.Create);
    }
}
