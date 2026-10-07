using System;
using System.Collections.Generic;
using Pine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
namespace Pine.Tests
{
    internal static class PineAuthoringChecks
    {
        private static int _count;

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
            _count++;
        }

        internal static void Run()
        {
            var left = P.Ref<Button>();
            var right = P.Ref<Button>();
            var visible = P.Source(true);
            var second = P.Button(
                "Right",
                reference: right,
                navigation: new Value<Navigation>(() =>
                    new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = left.Value }
                )
            );
            using (
                var mount = P.Mount(
                    P.Frame(children: () =>
                        visible.Value
                            ? new[]
                            {
                                P.Button(
                                    "Left",
                                    reference: left,
                                    navigation: new Value<Navigation>(() =>
                                        new Navigation
                                        {
                                            mode = Navigation.Mode.Explicit,
                                            selectOnRight = right.Value,
                                        }
                                    )
                                ),
                                second,
                            }
                            : new[] { second }
                    )
                )
            )
            {
                Check(
                    left.Value.navigation.selectOnRight == right.Value
                        && right.Value.navigation.selectOnLeft == left.Value,
                    "Forward and cyclic typed relationships resolve."
                );
                var retained = right.Value;
                visible.Value = false;
                Check(
                    left.Value == null
                        && right.Value == retained
                        && right.Value.navigation.selectOnLeft == null,
                    "Removed target references clear retained native links."
                );
            }

            Check(
                left.Value == null && right.Value == null,
                "References clear when their mount ends."
            );
            var fill = P.Ref<Image>();
            var handle = P.Ref<Image>();
            var graphic = P.Ref<Graphic>();
            var slider = P.Ref<Slider>();
            using (
                var mount = P.Mount(
                    P.Slider(
                        reference: slider,
                        fill: P.Image(name: "Custom fill", reference: fill),
                        handle: P.Image(name: "Custom handle", reference: handle),
                        targetGraphic: graphic,
                        children: new[] { P.Image(name: "Custom target", reference: graphic) }
                    )
                )
            )
            {
                Check(
                    slider.Value.fillRect == fill.Value.rectTransform
                        && slider.Value.handleRect == handle.Value.rectTransform,
                    "Named slider parts wire without default duplicates."
                );
                Check(
                    slider.Value.targetGraphic == graphic.Value
                        && mount.Root.transform.childCount == 3,
                    "Custom target graphics remain wired independently of handles."
                );
            }

