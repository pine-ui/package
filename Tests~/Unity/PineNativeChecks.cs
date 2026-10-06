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
    public sealed class PineNativeProbe : MonoBehaviour
    {
        public static PineNativeProbe Instance;
        public bool Initialized,
            AwakeReady,
            EnableReady;
        public static int Cleaned;

        public View Create(Value<bool> active)
        {
            Instance = this;
            Initialized = true;
            P.Cleanup(() => Cleaned++);
            return P.Text("Owned", active: active);
        }

        private void Awake() => AwakeReady = Initialized;

        private void OnEnable() => EnableReady = Initialized;
    }

    [InitializeOnLoad]
    public static class PineNativeChecks
    {
        private const string Requested = "Pine.NativeChecks";
        private static IEnumerator _checks;
        private static int _count;
        private static double _deadline;
        private static readonly List<string> _unexpected = new();

        static PineNativeChecks() => EditorApplication.playModeStateChanged += State;

        public static void Run()
        {
            try
            {
                Verify();
                Debug.Log($"PINE_NATIVE_EDIT_PASSED {_count}");
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        public static void RunPlay()
        {
            SessionState.SetBool(Requested, true);
            SessionState.SetInt(Requested + ".Exit", 1);
            EditorApplication.isPlaying = true;
        }

        private static void State(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Requested, false))
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _unexpected.Clear();
                Application.logMessageReceived += Log;
                _checks = Play();
                _deadline = EditorApplication.timeSinceStartup + 90;
                EditorApplication.update += Advance;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Requested, false);
                EditorApplication.Exit(SessionState.GetInt(Requested + ".Exit", 1));
            }
        }

        private static void Advance()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                    throw new TimeoutException();
                if (_checks.MoveNext())
                    return;
                Check(_unexpected.Count == 0, string.Join("\n", _unexpected));
                Debug.Log($"PINE_NATIVE_PLAY_PASSED {_count}");
                SessionState.SetInt(Requested + ".Exit", 0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            EditorApplication.update -= Advance;
            Application.logMessageReceived -= Log;
            (_checks as IDisposable)?.Dispose();
            EditorApplication.isPlaying = false;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
            _count++;
        }

        private static void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Exception && stack.Contains("Pine."))
                _unexpected.Add(message + "\n" + stack);
        }

        private static void Reject(View view, string message)
        {
            bool rejected = false;
            try
            {
                using var mount = P.Mount(view);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Check(rejected, message);
        }

        private static View InputModule()
        {
#if ENABLE_INPUT_SYSTEM
            return P.InputSystemUIInputModule();
#else
            return P.StandaloneInputModule();
#endif
        }

        private static void Verify()
        {
            var count = P.Source(0);
            UnityEngine.Events.UnityEvent click = null;
            Button button = null;
            TMP_Text label = null;
            var recipe = P.Button(onClick: () => count.Value++, reference: b => button = b)
                .With(
                    P.Self(P.Image(color: Color.gray)),
                    P.Shadow(effectDistance: new Vector2(1, -1)),
                    P.Outline(effectColor: Color.black),
                    P.Text(() => $"Count: {count.Value}", reference: t => label = t)
                        .With(P.Outline(effectColor: Color.red))
                );
            Check(button == null, "Declarations defer native creation.");
            using (var mount = P.Mount(P.Vertical().With(recipe)))
            {
                var background = button.GetComponent<Image>();
                Check(
                    button.targetGraphic == background && background.color == Color.gray,
                    "Self configures the wired background."
                );
                Check(
                    button.GetComponents<Shadow>().Length == 2,
                    "Shadow and Outline remain distinct native components."
                );
                Check(
                    button.GetComponent<Outline>().effectColor == Color.black,
                    "Button outline is on the background object."
                );
                Check(
                    label.GetComponent<Outline>().effectColor == Color.red
                        && label.transform.parent == button.transform,
                    "Child modifiers stay on their child object."
                );
                Check(
                    button.GetComponentsInChildren<TMP_Text>().Length == 1,
                    "Custom text creates no empty duplicate caption."
                );
                button.onClick.Invoke();
                click = button.onClick;
                Check(label.text == "Count: 1", "Native click updates a retained text getter.");
                var native = label;
                count.Value = 2;
                Check(
                    label == native && label.text == "Count: 2",
                    "Bindings retain native identity."
                );
                Check(
                    button.colors.Equals(ColorBlock.defaultColorBlock),
                    "Omitted colors keep Unity defaults."
                );
                Check(
                    button.navigation.mode == Navigation.defaultNavigation.mode,
                    "Native navigation is preserved."
                );
                P.ReducedMotion.Value = true;
                Check(
                    button.colors.fadeDuration == ColorBlock.defaultColorBlock.fadeDuration,
                    "Reduced motion does not rewrite native settings."
                );
                P.ReducedMotion.Value = false;
            }
            Check(label == null, "Mount disposal destroys native children.");
            click.Invoke();
            Check(count.Value == 2, "Disposed events are disconnected.");

            var colors = ColorBlock.defaultColorBlock;
            colors.highlightedColor = Color.magenta;
            colors.fadeDuration = .7f;
            using (
                var mount = P.Mount(
                    P.Button(
                        "Save",
                        colors: colors,
                        navigation: new Navigation { mode = Navigation.Mode.None },
                        reference: b => button = b
                    )
                )
            )
            {
                Check(
                    button.colors.Equals(colors) && button.navigation.mode == Navigation.Mode.None,
                    "Native structs pass through unchanged."
                );
                Check(
                    button.GetComponentInChildren<TMP_Text>().text == "Save",
                    "Button caption convenience is wired."
                );
            }
            TMP_InputField input = null;
            Slider slider = null;
            Toggle toggle = null;
            TMP_Dropdown dropdown = null;
            var text = P.Source("first");
            var value = P.Source(3f);
            var selected = P.Source(1);
            var isOn = P.Source(false);
            using (
                var mount = P.Mount(
                    P.Frame()
                        .With(
                            P.InputField(text: text, reference: i => input = i),
                            P.Slider(
                                minValue: 0,
                                maxValue: 10,
                                value: value,
                                reference: s => slider = s
                            ),
                            P.Toggle(isOn: isOn, reference: t => toggle = t),
                            P.Dropdown(
                                options: new List<TMP_Dropdown.OptionData>
                                {
                                    new("One"),
                                    new("Two"),
                                },
                                value: selected,
                                reference: d => dropdown = d
                            )
                        )
                )
            )
            {
                Check(
                    input.textComponent != null
                        && input.placeholder != null
                        && input.textViewport != null,
                    "TMP input required references are wired."
                );
                Check(
                    input.text == "first" && slider.value == 3 && dropdown.value == 1,
                    "Initial native control settings apply in dependency order."
                );
                input.text = "edited";
                slider.value = 8;
                toggle.isOn = true;
                dropdown.value = 0;
                Check(
                    text.Value == "edited" && value.Value == 8 && isOn.Value && selected.Value == 0,
                    "Native edits write back to Source props."
                );
                text.Value = "source";
                value.Value = 20;
                Check(
                    input.text == "source" && slider.value == 10 && value.Value == 10,
                    "Source updates use native validation and normalization."
                );
                Check(
                    slider.fillRect != null && slider.handleRect != null && toggle.graphic != null,
                    "Slider and toggle parts are wired."
                );
                Check(
                    dropdown.template != null
                        && !dropdown.template.gameObject.activeSelf
                        && dropdown.itemText != null,
                    "Dropdown owns an inactive native template."
                );
            }
            using (
                var mount = P.Mount(
                    P.InputField(
                        regexValue: "^[0-9]+$",
                        characterValidation: TMP_InputField.CharacterValidation.Regex,
                        reference: i => input = i
                    )
                )
            )
                Check(
                    (string)
                        typeof(TMP_InputField)
                            .GetField(
                                "m_RegexValue",
                                System.Reflection.BindingFlags.Instance
                                    | System.Reflection.BindingFlags.NonPublic
                            )
                            .GetValue(input) == "^[0-9]+$",
                    "Private serialized TMP regex setting matches the Inspector."
                );

            bool changed = false;
            UnityEngine.Events.UnityEvent<bool> toggleEvent = null;
            using (
                var mount = P.Mount(
                    P.Toggle(
                        toggleTransition: Toggle.ToggleTransition.None,
                        graphic: new Value<Graphic>((Graphic)null),
                        onValueChanged: next => changed = next,
                        reference: t => toggle = t
                    )
                )
            )
            {
                Check(
                    toggle.toggleTransition == Toggle.ToggleTransition.None
                        && toggle.graphic == null,
                    "Native public Inspector fields are named props."
                );
                toggle.isOn = true;
                toggleEvent = toggle.onValueChanged;
                Check(changed, "Public field UnityEvents accept owned callbacks.");
            }
            changed = false;
            toggleEvent.Invoke(true);
            Check(!changed, "Public field event callbacks detach on disposal.");
            using (var mount = P.Mount(P.InputField(isAlert: true, reference: i => input = i)))
                Check(input.isAlert, "Native mutable runtime fields retain their types.");
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.UI.InputSystemUIInputModule module = null;
            using (
                var mount = P.Mount(
                    P.EventSystem()
                        .With(
                            P.InputSystemUIInputModule(
                                sendPointerHoverToParent: false,
                                reference: m => module = m
                            )
                        )
                )
            )
                Check(
                    !(bool)
                        typeof(UnityEngine.EventSystems.BaseInputModule)
                            .GetField(
                                "m_SendPointerHoverToParent",
                                System.Reflection.BindingFlags.Instance
                                    | System.Reflection.BindingFlags.NonPublic
                            )
                            .GetValue(module),
                    "Serialized module Inspector settings are configurable."
                );
#endif
            var first = P.Text("First", name: "First");
            var second = P.Text("Second", name: "Second");
            var rows = P.Source<IReadOnlyList<View>>(new[] { first, second });
            using (var mount = P.Mount(P.Vertical().With(() => rows.Value)))
            {
                var original = mount.Root.transform.GetChild(0);
                rows.Value = new[] { second, first };
                Check(
                    mount.Root.transform.GetChild(1) == original,
                    "Tracked children retain identity when reordered."
                );
                rows.Value = new[] { second };
                Check(
                    mount.Root.transform.childCount == 1 && original == null,
                    "Removed tracked children are disposed."
                );
            }
            using (
                var mount = P.Mount(() =>
                {
                    var items = P.Source<IReadOnlyList<string>>(new[] { "A", "B" });
                    var mapped = P.Values(
                        () => items.Value,
                        (item, index) => P.Text(() => $"{index.Value}:{item}")
                    );
                    P.Cleanup(() => items.Value = Array.Empty<string>());
                    return P.Vertical().With(() => mapped.Value);
                })
            )
                Check(
                    mount.Root.transform.childCount == 2,
                    "Existing keyed operators compose deferred children."
                );

            var context = P.Context("fallback");
            string seen = null;
            using (
                var mount = P.Mount(() =>
                    context.Provide(
                        "provided",
                        () =>
                            P.Button(
                                "Context",
                                onClick: () => seen = context.Value,
                                reference: b => button = b
                            )
                    )
                )
            )
            {
                button.onClick.Invoke();
                Check(seen == "provided", "Deferred callbacks retain their declaration context.");
            }
            foreach (
                var attachment in new[]
                {
                    P.Outline(),
                    P.Self(P.Image()),
                    P.Component<PineNativeProbe>(probe => probe.Create(true)),
                }
            )
            {
                bool rejected = false;
                try
                {
                    attachment.With(() => Array.Empty<View>());
                }
                catch (InvalidOperationException)
                {
                    rejected = true;
                }
                Check(rejected, "Tracked children belong on the containing visual view.");
            }
            bool selfChildrenRejected = false;
            try
            {
                P.Self(P.Image().With(() => Array.Empty<View>()));
            }
            catch (InvalidOperationException)
            {
                selfChildrenRejected = true;
            }
            Check(
                selfChildrenRejected,
                "Self cannot hide a second tracked child group on its parent."
            );
            Reject(P.Outline(), "A modifier cannot be a root.");
            Reject(
                P.Image().With(P.Self(P.RawImage())),
                "Conflicting same-object Graphics fail clearly."
            );
            Reject(
                P.Button().With(P.Outline(), P.Outline()),
                "Duplicate same-object native declarations fail clearly."
            );
            Reject(
                P.Frame().With(() => new[] { first, first }),
                "Duplicate dynamic children fail clearly."
            );

            var owner = P.Text("Owner");
            var extended = owner.With(P.Text("Child"));
            using (var mount = P.Mount(P.Frame().With(owner, extended)))
                Check(
                    mount.Root.transform.GetChild(0).childCount == 0
                        && mount.Root.transform.GetChild(1).childCount == 1,
                    "With is immutable and reusable."
                );
            UnityEngine.Canvas explicitCanvas = null;
            using (
                var mount = P.Mount(
                    P.Canvas(sortingOrder: 7, reference: c => explicitCanvas = c)
                        .With(P.CanvasScaler(scaleFactor: 2), P.Text("Canvas"))
                )
            )
                Check(
                    mount.Canvas == explicitCanvas
                        && mount.Root == explicitCanvas.gameObject
                        && explicitCanvas.sortingOrder == 7,
                    "Explicit Canvas settings do not create a second canvas."
                );

            var factories = typeof(P)
                .GetMethods()
                .Where(m =>
                    m.ReturnType == typeof(View)
                    && m.GetParameters().Any(p => p.Name == "reference")
                    && !m.IsGenericMethod
                    && m.GetParameters().All(p => p.IsOptional)
                )
                .ToArray();
            foreach (var factory in factories)
            {
                Debug.Log("Catalog: " + factory.Name);
                var view = (View)
                    factory.Invoke(
                        null,
                        factory.GetParameters().Select(_ => Type.Missing).ToArray()
                    );
                View root;
                if (factory.Name == "EventSystem")
                    root = view.With(InputModule());
                else if (new[] { "CanvasScaler", "GraphicRaycaster" }.Contains(factory.Name))
                    root = P.Canvas().With(view);
                else if (
                    new[]
                    {
                        "StandaloneInputModule",
                        "InputSystemUIInputModule",
                        "BaseInput",
                    }.Contains(factory.Name)
                )
                    root = P.EventSystem().With(view, InputModule());
                else
                    root = P.Frame().With(view, P.Self(P.Image()));
                if (
                    factory.Name == "InputSystemUIInputModule"
                    || factory.Name == "StandaloneInputModule"
                )
                    root = P.EventSystem().With(view);
                using var mount = P.Mount(root);
                Check(mount.Root != null, "Native catalog factory mounts: " + factory.Name);
            }
        }

        private static IEnumerator Play()
        {
            Check(
                GameObject.Find("Pine Composition") != null,
                "Generated sample App starts automatically."
            );
            var visible = P.Source(false);
            PineNativeProbe.Cleaned = 0;
            var mount = P.Mount(P.Component<PineNativeProbe>(p => p.Create(visible)));
            var probe = PineNativeProbe.Instance;
            Check(
                !mount.Root.activeSelf && !probe.AwakeReady,
                "Owned behaviour honors initial reactive inactivity."
            );
            visible.Value = true;
            Check(
                probe.AwakeReady && probe.EnableReady,
                "Initialization precedes Awake and OnEnable."
            );
            UnityEngine.Object.Destroy(mount.Root);
            yield return null;
            yield return null;
            Check(
                mount.Scope.IsDisposed && PineNativeProbe.Cleaned == 1 && probe == null,
                "Native destruction disposes behaviour and bindings once."
            );
            mount.Dispose();

            TMP_Dropdown dropdown = null;
            using (
                var menu = P.Mount(
                    P.Dropdown(
                        options: new List<TMP_Dropdown.OptionData> { new("One"), new("Two") },
                        reference: d => dropdown = d
                    )
                )
            )
            {
                dropdown.Show();
                yield return null;
                Check(
                    dropdown.transform.Find("Dropdown List") != null,
                    "Native dropdown opens its wired template."
                );
                dropdown.Hide();
                yield return null;
            }
            Source<string> text = P.Source("Inactive");
            int reads = 0;
            using (
                var parent = P.Mount(
                    P.Frame()
                        .With(
                            P.Text(
                                () =>
                                {
                                    reads++;
                                    return text.Value;
                                },
                                active: false,
                                name: "Inactive child"
                            )
                        )
                )
            )
            {
                var child = parent.Root.transform.Find("Inactive child");
                UnityEngine.Object.Destroy(child.gameObject);
                yield return null;
                yield return null;
                int before = reads;
                text.Value = "Changed";
                Check(reads == before, "Never-active destroyed children stop observing sources.");
            }
            var inactive = P.Mount(
                P.Frame(active: false).With(P.Text("Inactive child", active: false))
            );
            UnityEngine.Object.Destroy(inactive.Root);
            yield return null;
            yield return null;
            Check(
                inactive.Scope.IsDisposed,
                "Never-active parent disposal safely removes all child observation records."
            );
            inactive.Dispose();
        }
    }
}
