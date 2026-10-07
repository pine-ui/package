using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pine.uGUI
{
    public static partial class P
    {
        private static readonly System.Reflection.FieldInfo _regexField =
            typeof(TMP_InputField).GetField(
                "m_RegexValue",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );

        private static void SetRegex(TMP_InputField target, string value)
        {
            if (_regexField == null)
                throw new NotSupportedException(
                    "This TMP version does not expose its serialized regex pattern."
                );
            _regexField.SetValue(target, value);
        }

        private static readonly System.Reflection.FieldInfo _pointerHoverField =
            typeof(UnityEngine.EventSystems.BaseInputModule).GetField(
                "m_SendPointerHoverToParent",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );

        private static void SetPointerHover(
            UnityEngine.EventSystems.BaseInputModule target,
            bool value
        )
        {
            if (_pointerHoverField == null)
                throw new NotSupportedException(
                    "This uGUI version does not expose its serialized pointer hover setting."
                );
            _pointerHoverField.SetValue(target, value);
        }

        internal static void Prop<T, TValue>(
            T target,
            Value<TValue>? value,
            Action<T, TValue> assign
        )
            where T : UnityEngine.Object
        {
            if (!value.HasValue)
                return;
            BindValue(
                target,
                value.Value,
                (item, next) =>
                {
                    if (item != null)
                        assign(item, next);
                }
            );
        }

        internal static void InputProp<T, TValue>(
            T target,
            Value<TValue>? value,
            Func<T, TValue> read,
            Action<T, TValue> assign,
            Func<T, UnityEvent<TValue>> events
        )
            where T : Component
        {
            if (!value.HasValue)
                return;
            var input = value.Value;
            BindValue(
                target,
                input,
                (item, next) =>
                {
                    if (item == null)
                        return;
                    assign(item, next);
                    if (input.Writable != null)
                        input.Writable.Value = read(item);
                }
            );
            if (input.Writable != null)
                Listen(events(target), next => input.Writable.Value = next);
        }

        internal static void Listen(UnityEvent events, Action action)
        {
            if (action == null)
                return;
            Scope scope = RequireScope();
            UnityAction handler = () =>
            {
                if (!scope.IsDisposed)
                    scope.Run(() => Batch(() => Untrack(action)));
            };
            events.AddListener(handler);
            Cleanup(() => events.RemoveListener(handler));
        }

        internal static void Listen<T>(UnityEvent<T> events, Action<T> action)
        {
            if (action == null)
                return;
            Scope scope = RequireScope();
            UnityAction<T> handler = value =>
            {
                if (!scope.IsDisposed)
                    scope.Run(() => Batch(() => Untrack(() => action(value))));
            };
            events.AddListener(handler);
            Cleanup(() => events.RemoveListener(handler));
        }

        internal static void Listen<T1, T2>(UnityEvent<T1, T2> events, Action<T1, T2> action)
        {
            if (action == null)
                return;
            Scope scope = RequireScope();
            UnityAction<T1, T2> handler = (a, b) =>
            {
                if (!scope.IsDisposed)
                    scope.Run(() => Batch(() => Untrack(() => action(a, b))));
            };
            events.AddListener(handler);
            Cleanup(() => events.RemoveListener(handler));
        }

        internal static void Listen<T1, T2, T3>(
            UnityEvent<T1, T2, T3> events,
            Action<T1, T2, T3> action
        )
        {
            if (action == null)
                return;
            Scope scope = RequireScope();
            UnityAction<T1, T2, T3> handler = (a, b, c) =>
            {
                if (!scope.IsDisposed)
                    scope.Run(() => Batch(() => Untrack(() => action(a, b, c))));
            };
            events.AddListener(handler);
            Cleanup(() => events.RemoveListener(handler));
        }

        internal static void Listen<T1, T2, T3, T4>(
            UnityEvent<T1, T2, T3, T4> events,
            Action<T1, T2, T3, T4> action
        )
        {
            if (action == null)
                return;
            Scope scope = RequireScope();
            UnityAction<T1, T2, T3, T4> handler = (a, b, c, d) =>
            {
                if (!scope.IsDisposed)
                    scope.Run(() => Batch(() => Untrack(() => action(a, b, c, d))));
            };
            events.AddListener(handler);
            Cleanup(() => events.RemoveListener(handler));
        }

        internal static void ListenTrigger(
            UnityEngine.EventSystems.EventTrigger target,
            UnityEngine.EventSystems.EventTriggerType type,
            Action<UnityEngine.EventSystems.BaseEventData> action
        )
        {
            if (action == null)
                return;
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = type };
            Listen(entry.callback, action);
            target.triggers.Add(entry);
            Cleanup(() =>
            {
                if (target != null)
                    target.triggers.Remove(entry);
            });
        }

#if ENABLE_INPUT_SYSTEM
        internal static void OwnInputDefaults(
            UnityEngine.InputSystem.UI.InputSystemUIInputModule module
        )
        {
            if (module.actionsAsset != null)
            {
                var owned = UnityEngine.Object.Instantiate(module.actionsAsset);
                module.actionsAsset = owned;
                var ownedReferences = new[]
                {
                    module.point,
                    module.move,
                    module.leftClick,
                    module.rightClick,
                    module.middleClick,
                    module.scrollWheel,
                    module.submit,
                    module.cancel,
                    module.trackedDevicePosition,
                    module.trackedDeviceOrientation,
                };
                Cleanup(() =>
                {
                    if (module != null)
                        module.enabled = false;
                    foreach (var reference in ownedReferences)
                        if (reference != null && reference.action?.actionMap?.asset == owned)
                            DestroyObject(reference);
                    DestroyObject(owned);
                });
                return;
            }
            if (
                module.point != null
                || module.leftClick != null
                || module.rightClick != null
                || module.middleClick != null
                || module.scrollWheel != null
                || module.submit != null
                || module.cancel != null
                || module.trackedDevicePosition != null
                || module.trackedDeviceOrientation != null
            )
                return;
            var move = module.move;
            var defaults = new UnityEngine.InputSystem.DefaultInputActions();
            var references = new List<UnityEngine.InputSystem.InputActionReference>();
            UnityEngine.InputSystem.InputActionReference Reference(
                UnityEngine.InputSystem.InputAction action
            )
            {
                var reference = UnityEngine.InputSystem.InputActionReference.Create(action);
                references.Add(reference);
                return reference;
            }
            module.actionsAsset = defaults.asset;
            module.point = Reference(defaults.UI.Point);
            module.leftClick = Reference(defaults.UI.Click);
            module.rightClick = Reference(defaults.UI.RightClick);
            module.middleClick = Reference(defaults.UI.MiddleClick);
            module.scrollWheel = Reference(defaults.UI.ScrollWheel);
            module.submit = Reference(defaults.UI.Submit);
            module.cancel = Reference(defaults.UI.Cancel);
            module.move = move != null ? move : Reference(defaults.UI.Navigate);
            module.trackedDevicePosition = Reference(defaults.UI.TrackedDevicePosition);
            module.trackedDeviceOrientation = Reference(defaults.UI.TrackedDeviceOrientation);
            Cleanup(() =>
            {
                if (module != null)
                    module.enabled = false;
                foreach (var reference in references)
                    DestroyObject(reference);
                defaults.Dispose();
            });
        }
#endif

        internal static void WireNative(
            Component target,
            Dictionary<string, Component> parts = null,
            NativePart[] supplied = null
        )
        {
            bool Missing(string name) =>
                supplied == null
                || Array.TrueForAll(supplied, part => part == null || part.Name != name);
            if (
                target is UnityEngine.EventSystems.EventSystem system
                && system.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null
            )
            {
#if ENABLE_INPUT_SYSTEM
                WireNative(
                    GetOrAdd<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(system.gameObject)
                );
#elif ENABLE_LEGACY_INPUT_MANAGER
                GetOrAdd<UnityEngine.EventSystems.StandaloneInputModule>(system.gameObject);
#endif
            }
#if ENABLE_INPUT_SYSTEM
            if (target is UnityEngine.InputSystem.UI.InputSystemUIInputModule inputModule)
                OwnInputDefaults(inputModule);
#endif
            if (target is TMP_Text text && text.font == null)
                text.font = ResolveFont();
            if (target is UnityEngine.UI.Text legacyText && legacyText.font == null)
                legacyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (
                target is Selectable selectable
                && selectable.targetGraphic == null
                && Missing("targetGraphic")
            )
                selectable.targetGraphic = GetOrAdd<Image>(target.gameObject);
            if (target is Toggle toggle && toggle.graphic == null && Missing("graphic"))
            {
                var mark = Part<Image>(target.transform, "Checkmark");
                mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0, .5f);
                mark.rectTransform.anchoredPosition = new Vector2(14, 0);
                mark.rectTransform.sizeDelta = new Vector2(20, 20);
                mark.color = Color.black;
                toggle.graphic = mark;
            }
            if (target is Slider slider)
            {
                if (slider.fillRect == null && Missing("fill"))
                {
                    var fill = Part<Image>(target.transform, "Fill");
                    StretchNative(fill.rectTransform);
                    slider.fillRect = fill.rectTransform;
                }
                if (slider.handleRect == null && Missing("handle"))
                {
                    var handle = Part<Image>(target.transform, "Handle");
                    handle.rectTransform.sizeDelta = new Vector2(20, 20);
                    slider.handleRect = handle.rectTransform;
                    if (Missing("targetGraphic"))
                        slider.targetGraphic = handle;
                }
            }
            if (target is Scrollbar bar && bar.handleRect == null && Missing("handle"))
            {
                var handle = Part<Image>(target.transform, "Handle");
                StretchNative(handle.rectTransform);
                bar.handleRect = handle.rectTransform;
                if (Missing("targetGraphic"))
                    bar.targetGraphic = handle;
            }
            if (target is TMP_InputField input)
            {
                WireInput(input, Missing);
                ReparentPart(parts, "textComponent", input.textViewport, supplied);
                ReparentPart(parts, "placeholder", input.textViewport, supplied);
            }
            if (target is InputField legacyInput)
                WireInput(legacyInput, Missing);
            if (target is TMP_Dropdown dropdown)
                WireDropdown(dropdown, parts, supplied, Missing);
            if (target is UnityEngine.UI.Dropdown legacyDropdown)
                WireDropdown(legacyDropdown, parts, supplied, Missing);
            if (target is ScrollRect scroll)
            {
                if (scroll.viewport == null && Missing("viewport"))
                {
                    var viewport = Part<RectTransform>(target.transform, "Viewport");
                    viewport.pivot = new Vector2(0, 1);
                    StretchNative(viewport);
                    GetOrAdd<RectMask2D>(viewport.gameObject);
                    var surface = GetOrAdd<Image>(viewport.gameObject);
                    surface.color = Color.clear;
                    surface.canvasRenderer.cullTransparentMesh = false;
                    scroll.viewport = viewport;
                }
                if (scroll.content == null && Missing("content"))
                {
                    var content = Part<RectTransform>(
                        scroll.viewport != null ? scroll.viewport : target.transform,
                        "Content"
                    );
                    StretchNative(content);
                    scroll.content = content;
                }
                ReparentPart(parts, "content", scroll.viewport, supplied);
            }
            if (target is UnityEngine.Canvas)
            {
                GetOrAdd<CanvasScaler>(target.gameObject);
                GetOrAdd<GraphicRaycaster>(target.gameObject);
            }
        }

        private static void ReparentPart(
            Dictionary<string, Component> parts,
            string name,
            Transform parent,
            NativePart[] supplied
        )
        {
            if (
                parent != null
                && supplied != null
                && Array.Exists(supplied, part => part?.Name == name && part.Declaration != null)
                && parts != null
                && parts.TryGetValue(name, out var part)
                && part != null
            )
                part.transform.SetParent(parent, false);
        }

        private static T Part<T>(Transform parent, string name)
            where T : Component
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.SetActive(false);
            Cleanup(root);
            root.transform.SetParent(parent, false);
            T target = GetOrAdd<T>(root);
            WireNative(target);
            root.SetActive(true);
            return target;
        }

        internal static void StretchNative(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        internal static void ControlCaption(
            Component target,
            Value<string> text,
            Dictionary<string, Component> parts,
            Part<TMP_Text>? supplied
        )
        {
            if (supplied.HasValue && supplied.Value.Declaration == null)
            {
                var value = supplied.Value.Value;
                Effect(() =>
                {
                    var caption = value.Read();
                    var content = text.Read();
                    if (caption != null)
                        caption.text = content;
                });
                return;
            }
            TMP_Text label =
                parts != null && parts.TryGetValue("caption", out var caption)
                    ? caption as TMP_Text
                    : null;
            if (label == null)
            {
                label = TMPPart(target.transform, "Text", "");
                if (target is Toggle)
                {
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    label.rectTransform.offsetMin = new Vector2(28, 0);
                }
                else
                    label.alignment = TextAlignmentOptions.Center;
            }
            Prop(label, (Value<string>?)text, (item, value) => item.text = value);
        }

        private static TextMeshProUGUI TMPPart(Transform parent, string name, string content)
        {
            var label = Part<TextMeshProUGUI>(parent, name);
            StretchNative(label.rectTransform);
            label.text = content;
            label.color = Color.black;
            label.raycastTarget = false;
            return label;
        }

        private static UnityEngine.UI.Text LegacyPart(Transform parent, string name, string content)
        {
            var label = Part<UnityEngine.UI.Text>(parent, name);
            StretchNative(label.rectTransform);
            label.text = content;
            label.color = Color.black;
            label.raycastTarget = false;
            return label;
        }

        private static void WireInput(TMP_InputField field, Func<string, bool> missing)
        {
            if (field.textViewport == null && missing("viewport"))
            {
                field.textViewport = Part<RectTransform>(field.transform, "Text viewport");
                StretchNative(field.textViewport);
                GetOrAdd<RectMask2D>(field.textViewport.gameObject);
            }
            if (field.textComponent == null && missing("textComponent"))
                field.textComponent = TMPPart(
                    field.textViewport != null ? field.textViewport : field.transform,
                    "Text",
                    ""
                );
            if (field.placeholder == null && missing("placeholder"))
                field.placeholder = TMPPart(
                    field.textViewport != null ? field.textViewport : field.transform,
                    "Placeholder",
                    "Enter text..."
                );
        }

        private static void WireInput(InputField field, Func<string, bool> missing)
        {
            if (field.textComponent == null && missing("textComponent"))
                field.textComponent = LegacyPart(field.transform, "Text", "");
            if (field.placeholder == null && missing("placeholder"))
                field.placeholder = LegacyPart(field.transform, "Placeholder", "Enter text...");
        }

        private static bool OwnsPart(NativePart[] supplied, string name) =>
            supplied != null
            && Array.Exists(supplied, part => part?.Name == name && part.Declaration != null);

        private static (RectTransform Template, Toggle Item, Graphic Text) DropdownTemplate(
            Transform parent,
            bool tmp,
            RectTransform template,
            Component item,
            Graphic text,
            Dictionary<string, Component> parts,
            NativePart[] supplied,
            Func<string, bool> missing
        )
        {
            bool generated = template == null && missing("template");
            bool owned = generated || OwnsPart(supplied, "template");
            if (generated)
            {
                template = Part<RectTransform>(parent, "Template");
                template.anchorMin = new Vector2(0, 0);
                template.anchorMax = new Vector2(1, 0);
                template.pivot = new Vector2(0.5f, 1);
                template.sizeDelta = new Vector2(0, 160);
            }
            if (template == null)
                return (null, item as Toggle, text);
            Transform content = template;
            var scroll = template.GetComponentInChildren<ScrollRect>(true);
            if (generated)
            {
                if (template.GetComponent<Graphic>() == null)
                    GetOrAdd<Image>(template.gameObject);
                scroll = GetOrAdd<ScrollRect>(template.gameObject);
                WireNative(scroll);
                scroll.horizontal = false;
                scroll.content.anchorMin = new Vector2(0, 1);
                scroll.content.anchorMax = Vector2.one;
                scroll.content.pivot = new Vector2(.5f, 1);
                scroll.content.sizeDelta = new Vector2(0, 32);
            }
            if (scroll != null && scroll.content != null)
                content = scroll.content;
            var toggle = item as Toggle ?? template.GetComponentInChildren<Toggle>(true);
            if (toggle == null && owned && missing("item"))
            {
                toggle = Part<Toggle>(content, "Item");
                StretchNative((RectTransform)toggle.transform);
                ((RectTransform)toggle.transform).anchorMin = new Vector2(0, .5f);
                ((RectTransform)toggle.transform).anchorMax = new Vector2(1, .5f);
                ((RectTransform)toggle.transform).sizeDelta = new Vector2(0, 32);
                var mark = toggle.graphic?.transform as RectTransform;
                if (mark != null)
                {
                    mark.anchorMin = mark.anchorMax = new Vector2(1, .5f);
                    mark.anchoredPosition = new Vector2(-14, 0);
                }
            }
            ReparentPart(parts, "item", content, supplied);
            if (toggle != null)
            {
                ReparentPart(parts, "itemText", toggle.transform, supplied);
                ReparentPart(parts, "itemImage", toggle.transform, supplied);
                text ??= tmp
                    ? (Graphic)toggle.GetComponentInChildren<TMP_Text>(true)
                    : toggle.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (text == null && owned && missing("itemText"))
                    text = DropdownLabel(toggle.transform, "Item label", tmp);
            }
            if (owned)
                template.gameObject.SetActive(false);
            return (template, toggle, text);
        }

        private static Image DropdownImage(Transform parent, string name)
        {
            var image = Part<Image>(parent, name);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0, 0.5f);
            image.rectTransform.anchoredPosition = new Vector2(14, 0);
            image.rectTransform.sizeDelta = new Vector2(20, 20);
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static Graphic DropdownLabel(Transform parent, string name, bool tmp)
        {
            Graphic label = tmp ? (Graphic)TMPPart(parent, name, "") : LegacyPart(parent, name, "");
            label.rectTransform.offsetMin = new Vector2(28, 0);
            label.rectTransform.offsetMax = new Vector2(-28, 0);
            return label;
        }

        private static void WireDropdown(
            TMP_Dropdown dropdown,
            Dictionary<string, Component> parts,
            NativePart[] supplied,
            Func<string, bool> missing
        )
        {
            if (dropdown.captionText == null && missing("captionText"))
                dropdown.captionText = (TMP_Text)DropdownLabel(dropdown.transform, "Label", true);
            Component item = null;
            parts?.TryGetValue("item", out item);
            var built = DropdownTemplate(
                dropdown.transform,
                true,
                dropdown.template,
                item,
                dropdown.itemText,
                parts,
                supplied,
                missing
            );
            dropdown.template = built.Template;
            if (dropdown.itemText == null && missing("itemText"))
                dropdown.itemText = built.Text as TMP_Text;
            if (
                dropdown.itemImage == null
                && missing("itemImage")
                && built.Item != null
                && (missing("template") || OwnsPart(supplied, "template"))
            )
                dropdown.itemImage = DropdownImage(built.Item.transform, "Item image");
            if (dropdown.captionImage == null && missing("captionImage"))
                dropdown.captionImage = DropdownImage(dropdown.transform, "Caption image");
        }

        private static void WireDropdown(
            UnityEngine.UI.Dropdown dropdown,
            Dictionary<string, Component> parts,
            NativePart[] supplied,
            Func<string, bool> missing
        )
        {
            if (dropdown.captionText == null && missing("captionText"))
                dropdown.captionText = (UnityEngine.UI.Text)DropdownLabel(
                    dropdown.transform,
                    "Label",
                    false
                );
            Component item = null;
            parts?.TryGetValue("item", out item);
            var built = DropdownTemplate(
                dropdown.transform,
                false,
                dropdown.template,
                item,
                dropdown.itemText,
                parts,
                supplied,
                missing
            );
            dropdown.template = built.Template;
            if (dropdown.itemText == null && missing("itemText"))
                dropdown.itemText = built.Text as UnityEngine.UI.Text;
            if (
                dropdown.itemImage == null
                && missing("itemImage")
                && built.Item != null
                && (missing("template") || OwnsPart(supplied, "template"))
            )
                dropdown.itemImage = DropdownImage(built.Item.transform, "Item image");
            if (dropdown.captionImage == null && missing("captionImage"))
                dropdown.captionImage = DropdownImage(dropdown.transform, "Caption image");
        }
    }
}