            using (
                var mount = P.Mount(
                    P.Slider(targetGraphic: P.Image(name: "Custom target"), reference: slider)
                )
            )
                Check(
                    slider.Value.targetGraphic.name == "Custom target",
                    "A default handle preserves an explicitly supplied target graphic."
                );
            var scroll = P.Ref<ScrollRect>();
            var content = P.Ref<VerticalLayoutGroup>();
            var viewport = P.Ref<RectTransform>();
            using (
                var mount = P.Mount(
                    P.ScrollRect(
                        reference: scroll,
                        content: P.Vertical(reference: content, children: new[] { P.Text("Row") }),
                        viewport: P.Frame(reference: viewport, children: new[] { P.RectMask2D() })
                    )
                )
            )
                Check(
                    scroll.Value.content == content.Value.transform
                        && scroll.Value.viewport == viewport.Value
                        && content.Value.transform.parent == viewport.Value
                        && mount.Root.transform.childCount == 1,
                    "Custom scroll content is wired and placed inside its declared viewport."
                );
            var dropdown = P.Ref<TMP_Dropdown>();
            var label = P.Ref<TMP_Text>();
            var lateCaption = P.Ref<TMP_Text>();
            var captionVisible = P.Source(false);
            var captionContent = P.Source("Initial");
            using (
                var mount = P.Mount(
                    P.Button(
                        text: captionContent,
                        caption: lateCaption,
                        children: () =>
                            captionVisible.Value
                                ? new[] { P.Text("", reference: lateCaption) }
                                : Array.Empty<View>()
                    )
                )
            )
            {
                Check(
                    mount.Root.GetComponentsInChildren<TMP_Text>(true).Length == 0,
                    "An unresolved supplied caption suppresses default helpers."
                );
                captionVisible.Value = true;
                captionContent.Value = "Updated";
                Check(
                    lateCaption.Value.text == "Updated",
                    "A late typed caption follows both target and text updates."
                );
            }
            var suppliedFont = UnityEngine.Object.Instantiate(
                Resources.Load<TMP_FontAsset>("Pine/Fonts/Latin")
            );
            var inputText = P.Ref<TMP_Text>();
            var placeholderText = P.Ref<TMP_Text>();
            try
            {
                using (
                    var mount = P.Mount(
                        P.InputField(
                            textComponent: P.Text("", font: suppliedFont, reference: inputText),
                            placeholder: P.Text(
                                "Hint",
                                font: suppliedFont,
                                reference: placeholderText
                            )
                        )
                    )
                )
                    Check(
                        inputText.Value.font == suppliedFont
                            && placeholderText.Value.font == suppliedFont,
                        "Missing input defaults preserve fonts supplied by declared text parts."
                    );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(suppliedFont);
            }
            using (var mount = P.Mount(P.Dropdown(reference: dropdown)))
                Check(
                    Mathf.Approximately(
                        dropdown
                            .Value.template.GetComponentInChildren<Toggle>(true)
                            .GetComponent<RectTransform>()
                            .rect.height,
                        32
                    ),
                    "Default dropdown items have a fixed height inside the native scroll viewport."
                );
            using (
                var mount = P.Mount(
                    P.Dropdown(
                        reference: dropdown,
                        itemText: P.Text("Custom item", reference: label)
                    )
                )
            )
                Check(
                    dropdown.Value.itemText == label.Value
                        && label
                            .Value.transform.parent.GetComponentsInChildren<TMP_Text>(true)
                            .Length == 1,
                    "Custom item text replaces the generated dropdown label."
                );
            using (
                var mount = P.Mount(
                    P.Button(
                        "Nested caption",
                        caption: P.Frame(children: new[] { P.Self(P.Text("")) })
                    )
                )
            )
                Check(
                    mount.Root.GetComponentsInChildren<TMP_Text>(true).Length == 1
                        && mount.Root.GetComponentInChildren<TMP_Text>().text == "Nested caption",
                    "Named parts resolve required components on a declared composite root."
                );
            using (
                var mount = P.Mount(
                    P.Dropdown(
                        reference: dropdown,
                        template: P.Frame(
                            children: new[]
                            {
                                P.ScrollRect(
                                    content: P.Frame(
                                        children: new[]
                                        {
                                            P.Toggle(children: new[] { P.Text("Existing item") }),
                                        }
                                    )
                                ),
                            }
                        )
                    )
                )
            )
                Check(
                    dropdown.Value.template.GetComponentsInChildren<ScrollRect>(true).Length == 1
                        && dropdown.Value.itemText.text == "Existing item",
                    "A complete supplied dropdown template preserves its authored scroll hierarchy."
                );

