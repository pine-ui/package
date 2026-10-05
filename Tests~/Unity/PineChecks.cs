using System;
using System.Collections;
using System.Collections.Generic;
using Pine;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pine.Verification
{
    [InitializeOnLoad]
    public static class PineChecks
    {
        private const string PlayRequested = "PineChecks.PlayRequested";
        private const string ExitCode = "PineChecks.ExitCode";
        private static IEnumerator _playChecks;
        private static double _deadline;

        static PineChecks() => EditorApplication.playModeStateChanged += PlayStateChanged;

        [MenuItem("Tools/Pine/Run Adapter Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Start Pine checks from Edit Mode.");
            try
            {
                AdapterChecks();
                Debug.Log("Pine adapter checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void AdapterChecks()
        {
            Source<string> caption = UI.Source("before");
            TextMeshProUGUI label = null;
            Mount mount = UI.Mount(() => label = UI.Label(caption));
            try
            {
                Check(mount.Canvas != null && mount.Root.GetComponentInParent<Canvas>() == mount.Canvas, "Mount owns a Canvas and parents its root.");
                caption.Value = "after";
                Check(label.text == "after", "Reactive text updates the native component.");
                Check(UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null, "Mount supplies or reuses an EventSystem.");
            }
            finally { mount.Dispose(); }
            Check(mount.Scope.IsDisposed && mount.Root == null && mount.Canvas == null, "Disposal removes the mounted objects.");

            var size = UI.Source(new Vector2(360, 180));
            RectTransform frame = null;
            TextMeshProUGUI child = null;
            int builds = 0;
            mount = UI.Mount(() =>
            {
                builds++;
                frame = UI.Frame(UI.Name("Saved frame"));
                child = UI.Label("Saved child");
                return UI.Apply(frame, UI.Size(size), UI.Children(child));
            });
            try
            {
                Check(frame == mount.Root.GetComponent<RectTransform>() && child.transform.parent == frame,
                    "Saved builder results are the mounted native components.");
                size.Value = new Vector2(480, 240);
                Check(frame.sizeDelta == size.Value && builds == 1,
                    "A reactive Apply binding updates the saved component without rebuilding it.");
                mount.Scope.Run(() => UI.Apply(frame, UI.Name("Updated frame")));
                Check(frame.name == "Updated frame", "Apply can configure a saved component in its live scope.");
                frame.anchoredPosition = new Vector2(12, 24);
                Check(frame.anchoredPosition == new Vector2(12, 24), "Saved native properties remain directly accessible.");
            }
            finally { mount.Dispose(); }
            Check(frame == null && child == null, "Scope disposal destroys its saved native objects.");

            GameObject external = new("Pine event check", typeof(RectTransform), typeof(Button));
            try
            {
                Button button = external.GetComponent<Button>();
                int clicks = 0;
                Scope scope = UI.Root(() => UI.Apply(button, UI.OnClick(() => clicks++)));
                try { button.onClick.Invoke(); Check(clicks == 1, "Native events invoke bindings."); }
                finally { scope.Dispose(); }
                button.onClick.Invoke();
                Check(clicks == 1 && button != null, "Apply unregisters events without destroying the external object.");
            }
            finally { UnityEngine.Object.DestroyImmediate(external); }
        }

        // Batch entry point: do not pass -quit; the state callbacks exit after checks finish.
        [MenuItem("Tools/Pine/Run Play Mode Checks")]
        public static void RunPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Start Pine checks from Edit Mode.");
            SessionState.SetBool(PlayRequested, true);
            SessionState.SetInt(ExitCode, 1);
            EditorApplication.isPlaying = true;
        }

        private static void PlayStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayRequested, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _playChecks = PlayChecks();
                _deadline = EditorApplication.timeSinceStartup + 15;
                EditorApplication.update += AdvancePlayChecks;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= AdvancePlayChecks;
                (_playChecks as IDisposable)?.Dispose();
                _playChecks = null;
                SessionState.SetBool(PlayRequested, false);
                if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(ExitCode, 1));
            }
        }

        private static void AdvancePlayChecks()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > _deadline) throw new TimeoutException("Pine Play Mode checks timed out.");
                if (_playChecks.MoveNext()) return;
                SessionState.SetInt(ExitCode, 0);
                Debug.Log("Pine Play Mode checks passed.");
            }
            catch (Exception error) { Debug.LogException(error); }
            EditorApplication.update -= AdvancePlayChecks;
            (_playChecks as IDisposable)?.Dispose();
            _playChecks = null;
            EditorApplication.isPlaying = false;
        }

        private static IEnumerator PlayChecks()
        {
            var target = UI.Source(0f);
            Spring<float> spring = null;
            Button button = null;
            TextMeshProUGUI label = null;
            Mount mount = UI.Mount(() =>
            {
                spring = UI.Spring(() => target.Value, period: 0.1);
                label = UI.Label("Pine geometry", UI.Size(400, 50));
                button = UI.Button("Pine raycast", () => { }, UI.Size(400, 50));
                return UI.Column(UI.Size(400, 200), UI.Children(label, button));
            });
            try
            {
                target.Value = 10;
                int frame = Time.frameCount;
                while (Time.frameCount < frame + 3) yield return null;
                Check(spring.Value > 0 && spring.Value <= 10, "RuntimeHost automatically advances springs.");
                Canvas.ForceUpdateCanvases();
                label.ForceMeshUpdate();
                Check(label.textInfo.characterCount > 0 && label.textInfo.meshInfo[0].vertexCount > 0, "TMP creates text geometry.");
                Check(((RectTransform)button.transform).rect.height > 0, "Native layout gives the button a size.");
                var hits = new List<RaycastResult>();
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
                };
                EventSystem.current.RaycastAll(pointer, hits);
                Check(hits.Exists(hit => hit.gameObject == button.gameObject), "The mounted button receives uGUI raycasts.");
                UnityEngine.Object.Destroy(mount.Root);
                frame = Time.frameCount;
                while (Time.frameCount < frame + 2) yield return null;
                Check(mount.Scope.IsDisposed, "Destroying the root disposes its scope.");
            }
            finally { mount.Dispose(); }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
