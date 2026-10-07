using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pine;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pine.Tests
{
    internal static partial class PineInteractionChecks
    {
        private static IEnumerable<object> Complex(
            Camera camera,
            Mouse mouse,
            Keyboard keyboard,
            RenderTexture texture
        )
        {
            var scroll = P.Ref<ScrollRect>();
            var horizontal = P.Ref<Scrollbar>();
            var vertical = P.Ref<Scrollbar>();
            var contentSize = P.Source(new Vector2(800, 600));
            var visibility = P.Source(ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport);
            using (
                var mount = P.Mount(
                    WorldCanvas(
                        camera,
                        P.ScrollRect(
                            reference: scroll,
                            sizeDelta: new Vector2(400, 240),
                            horizontalScrollbar: P.Scrollbar(
                                reference: horizontal,
                                direction: Scrollbar.Direction.LeftToRight,
                                anchorMin: Vector2.zero,
                                anchorMax: new Vector2(1, 0),
                                pivot: new Vector2(.5f, 0),
                                sizeDelta: new Vector2(0, 20)
                            ),
                            verticalScrollbar: P.Scrollbar(
                                reference: vertical,
                                direction: Scrollbar.Direction.BottomToTop,
                                anchorMin: new Vector2(1, 0),
                                anchorMax: Vector2.one,
                                pivot: new Vector2(1, .5f),
                                sizeDelta: new Vector2(20, 0)
                            ),
                            horizontalScrollbarVisibility: visibility,
                            verticalScrollbarVisibility: visibility,
                            content: P.Image(
                                color: new Color(.15f, .55f, .3f),
                                sizeDelta: contentSize,
                                children: new[] { Caption("World-space scroll content") }
                            )
                        )
                    )
                )
            )
            {
                foreach (var _ in Wait(4))
                    yield return null;
                Check(
                    scroll.Value.viewport.rect.width < 400
                        && scroll.Value.viewport.rect.height < 240,
                    "Linked scrollbars reserve viewport space in both orientations."
                );
                Check(
                    horizontal.Value.size < 1 && vertical.Value.size < 1,
                    "Both scrollbar handles reflect overflowing content."
                );

                var before = scroll.Value.content.anchoredPosition;
                var position = Center(horizontal.Value, camera);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                var hits = new List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(
                    new UnityEngine.EventSystems.PointerEventData(
                        UnityEngine.EventSystems.EventSystem.current
                    )
                    {
                        position = position,
                    },
                    hits
                );
                Check(
                    hits.Count > 0
                        && hits[0].gameObject.GetComponentInParent<Scrollbar>() == horizontal.Value,
                    "World-space scrollbar raycast: "
                        + string.Join(",", hits.ConvertAll(hit => hit.gameObject.name))
                );
                MouseState(mouse, position + new Vector2(-70, 0), true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position + new Vector2(70, 0), true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position + new Vector2(70, 0));
                foreach (var _ in Wait())
                    yield return null;
                Check(
                    Mathf.Abs(scroll.Value.content.anchoredPosition.x - before.x) > 20,
                    "World-space pointer dragging the linked horizontal scrollbar moves content. Before="
                        + before
                        + "; after="
                        + scroll.Value.content.anchoredPosition
                        + "; value="
                        + horizontal.Value.value
                        + "; center="
                        + position
                        + "; cameraSize="
                        + camera.orthographicSize
                        + "; bar="
                        + ((RectTransform)horizontal.Value.transform).rect
                );

                before = scroll.Value.content.anchoredPosition;
                position = Center(vertical.Value, camera);
                MouseState(mouse, position + new Vector2(0, -45), true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position + new Vector2(0, 45), true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position + new Vector2(0, 45));
                foreach (var _ in Wait())
                    yield return null;
                Check(
                    Mathf.Abs(scroll.Value.content.anchoredPosition.y - before.y) > 20,
                    "World-space pointer dragging the linked vertical scrollbar moves content."
                );
                CaptureComplex(camera, texture, "scroll");

                contentSize.Value = new Vector2(100, 80);
                foreach (var _ in Wait(4))
                    yield return null;
                Check(
                    !horizontal.Value.gameObject.activeSelf
                        && !vertical.Value.gameObject.activeSelf,
                    "Auto-hide removes both scrollbars for content that fits."
                );
                Check(
                    Mathf.Approximately(scroll.Value.viewport.rect.width, 400)
                        && Mathf.Approximately(scroll.Value.viewport.rect.height, 240),
                    "Auto-hide-and-expand restores the whole viewport."
                );

                visibility.Value = ScrollRect.ScrollbarVisibility.Permanent;
                foreach (var _ in Wait(3))
                    yield return null;
                Check(
                    horizontal.Value.gameObject.activeSelf && vertical.Value.gameObject.activeSelf,
                    "Permanent scrollbars stay visible when content fits."
                );
                visibility.Value = ScrollRect.ScrollbarVisibility.AutoHide;
                foreach (var _ in Wait(3))
                    yield return null;
                Check(
                    !horizontal.Value.gameObject.activeSelf
                        && !vertical.Value.gameObject.activeSelf,
                    "Auto-hide visibility works independently of viewport expansion."
                );
            }
            foreach (var _ in ImageDropdowns(camera, mouse, texture))
                yield return null;
            foreach (var _ in MultilineAndGroups(camera, mouse))
                yield return null;
            foreach (var _ in AnimatedButton(camera, mouse, texture))
                yield return null;
        }

        private static IEnumerable<object> Click(Mouse mouse, Vector2 position)
        {
            MouseState(mouse, position);
            foreach (var _ in Wait())
                yield return null;
            MouseState(mouse, position, true);
            foreach (var _ in Wait())
                yield return null;
            MouseState(mouse, position);
            foreach (var _ in Wait(3))
                yield return null;
        }

        private static View WorldCanvas(Camera camera, params View[] children) =>
            P.Canvas(
                renderMode: RenderMode.WorldSpace,
                worldCamera: camera,
                localScale: new Vector3(.01f, .01f, .01f),
                sizeDelta: new Vector2(960, 720),
                sortingOrder: 200,
                children: children
            );

        private static Color Pixel(Camera camera, RenderTexture texture, Component target)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = texture;
                var point = Center(target, camera);
                pixel.ReadPixels(new Rect((int)point.x, (int)point.y, 1, 1), 0, 0);
                pixel.Apply();
                return pixel.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(pixel);
            }
        }

        private static void CaptureComplex(Camera camera, RenderTexture texture, string scenario)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                image.Apply();
                string output = Path.GetFullPath(
                    "advanced-" + scenario + "-" + Application.unityVersion + ".png"
                );
                File.WriteAllBytes(output, image.EncodeToPNG());
                Debug.Log("PINE_RENDER_IMAGE " + output);
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(image);
            }
        }

        private static IEnumerable<object> ImageDropdowns(
            Camera camera,
            Mouse mouse,
            RenderTexture texture
        )
        {
            var image = new Texture2D(16, 16);
            image.SetPixels(Enumerable.Repeat(Color.magenta, 256).ToArray());
            image.Apply();
            var icon = Sprite.Create(image, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f));
            var dropdown = P.Ref<TMP_Dropdown>();
            var legacy = P.Ref<Dropdown>();
            var selected = P.Source(0);
            var legacySelected = P.Source(0);
            try
            {
                using (
                    var mount = P.Mount(
                        WorldCanvas(
                            camera,
                            P.Dropdown(
                                reference: dropdown,
                                value: selected,
                                anchoredPosition: new Vector2(0, 200),
                                sizeDelta: new Vector2(360, 48),
                                options: new List<TMP_Dropdown.OptionData>
                                {
                                    new("Text only"),
                                    new("", icon, Color.white),
                                    new("Mixed", icon, Color.white),
                                }
                            ),
                            P.LegacyDropdown(
                                reference: legacy,
                                value: legacySelected,
                                anchoredPosition: new Vector2(0, -50),
                                sizeDelta: new Vector2(360, 48),
                                options: new List<Dropdown.OptionData>
                                {
                                    new("Text only"),
                                    new("", icon),
                                    new("Mixed", icon),
                                }
                            )
                        )
                    )
                )
                {
                    foreach (var _ in Wait(4))
                        yield return null;
                    foreach (var _ in Click(mouse, Center(dropdown.Value, camera)))
                        yield return null;
                    var list = dropdown.Value.transform.Find("Dropdown List");
                    Check(list != null, "World-space pointer input opens the TMP dropdown.");
                    var items = list.GetComponentsInChildren<Toggle>()
                        .Where(t => t.isActiveAndEnabled)
                        .ToArray();
                    Check(
                        items.Length == 3
                            && items[0].GetComponentInChildren<TMP_Text>().text == "Text only"
                            && items[1].GetComponentInChildren<TMP_Text>().text == ""
                            && items[2].GetComponentInChildren<TMP_Text>().text == "Mixed",
                        "TMP dropdown generates text-only, image-only and mixed options."
                    );
                    var optionImage = items[1]
                        .GetComponentsInChildren<Image>()
                        .Single(item => item.sprite == icon);
                    var mixedImage = items[2]
                        .GetComponentsInChildren<Image>()
                        .Single(item => item.sprite == icon);
                    var mixedText = items[2].GetComponentInChildren<TMP_Text>();
                    Check(
                        RectTransformUtility
                            .CalculateRelativeRectTransformBounds(
                                items[2].transform,
                                mixedText.transform
                            )
                            .min.x
                            > RectTransformUtility
                                .CalculateRelativeRectTransformBounds(
                                    items[2].transform,
                                    mixedImage.transform
                                )
                                .max.x,
                        "Mixed dropdown text and its image occupy separate visible regions."
                    );
                    float visibleAt = Time.time + .2f;
                    while (Time.time < visibleAt)
                        yield return null;
                    var color = Pixel(camera, texture, optionImage);
                    Check(
                        color.r > .8f && color.b > .8f && color.g < .2f,
                        "The supplied TMP option sprite is visible in the rendered world-space list."
                    );
                    CaptureComplex(camera, texture, "dropdown");
                    foreach (var _ in Click(mouse, Center(items[1], camera)))
                        yield return null;
                    foreach (var _ in Wait(20))
                        yield return null;
                    Check(
                        selected.Value == 1
                            && dropdown.Value.captionImage.sprite == icon
                            && dropdown.Value.captionImage.enabled
                            && dropdown.Value.captionText.text == "",
                        "Selecting an image-only TMP option updates the Source and visible caption image."
                    );
                    foreach (var _ in Click(mouse, Center(dropdown.Value, camera)))
                        yield return null;
                    list = dropdown.Value.transform.Find("Dropdown List");
                    items = list.GetComponentsInChildren<Toggle>()
                        .Where(t => t.isActiveAndEnabled)
                        .ToArray();
                    foreach (var _ in Click(mouse, Center(items[2], camera)))
                        yield return null;
                    foreach (var _ in Wait(20))
                        yield return null;
                    Check(
                        selected.Value == 2
                            && dropdown.Value.captionImage.sprite == icon
                            && dropdown.Value.captionText.text == "Mixed",
                        "Mixed TMP selection preserves both caption parts."
                    );

                    foreach (var _ in Click(mouse, Center(legacy.Value, camera)))
                        yield return null;
                    list = legacy.Value.transform.Find("Dropdown List");
                    Check(list != null, "World-space pointer input opens the legacy dropdown.");
                    items = list.GetComponentsInChildren<Toggle>()
                        .Where(t => t.isActiveAndEnabled)
                        .ToArray();
                    Check(
                        items.Length == 3
                            && items[0].GetComponentInChildren<Text>().text == "Text only"
                            && items[1].GetComponentInChildren<Text>().text == ""
                            && items[2].GetComponentInChildren<Text>().text == "Mixed",
                        "Legacy dropdown generates text-only, image-only and mixed options."
                    );
                    optionImage = items[1]
                        .GetComponentsInChildren<Image>()
                        .Single(item => item.sprite == icon);
                    visibleAt = Time.time + .2f;
                    while (Time.time < visibleAt)
                        yield return null;
                    color = Pixel(camera, texture, optionImage);
                    Check(
                        color.r > .8f && color.b > .8f && color.g < .2f,
                        "The supplied legacy option sprite is visible in the rendered world-space list."
                    );
                    foreach (var _ in Click(mouse, Center(items[1], camera)))
                        yield return null;
                    foreach (var _ in Wait(20))
                        yield return null;
                    Check(
                        legacySelected.Value == 1
                            && legacy.Value.captionImage.sprite == icon
                            && legacy.Value.captionImage.enabled,
                        "Legacy image selection updates its Source and caption."
                    );
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(icon);
                UnityEngine.Object.Destroy(image);
            }
        }

        private static IEnumerable<object> MultilineAndGroups(Camera camera, Mouse mouse)
        {
            var input = P.Ref<TMP_InputField>();
            var label = P.Ref<TMP_Text>();
            var scrollbar = P.Ref<Scrollbar>();
            var text = P.Source("");
            var group = P.Ref<ToggleGroup>();
            var first = P.Ref<Toggle>();
            var second = P.Ref<Toggle>();
            var third = P.Ref<Toggle>();
            var firstOn = P.Source(true);
            var secondOn = P.Source(false);
            var thirdOn = P.Source(false);
            var joined = P.Source(true);
            using (
                var mount = P.Mount(
                    WorldCanvas(
                        camera,
                        P.InputField(
                            reference: input,
                            text: text,
                            lineType: TMP_InputField.LineType.MultiLineNewline,
                            caretWidth: 3,
                            customCaretColor: true,
                            caretColor: Color.green,
                            onFocusSelectAll: false,
                            anchoredPosition: new Vector2(-230, 0),
                            sizeDelta: new Vector2(400, 180),
                            viewport: P.Frame(
                                anchorMin: Vector2.zero,
                                anchorMax: Vector2.one,
                                offsetMin: new Vector2(8, 8),
                                offsetMax: new Vector2(-28, -8),
                                components: new[] { P.RectMask2D() }
                            ),
                            textComponent: P.Text(
                                "",
                                reference: label,
                                color: Color.black,
                                fontSize: 24,
                                alignment: TextAlignmentOptions.TopLeft,
                                anchorMin: Vector2.zero,
                                anchorMax: Vector2.one,
                                sizeDelta: Vector2.zero
                            ),
                            verticalScrollbar: P.Scrollbar(
                                reference: scrollbar,
                                direction: Scrollbar.Direction.BottomToTop,
                                anchorMin: new Vector2(1, 0),
                                anchorMax: Vector2.one,
                                pivot: new Vector2(1, .5f),
                                sizeDelta: new Vector2(20, 0)
                            )
                        ),
                        P.Frame(
                            anchoredPosition: new Vector2(230, 0),
                            sizeDelta: new Vector2(360, 200),
                            components: new[] { P.ToggleGroup(reference: group) },
                            children: new[]
                            {
                                P.Toggle(
                                    "First",
                                    group: group,
                                    reference: first,
                                    isOn: firstOn,
                                    anchoredPosition: new Vector2(0, 65),
                                    sizeDelta: new Vector2(320, 48)
                                ),
                                P.Toggle(
                                    "Second",
                                    group: group,
                                    reference: second,
                                    isOn: secondOn,
                                    sizeDelta: new Vector2(320, 48)
                                ),
                                P.Toggle(
                                    "Third",
                                    reference: third,
                                    isOn: thirdOn,
                                    group: new Value<ToggleGroup>(() =>
                                        joined.Value ? group.Value : null
                                    ),
                                    anchoredPosition: new Vector2(0, -65),
                                    sizeDelta: new Vector2(320, 48)
                                ),
                            }
                        )
                    )
                )
            )
            {
                foreach (var _ in Wait(4))
                    yield return null;
                foreach (var _ in Click(mouse, Center(input.Value, camera)))
                    yield return null;
                Check(
                    input.Value.isFocused
                        && input.Value.caretWidth == 3
                        && input.Value.caretColor == Color.green
                        && label.Value.transform.parent == input.Value.textViewport,
                    "World-space multiline input uses its declared viewport, text and caret settings."
                );
                foreach (char character in "Line one\nLine two")
                {
                    EditorGUIUtility.QueueGameViewInputEvent(
                        new Event
                        {
                            type = EventType.KeyDown,
                            character = character,
                            keyCode = character == '\n' ? KeyCode.Return : KeyCode.None,
                        }
                    );
                    foreach (var _ in Wait())
                        yield return null;
                }
                Check(
                    text.Value == "Line one\nLine two"
                        && input.Value.isFocused
                        && label.Value.textInfo.lineCount == 2,
                    "Native multiline text entry preserves newline, focus and Source write-back."
                );
                text.Value = string.Join("\n", Enumerable.Range(1, 16).Select(i => "Line " + i));
                foreach (var _ in Wait(4))
                    yield return null;
                Check(
                    scrollbar.Value.size < 1,
                    "The declared input scrollbar reflects overflowing multiline text."
                );
                input.Value.DeactivateInputField();
                var before = label.Value.rectTransform.anchoredPosition;
                var position = Center(scrollbar.Value, camera);
                MouseState(mouse, position + new Vector2(0, -35), true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position + new Vector2(0, 35), true);
                foreach (var _ in Wait())
                    yield return null;
                MouseState(mouse, position + new Vector2(0, 35));
                foreach (var _ in Wait(3))
                    yield return null;
                Check(
                    label.Value.rectTransform.anchoredPosition != before,
                    "Pointer dragging a declared multiline scrollbar moves its text viewport."
                );

                foreach (var _ in Click(mouse, Center(second.Value, camera)))
                    yield return null;
                Check(
                    secondOn.Value && !firstOn.Value && !thirdOn.Value,
                    "Native toggle groups enforce one selection and write every affected Source."
                );
                foreach (var _ in Click(mouse, Center(third.Value, camera)))
                    yield return null;
                Check(
                    thirdOn.Value && !secondOn.Value,
                    "Selecting another group member clears the previous selection."
                );
                joined.Value = false;
                foreach (var _ in Click(mouse, Center(second.Value, camera)))
                    yield return null;
                Check(
                    third.Value.group == null && thirdOn.Value && secondOn.Value,
                    "Reactive removal from a toggle group permits independent selection."
                );
                joined.Value = true;
                foreach (var _ in Wait())
                    yield return null;
                Check(
                    third.Value.group == group.Value && thirdOn.Value && !secondOn.Value,
                    "Rejoining an active toggle restores native mutual exclusion."
                );
                float settledAt = Time.time + .2f;
                while (Time.time < settledAt)
                    yield return null;
                Check(
                    first.Value.graphic.canvasRenderer.GetAlpha() < .1f
                        && second.Value.graphic.canvasRenderer.GetAlpha() < .1f
                        && third.Value.graphic.canvasRenderer.GetAlpha() > .9f,
                    "Toggle-group checkmarks render only the final active selection."
                );
                CaptureComplex(camera, camera.targetTexture, "input-groups");
            }
        }

        private static IEnumerable<object> AnimatedButton(
            Camera camera,
            Mouse mouse,
            RenderTexture texture
        )
        {
            const string controllerPath = "Assets/PineInteractionAnimation.controller";
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var states = new[] { "Normal", "Highlighted", "Pressed", "Selected", "Disabled" };
            var colors = new[] { Color.red, Color.green, Color.blue, Color.yellow, Color.gray };
            var machine = controller.layers[0].stateMachine;
            for (int i = 0; i < states.Length; i++)
            {
                var clip = new AnimationClip { name = states[i] };
                AssetDatabase.AddObjectToAsset(clip, controller);
                foreach (var channel in new[] { "r", "g", "b", "a" })
                    AnimationUtility.SetEditorCurve(
                        clip,
                        EditorCurveBinding.FloatCurve("", typeof(Image), "m_Color." + channel),
                        AnimationCurve.Constant(
                            0,
                            .1f,
                            channel switch
                            {
                                "r" => colors[i].r,
                                "g" => colors[i].g,
                                "b" => colors[i].b,
                                _ => colors[i].a,
                            }
                        )
                    );
                var state = machine.AddState(states[i]);
                state.motion = clip;
                if (i == 0)
                    machine.defaultState = state;
                controller.AddParameter(states[i], AnimatorControllerParameterType.Trigger);
                var transition = machine.AddAnyStateTransition(state);
                transition.hasExitTime = false;
                transition.duration = 0;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.If, 0, states[i]);
            }
            var button = P.Ref<Button>();
            var animator = P.Ref<Animator>();
            var graphic = P.Ref<Image>();
            var enabled = P.Source(true);
            try
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                MouseState(mouse, new Vector2(20, 20));
                using (
                    var mount = P.Mount(
                        WorldCanvas(
                            camera,
                            P.Button(
                                "",
                                reference: button,
                                interactable: enabled,
                                transition: Selectable.Transition.Animation,
                                sizeDelta: new Vector2(320, 120),
                                components: new[]
                                {
                                    P.Self(P.Image(reference: graphic)),
                                    P.Animator(
                                        runtimeAnimatorController: controller,
                                        reference: animator,
                                        cullingMode: AnimatorCullingMode.AlwaysAnimate
                                    ),
                                }
                            )
                        )
                    )
                )
                {
                    for (int i = 0; i < states.Length; i++)
                    {
                        if (i == 1)
                            MouseState(mouse, Center(button.Value, camera));
                        if (i == 2)
                            MouseState(mouse, Center(button.Value, camera), true);
                        if (i == 3)
                            MouseState(mouse, Center(button.Value, camera));
                        if (i == 4)
                            enabled.Value = false;
                        float deadline = Time.time + 2;
                        while (
                            (
                                !animator.Value.GetCurrentAnimatorStateInfo(0).IsName(states[i])
                                || graphic.Value.color != colors[i]
                            )
                            && Time.time < deadline
                        )
                            yield return null;
                        var color = Pixel(camera, texture, graphic.Value);
                        var expected =
                            QualitySettings.activeColorSpace == ColorSpace.Linear
                                ? colors[i].gamma
                                : colors[i];
                        Check(
                            animator.Value.GetCurrentAnimatorStateInfo(0).IsName(states[i])
                                && graphic.Value.color == colors[i]
                                && Mathf.Abs(color.r - expected.r) < .08f
                                && Mathf.Abs(color.g - expected.g) < .08f
                                && Mathf.Abs(color.b - expected.b) < .08f,
                            "Native "
                                + states[i]
                                + " input transition animates and renders the supplied graphic."
                        );
                        CaptureComplex(camera, texture, "animation-" + states[i]);
                    }
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }
        }
    }
}
