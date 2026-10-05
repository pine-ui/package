using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Pine.CompilerServices
{
    /// <summary>Compiler-generated application startup support. Application code declares App.Mount in App.cs; generated initializers register direct calls here. This infrastructure is not needed in ordinary UI declarations.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AppStartup
    {
        private static readonly Dictionary<string, Action> Entries = new Dictionary<string, Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Entries.Clear();

        /// <summary>Registers one compiler-generated application entry before scene loading. Startup rejects multiple app assemblies before invoking any entry. This method is called only by Pine's generated code.</summary>
        /// <param name="assembly">The compiler's declaring assembly identity.</param>
        /// <param name="start">The direct application startup callback.</param>
        public static void Register(string assembly, Action start)
        {
            if (string.IsNullOrEmpty(assembly)) throw new ArgumentException("An application assembly is required.", nameof(assembly));
            if (start == null) throw new ArgumentNullException(nameof(start));
#if UNITY_EDITOR
            bool playerAssembly = false;
            foreach (var candidate in UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player))
                if (candidate.name == assembly) { playerAssembly = true; break; }
            if (!playerAssembly) return;
#endif
            if (Entries.ContainsKey(assembly)) throw new InvalidOperationException("Duplicate Pine application entry in " + assembly + ".");
            Entries.Add(assembly, start);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (Entries.Count > 1)
                throw new InvalidOperationException("Pine requires one App.cs entry. Found applications in: " + string.Join(", ", Entries.Keys));
            foreach (var entry in Entries.Values) entry();
        }
    }
}
