using System;
using System.Collections;
using Pine;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pine.Tests
{
    public sealed class PineCallbackProbe : MonoBehaviour
    {
        public static PineCallbackProbe Instance;
        public static int Destroyed,
            Cleaned,
            Reads;
        public bool Initialized,
            AwakeAfterCreate,
            EnabledAfterCreate;
        public int Updates;
        public Source<int> Count;

        public View Create()
        {
            Instance = this;
            Count = P.Source(value: 5);
            P.Effect(() =>
            {
                _ = Count.Value;
                Reads++;
            });
            P.Cleanup(() => Cleaned++);
            Initialized = true;
            return P.Vertical(spacing: 8, children: new[] { P.Text(() => Count.Value.ToString()) });
        }

        private void Awake() => AwakeAfterCreate = Initialized;

        private void OnEnable() => EnabledAfterCreate = Initialized;

        private void Update() => Updates++;

        private void OnDestroy() => Destroyed++;
    }

    [InitializeOnLoad]
    public static class PineStartupChecks
    {
        private const string Requested = "Pine.StartupChecks";
        private static IEnumerator _checks;
        private static double _deadline;
        private static int _assertions;

        static PineStartupChecks() => EditorApplication.playModeStateChanged += State;

        public static void Run()
        {
            SessionState.SetBool(Requested, true);
            SessionState.SetInt(Requested + ".Exit", 1);
            EditorApplication.isPlaying = true;
        }

        public static void RunWithoutDomainReload()
        {
            SessionState.SetBool(
                Requested + ".OldOptionsEnabled",
                EditorSettings.enterPlayModeOptionsEnabled
            );
            SessionState.SetInt(
                Requested + ".OldOptions",
                (int)EditorSettings.enterPlayModeOptions
            );
            SessionState.SetInt(Requested + ".Repeat", 1);
            SessionState.SetBool(Requested + ".RestoreOptions", true);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Run();
        }

        private static void State(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Requested, false))
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _checks = Verify();
                _deadline = EditorApplication.timeSinceStartup + 90;
                EditorApplication.update += Advance;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (
                    SessionState.GetInt(Requested + ".Repeat", 0) > 0
                    && SessionState.GetInt(Requested + ".Exit", 1) == 0
                )
                {
                    SessionState.SetInt(Requested + ".Repeat", 0);
                    SessionState.SetBool(Requested + ".RestoreOptions", true);
                    EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
                    return;
                }

                if (SessionState.GetBool(Requested + ".RestoreOptions", false))
                {
                    EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(
                        Requested + ".OldOptionsEnabled",
                        false
                    );
                    EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)
                        SessionState.GetInt(Requested + ".OldOptions", 0);
                    SessionState.SetBool(Requested + ".RestoreOptions", false);
                }

                SessionState.SetBool(Requested, false);
                EditorApplication.Exit(SessionState.GetInt(Requested + ".Exit", 1));
            }
        }

        private static void Advance()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                    throw new TimeoutException("Startup checks timed out.");
                if (_checks.MoveNext())
                    return;
                Debug.Log($"Pine startup checks passed: {_assertions} assertions.");
                SessionState.SetInt(Requested + ".Exit", 0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }

            EditorApplication.update -= Advance;
            (_checks as IDisposable)?.Dispose();
            EditorApplication.isPlaying = false;
        }

        private static void Check(bool condition, string message)
        {
            _assertions++;
            if (!condition)
                throw new Exception(message);
        }

        private static IEnumerator Verify()
        {
            _assertions = 0;
            // Import the Composition sample into Assets before running this check.
            var application = GameObject.Find("Pine Composition");
            Check(
                application != null,
                "App.cs starts automatically without a scene owner or explicit caller."
            );
            Check(
                application.GetComponentInParent<Canvas>().gameObject.scene.name
                    == "DontDestroyOnLoad",
                "Application canvas persists by default."
            );
            Check(
                application.GetComponentsInChildren<MonoBehaviour>().Length >= 4,
                "Generated factories create nested Unity behaviours."
            );
            PineCallbackProbe.Destroyed = PineCallbackProbe.Cleaned = PineCallbackProbe.Reads = 0;
            RectTransform view = null;
            var mount = P.Mount(component: () =>
                P.Component<PineCallbackProbe>(p =>
                {
                    var declaration = p.Create();
                    view = (RectTransform)p.transform;
                    return declaration;
                })
            );
            view = mount.Root.GetComponent<RectTransform>();
            var instance = PineCallbackProbe.Instance;
            var count = instance.Count;
            try
            {
                Check(
                    instance.AwakeAfterCreate && instance.EnabledAfterCreate,
                    "Props and UI initialize before Awake and OnEnable."
                );
                Check(
                    view == mount.Root.GetComponent<RectTransform>()
                        && instance.transform.parent == view,
                    "Composition retains the renderer's native root."
                );
                int frame = Time.frameCount;
                while (Time.frameCount < frame + 3)
                    yield return null;
                Check(instance.Updates > 0, "Unity Update runs on a declarative component.");
                view.gameObject.SetActive(false);
                int updates = instance.Updates;
                frame = Time.frameCount;
                while (Time.frameCount < frame + 3)
                    yield return null;
                count.Value = 6;
                Check(
                    instance.Updates == updates
                        && view.GetComponentInChildren<TMP_Text>(true).text == "6",
                    "Inactive views stop Unity Update while retaining reactive state."
                );
                view.gameObject.SetActive(true);
                frame = Time.frameCount;
                while (Time.frameCount < frame + 3)
                    yield return null;
                Check(
                    instance.Updates > updates,
                    "Re-enabling retains the same component and resumes callbacks."
                );
                UnityEngine.Object.Destroy(view.gameObject);
                frame = Time.frameCount;
                while (Time.frameCount < frame + 3)
                    yield return null;
                Check(
                    mount.Scope.IsDisposed
                        && instance == null
                        && PineCallbackProbe.Destroyed == 1
                        && PineCallbackProbe.Cleaned == 1,
                    "Destroying the view ends both Unity and reactive lifetimes once."
                );
                int reads = PineCallbackProbe.Reads;
                count.Value = 7;
                Check(
                    PineCallbackProbe.Reads == reads,
                    "Destroyed components stop observing sources."
                );
            }
            finally
            {
                mount.Dispose();
            }

            var scene = SceneManager.CreateScene("Pine scene-lived check");
            var sceneMount = P.Mount(
                component: () => P.Frame(),
                options: new CanvasOptions { Persistent = false }
            );
            SceneManager.MoveGameObjectToScene(sceneMount.Canvas.gameObject, scene);
            var unload = SceneManager.UnloadSceneAsync(scene);
            while (!unload.isDone)
                yield return null;
            Check(
                sceneMount.Scope.IsDisposed && sceneMount.Root == null && application != null,
                "Scene-lived opt-out cleans up while the default app survives."
            );
            var external = new GameObject("External parent", typeof(RectTransform));
            bool rejected = false;
            try
            {
                P.Mount(
                    component: () => P.Frame(),
                    parent: external.transform,
                    options: new CanvasOptions()
                );
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Check(
                rejected && external.transform.parent == null,
                "Persistence never reparents an externally owned parent."
            );
            using (var attached = P.Mount(component: () => P.Frame(), parent: external.transform))
                Check(
                    attached.Root.transform.parent == external.transform,
                    "External-parent mounting remains available without taking persistence ownership."
                );
            UnityEngine.Object.Destroy(external);
        }
    }
}
