using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace Pine.Editor
{
    [InitializeOnLoad]
    internal sealed class PineAppValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static PineAppValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingEditMode)
                    return;
                string error = DuplicateError();
                if (error == null)
                    return;
                Debug.LogError(error);
                EditorApplication.isPlaying = false;
            };
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            string error = DuplicateError();
            if (error != null)
                throw new BuildFailedException(error);
        }

        private static string DuplicateError()
        {
            var players = CompilationPipeline
                .GetAssemblies(AssembliesType.Player)
                .Select(a => a.name)
                .ToHashSet();
            var entries = AppDomain
                .CurrentDomain.GetAssemblies()
                .Where(a =>
                    players.Contains(a.GetName().Name)
                    && a.GetType("PineGeneratedAppEntry", false) != null
                )
                .Select(a => a.GetName().Name)
                .ToArray();
            return entries.Length > 1
                ? "Pine requires one App.cs entry. Found applications in: "
                    + string.Join(", ", entries)
                : null;
        }
    }
}
