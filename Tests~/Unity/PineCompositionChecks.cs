using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pine;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Pine.Tests
{
    [InitializeOnLoad]
    internal static class PineCompositionChecks
    {
        private const string Requested = "Pine.CompositionChecks";
        private static IEnumerator _checks;
        private static double _deadline;
        private static int _assertions;
        private static int _builds;
        private static readonly Dictionary<string, Source<int>> _counts = new();
        private static readonly Dictionary<string, int> _reads = new();
        private static readonly List<string> _cleaned = new();
        static PineCompositionChecks() => EditorApplication.playModeStateChanged += State;

        [MenuItem("Tools/Pine/Run Composition Checks")]
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
            {
                _checks = Verify(); _deadline = EditorApplication.timeSinceStartup + 90;
                EditorApplication.update += Advance;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Requested, false);
                EditorApplication.Exit(SessionState.GetInt(Requested + ".Exit", 1));
            }
        }
        private static void Advance()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > _deadline) throw new TimeoutException("Composition checks timed out.");
                if (_checks.MoveNext()) return;
                Debug.Log($"Pine composition checks passed: {_assertions} assertions.");
                SessionState.SetInt(Requested + ".Exit", 0);
            }
            catch (Exception error) { Debug.LogException(error); }
            EditorApplication.update -= Advance;
            (_checks as IDisposable)?.Dispose(); EditorApplication.isPlaying = false;
        }
        private static RectTransform Counter(string name, Source<int> supplied = null)
        {
            _builds++;
            var count = supplied ?? UI.Source(0); _counts[name] = count;
            UI.Effect(() => { _ = count.Value; _reads[name] = _reads.TryGetValue(name, out int reads) ? reads + 1 : 1; });
            UI.Cleanup(() => _cleaned.Add(name));
            var root = UI.Column(6,
                UI.Label(() => $"{name}: {count.Value}", UI.Size(180, 24)),
                UI.Button("Increment", () => count.Value++, UI.Size(180, 32)));
            UI.Apply(root, UI.Name(name)); return root;
        }
        private static RectTransform Card(params Component[] children) => UI.Row(4, UI.Column(2, children));
        private static string Text(Component root) => root.GetComponentInChildren<TMP_Text>(true).text;
        private static void Click(Component root) => root.GetComponentInChildren<Button>(true).onClick.Invoke();
        private static void Check(bool condition, string message)
        { _assertions++; if (!condition) throw new Exception(message); }

        private static IEnumerator Verify()
        {
            _assertions = 0; _builds = 0; _counts.Clear(); _reads.Clear(); _cleaned.Clear();
            var shared = UI.Source(5); var visible = UI.Source(true);
            var items = UI.Source(new[] { new KeyValuePair<int, string>(1, "One"), new KeyValuePair<int, string>(2, "Two") });
            RectTransform first = null, second = null, sharedA = null, sharedB = null, temporary = null, persistent = null, list = null;
            var rows = new Dictionary<int, RectTransform>();
            var before = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length;
            var mount = UI.Mount(() =>
            {
                first = Counter("First"); second = Counter("Second");
                sharedA = Counter("Shared A", shared); sharedB = Counter("Shared B", shared);
                var conditional = UI.Show(() => visible.Value, () => temporary = Counter("Temporary"));
                var keptState = UI.Show(() => visible.Value, () => persistent = Counter("Persistent", shared));
                var keyed = UI.Indexes(() => items.Value, (key, value, present) =>
                {
                    var row = UI.Column(3, UI.Label(() => value.Value), Counter("Item " + key));
                    rows[key] = row; return new Branch<RectTransform>(row);
                });
                list = UI.Column(UI.Children(() => keyed.Value));
                return UI.Column(12, Card(first, second), sharedA, sharedB,
                    UI.Column(UI.Children(() => conditional.Value)),
                    UI.Column(UI.Children(() => keptState.Value)), list);
            });
            try
            {
                yield return null;
                Check(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length == before + 1, "Nested component factories must create only one mounted canvas.");
                Check(mount.Root.GetComponent<VerticalLayoutGroup>().spacing == 12, "Column shorthand must apply the supplied gap.");
                Check(mount.Root.transform.GetChild(0).GetComponent<HorizontalLayoutGroup>().spacing == 4, "Row shorthand must apply the supplied gap.");
                Check(first.parent == second.parent && first.GetSiblingIndex() == 0 && second.GetSiblingIndex() == 1, "Custom containers must accept and preserve caller-provided child order.");
                int built = _builds;
                Click(first);
                Check(Text(first) == "First: 1" && Text(second) == "Second: 0", "Repeated factories must have independent local state.");
                Check(_builds == built, "State changes must update bindings without rerunning factories.");
                Click(sharedA);
                Check(Text(sharedA) == "Shared A: 6" && Text(sharedB) == "Shared B: 6", "Explicitly supplied sources must update every sharing instance.");
                mount.Scope.Run(() => UI.Apply(first, UI.Active(false)));
                _counts["First"].Value = 2;
                Check(!first.gameObject.activeSelf && Text(first) == "First: 2" && !_cleaned.Contains("First"), "Hiding must retain state and bindings without cleanup.");
                mount.Scope.Run(() => UI.Apply(first, UI.Active(true)));
                Check(Text(first) == "First: 2" && _builds == built, "Showing a retained instance must not rebuild it.");

                Click(temporary); var oldTemporary = temporary; var oldPersistent = persistent;
                var oldState = _counts["Temporary"]; int oldReads = _reads["Temporary"];
                visible.Value = false;
                Check(_cleaned.Contains("Temporary") && _cleaned.Contains("Persistent"), "Removing conditional components must dispose their owned scopes.");
                oldState.Value = 99;
                Check(_reads["Temporary"] == oldReads, "Removed components must stop observing their old state.");
                yield return null;
                Check(oldTemporary == null && oldPersistent == null, "Removal must destroy owned native component roots.");
                visible.Value = true;
                Check(Text(temporary) == "Temporary: 0" && _counts["Temporary"] != oldState, "Reconstruction must create fresh local state.");
                Check(Text(persistent) == "Persistent: 6", "Externally supplied state must survive removal and reconstruction.");

                var one = rows[1]; var two = rows[2];
                Click(one.GetComponentsInChildren<RectTransform>().Single(rect => rect.name == "Item 1"));
                int beforeReorder = _builds;
                items.Value = new[] { new KeyValuePair<int, string>(2, "Two updated"), new KeyValuePair<int, string>(1, "One updated") };
                Check(rows[1] == one && rows[2] == two && _builds == beforeReorder, "Stable keys must retain component instances across reordering and value updates.");
                Check(list.GetChild(0) == two && list.GetChild(1) == one, "Retained native rows must follow the new display order.");
                Check(Text(two) == "Two updated" && one.GetComponentsInChildren<TMP_Text>().Any(text => text.text == "Item 1: 1"), "Reordered rows must update data while retaining local state.");
                items.Value = new[] { new KeyValuePair<int, string>(2, "Two updated") };
                Check(_cleaned.Contains("Item 1"), "Removing a keyed item must dispose its component scope.");
                yield return null;
                Check(one == null, "Removed keyed rows must release their native subtree.");
                items.Value = new[] { new KeyValuePair<int, string>(1, "One again"), new KeyValuePair<int, string>(2, "Two updated") };
                Check(_counts["Item 1"].Value == 0 && rows[2] == two, "Reinserted items must reset local state without rebuilding retained neighbours.");
            }
            finally { mount.Dispose(); }
            yield return null;
            Check(first == null && second == null && list == null, "Disposing the sole mount must release all remaining nested objects.");
            Check(_cleaned.Contains("First") && _cleaned.Contains("Second") && _cleaned.Contains("Shared A") && _cleaned.Contains("Item 2"), "Mount teardown must run cleanup throughout its composed tree.");
        }
    }
}
