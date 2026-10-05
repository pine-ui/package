using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
public static class PineSetupChecks
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        AssetDatabase.DeleteAsset("Assets/PineGenerated");
        const string path = "Assets/PineUnrelatedDirty.asset";
        var probe = ScriptableObject.CreateInstance<PineDirtyProbe>();
        probe.Value = 1; AssetDatabase.CreateAsset(probe, path); AssetDatabase.SaveAssetIfDirty(probe);
        string original = File.ReadAllText(path); probe.Value = 2; EditorUtility.SetDirty(probe);
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").First();
        var serialized = new SerializedObject(settings); serialized.FindProperty("activeInputHandler").intValue = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        string backendFile = File.ReadAllText("ProjectSettings/ProjectSettings.asset");
        File.WriteAllText("ProjectSettings/ProjectSettings.asset", backendFile.Replace("activeInputHandler: 2", "activeInputHandler: 0"));
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Pine.Editor.PinePackageSetup") ?? a.GetType("Assets.Editor.PinePackageSetup")).First(t => t != null);
        type.GetMethod("Configure", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        Check(EditorUtility.IsDirty(probe) && File.ReadAllText(path) == original, "Setup saved an unrelated dirty asset.");
        Check(File.ReadAllText("ProjectSettings/ProjectSettings.asset").Contains("activeInputHandler: 2"), "Input backend change was not persisted independently.");
        var configureInput = type.GetMethod("ConfigureInput", BindingFlags.NonPublic | BindingFlags.Static);
        configureInput.Invoke(null, new object[] { BuildTarget.Android });
        Check(File.ReadAllText("ProjectSettings/ProjectSettings.asset").Contains("activeInputHandler: 0"), "Android did not restore Pine-owned original legacy input.");
        configureInput.Invoke(null, new object[] { BuildTarget.StandaloneOSX });
        Check(File.ReadAllText("ProjectSettings/ProjectSettings.asset").Contains("activeInputHandler: 2"), "Desktop did not restore owned coexistence.");
        const string ownership = "ProjectSettings/PineInput.json";
        File.WriteAllText(ownership, "{\"Original\":0,\"LastWritten\":0,\"Pending\":2,\"HasPending\":true}");
        configureInput.Invoke(null, new object[] { BuildTarget.Android });
        Check(File.ReadAllText("ProjectSettings/ProjectSettings.asset").Contains("activeInputHandler: 0"), "An interrupted post-scalar ownership write prevented owned Android fallback.");
        File.WriteAllText(ownership, "{\"Original\":0,\"LastWritten\":0,\"Pending\":2,\"HasPending\":true}");
        configureInput.Invoke(null, new object[] { BuildTarget.Android });
        Check(File.ReadAllText(ownership).Contains("\"HasPending\":false"), "An interrupted pre-scalar write was not cancelled on an unchanged target.");
        configureInput.Invoke(null, new object[] { BuildTarget.StandaloneOSX });
        File.Delete(ownership);
        configureInput.Invoke(null, new object[] { BuildTarget.Android });
        Check(File.ReadAllText("ProjectSettings/ProjectSettings.asset").Contains("activeInputHandler: 2") && !File.Exists(ownership), "Pine changed externally owned Both mode.");
        string tmp = AssetDatabase.GetAssetPath(Resources.Load<TMP_Settings>("TMP Settings"));
        Check(!AssetDatabase.GetDependencies(tmp, true).Any(p => p.StartsWith("Packages/com.kbenim.pine/")), "Generated defaults retain a removable Pine dependency.");
        Check(TMP_Settings.defaultFontAsset != null && TMP_Settings.defaultFontAsset.material.shader != null, "Project-owned text resources are incomplete.");
        AssetDatabase.DeleteAsset(path);
        Debug.Log("Pine setup checks passed: dirty asset preserved, backend persisted, project defaults independent of Pine.");
        EditorApplication.Exit(0);
    }
    public static void AfterRemoval()
    {
        var font = TMP_Settings.defaultFontAsset;
        Check(font != null && font.material.shader != null && font.HasCharacter('Ő'), "Removing Pine invalidated default text resources.");
        string tmp = AssetDatabase.GetAssetPath(Resources.Load<TMP_Settings>("TMP Settings"));
        Check(!AssetDatabase.GetDependencies(tmp, true).Any(p => p.StartsWith("Packages/com.kbenim.pine/")), "Removed-package references remain.");
        var canvas = new GameObject("Independent Canvas", typeof(RectTransform), typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var go = new GameObject("Independent TMP", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(320, 48);
        var text = go.AddComponent<TextMeshProUGUI>(); text.text = "Árvíztűrő"; Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate(true);
        Check(text.textInfo.characterCount > 0 && text.textInfo.meshInfo[0].vertexCount > 0, "TMP no longer renders after Pine removal.");
        UnityEngine.Object.DestroyImmediate(canvas);
        Debug.Log("Pine removal check passed: ordinary TMP still renders with project-owned defaults.");
        EditorApplication.Exit(0);
    }
}
