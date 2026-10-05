using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Pine
{
    public abstract class Property
    {
        public static implicit operator Property(Component child)
            => new PropertyOperation(target => Pine.AttachChild(target, child), "Child." + (child != null ? child.transform.GetEntityId().ToString() : "null"), parent: true);
        internal virtual bool IsGroup => false;
        internal virtual bool IsParent => false;
        internal virtual int? Priority => null;
        internal virtual string Identity => null;
        internal abstract void Apply(GameObject target);
    }

    internal sealed class PropertyOperation : Property
    {
        private readonly Action<GameObject> _apply;
        private readonly string _identity;
        private readonly int? _priority;
        private readonly bool _parent;
        internal PropertyOperation(Action<GameObject> apply, string identity = null, int? priority = null, bool parent = false)
        { _apply = apply; _identity = identity; _priority = priority; _parent = parent; }
        internal override string Identity => _identity;
        internal override int? Priority => _priority;
        internal override bool IsParent => _parent;
        internal override void Apply(GameObject target) => _apply(target);
    }

    internal sealed class PropertyGroup : Property
    {
        internal readonly Property[] Properties;
        internal PropertyGroup(Property[] properties) => Properties = properties;
        internal override bool IsGroup => true;
        internal override void Apply(GameObject target) => Pine.ApplyProperties(target, Properties);
    }

    public sealed class MountHandle : IDisposable
    {
        public Scope Scope { get; internal set; }
        public GameObject Root { get; internal set; }
        public Canvas Canvas { get; internal set; }
        public void Dispose() => Scope?.Dispose();
    }

    public static partial class Pine
    {
        public static void Cleanup(UnityEngine.Object target) => Cleanup(() => DestroyObject(target));
        internal static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }

        public static T Create<T>(params Property[] properties) where T : Component
        {
            RequireScope();
            GameObject target = new(typeof(T).Name, typeof(RectTransform));
            Cleanup(target);
            T component = GetOrAdd<T>(target);
            if (Defaults) ApplyDefaults(component);
            ApplyProperties(target, properties);
            return component;
        }
        public static T Clone<T>(T template, params Property[] properties) where T : Component
        {
            RequireScope();
            GameObject target = UnityEngine.Object.Instantiate(template.gameObject);
            Cleanup(target); ApplyProperties(target, properties);
            return target.GetComponent<T>();
        }
        public static T Apply<T>(T target, params Property[] properties) where T : Component
        {
            RequireScope(); ApplyProperties(target.gameObject, properties); return target;
        }
        public static Property Group(params Property[] properties) => new PropertyGroup(properties);
        public static Property Action<T>(System.Action<T> action, int priority = 1) where T : Component
            => new PropertyOperation(target => action(Require<T>(target)), priority: priority);
        public static Property Set<T, TValue>(string name, System.Action<T, TValue> set, Value<TValue> value) where T : Component
        {
            return new PropertyOperation(target =>
            {
                BindValue(Require<T>(target), value, set);
            }, typeof(T).FullName + "." + name);
        }
        public static Property Configure<T>(System.Action<T> configure) where T : Component
            => new PropertyOperation(target => configure(Require<T>(target)));
        public static Property Set<T, TValue>(string name, System.Action<T, TValue> set, Func<TValue> read) where T : Component
            => Set(name, set, new Value<TValue>(read));
        private static void BindValue<T, TValue>(T target, Value<TValue> value, System.Action<T, TValue> set)
        {
            if (!value.IsDynamic) { set(target, value.Read()); return; }
            Effect(() =>
            {
                TValue next = value.Read();
                Observer previous = ReactiveRuntime.Observer; ReactiveRuntime.Observer = null;
                try { set(target, next); }
                finally { ReactiveRuntime.Observer = previous; }
            });
        }
        public static Property Name(string name) => new PropertyOperation(target => target.name = name, "Name");
        public static Property Parent(Value<Transform> parent) => new PropertyOperation(target =>
        {
            BindValue(target.transform, parent, (transform, next) => transform.SetParent(next, false));
        }, "Parent", parent: true);
        public static Property Active(Value<bool> active) => new PropertyOperation(target =>
        {
            BindValue(target, active, (item, next) => item.SetActive(next));
        }, "Active");
        internal static void AttachChild(GameObject target, Component child)
        {
            if (child == null) return;
            child.transform.SetParent(target.transform, false); child.transform.SetAsLastSibling();
            Cleanup(() => { if (child != null && child.transform.parent == target.transform) child.transform.SetParent(null, false); });
        }
        public static Property Children(params Component[] children) => new PropertyOperation(target =>
        {
            HashSet<Transform> seen = Strict ? new() : null;
            for (int index = 0; index < children.Length; index++)
            {
                Component child = children[index]; if (child == null) continue;
                if (seen != null && !seen.Add(child.transform)) throw new InvalidOperationException("A child can only occur once.");
                AttachChild(target, child); child.transform.SetSiblingIndex(index);
            }
        }, "Children", parent: true);
        public static Property Children(Func<IEnumerable<Component>> read) => Children(() => ComponentsToObjects(read()));
        private static IEnumerable<GameObject> ComponentsToObjects(IEnumerable<Component> components)
        {
            foreach (Component component in components) if (component != null) yield return component.gameObject;
        }
        public static Property Children(Func<IEnumerable<GameObject>> read)
        {
            return new PropertyOperation(target =>
            {
                List<Transform> previous = new(), next = new();
                HashSet<Transform> active = new();
                Cleanup(() =>
                {
                    foreach (Transform child in previous) if (child != null && child.parent == target.transform) child.SetParent(null, false);
                });
                Effect(() =>
                {
                    next.Clear(); active.Clear();
                    foreach (GameObject item in read())
                    {
                        if (item == null) continue;
                        if (!active.Add(item.transform) && Strict) throw new InvalidOperationException("A child can only occur once.");
                        next.Add(item.transform);
                    }
                    Observer observer = ReactiveRuntime.Observer; ReactiveRuntime.Observer = null;
                    try
                    {
                        foreach (Transform child in previous) if (child != null && child.parent == target.transform && !active.Contains(child)) child.SetParent(null, false);
                        for (int index = 0; index < next.Count; index++)
                        {
                            Transform child = next[index];
                            if (child.parent != target.transform) child.SetParent(target.transform, false);
                            if (child.GetSiblingIndex() != index) child.SetSiblingIndex(index);
                        }
                        List<Transform> swap = previous; previous = next; next = swap;
                    }
                    finally { ReactiveRuntime.Observer = observer; }
                });
            }, "Children", parent: true);
        }
        public static Property On<T>(Func<T, UnityEvent> select, System.Action action) where T : Component
            => Action<T>(target =>
            {
                UnityEvent evt = select(target); UnityAction handler = () => Batch(() => Untrack(action));
                evt.AddListener(handler); Cleanup(() => evt.RemoveListener(handler));
            });
        public static Property On<T, TValue>(Func<T, UnityEvent<TValue>> select, System.Action<TValue> action) where T : Component
            => Action<T>(target =>
            {
                UnityEvent<TValue> evt = select(target); UnityAction<TValue> handler = value => Batch(() => Untrack(() => action(value)));
                evt.AddListener(handler); Cleanup(() => evt.RemoveListener(handler));
            });
        public static Property OnClick(System.Action action) => On<UnityEngine.UI.Button>(target => target.onClick, action);
        public static Property Changed<T, TValue>(Func<T, TValue> read, System.Action<TValue> changed, Func<T, UnityEvent<TValue>> events = null, IEqualityComparer<TValue> comparer = null) where T : Component
        {
            return Action<T>(target =>
            {
                TValue previous = read(target); changed(previous);
                UnityAction<TValue> handler = value =>
                {
                    if ((comparer ?? EqualityComparer<TValue>.Default).Equals(previous, value)) return;
                    previous = value; Batch(() => Untrack(() => changed(value)));
                };
                if (events != null)
                {
                    UnityEvent<TValue> evt = events(target); evt.AddListener(handler); Cleanup(() => evt.RemoveListener(handler));
                }
                else
                {
                    IDisposable listener = Clock.Listen(_ => { if (target != null) handler(read(target)); });
                    Cleanup(listener);
                }
            });
        }
        public static Property ToggleValue(Source<bool> source) => Group(
            Set<UnityEngine.UI.Toggle, bool>("Value", (target, value) => target.SetIsOnWithoutNotify(value), source),
            On<UnityEngine.UI.Toggle, bool>(target => target.onValueChanged, value => source.Value = value)
        );
        public static Property SliderValue(Source<float> source) => Group(
            Set<UnityEngine.UI.Slider, float>("Value", (target, value) => target.SetValueWithoutNotify(value), source),
            On<UnityEngine.UI.Slider, float>(target => target.onValueChanged, value => source.Value = value)
        );
        public static Property InputValue(Source<string> source) => Group(
            Set<TMP_InputField, string>("Value", (target, value) => target.SetTextWithoutNotify(value), source),
            On<TMP_InputField, string>(target => target.onValueChanged, value => source.Value = value)
        );
        public static Property Text(Value<string> text) => Set<TextMeshProUGUI, string>("Text", (target, value) => target.text = value, text);
        public static Property Text(Func<string> text) => Text(new Value<string>(text));
        public static Property FontSize(Value<float> size) => Set<TextMeshProUGUI, float>("FontSize", (target, value) => target.fontSize = value, size);
        public static Property FontSize(Func<float> size) => FontSize(new Value<float>(size));
        public static Property Tint(Value<Color> color) => Set<UnityEngine.UI.Graphic, Color>("Color", (target, value) => target.color = value, color);
        public static Property Tint(Func<Color> color) => Tint(new Value<Color>(color));
        public static Property Size(Value<Vector2> size) => Set<RectTransform, Vector2>("Size", (target, value) => target.sizeDelta = value, size);
        public static Property Size(float width, float height) => Size(new Vector2(width, height));
        public static Property Size(Func<Vector2> size) => Size(new Value<Vector2>(size));
        public static Property Position(Value<Vector2> position) => Set<RectTransform, Vector2>("Position", (target, value) => target.anchoredPosition = value, position);
        public static Property Position(float x, float y) => Position(new Vector2(x, y));
        public static Property Position(Func<Vector2> position) => Position(new Value<Vector2>(position));
        public static Property Enabled(Value<bool> enabled) => Set<UnityEngine.UI.Selectable, bool>("Interactable", (target, value) => target.interactable = value, enabled);
        public static Property Enabled(Func<bool> enabled) => Enabled(new Value<bool>(enabled));
        public static Property Parent(Func<Transform> parent) => Parent(new Value<Transform>(parent));
        public static Property Active(Func<bool> active) => Active(new Value<bool>(active));
        public static Property Opacity(Value<float> opacity) => Action<RectTransform>(target =>
        {
            CanvasGroup group = GetOrAdd<CanvasGroup>(target.gameObject);
            BindValue(group, opacity, (item, value) => item.alpha = Mathf.Clamp01(value));
        });
        public static Property Opacity(Func<float> opacity) => Opacity(new Value<float>(opacity));
        public static Property Stretch() => Configure<RectTransform>(target =>
        {
            target.anchorMin = Vector2.zero; target.anchorMax = Vector2.one; target.offsetMin = Vector2.zero; target.offsetMax = Vector2.zero;
        });
        public static Property Vertical(float spacing = 8) => Action<RectTransform>(target =>
        {
            UnityEngine.UI.VerticalLayoutGroup group = GetOrAdd<UnityEngine.UI.VerticalLayoutGroup>(target.gameObject);
            group.spacing = spacing; group.childControlWidth = true; group.childControlHeight = true;
            group.childForceExpandWidth = true; group.childForceExpandHeight = false;
        });
        public static Property Horizontal(float spacing = 8) => Action<RectTransform>(target =>
        {
            UnityEngine.UI.HorizontalLayoutGroup group = GetOrAdd<UnityEngine.UI.HorizontalLayoutGroup>(target.gameObject);
            group.spacing = spacing; group.childControlWidth = true; group.childControlHeight = true;
            group.childForceExpandWidth = false; group.childForceExpandHeight = true;
        });
        public static Property PreferredSize(Value<Vector2> size) => Action<RectTransform>(target =>
        {
            UnityEngine.UI.LayoutElement layout = GetOrAdd<UnityEngine.UI.LayoutElement>(target.gameObject);
            BindValue(layout, size, (item, value) => { item.preferredWidth = value.x; item.preferredHeight = value.y; });
        });
        public static Property PreferredSize(float width, float height) => PreferredSize(new Vector2(width, height));
        public static Property PreferredSize(Func<Vector2> size) => PreferredSize(new Value<Vector2>(size));
        public static RectTransform Frame(params Property[] properties) => Create<RectTransform>(properties);
        public static RectTransform Column(params Property[] properties) => Frame(Vertical(), Group(properties));
        public static RectTransform Row(params Property[] properties) => Frame(Horizontal(), Group(properties));
        public static TextMeshProUGUI Label(Value<string> text, params Property[] properties) => Create<TextMeshProUGUI>(Text(text), Group(properties));
        public static TextMeshProUGUI Label(Func<string> text, params Property[] properties) => Label(new Value<string>(text), properties);
        public static UnityEngine.UI.Image Image(params Property[] properties) => Create<UnityEngine.UI.Image>(properties);
        public static UnityEngine.UI.Button Button(Value<string> text, System.Action click, params Property[] properties)
        {
            TextMeshProUGUI label = Create<TextMeshProUGUI>(Text(text), Stretch(), Configure<TextMeshProUGUI>(target => target.alignment = TextAlignmentOptions.Center));
            UnityEngine.UI.Button button = Create<UnityEngine.UI.Button>(OnClick(click), Children(label));
            Apply(button, properties); return button;
        }
        public static T Bind<T, TValue>(this T component, Func<TValue> read, System.Action<T, TValue> apply) where T : Component
        {
            BindValue(component, new Value<TValue>(read), apply); return component;
        }

        internal static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
        public static UnityEngine.UI.Button Button(Func<string> text, System.Action click, params Property[] properties) => Button(new Value<string>(text), click, properties);
        internal static T Require<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : throw new InvalidOperationException($"{target.name} needs {typeof(T).Name} for this property.");
        }
        internal static void ApplyProperties(GameObject target, Property[] properties)
        {
            List<Property> ordered = new(); Flatten(properties, ordered);
            foreach (Property operation in ordered.Where(item => item.Priority.HasValue).OrderBy(item => item.Priority.Value)) operation.Apply(target);
            foreach (Property operation in ordered.Where(item => !item.Priority.HasValue && !item.IsParent)) operation.Apply(target);
            foreach (Property operation in ordered.Where(item => !item.Priority.HasValue && item.IsParent)) operation.Apply(target);
        }
        private static void Flatten(Property[] properties, List<Property> output)
        {
            HashSet<string> names = new();
            foreach (Property property in properties)
            {
                if (property == null) throw new ArgumentNullException(nameof(properties));
                if (Strict && property.Identity != null && !names.Add(property.Identity)) throw new InvalidOperationException($"Duplicate property {property.Identity}.");
                if (property is PropertyGroup group)
                {
                    if (!DeferNestedProperties) Flatten(group.Properties, output);
                }
                else output.Add(property);
            }
            if (DeferNestedProperties)
                foreach (PropertyGroup group in properties.OfType<PropertyGroup>()) Flatten(group.Properties, output);
        }
        private static void ApplyDefaults(Component component)
        {
            RectTransform rect = component.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(160, 40);
            if (component is TextMeshProUGUI text)
            {
                text.font = TMP_Settings.defaultFontAsset;
                if (text.font == null) throw new InvalidOperationException("No TMP default font is available. Include a font in the application's code configuration.");
                text.fontSize = 24; text.color = Color.white; text.raycastTarget = false;
            }
            if (component is UnityEngine.UI.Button button)
            {
                UnityEngine.UI.Image image = GetOrAdd<UnityEngine.UI.Image>(component.gameObject);
                image.color = new Color(0.12f, 0.35f, 0.24f); button.targetGraphic = image;
            }
            if (component is UnityEngine.UI.Graphic graphic && !(component is TextMeshProUGUI)) graphic.raycastTarget = component is UnityEngine.UI.Selectable;
        }
    }
}
