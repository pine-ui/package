using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pine;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Pine.Tests
{
    [InitializeOnLoad]
    internal static class PineInteractionChecks
    {
        private const string Request = "Pine.InteractionChecks";
        private static IEnumerator _checks;
        private static int _count;
        private static double _deadline;
        private static readonly List<string> _errors = new();

        static PineInteractionChecks() => EditorApplication.playModeStateChanged += State;

        public static void Run()
        {
            SessionState.SetBool(Request, true);
            SessionState.SetInt(Request + ".Exit", 1);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }

        private static void State(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Request, false))
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _count = 0;
                _errors.Clear();
                Application.logMessageReceived += Log;
                _checks = Verify();
                _deadline = EditorApplication.timeSinceStartup + 90;
                EditorApplication.update += Advance;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Request, false);
                EditorApplication.Exit(SessionState.GetInt(Request + ".Exit", 1));
            }
        }

        private static void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
                _errors.Add(message + "\n" + stack);
        }

        private static void Advance()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                    throw new TimeoutException("Interaction check timed out.");
                if (_checks.MoveNext())
                    return;
                Check(_errors.Count == 0, string.Join("\n", _errors));
                Debug.Log(
                    $"PINE_INTERACTION_RENDER_PASSED {_count} Unity {Application.unityVersion}"
                );
                SessionState.SetInt(Request + ".Exit", 0);
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

        private static IEnumerator Frames(int count = 2)
        {
            int target = Time.frameCount + count;
            while (Time.frameCount < target)
                yield return null;
        }

        private static IEnumerable<object> Wait(int count = 2)
        {
            var frames = Frames(count);
            while (frames.MoveNext())
                yield return null;
        }

        private static Vector2 Center(Component target, Camera camera) =>
            RectTransformUtility.WorldToScreenPoint(
                camera,
                ((RectTransform)target.transform).TransformPoint(
                    ((RectTransform)target.transform).rect.center
                )
            );

        private static void MouseState(
            Mouse mouse,
            Vector2 position,
            bool down = false,
            Vector2 scroll = default
        )
        {
            InputSystem.QueueStateEvent(
                mouse,
                new UnityEngine.InputSystem.LowLevel.MouseState
                {
                    position = position,
                    scroll = scroll,
                }.WithButton(MouseButton.Left, down)
            );
        }

        private static View Caption(string text) =>
            P.Text(
                text,
                color: Color.white,
                fontSize: 20,
                alignment: TextAlignmentOptions.Center,
                anchorMin: Vector2.zero,
                anchorMax: Vector2.one,
                sizeDelta: Vector2.zero,
                raycastTarget: false
            );

        private static IEnumerator Verify()
        {
            var sample = GameObject.Find("Pine Composition");
            Check(sample != null, "Generated sample App starts before the interaction scenario.");
            UnityEngine.Object.Destroy(sample);
            foreach (var _ in Wait())
                yield return null;

            var cameraObject = new GameObject("Interaction camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var texture = new RenderTexture(1024, 768, 24);
            camera.targetTexture = texture;
            var oldBackground = InputSystem.settings.backgroundBehavior;
            var oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings
                .EditorInputBehaviorInPlayMode
                .AllDeviceInputAlwaysGoesToGameView;
            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var button = P.Ref<Button>();
            var toggle = P.Ref<Toggle>();
            var slider = P.Ref<Slider>();
            var input = P.Ref<TMP_InputField>();
            var dropdown = P.Ref<TMP_Dropdown>();
            var scroll = P.Ref<ScrollRect>();
            var handle = P.Ref<Graphic>();
            var clicks = P.Source(0);
            var enabled = P.Source(false);
            var volume = P.Source(.25f);
            var text = P.Source("");
            var selected = P.Source(0);
            Mount mount = null;
            try
            {
                mount = P.Mount(
                    P.Canvas(
                        name: "Pine 1.1.0 interaction",
                        renderMode: RenderMode.ScreenSpaceCamera,
                        worldCamera: camera,
                        planeDistance: 5,
                        sortingOrder: 100,
                        children: new[]
                        {
                            P.CanvasScaler(uiScaleMode: CanvasScaler.ScaleMode.ConstantPixelSize),
                            P.Image(
                                color: new Color(.07f, .1f, .13f),
                                sizeDelta: new Vector2(960, 720),
                                raycastTarget: false
                            ),
                            P.Text(
                                "Pine 1.1.0 · native uGUI",
                                color: Color.white,
                                fontSize: 30,
                                alignment: TextAlignmentOptions.Center,
                                anchoredPosition: new Vector2(0, 315),
                                sizeDelta: new Vector2(700, 60)
                            ),
                            P.Button(
                                "Increment",
                                caption: Caption("Increment"),
                                reference: button,
                                onClick: () => clicks.Value++,
                                anchoredPosition: new Vector2(-240, 235),
                                sizeDelta: new Vector2(400, 48),
                                navigation: new Value<Navigation>(() =>
                                    new Navigation
                                    {
                                        mode = Navigation.Mode.Explicit,
                                        selectOnDown = toggle.Value,
                                    }
                                ),
                                children: new[]
                                {
                                    P.Self(P.Image(color: new Color(.16f, .38f, .29f))),
                                }
                            ),
                            P.Toggle(
                                "Enabled",
                                caption: Caption("Enabled"),
                                reference: toggle,
                                isOn: enabled,
                                anchoredPosition: new Vector2(-240, 160),
                                sizeDelta: new Vector2(400, 48),
                                graphic: P.Image(
                                    color: Color.green,
                                    sizeDelta: new Vector2(20, 20),
                                    anchoredPosition: new Vector2(-150, 0)
                                ),
                                children: new[]
                                {
                                    P.Self(P.Image(color: new Color(.2f, .25f, .3f))),
                                }
                            ),
                            P.Slider(
                                reference: slider,
                                value: volume,
                                minValue: 0,
                                maxValue: 1,
                                anchoredPosition: new Vector2(-240, 85),
                                sizeDelta: new Vector2(400, 48),
                                fill: P.Image(
                                    color: new Color(.2f, .7f, .4f),
                                    anchorMin: new Vector2(0, .25f),
                                    anchorMax: new Vector2(1, .75f),
                                    sizeDelta: Vector2.zero
                                ),
                                handle: P.Image(
                                    reference: handle,
                                    color: Color.white,
                                    sizeDelta: new Vector2(18, 42)
                                ),
                                targetGraphic: handle,
                                children: new[]
                                {
                                    P.Self(P.Image(color: new Color(.2f, .25f, .3f))),
                                }
                            ),
                            P.InputField(
                                reference: input,
                                text: text,
                                anchoredPosition: new Vector2(-240, 10),
                                sizeDelta: new Vector2(400, 48)
                            ),
                            P.Dropdown(
                                reference: dropdown,
                                value: selected,
                                options: new List<TMP_Dropdown.OptionData>
                                {
                                    new("First option"),
                                    new("Second option"),
                                },
                                anchoredPosition: new Vector2(-240, -65),
                                sizeDelta: new Vector2(400, 48)
                            ),
                            P.ScrollRect(
                                reference: scroll,
                                horizontal: false,
                                anchoredPosition: new Vector2(240, 120),
                                sizeDelta: new Vector2(400, 240),
                                content: P.Vertical(
                                    name: "Rows",
                                    childControlWidth: true,
                                    childControlHeight: true,
                                    childForceExpandHeight: false,
                                    spacing: 8,
                                    pivot: new Vector2(.5f, 1),
                                    anchorMin: new Vector2(0, 1),
                                    anchorMax: Vector2.one,
                                    sizeDelta: new Vector2(0, 640),
                                    children: Enumerable
                                        .Range(1, 10)
                                        .Select(i =>
                                            P.Text(
                                                "Scroll row " + i,
                                                fontSize: 24,
                                                color: Color.white,
                                                children: new[]
                                                {
                                                    P.LayoutElement(preferredHeight: 56),
                                                }
                                            )
                                        )
                                        .ToArray()
                                ),
                                children: new[]
                                {
                                    P.Self(P.Image(color: new Color(.14f, .18f, .22f))),
                                }
                            ),
                            P.Text(
                                () =>
                                    $"Clicks: {clicks.Value}\nToggle: {enabled.Value}\nSlider: {volume.Value:F2}\nText: {text.Value}\nOption: {selected.Value}",
                                color: Color.white,
                                fontSize: 24,
                                anchoredPosition: new Vector2(220, -170),
                                sizeDelta: new Vector2(440, 220)
                            ),
                        }
                    )
                );
                foreach (var _ in Wait(4))
                    yield return null;
                Check(
                    button.Value.GetComponentInParent<Canvas>().worldCamera == camera,
                    "Canvas and supplied camera are wired before interaction."
                );
                var position = Center(button.Value, camera);
                MouseState(mouse, position);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position, true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position);
                foreach (var _ in Wait())
                    yield return null;
                Check(
                    clicks.Value == 1,
                    "Queued mouse input raycasts and clicks the native button. Position="
                        + position
                        + "; module="
                        + EventSystem.current.currentInputModule
                        + "; point="
                        + (
                            (UnityEngine.InputSystem.UI.InputSystemUIInputModule)
                                EventSystem.current.currentInputModule
                        ).point.action.ReadValue<Vector2>()
                );

                position = Center(toggle.Value, camera);
                MouseState(mouse, position, true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position);
                foreach (var _ in Wait())
                    yield return null;
                Check(
                    enabled.Value,
                    "Pointer interaction toggles native state and writes its Source."
                );

                position = Center(slider.Value, camera);
                MouseState(mouse, position + new Vector2(-100, 0), true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position + new Vector2(130, 0), true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position + new Vector2(130, 0));
                foreach (var _ in Wait())
                    yield return null;
                Check(
                    volume.Value > .7f && Mathf.Approximately(volume.Value, slider.Value.value),
                    "Native pointer dragging updates a custom slider hierarchy."
                );

                position = Center(input.Value, camera);
                MouseState(mouse, position, true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position);
                foreach (var _ in Wait())
                    yield return null;
                Check(
                    input.Value.isFocused,
                    "Pointer interaction focuses the native TMP input field."
                );
                foreach (var character in "Pine")
                {
                    EditorGUIUtility.QueueGameViewInputEvent(
                        new Event { type = EventType.KeyDown, character = character }
                    );
                    foreach (var _ in Wait())
                        yield return null;
                }
                Check(
                    text.Value == "Pine",
                    "Queued native text events edit the TMP input and its Source."
                );

                position = Center(dropdown.Value, camera);
                MouseState(mouse, position);
                foreach (var _ in Wait())
                    yield return null;
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(
                    new PointerEventData(EventSystem.current) { position = position },
                    hits
                );
                Check(
                    hits.Count > 0
                        && hits[0].gameObject.GetComponentInParent<TMP_Dropdown>()
                            == dropdown.Value,
                    "Dropdown pointer reaches its native Graphic: "
                        + string.Join(",", hits.Select(h => h.gameObject.name))
                );
                MouseState(mouse, position, true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position);
                foreach (var _ in Wait(3))
                    yield return null;
                var list = dropdown.Value.transform.Find("Dropdown List");
                Check(
                    list != null,
                    "Pointer interaction opens the generated native dropdown template. Selected="
                        + EventSystem.current.currentSelectedGameObject
                        + "; children="
                        + string.Join(
                            ",",
                            dropdown.Value.transform.Cast<Transform>().Select(t => t.name)
                        )
                );
                var options = list.GetComponentsInChildren<Toggle>()
                    .Where(t => t.gameObject.activeInHierarchy)
                    .ToArray();
                Check(options.Length == 2, "Dropdown template generates both options.");
                position = Center(options[1], camera);
                MouseState(mouse, position, true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position);
                foreach (var _ in Wait(12))
                    yield return null;
                Check(
                    selected.Value == 1,
                    "Pointer selection updates the native dropdown and its Source."
                );
                float hiddenAt = Time.time + .3f;
                while (Time.time < hiddenAt)
                    yield return null;
                Check(
                    dropdown.Value.transform.Find("Dropdown List") == null,
                    "The native dropdown closes after option selection."
                );

                var before = scroll.Value.content.anchoredPosition;
                MouseState(mouse, Center(scroll.Value, camera), scroll: new Vector2(0, -1));
                foreach (var _ in Wait(3))
                    yield return null;
                Check(
                    scroll.Value.content.anchoredPosition != before,
                    "Scroll wheel events move supplied native scroll content."
                );
                scroll.Value.StopMovement();
                scroll.Value.verticalNormalizedPosition = .75f;

                EventSystem.current.SetSelectedGameObject(button.Value.gameObject);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                foreach (var _ in Wait())
                    yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                foreach (var _ in Wait())
                    yield return null;
                Check(clicks.Value == 2, "Keyboard submit invokes the native selected button.");
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.down });
                foreach (var _ in Wait())
                    yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                foreach (var _ in Wait())
                    yield return null;
                Check(
                    EventSystem.current.currentSelectedGameObject == toggle.Value.gameObject,
                    "Gamepad navigation follows a forward typed native Navigation link."
                );

                Canvas.ForceUpdateCanvases();
                camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = texture;
                var screenshot = new Texture2D(
                    texture.width,
                    texture.height,
                    TextureFormat.RGB24,
                    false
                );
                screenshot.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                screenshot.Apply();
                RenderTexture.active = previous;
                Check(
                    screenshot.GetPixels().Count(c => c.g > .3f) > 10000,
                    "The native Canvas renders visible controls and text into a desktop render target."
                );
                var output = Path.GetFullPath("interaction-" + Application.unityVersion + ".png");
                File.WriteAllBytes(output, screenshot.EncodeToPNG());
                UnityEngine.Object.Destroy(screenshot);
                Debug.Log("PINE_RENDER_IMAGE " + output);
            }
            finally
            {
                mount?.Dispose();
                InputSystem.settings.backgroundBehavior = oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
                InputSystem.RemoveDevice(gamepad);
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
                camera.targetTexture = null;
                texture.Release();
                UnityEngine.Object.Destroy(texture);
                UnityEngine.Object.Destroy(cameraObject);
            }
        }
    }
}
