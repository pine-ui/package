using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Pine;
using UnityEngine;
using Unity.Profiling;
using Debug = UnityEngine.Debug;

public sealed class PinePlayerProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    private static void StartProbe() { if (Application.isEditor) return; Application.runInBackground = true; Debug.Log("Pine player probe starting."); new GameObject("Pine player probe").AddComponent<PinePlayerProbe>(); }
    [Serializable]
    private sealed class Report
    {
        public string unity, processor, graphics;
        public int memoryMiB, width, height, controls, changedBindings, springs;
        public long idlePineBytes, drawCalls = -1, batches = -1, setPassCalls = -1;
        public double pineMedianMs, pineP95Ms, frameMedianMs, frameP95Ms;
        public string mode = "Controlled runtime Update plus synchronous bindings; native layout/render in frame timing";
        public int warmupFrames = 120, sampleFrames = 600;
    }
    private IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        var values = Enumerable.Range(0, 100).Select(_ => UI.Source(0)).ToArray();
        var target = UI.Source(0f);
        var samples = new double[600];
        var frameTimes = new double[600];
        using (var mount = UI.Mount(() =>
        {
            var controls = new Component[300];
            for (int i = 0; i < controls.Length; i++)
            {
                int index = i;
                var button = i < 100
                    ? UI.Button(() => values[index].Value.ToString(), () => values[index].Value++)
                    : UI.Button(i.ToString(), () => {});
                if (i < 50)
                {
                    var spring = UI.Spring(() => target.Value, period: 0.4, dampingRatio: 0.8);
                    UI.Apply(button, UI.Children(
                        button.GetComponentInChildren<TMPro.TextMeshProUGUI>(),
                        UI.Image(UI.Size(4, 4), UI.Tint(Color.white), UI.Position(() => new Vector2(spring.Value, -8)))));
                }
                controls[i] = button;
            }
            return UI.Grid(new Vector2(60, 32), 20, UI.Size(1200, 480), UI.Children(controls));
        }, options: new CanvasOptions { ReferenceResolution = new Vector2(1280, 720) }))
        {
            var host = GameObject.Find("Pine Runtime").GetComponents<MonoBehaviour>().Single();
            host.enabled = false;
            var update = (Action)Delegate.CreateDelegate(typeof(Action), host, host.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic));
            for (int i = 0; i < 120; i++) { update(); yield return null; }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 300; i++) update();
            long idle = GC.GetAllocatedBytesForCurrentThread() - before;
            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);
            using var passes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
            var watch = new Stopwatch();
            for (int frame = 0; frame < samples.Length; frame++)
            {
                watch.Restart();
                UI.Batch(() =>
                {
                    for (int i = 0; i < values.Length; i++) values[i].Value = frame + 1;
                    target.Value = frame % 2 == 0 ? -20 : 20;
                });
                update(); watch.Stop();
                samples[frame] = watch.Elapsed.TotalMilliseconds;
                frameTimes[frame] = Time.unscaledDeltaTime * 1000;
                yield return null;
            }
            Array.Sort(samples); Array.Sort(frameTimes);
            string result = JsonUtility.ToJson(new Report
            {
                unity = Application.unityVersion, processor = SystemInfo.processorType,
                memoryMiB = SystemInfo.systemMemorySize, graphics = SystemInfo.graphicsDeviceName,
                width = Screen.width, height = Screen.height, controls = 300, changedBindings = 100,
                springs = 50, idlePineBytes = idle, drawCalls = draws.Valid ? draws.LastValue : -1,
                batches = batches.Valid ? batches.LastValue : -1, setPassCalls = passes.Valid ? passes.LastValue : -1, pineMedianMs = samples[300], pineP95Ms = samples[570], frameMedianMs = frameTimes[300], frameP95Ms = frameTimes[570]
            });
            string path = Environment.GetEnvironmentVariable("PINE_PROBE_RESULT");
            if (string.IsNullOrEmpty(path)) path = Path.Combine(Application.persistentDataPath, "pine-probe.json");
            File.WriteAllText(path, result); Debug.Log(result);
            Application.Quit(idle == 0 && samples[570] <= 1 ? 0 : 1);
        }
    }
}
