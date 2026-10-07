#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class PineToolkitBuildChecks
{
    public static string Build()
    {
        const string scenePath = "Assets/ToolkitVerification.unity";
        EditorSceneManager.SaveScene(
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single),
            scenePath
        );
        var target = NamedBuildTarget.Standalone;
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High);
        PlayerSettings.productName = "PineToolkitVerification";
        PlayerSettings.companyName = "PineVerification";
        string output = Path.GetFullPath("Build/ToolkitVerification.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(
            new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            }
        );
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception(
                $"IL2CPP build failed: {report.summary.result}, {report.summary.totalErrors} errors."
            );
        UnityEngine.Debug.Log("PINE_TOOLKIT_IL2CPP_BUILD_PASSED " + output);
        return output;
    }
}
#endif
