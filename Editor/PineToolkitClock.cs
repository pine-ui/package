using UnityEditor;

namespace Pine.Editor
{
    [InitializeOnLoad]
    internal static class PineToolkitClock
    {
        private static double _previous;

        static PineToolkitClock()
        {
            _previous = EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += UIToolkit.Mount.DisposeAll;
            EditorApplication.quitting += UIToolkit.Mount.DisposeAll;
        }

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Clock.EnsureHost = () => { };
                Clock.Step(System.Math.Max(0, now - _previous), false);
            }
            _previous = now;
        }
    }
}
