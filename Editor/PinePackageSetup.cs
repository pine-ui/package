using System;
using System.IO;
using System.Linq;
using Pine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pine.Editor
{
    // Only missing TMP settings and supported backend coexistence are configured.
    [InitializeOnLoad]
    internal static class PinePackageSetup
    {
        private const string RestartKey = "Pine.Setup.RestartPending";

        static PinePackageSetup()
        {
            EditorApplication.delayCall += Configure;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    Configure();
            };
        }

        internal static void Configure()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Configure;
                return;
            }
            EnsureTextResources();
            ConfigureInput(EditorUserBuildSettings.activeBuildTarget);
        }

        private static void EnsureTextResources()
        {
            const string path = "Assets/PineGenerated/Resources/TMP Settings.asset";
            var existing = Resources.Load<TMP_Settings>("TMP Settings");
            // External canonical settings are never modified. Migrate only our earlier generated
            // settings so all their bundled dependencies survive removing the package.
            if (existing != null && AssetDatabase.GetAssetPath(existing) != path)
                return;
            var font = Resources.Load<TMP_FontAsset>("Pine/Fonts/Latin");
            if (font == null)
            {
                Debug.LogError("Pine's bundled font was not imported. Reinstall the package.");
                return;
            }
            var copies = CopyDefaultResources(font);
            var settings =
                existing != null ? existing : ScriptableObject.CreateInstance<TMP_Settings>();
            typeof(TMP_Settings)
                .GetMethod(
                    "SetAssetVersion",
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                )
                ?.Invoke(settings, null);
            var serialized = new SerializedObject(settings);
            foreach (
                var pair in new[]
                {
                    ("m_defaultFontAsset", (UnityEngine.Object)font),
                    ("m_defaultStyleSheet", Resources.Load<TMP_StyleSheet>("Pine/Defaults/Styles")),
                    ("m_leadingCharacters", Resources.Load<TextAsset>("Pine/Defaults/Leading")),
                    ("m_followingCharacters", Resources.Load<TextAsset>("Pine/Defaults/Following")),
                }
            )
            {
                var property = serialized.FindProperty(pair.Item1);
                if (
                    existing == null
                    || property.objectReferenceValue == null
                    || property.objectReferenceValue == pair.Item2
                )
                    property.objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                            copies[AssetDatabase.GetAssetPath(pair.Item2)]
                        );
            }
            if (existing == null)
            {
                serialized.FindProperty("m_defaultFontSize").floatValue = 24;
                serialized.FindProperty("m_defaultFontAssetPath").stringValue =
                    "PineGenerated/Fonts/";
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (existing == null)
            {
                EnsureAssetFolder("Assets/PineGenerated/Resources");
                AssetDatabase.CreateAsset(settings, path);
            }
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        private static System.Collections.Generic.Dictionary<string, string> CopyDefaultResources(
            TMP_FontAsset font
        )
        {
            string source = Path.GetDirectoryName(
                    Path.GetDirectoryName(AssetDatabase.GetAssetPath(font))
                )
                .Replace('\\', '/');
            const string destination = "Assets/PineGenerated/Resources/PineGenerated";
            var paths = new System.Collections.Generic.Dictionary<string, string>();
            var guids = new System.Collections.Generic.Dictionary<string, string>();
            foreach (
                string file in AssetDatabase
                    .FindAssets("", new[] { source })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(file => !AssetDatabase.IsValidFolder(file))
                    .OrderBy(file =>
                        file.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) ? 1 : 0
                    )
            )
            {
                string original = file.Replace('\\', '/');
                string copy = destination + original.Substring(source.Length);
                EnsureAssetFolder(Path.GetDirectoryName(copy).Replace('\\', '/'));
                if (!File.Exists(copy))
                {
                    // CopyAsset saves unrelated dirty assets on a fresh resource copy.
                    // Copy bytes and a new importer GUID, then import only the owned asset.
                    var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(original);
                    string physical =
                        package == null
                            ? original
                            : Path.Combine(
                                package.resolvedPath,
                                original.Substring(package.assetPath.Length + 1)
                            );
                    string meta = File.ReadAllText(physical + ".meta");
                    var guidField = new System.Text.RegularExpressions.Regex(
                        @"(?m)^guid: [0-9a-f]{32}(?=\r?$)"
                    );
                    if (guidField.Matches(meta).Count != 1)
                        throw new IOException(
                            "Pine resource metadata must contain one importer GUID: " + original
                        );
                    meta = guidField.Replace(meta, "guid: " + Guid.NewGuid().ToString("N"));
                    File.Copy(physical, copy);
                    File.WriteAllText(copy + ".meta", meta);
                    AssetDatabase.ImportAsset(copy, ImportAssetOptions.ForceSynchronousImport);
                }
                paths[original] = copy;
                guids[AssetDatabase.AssetPathToGUID(original)] = AssetDatabase.AssetPathToGUID(
                    copy
                );
            }
            foreach (string copy in paths.Values.Where(path => path.EndsWith(".asset")))
            {
                string before = File.ReadAllText(copy),
                    after = before;
                foreach (var guid in guids)
                    after = after.Replace(guid.Key, guid.Value);
                if (before == after)
                    continue;
                File.WriteAllText(copy, after);
                AssetDatabase.ImportAsset(copy, ImportAssetOptions.ForceUpdate);
            }
            return paths;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        [Serializable]
        private sealed class BackendOwnership
        {
            public int Original;
            public int LastWritten;
            public int Pending;
            public bool HasPending;
        }

        private const string BackendPath = "ProjectSettings/PineInput.json";

        private static void ConfigureInput(BuildTarget target)
        {
            var asset = AssetDatabase
                .LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")
                .FirstOrDefault();
            if (asset == null)
                return;
            var settings = new SerializedObject(asset);
            var backend = settings.FindProperty("activeInputHandler");
            if (backend == null)
                throw new InvalidOperationException(
                    "This Editor does not expose the supported input backend setting."
                );
            var owned = File.Exists(BackendPath)
                ? JsonUtility.FromJson<BackendOwnership>(File.ReadAllText(BackendPath))
                : null;
            if (owned != null && owned.HasPending && owned.Pending == backend.intValue)
            {
                // The scalar was committed before an interrupted final ownership write.
                owned.LastWritten = owned.Pending;
                owned.HasPending = false;
                SaveBackendOwnership(owned);
            }
            if (owned != null && owned.LastWritten != backend.intValue)
            {
                owned = null;
                File.Delete(BackendPath);
            }
            int next = backend.intValue;
            if (target == BuildTarget.Android)
            {
                // Revert only Pine's own coexistence change. Unknown Both configurations may
                // serve mixed gameplay; never discard their new-input code by guessing.
                if (next == 2 && owned != null && owned.Original == 0)
                    next = 0;
                else if (next == 2)
                    Debug.LogError(
                        "Pine: Android does not support Both input backends. Select the backend your gameplay uses; Pine preserves an externally configured Both setting."
                    );
            }
            else if (next == 0)
            {
                owned ??= new BackendOwnership { Original = 0 };
                next = 2;
            }
            if (next == backend.intValue)
            {
                if (owned != null && owned.HasPending)
                {
                    owned.HasPending = false;
                    SaveBackendOwnership(owned);
                }
                return;
            }
            // Establish ownership before changing PlayerSettings, so either side of an
            // interrupted write can be recovered without claiming external changes.
            owned.Pending = next;
            owned.HasPending = true;
            SaveBackendOwnership(owned);
            PersistBackend(next);
            backend.intValue = next;
            settings.ApplyModifiedPropertiesWithoutUndo();
            owned.LastWritten = next;
            owned.HasPending = false;
            SaveBackendOwnership(owned);
            SessionState.SetBool(RestartKey, true);
            Debug.Log(
                next == 0
                    ? "Pine restored the original legacy backend for Android. An Editor restart completes the compatible UI fallback."
                    : "Pine enabled input backend coexistence to preserve legacy gameplay. An Editor restart is required."
            );
            if (!Application.isBatchMode)
                EditorApplication.update += RestartWhenSafe;
        }

        private static void SaveBackendOwnership(BackendOwnership owned)
        {
            string temporary = BackendPath + ".pine-tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(owned));
            if (File.Exists(BackendPath))
                File.Replace(temporary, BackendPath, null);
            else
                File.Move(temporary, BackendPath);
        }

        private static void PersistBackend(int value)
        {
            // PlayerSettings is not an imported asset, so SaveAssetIfDirty does not persist it.
            // Change only the supported backend scalar; SaveAssets would save unrelated dirty work.
            const string path = "ProjectSettings/ProjectSettings.asset";
            string before = File.ReadAllText(path);
            var field = new System.Text.RegularExpressions.Regex(
                @"^([ \t]*activeInputHandler:[ \t]*)[012]([ \t]*\r?)$",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
            if (field.Matches(before).Count != 1)
                throw new InvalidOperationException(
                    "This PlayerSettings format cannot be configured safely by Pine."
                );
            string after = field.Replace(
                before,
                match => match.Groups[1].Value + value + match.Groups[2].Value
            );
            if (before == after)
                return;
            string temporary = path + ".pine-tmp";
            File.WriteAllText(temporary, after);
            File.Replace(temporary, path, null);
        }

        private static void RestartWhenSafe()
        {
            if (!SessionState.GetBool(RestartKey, false))
            {
                EditorApplication.update -= RestartWhenSafe;
                return;
            }
            if (
                EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating
                || Enumerable
                    .Range(0, EditorSceneManager.sceneCount)
                    .Any(index => EditorSceneManager.GetSceneAt(index).isDirty)
            )
                return;
            // OpenProject prompts for any unsaved assets/scenes; Pine never discards them.
            SessionState.SetBool(RestartKey, false);
            EditorApplication.update -= RestartWhenSafe;
            EditorApplication.OpenProject(Path.GetFullPath("."));
        }
    }
}