            var external = new GameObject("External template", typeof(RectTransform));
            var item = new GameObject("External item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(external.transform, false);
            var textObject = new GameObject(
                "External text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );
            textObject.transform.SetParent(item.transform, false);
            try
            {
                using (
                    var mount = P.Mount(
                        P.Dropdown(
                            template: (RectTransform)external.transform,
                            item: item.GetComponent<Toggle>(),
                            itemText: textObject.GetComponent<TMP_Text>(),
                            reference: dropdown
                        )
                    )
                )
                    Check(
                        external.activeSelf
                            && external.GetComponent<ScrollRect>() == null
                            && item.transform.parent == external.transform
                            && textObject.transform.parent == item.transform,
                        "External dropdown hierarchy and activation remain externally owned."
                    );
                Check(
                    external != null && item != null && textObject != null,
                    "Disposal preserves supplied external parts."
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(external);
            }

            var rows = P.Source<IReadOnlyList<View>>(Array.Empty<View>());
            var late = P.Ref<TMP_Text>();
            using (
                var mount = P.Mount(
                    P.Frame(
                        children: () => rows.Value,
                        reference: _ =>
                            rows.Value = new[] { P.Text("Late", reference: late, color: Color.red) }
                    )
                )
            )
                Check(
                    late.Value != null
                        && late.Value.color == Color.red
                        && late.Value.gameObject.activeInHierarchy,
                    "Children added during construction's final reactive flush are configured and activated."
                );
#if ENABLE_INPUT_SYSTEM
            var externalActions = new UnityEngine.InputSystem.DefaultInputActions();
            var customMove = UnityEngine.InputSystem.InputActionReference.Create(
                externalActions.UI.Navigate
            );
            var suppliedInput = P.Ref<InputSystemUIInputModule>();
            try
            {
                using (
                    var mount = P.Mount(
                        P.EventSystem(
                            children: new[]
                            {
                                P.InputSystemUIInputModule(
                                    reference: suppliedInput,
                                    move: customMove,
                                    actionsAsset: externalActions.asset
                                ),
                            }
                        )
                    )
                )
                    Check(
                        suppliedInput.Value.actionsAsset == externalActions.asset
                            && suppliedInput.Value.move.action == customMove.action,
                        "Supplied action assets apply before individual action references."
                    );
                Check(
                    externalActions.asset != null && customMove.action != null,
                    "Disposal preserves supplied input assets and action references."
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(customMove);
                externalActions.Dispose();
            }
            var player = P.Ref<UnityEngine.InputSystem.PlayerInput>();
            var module = P.Ref<InputSystemUIInputModule>();
            using (
                var mount = P.Mount(
                    P.EventSystem(
                        children: new[]
                        {
                            P.PlayerInput(reference: player, uiInputModule: module),
                            P.InputSystemUIInputModule(reference: module),
                        }
                    )
                )
            )
                Check(
                    player.Value.uiInputModule == module.Value,
                    "PlayerInput declares its forward UI module relationship."
                );
            var firstInput = P.Ref<InputSystemUIInputModule>();
            var secondInput = P.Ref<InputSystemUIInputModule>();
            using (
                var firstPlayer = P.Mount(
                    P.MultiplayerEventSystem(
                        children: new[] { P.InputSystemUIInputModule(reference: firstInput) }
                    )
                )
            )
            {
                using (
                    var secondPlayer = P.Mount(
                        P.MultiplayerEventSystem(
                            children: new[] { P.InputSystemUIInputModule(reference: secondInput) }
                        )
                    )
                )
                    Check(
                        firstInput.Value.actionsAsset != secondInput.Value.actionsAsset,
                        "Default multiplayer action assets are owned independently."
                    );
                Check(
                    firstInput.Value.point.action != null && firstInput.Value.actionsAsset != null,
                    "Disposing another EventSystem preserves this player's input actions."
                );
            }
#endif
            const string controllerPath = "Assets/PineAuthoringAnimation.controller";
            var controller =
                UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(
                    controllerPath
                );
            var animator = P.Ref<Animator>();
            try
            {
                using (
                    var mount = P.Mount(
                        P.Button(
                            "Animated",
                            transition: Selectable.Transition.Animation,
                            children: new[]
                            {
                                P.Animator(
                                    runtimeAnimatorController: controller,
                                    reference: animator
                                ),
                            }
                        )
                    )
                )
                    Check(
                        mount.Root.GetComponent<Button>().animator == animator.Value
                            && animator.Value.runtimeAnimatorController == controller,
                        "Native animation transitions receive their declared Animator and supplied controller."
                    );
            }
            finally
            {
                UnityEditor.AssetDatabase.DeleteAsset(controllerPath);
            }

            var trigger = P.Ref<UnityEngine.EventSystems.EventTrigger>();
            UnityEngine.Events.UnityEvent<UnityEngine.EventSystems.BaseEventData> saved = null;
            int events = 0;
            using (
                var mount = P.Mount(
                    P.Frame(
                        children: () => Array.Empty<View>(),
                        components: new[]
                        {
                            P.LayoutElement(preferredHeight: 42),
                            P.EventTrigger(onPointerClick: _ => events++, reference: trigger),
                        }
                    )
                )
            )
            {
                Check(
                    mount.Root.GetComponent<LayoutElement>().preferredHeight == 42,
                    "Static component attachments coexist with reactive child getters."
                );
                saved = trigger.Value.triggers[0].callback;
                saved.Invoke(null);
                Check(events == 1, "EventTrigger entries are wired through declarative callbacks.");
            }
            saved.Invoke(null);
            Check(events == 1, "EventTrigger callbacks detach with their native owner.");
            Debug.Log($"PINE_AUTHORING_EDIT_PASSED {_count}");
        }
    }
}
