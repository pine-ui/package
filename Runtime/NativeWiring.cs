using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pine
{
    public static partial class P
    {
        private static readonly System.Reflection.FieldInfo _regexField = typeof(TMP_InputField)
            .GetField("m_RegexValue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private static void SetRegex(TMP_InputField target, string value)
        {
            if (_regexField == null) throw new NotSupportedException("This TMP version does not expose its serialized regex pattern.");
            _regexField.SetValue(target, value);
        }

        private static readonly System.Reflection.FieldInfo _pointerHoverField = typeof(UnityEngine.EventSystems.BaseInputModule)
            .GetField("m_SendPointerHoverToParent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private static void SetPointerHover(UnityEngine.EventSystems.BaseInputModule target, bool value)
        {
            if (_pointerHoverField == null) throw new NotSupportedException("This uGUI version does not expose its serialized pointer hover setting.");
            _pointerHoverField.SetValue(target, value);
        }

        internal static void Prop<T, TValue>(T target, Value<TValue>? value, Action<T, TValue> assign)
            where T : UnityEngine.Object
        {
            if (!value.HasValue) return;
            BindValue(target, value.Value, (item, next) => { if (item != null) assign(item, next); });
        }

        internal static void InputProp<T, TValue>(T target, Value<TValue>? value, Func<T, TValue> read,
            Action<T, TValue> assign, Func<T, UnityEvent<TValue>> events) where T : Component
        {
            if (!value.HasValue) return;
            var input = value.Value;
            BindValue(target, input, (item, next) =>
            {
                if (item == null) return;
                assign(item, next);
                if (input.Writable != null) input.Writable.Value = read(item);
            });
            if (input.Writable != null) Listen(events(target), next => input.Writable.Value = next);
        }

        internal static void Listen(UnityEvent events, Action action)
        {
            if (action == null) return;
            Scope scope = RequireScope();
            UnityAction handler = () => { if (!scope.IsDisposed) scope.Run(() => Batch(() => Untrack(action))); };
            events.AddListener(handler); Cleanup(() => events.RemoveListener(handler));
        }
        internal static void Listen<T>(UnityEvent<T> events, Action<T> action)
        {
            if (action == null) return;
            Scope scope = RequireScope();
            UnityAction<T> handler = value => { if (!scope.IsDisposed) scope.Run(() => Batch(() => Untrack(() => action(value)))); };
            events.AddListener(handler); Cleanup(() => events.RemoveListener(handler));
        }
        internal static void Listen<T1, T2>(UnityEvent<T1, T2> events, Action<T1, T2> action)
        {
            if (action == null) return;
            Scope scope = RequireScope();
            UnityAction<T1, T2> handler = (a, b) => { if (!scope.IsDisposed) scope.Run(() => Batch(() => Untrack(() => action(a, b)))); };
            events.AddListener(handler); Cleanup(() => events.RemoveListener(handler));
        }
        internal static void Listen<T1, T2, T3>(UnityEvent<T1, T2, T3> events, Action<T1, T2, T3> action)
        {
            if (action == null) return;
            Scope scope = RequireScope();
            UnityAction<T1, T2, T3> handler = (a, b, c) => { if (!scope.IsDisposed) scope.Run(() => Batch(() => Untrack(() => action(a, b, c)))); };
            events.AddListener(handler); Cleanup(() => events.RemoveListener(handler));
        }
        internal static void Listen<T1, T2, T3, T4>(UnityEvent<T1, T2, T3, T4> events, Action<T1, T2, T3, T4> action)
        {
            if (action == null) return;
            Scope scope = RequireScope();
            UnityAction<T1, T2, T3, T4> handler = (a, b, c, d) => { if (!scope.IsDisposed) scope.Run(() => Batch(() => Untrack(() => action(a, b, c, d)))); };
            events.AddListener(handler); Cleanup(() => events.RemoveListener(handler));
        }

        internal static void WireNative(Component target)
        {
#if ENABLE_INPUT_SYSTEM
            if (target is UnityEngine.InputSystem.UI.InputSystemUIInputModule inputModule && inputModule.actionsAsset == null)
                inputModule.AssignDefaultActions();
#endif
            if (target is TMP_Text text && text.font == null) text.font = ResolveFont();
            if (target is UnityEngine.UI.Text legacyText && legacyText.font == null)
                legacyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (target is Selectable selectable && selectable.targetGraphic == null)
                selectable.targetGraphic = GetOrAdd<Image>(target.gameObject);
            if (target is Toggle toggle && toggle.graphic == null)
            {
                var mark = Part<Image>(target.transform, "Checkmark");
                mark.rectTransform.sizeDelta = new Vector2(20, 20); mark.color = Color.black;
                toggle.graphic = mark;
            }
            if (target is Slider slider)
            {
                if (slider.fillRect == null)
                {
                    var fill = Part<Image>(target.transform, "Fill"); StretchNative(fill.rectTransform);
                    slider.fillRect = fill.rectTransform;
                }
                if (slider.handleRect == null)
                {
                    var handle = Part<Image>(target.transform, "Handle");
                    handle.rectTransform.sizeDelta = new Vector2(20, 20);
                    slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
                }
            }
            if (target is Scrollbar bar && bar.handleRect == null)
            {
                var handle = Part<Image>(target.transform, "Handle"); StretchNative(handle.rectTransform);
                bar.handleRect = handle.rectTransform; bar.targetGraphic = handle;
            }
            if (target is TMP_InputField input) WireInput(input);
            if (target is InputField legacyInput) WireInput(legacyInput);
            if (target is TMP_Dropdown dropdown) WireDropdown(dropdown);
            if (target is UnityEngine.UI.Dropdown legacyDropdown) WireDropdown(legacyDropdown);
            if (target is ScrollRect scroll && scroll.viewport == null)
            {
                var viewport = Part<RectTransform>(target.transform, "Viewport"); StretchNative(viewport);
                GetOrAdd<RectMask2D>(viewport.gameObject);
                var surface = GetOrAdd<Image>(viewport.gameObject); surface.color = Color.clear;
                scroll.viewport = viewport;
                var content = Part<RectTransform>(viewport, "Content"); StretchNative(content);
                scroll.content = content;
            }
            if (target is UnityEngine.Canvas)
            {
                GetOrAdd<CanvasScaler>(target.gameObject);
                GetOrAdd<GraphicRaycaster>(target.gameObject);
            }
        }

        private static T Part<T>(Transform parent, string name) where T : Component
        {
            var root = new GameObject(name, typeof(RectTransform)); root.SetActive(false);
            Cleanup(root); root.transform.SetParent(parent, false);
            T target = GetOrAdd<T>(root); WireNative(target); root.SetActive(true);
            return target;
        }
        internal static void StretchNative(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        internal static void ControlCaption(Component target, Value<string> text)
        {
            Text(text, color: Color.black, raycastTarget: false, alignment: TextAlignmentOptions.Center,
                anchorMin: Vector2.zero, anchorMax: Vector2.one, sizeDelta: Vector2.zero, name: "Text")
                .Build(target.transform);
        }
        private static TextMeshProUGUI TMPPart(Transform parent, string name, string content)
        {
            var label = Part<TextMeshProUGUI>(parent, name);
            StretchNative(label.rectTransform); label.text = content; label.color = Color.black;
            label.raycastTarget = false;
            return label;
        }
        private static UnityEngine.UI.Text LegacyPart(Transform parent, string name, string content)
        {
            var label = Part<UnityEngine.UI.Text>(parent, name);
            StretchNative(label.rectTransform); label.text = content; label.color = Color.black;
            label.raycastTarget = false;
            return label;
        }
        private static void WireInput(TMP_InputField field)
        {
            if (field.textViewport == null)
            {
                field.textViewport = Part<RectTransform>(field.transform, "Text viewport");
                StretchNative(field.textViewport); GetOrAdd<RectMask2D>(field.textViewport.gameObject);
            }
            if (field.textComponent == null)
                field.textComponent = TMPPart(field.textViewport, "Text", "");
            if (field.placeholder == null)
                field.placeholder = TMPPart(field.textViewport, "Placeholder", "Enter text...");
            if (field.fontAsset == null) field.fontAsset = ResolveFont();
        }
        private static void WireInput(InputField field)
        {
            if (field.textComponent == null) field.textComponent = LegacyPart(field.transform, "Text", "");
            if (field.placeholder == null) field.placeholder = LegacyPart(field.transform, "Placeholder", "Enter text...");
        }
        private static RectTransform DropdownTemplate(Transform parent, bool tmp, out Graphic itemText)
        {
            var template = Part<RectTransform>(parent, "Template");
            template.anchorMin = new Vector2(0, 0); template.anchorMax = new Vector2(1, 0);
            template.pivot = new Vector2(0.5f, 1); template.sizeDelta = new Vector2(0, 160);
            var background = GetOrAdd<Image>(template.gameObject);
            var scroll = GetOrAdd<ScrollRect>(template.gameObject);
            var viewport = Part<RectTransform>(template, "Viewport"); StretchNative(viewport);
            GetOrAdd<RectMask2D>(viewport.gameObject);
            var content = Part<RectTransform>(viewport, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = new Vector2(0, 32);
            var item = Part<Toggle>(content, "Item");
            var rect = (RectTransform)item.transform; StretchNative(rect); rect.sizeDelta = new Vector2(0, 32);
            itemText = tmp ? (Graphic)TMPPart(item.transform, "Item label", "") : LegacyPart(item.transform, "Item label", "");
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            template.gameObject.SetActive(false);
            return template;
        }
        private static void WireDropdown(TMP_Dropdown dropdown)
        {
            if (dropdown.captionText == null) dropdown.captionText = TMPPart(dropdown.transform, "Label", "");
            if (dropdown.template == null)
            {
                dropdown.template = DropdownTemplate(dropdown.transform, true, out var label);
                dropdown.itemText = (TMP_Text)label;
            }
        }
        private static void WireDropdown(UnityEngine.UI.Dropdown dropdown)
        {
            if (dropdown.captionText == null) dropdown.captionText = LegacyPart(dropdown.transform, "Label", "");
            if (dropdown.template == null)
            {
                dropdown.template = DropdownTemplate(dropdown.transform, false, out var label);
                dropdown.itemText = (UnityEngine.UI.Text)label;
            }
        }
    }
}
