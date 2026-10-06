using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Pine.CompilerServices
{
    /// <summary>Compiler-generated application startup support.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AppStartup
    {
        private static readonly Dictionary<string, Action> Entries =
            new Dictionary<string, Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Entries.Clear();

        /// <summary>Registers one compiler-generated application entry before scene loading.</summary>
        public static void Register(string assembly, Action start)
        {
            if (string.IsNullOrEmpty(assembly))
                throw new ArgumentException(
                    "An application assembly is required.",
                    nameof(assembly)
                );
            if (start == null)
                throw new ArgumentNullException(nameof(start));
#if UNITY_EDITOR
            bool playerAssembly = false;
            foreach (
                var candidate in UnityEditor.Compilation.CompilationPipeline.GetAssemblies(
                    UnityEditor.Compilation.AssembliesType.Player
                )
            )
                if (candidate.name == assembly)
                {
                    playerAssembly = true;
                    break;
                }
            if (!playerAssembly)
                return;
#endif
            if (Entries.ContainsKey(assembly))
                throw new InvalidOperationException(
                    "Duplicate Pine application entry in " + assembly + "."
                );
            Entries.Add(assembly, start);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (Entries.Count > 1)
                throw new InvalidOperationException(
                    "Pine requires one App.cs entry. Found applications in: "
                        + string.Join(", ", Entries.Keys)
                );
            foreach (var entry in Entries.Values)
                entry();
        }
    }
}
