using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Pine;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Pine.Tests
{
    [InitializeOnLoad]
    internal static class PineProductionChecks
    {
        private const string Requested = "Pine.ProductionChecks";
        private static IEnumerator _checks;
        private static double _deadline;
        private static int _count;
        static PineProductionChecks() => EditorApplication.playModeStateChanged += State;
        [MenuItem("Tools/Pine/Run Production Checks")]
        public static void Run()
        {
            SessionState.SetBool(Requested, true);
            SessionState.SetInt(Requested + ".Exit", 1);
            EditorApplication.isPlaying = true;
        }
        private static void State(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Requested, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            { _checks = Verify(); _deadline = EditorApplication.timeSinceStartup + 120; EditorApplication.update += Advance; }
            else if (state == PlayModeStateChange.EnteredEditMode)
            { SessionState.SetBool(Requested, false); EditorApplication.Exit(SessionState.GetInt(Requested + ".Exit", 1)); }
        }
        private static void Advance()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > _deadline) throw new TimeoutException("Pine production checks timed out.");
                if (_checks.MoveNext()) return;
                Debug.Log($"Pine production checks passed: {_count} assertions.");
                SessionState.SetInt(Requested + ".Exit", 0);
            }
            catch (Exception error) { Debug.LogException(error); }
            EditorApplication.update -= Advance;
            (_checks as IDisposable)?.Dispose(); EditorApplication.isPlaying = false;
        }
        private static IEnumerator Verify()
        {
            var toggled = UI.Source(false); var amount = UI.Source(-0.5f); var input = UI.Source("start"); var selected = UI.Source(1);
            var options = UI.Source(new[] { "A", "B", "C" }); var progress = UI.Source(0.25f); var barValue = UI.Source(0.2f);
            Toggle toggle = null; Slider slider = null; TMP_InputField field = null; TMP_Dropdown dropdown = null; Image fill = null; Scrollbar bar = null; ScrollRect scroll = null;
            using (var mount = UI.Mount(() =>
            {
                toggle = UI.Toggle(toggled, "Toggle", UI.Size(300, 40));
                slider = UI.Slider(amount, -1f, 1f, UI.Size(300, 40));
                field = UI.TextField(input, "Type here", UI.Size(300, 40));
                dropdown = UI.Dropdown(selected, options, UI.Size(300, 40));
                fill = UI.Progress(progress, UI.Size(300, 20));
                bar = UI.Scrollbar(barValue, UI.Size(300, 20));
                scroll = UI.ScrollView(UI.Column(UI.AutoHeight(), UI.FillWidth(), UI.Children(UI.Label("Scrollable", UI.Size(300, 80)))), UI.Size(300, 60));
                return UI.Column(UI.Size(300, 360), UI.Children(toggle, slider, field, dropdown, fill, bar, scroll));
            }))
            {
                yield return null; Canvas.ForceUpdateCanvases();
                Check(toggle.graphic != null && slider.handleRect != null && slider.fillRect != null, "Controls own their required graphics and handles.");
                Check(field.textViewport != null && field.textComponent != null && field.placeholder != null, "Text field is fully wired.");
                Check(dropdown.template != null && dropdown.itemText != null && !dropdown.template.gameObject.activeSelf, "Dropdown has an inactive native template.");
                Check(scroll.viewport.GetComponent<RectMask2D>() != null && scroll.content != null, "Scroll view provides an explicit clipped viewport.");
                Check(Mathf.Approximately(slider.value, -0.5f), "Sliders support negative finite ranges.");
                toggle.isOn = true; slider.value = 0.75f; field.text = "edited"; dropdown.value = 2; bar.value = 0.6f;
                Check(toggled.Value && amount.Value == 0.75f && input.Value == "edited" && selected.Value == 2 && Mathf.Approximately(barValue.Value, 0.6f), "Native events update typed two-way sources.");
                UI.Batch(() => { toggled.Value = false; amount.Value = -1; input.Value = "bound"; selected.Value = 0; progress.Value = 0.8f; });
                Check(!toggle.isOn && slider.value == -1 && field.text == "bound" && dropdown.value == 0 && Mathf.Approximately(fill.fillAmount, 0.8f), "Sources update all native controls.");
                selected.Value = 2; options.Value = new[] { "Only" };
                Check(dropdown.value == 0 && selected.Value == 0, "Changing options keeps two-way selection normalized.");
                mount.Scope.Run(() => UI.Apply(toggle, UI.Focus()));
                Check(EventSystem.current.currentSelectedGameObject == toggle.gameObject, "Focus selects a native control.");
                dropdown.Show(); yield return null;
                Check(dropdown.transform.childCount > 2, "Native dropdown opens its generated list.");
                selected.Value = 0;
                Check(dropdown.IsExpanded, "Selection-only updates preserve the open native menu.");
                options.Value = new[] { "Changed", "Second" };
                Check(!dropdown.IsExpanded && dropdown.options[0].text == "Changed", "Changed choices close stale cloned menu items immediately.");
                foreach (char c in "ÁÉÍÓÖŐÚÜŰáéíóöőúüűŁłŞş€") Check(field.fontAsset.HasCharacter(c), "Bundled Latin font contains " + c);
                field.textComponent.ForceMeshUpdate(); Check(field.textComponent.textInfo.characterCount > 0, "Install-only font produces native text geometry.");
            }
            yield return null;

            RectTransform exact = null, flexible = null, automatic = null; var width = UI.Source(360f);
            using (var mount = UI.Mount(() =>
            {
                exact = UI.Frame(UI.Width(width), UI.Height(48));
                flexible = UI.Frame(UI.FillWidth(), UI.Height(20));
                automatic = UI.Label("Content", UI.AutoWidth(), UI.Height(30)).rectTransform;
                return UI.Column(UI.Size(180, 180), UI.Children(exact, flexible, automatic));
            }))
            {
                yield return null; Canvas.ForceUpdateCanvases();
                Check(Mathf.Approximately(exact.rect.width, 360) && exact.GetComponent<LayoutElement>().minWidth == 360, "Exact sizing survives narrower native layout.");
                Check(exact.parent.GetComponent<RectMask2D>() == null, "Overflow is visible until explicitly clipped.");
                Check(Mathf.Approximately(flexible.rect.width, 180), "Fill uses available parent width.");
                Check(automatic.rect.width > 0, "Auto derives size from native content.");
                width.Value = 420; Canvas.ForceUpdateCanvases(); Check(Mathf.Approximately(exact.rect.width, 420), "Reactive exact size invalidates native layout.");
            }
            var gridChildren = UI.Source<Component[]>(Array.Empty<Component>());
            RectTransform validChild = null, invalidChild = null;
            using (var mount = UI.Mount(() =>
            {
                validChild = UI.Frame(UI.Size(40, 30)); invalidChild = UI.Frame(UI.Size(100, 100));
                gridChildren.Value = new Component[] { validChild };
                return UI.Grid(new Vector2(40, 30), 2, UI.Children(() => gridChildren.Value));
            }))
            {
                bool rejected = false;
                try { gridChildren.Value = new Component[] { invalidChild }; }
                catch (AggregateException) { rejected = true; }
                Check(rejected && validChild.parent == mount.Root.transform && invalidChild.parent == null,
                    "Rejected reactive grid children preserve the prior hierarchy.");
                gridChildren.Value = new Component[] { validChild };
            }
            var cellSize = UI.Source(new Vector2(40, 30)); GridLayoutGroup grid = null;
            using (var mount = UI.Mount(() => grid = UI.Grid(cellSize, 2, UI.Children(UI.Frame(), UI.Frame()))))
            {
                cellSize.Value = new Vector2(50, 35); Canvas.ForceUpdateCanvases();
                Check(grid.cellSize == cellSize.Value && ((RectTransform)grid.transform.GetChild(0)).rect.size == cellSize.Value, "Uniform reactive CellSize controls every cell.");
            }
            bool conflict = false;
            try { using var invalid = UI.Mount(() => UI.Grid(new Vector2(40, 30), 2, UI.Children(UI.Frame(UI.Size(100, 100))))); }
            catch (InvalidOperationException) { conflict = true; }
            Check(conflict, "Conflicting exact grid child size reports an error.");
            var cameraObject = new GameObject("Code camera"); var camera = cameraObject.AddComponent<Camera>();
            using (var mount = UI.Mount(() => UI.Frame(), options: new CanvasOptions { RenderMode = RenderMode.ScreenSpaceCamera, Camera = camera }))
                Check(mount.Canvas.renderMode == RenderMode.ScreenSpaceCamera && mount.Canvas.worldCamera == camera, "Camera mounts are configured in code.");
            using (var mount = UI.Mount(() => UI.Frame(), options: new CanvasOptions { RenderMode = RenderMode.WorldSpace, Camera = camera, WorldSize = new Vector2(200, 100), Scale = 0.01f }))
                Check(mount.Canvas.renderMode == RenderMode.WorldSpace && mount.Canvas.transform.localScale == Vector3.one * 0.01f, "World mounts are configured in code.");
            UnityEngine.Object.Destroy(cameraObject);
            var target = UI.Source(0f); Spring<float> spring = null;
            using (var scope = UI.Root(() => spring = UI.Spring(() => target.Value)))
            {
                UI.ReducedMotion.Value = true; target.Value = 30;
                Check(spring.Value == 30, "Reduced motion snaps springs to their targets."); UI.ReducedMotion.Value = false;
            }
            yield return null;
            var retained = UI.Source(0); int builds = 0, updates = 0, cleanups = 0;
            var lifetime = UI.Mount(() =>
            {
                builds++; UI.Cleanup(() => cleanups++); UI.Effect(() => { retained.Value.ToString(); updates++; });
                return UI.Label(() => retained.Value.ToString());
            });
            retained.Value = 1;
            Check(builds == 1 && updates == 2, "Mount builds once; retained bindings update without rerunning the tree.");
            lifetime.Root.SetActive(false); lifetime.Root.SetActive(true);
            Check(!lifetime.Scope.IsDisposed && builds == 1, "Disabling mounted objects does not dispose or remount their tree.");
            UnityEngine.Object.Destroy(lifetime.Root);
            while (!lifetime.Scope.IsDisposed || lifetime.Canvas != null) yield return null;
            Check(lifetime.Scope.IsDisposed && lifetime.Canvas == null && cleanups == 1, "Root destruction disposes bindings and the owned canvas once.");
            retained.Value = 2; Check(updates == 2, "Destroyed mounts stop observing state."); lifetime.Dispose();
            var inactive = UI.Mount(() => UI.Frame(UI.Active(false)));
            UnityEngine.Object.Destroy(inactive.Root);
            int inactiveFrame = Time.frameCount;
            while (Time.frameCount < inactiveFrame + 3) yield return null;
            Check(inactive.Scope.IsDisposed, "Destroying a never-enabled UI root ends its mount scope.");
            var isolatedScene = UnityEngine.SceneManagement.SceneManager.CreateScene("Pine lifetime check");
            var sceneMount = UI.Mount(() => UI.Label("Scene owned"));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sceneMount.Canvas.gameObject, isolatedScene);
            var unload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(isolatedScene);
            while (!unload.isDone) yield return null;
            Check(sceneMount.Scope.IsDisposed && sceneMount.Root == null, "Scene unload disposes an unretained startup mount.");
            var externalScene = UnityEngine.SceneManagement.SceneManager.CreateScene("External input check");
            var externalInput = new GameObject("External input", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            externalInput.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
#else
            externalInput.AddComponent<StandaloneInputModule>();
#endif
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(externalInput, externalScene);
            using (var persistent = UI.Mount(() => UI.Button("Persistent", () => {})))
            {
                UnityEngine.Object.DontDestroyOnLoad(persistent.Canvas.gameObject);
                Check(!GameObject.Find("Pine Input") && externalInput != null, "Compatible external input is reused without being mutated.");
                var externalUnload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(externalScene);
                while (!externalUnload.isDone) yield return null;
                Check(GameObject.Find("Pine Input") != null && persistent.Root != null,
                    "Persistent UI gets owned input after external input's scene unloads.");
            }
            var benchmark = Benchmark();
            while (benchmark.MoveNext()) yield return benchmark.Current;
        }
        private static IEnumerator Benchmark()
        {
            var counters = Enumerable.Range(0, 100).Select(_ => UI.Source(0)).ToArray(); var target = UI.Source(0f); var timings = new double[240];
            using (var mount = UI.Mount(() =>
            {
                var springs = Enumerable.Range(0, 50).Select(_ => UI.Spring(() => target.Value)).ToArray();
                var labels = Enumerable.Range(0, 300).Select(index => UI.Label(() => counters[index % 100].Value.ToString(), UI.Size(60, 24))).ToArray();
                return UI.Grid(new Vector2(60, 24), 20, UI.Children(labels), UI.Size(1200, 360));
            }))
            {
                yield return null;
                for (int i = 0; i < 120; i++) UI.Step(1.0 / 60);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 300; i++) UI.Step(1.0 / 60);
                long idle = GC.GetAllocatedBytesForCurrentThread() - before;
                Check(idle == 0, "Pine clock allocates zero bytes while idle.");
                var watch = new Stopwatch();
                for (int i = 0; i < timings.Length; i++)
                {
                    watch.Restart();
                    UI.Batch(() => { for (int j = 0; j < counters.Length; j++) counters[j].Value = i; target.Value = i % 2; });
                    UI.Step(1.0 / 60); watch.Stop(); timings[i] = watch.Elapsed.TotalMilliseconds;
                }
                Array.Sort(timings);
                Debug.Log($"Pine Editor workload (not a device release gate): 300 labels, 100 changed sources, 50 springs; idle={idle} bytes; p95={timings[(int)(timings.Length * .95)]:F3} ms. Native layout/render and full-frame time are excluded.");
            }
        }
        private static void Check(bool valid, string description)
        { if (!valid) throw new InvalidOperationException(description); _count++; }
    }
}
