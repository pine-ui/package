using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pine.uGUI
{
    /// <summary>A contravariant declaration operation restricted to compatible native component types.</summary>
    public interface IProperty<in T>
        where T : Component
    {
        /// <summary>Applies this compatible property to the supplied native target in the current ownership scope.</summary>
        void Apply(T target);
    }

    internal interface IOperation
    {
        string Identity { get; }
        int? Priority { get; }
        bool Parent { get; }
        System.Collections.IEnumerable Children { get; }
    }

    internal sealed class Property<T> : IProperty<T>, IOperation
        where T : Component
    {
        private readonly Action<T> _apply;
        public string Identity { get; }
        public int? Priority { get; }
        public bool Parent { get; }
        public System.Collections.IEnumerable Children { get; }

        internal Property(
            Action<T> apply,
            string identity = null,
            int? priority = null,
            bool parent = false,
            System.Collections.IEnumerable children = null
        )
        {
            _apply = apply;
            Identity = identity;
            Priority = priority;
            Parent = parent;
            Children = children;
        }

        public void Apply(T target) => _apply(target);
    }

    /// <summary>A color binding shared by native Graphics and Selectables.</summary>
    public sealed class GraphicProperty : IProperty<Graphic>, IProperty<Selectable>, IOperation
    {
        private readonly Action<Graphic> _apply;

        internal GraphicProperty(Action<Graphic> apply, string identity)
        {
            _apply = apply;
            Identity = identity;
        }

        /// <summary>The named native operation used by strict duplicate diagnostics.</summary>
        public string Identity { get; }
        int? IOperation.Priority => null;
        bool IOperation.Parent => false;
        System.Collections.IEnumerable IOperation.Children => null;

        void IProperty<Graphic>.Apply(Graphic target) => _apply(target);

        void IProperty<Selectable>.Apply(Selectable target) =>
            _apply(
                target.targetGraphic != null
                    ? target.targetGraphic
                    : P.Require<Graphic>(target.gameObject)
            );
    }

    /// <summary>An explicit mounted interface lifetime.</summary>
    public sealed class Mount : IDisposable
    {
        /// <summary>The scope owning this mounted interface, including bindings and native objects.</summary>
        public Scope Scope { get; internal set; }

        /// <summary>The native root returned by the mount builder.</summary>
        public GameObject Root { get; internal set; }

        /// <summary>The Pine-created canvas or nearest ancestor canvas of an explicit parent.</summary>
        public Canvas Canvas { get; internal set; }

        /// <summary>Ends this owned lifetime idempotently.</summary>
        public void Dispose() => Scope?.Dispose();
    }

    public static partial class P
    {
        /// <summary>Registers a callback, disposable or Unity object with the active scope.</summary>
        public static void Cleanup(UnityEngine.Object target) =>
            Cleanup(() => DestroyObject(target));

        internal static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }

        /// <summary>Creates and owns a GameObject, RectTransform and the native component T, then applies compatible typed properties.</summary>
        public static T Create<T>(params IProperty<T>[] properties)
            where T : Component
        {
            RequireScope();
            var target = new GameObject(typeof(T).Name, typeof(RectTransform));
            Cleanup(target);
            T component = GetOrAdd<T>(target);
            if (Defaults)
                ApplyDefaults(component);
            ApplyProperties(component, properties);
            return component;
        }

        /// <summary>Owns a native clone of the supplied component's GameObject, preserves its serialized native configuration, and applies compatible properties.</summary>
        public static T Clone<T>(T template, params IProperty<T>[] properties)
            where T : Component
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            RequireScope();
            var target = UnityEngine.Object.Instantiate(template.gameObject);
            Cleanup(target);
            T component = target.GetComponent<T>();
            ApplyProperties(component, properties);
            return component;
        }

        /// <summary>Applies compatible declarations to an existing native component in the active scope.</summary>
        public static T Apply<T>(T target, params IProperty<T>[] properties)
            where T : Component
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            RequireScope();
            ApplyProperties(target, properties);
            return target;
        }

        /// <summary>Composes reusable properties for one explicit native target type.</summary>
        public static IProperty<T> Group<T>(params IProperty<T>[] properties)
            where T : Component =>
            new Property<T>(
                _ => { },
                children: properties ?? throw new ArgumentNullException(nameof(properties))
            );

        /// <summary>Runs a one-time typed action before ordinary properties and parenting.</summary>
        public static IProperty<T> Action<T>(Action<T> action, int priority = 1)
            where T : Component =>
            new Property<T>(
                action ?? throw new ArgumentNullException(nameof(action)),
                priority: priority
            );

        /// <summary>Runs a one-time typed native configuration in the ordinary-property phase.</summary>
        public static IProperty<T> Configure<T>(Action<T> configure)
            where T : Component =>
            new Property<T>(configure ?? throw new ArgumentNullException(nameof(configure)));

        /// <summary>Declares a named typed native assignment.</summary>
        public static IProperty<T> Set<T, TValue>(
            string name,
            Action<T, TValue> set,
            Value<TValue> value
        )
            where T : Component
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("A property name is required.", nameof(name));
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            return new Property<T>(
                target => BindValue(target, value, set),
                typeof(T).FullName + "." + name
            );
        }

        /// <summary>Declares a named typed native assignment.</summary>
        public static IProperty<T> Set<T, TValue>(
            string name,
            Action<T, TValue> set,
            Func<TValue> read
        )
            where T : Component => Set(name, set, new Value<TValue>(read));

        internal static void BindValue<T, TValue>(
            T target,
            Value<TValue> value,
            Action<T, TValue> set
        )
        {
            if (!value.IsDynamic)
            {
                set(target, value.Read());
                return;
            }
            Effect(() =>
            {
                TValue next = value.Read();
                Untrack(() => set(target, next));
            });
        }

        /// <summary>Adds an owned reactive getter/setter binding to a saved native component and returns the same component.</summary>
        public static T Bind<T, TValue>(
            this T component,
            Func<TValue> read,
            Action<T, TValue> apply
        )
            where T : Component
        {
            RequireScope();
            BindValue(component, new Value<TValue>(read), apply);
            return component;
        }

        /// <summary>Binds the native GameObject name.</summary>
        public static IProperty<Component> Name(Value<string> name) =>
            Set<Component, string>("Name", (target, value) => target.name = value, name);

        /// <summary>Binds the native GameObject name.</summary>
        public static IProperty<Component> Name(Func<string> name) => Name(new Value<string>(name));

        /// <summary>Binds GameObject activeSelf; disabling a native object does not dispose its construction scope.</summary>
        public static IProperty<Component> Active(Value<bool> active) =>
            new Property<Component>(
                target =>
                {
                    if (!View.TrySetActive(target.gameObject, active))
                        BindValue(
                            target,
                            active,
                            (item, value) => item.gameObject.SetActive(value)
                        );
                },
                typeof(Component).FullName + ".Active"
            );

        /// <summary>Binds GameObject activeSelf; disabling a native object does not dispose its construction scope.</summary>
        public static IProperty<Component> Active(Func<bool> active) =>
            Active(new Value<bool>(active));

        /// <summary>Binds the native parent Transform while preserving local UI transforms.</summary>
        public static IProperty<Component> Parent(Value<Transform> parent) =>
            new Property<Component>(
                target =>
                    BindValue(
                        target.transform,
                        parent,
                        (item, next) =>
                        {
                            if (next != null && next.TryGetComponent<PineGrid>(out var grid))
                                grid.ValidateChild(item);
                            item.SetParent(next, false);
                        }
                    ),
                "Parent",
                parent: true
            );

        /// <summary>Binds the native parent Transform while preserving local UI transforms.</summary>
        public static IProperty<Component> Parent(Func<Transform> parent) =>
            Parent(new Value<Transform>(parent));

        /// <summary>Composes native child components explicitly.</summary>
        public static IProperty<Component> Children(params Component[] children) =>
            new Property<Component>(
                target =>
                {
                    if (children == null)
                        throw new ArgumentNullException(nameof(children));
                    var seen = Strict ? new HashSet<Transform>() : null;
                    int index = 0;
                    foreach (Component child in children)
                    {
                        if (child == null)
                            continue;
                        if (seen != null && !seen.Add(child.transform))
                            throw new InvalidOperationException("A child can only occur once.");
                        AttachChild(target.transform, child.transform);
                        child.transform.SetSiblingIndex(index++);
                    }
                },
                "Children",
                parent: true
            );

        internal static void AttachChild(Transform parent, Transform child)
        {
            if (parent != null && parent.TryGetComponent<PineGrid>(out var grid))
                grid.ValidateChild(child);
            child.SetParent(parent, false);
            Cleanup(() =>
            {
                if (child != null && child.parent == parent)
                    child.SetParent(null, false);
            });
        }

        /// <summary>Composes native child components explicitly.</summary>
        public static IProperty<Component> Children(Func<IEnumerable<Component>> read) =>
            Children(() => ComponentsToObjects(read()));

        private static IEnumerable<GameObject> ComponentsToObjects(
            IEnumerable<Component> components
        )
        {
            foreach (Component component in components)
                if (component != null)
                    yield return component.gameObject;
        }

        /// <summary>Composes native child components explicitly.</summary>
        public static IProperty<Component> Children(Func<IEnumerable<GameObject>> read)
        {
            if (read == null)
                throw new ArgumentNullException(nameof(read));
            return new Property<Component>(
                target =>
                {
                    var previous = new List<Transform>();
                    var next = new List<Transform>();
                    var active = new HashSet<Transform>();
                    Cleanup(() =>
                    {
                        foreach (Transform child in previous)
                            if (child != null && child.parent == target.transform)
                                child.SetParent(null, false);
                    });
                    Effect(() =>
                    {
                        next.Clear();
                        active.Clear();
                        foreach (GameObject item in read())
                        {
                            if (item == null)
                                continue;
                            if (!active.Add(item.transform))
                            {
                                if (Strict)
                                    throw new InvalidOperationException(
                                        "A child can only occur once."
                                    );
                                continue;
                            }
                            next.Add(item.transform);
                        }
                        Untrack(() =>
                        {
                            if (target.TryGetComponent<PineGrid>(out var grid))
                                foreach (Transform child in next)
                                    grid.ValidateChild(child);
                            foreach (Transform child in previous)
                                if (
                                    child != null
                                    && child.parent == target.transform
                                    && !active.Contains(child)
                                )
                                    child.SetParent(null, false);
                            for (int index = 0; index < next.Count; index++)
                            {
                                Transform child = next[index];
                                if (child.parent != target.transform)
                                    child.SetParent(target.transform, false);
                                if (child.GetSiblingIndex() != index)
                                    child.SetSiblingIndex(index);
                            }
                            var swap = previous;
                            previous = next;
                            next = swap;
                        });
                    });
                },
                "Children",
                parent: true
            );
        }

        /// <summary>Registers a native UnityEvent handler for the compatible target type and removes it when the scope ends.</summary>
        public static IProperty<T> On<T>(Func<T, UnityEvent> select, Action action)
            where T : Component
        {
            if (select == null)
                throw new ArgumentNullException(nameof(select));
            return Action<T>(target => Listen(select(target), action));
        }

        /// <summary>Registers a native UnityEvent handler for the compatible target type and removes it when the scope ends.</summary>
        public static IProperty<T> On<T, TValue>(
            Func<T, UnityEvent<TValue>> select,
            Action<TValue> action
        )
            where T : Component
        {
            if (select == null)
                throw new ArgumentNullException(nameof(select));
            return Action<T>(target => Listen(select(target), action));
        }

        /// <summary>Registers an owned Button click handler.</summary>
        public static IProperty<Button> OnClick(Action action) =>
            On<Button>(target => target.onClick, action);

        /// <summary>Observes a native value, calls the callback initially, and reports distinct changes under the supplied comparer.</summary>
        public static IProperty<T> Changed<T, TValue>(
            Func<T, TValue> read,
            Action<TValue> changed,
            Func<T, UnityEvent<TValue>> events = null,
            IEqualityComparer<TValue> comparer = null
        )
            where T : Component
        {
            return Action<T>(target =>
            {
                TValue previous = read(target);
                changed(previous);
                Scope scope = RequireScope();
                UnityAction<TValue> handler = value =>
                {
                    if (
                        (comparer ?? EqualityComparer<TValue>.Default).Equals(previous, value)
                        || scope.IsDisposed
                    )
                        return;
                    previous = value;
                    scope.Run(() => Batch(() => Untrack(() => changed(value))));
                };
                if (events != null)
                {
                    UnityEvent<TValue> evt = events(target);
                    evt.AddListener(handler);
                    Cleanup(() => evt.RemoveListener(handler));
                }
                else
                    Cleanup(
                        Clock.Listen(_ =>
                        {
                            if (target != null)
                                handler(read(target));
                        })
                    );
            });
        }

        /// <summary>Installs a two-way binding between Source&lt;bool&gt; and native Toggle.isOn.</summary>
        public static IProperty<Toggle> ToggleValue(Source<bool> source) =>
            Group<Toggle>(
                Set<Toggle, bool>(
                    "Value",
                    (target, value) => target.SetIsOnWithoutNotify(value),
                    source
                ),
                On<Toggle, bool>(target => target.onValueChanged, value => source.Value = value)
            );

        /// <summary>Installs a two-way slider binding and normalizes the source to the native slider's clamped/rounded value.</summary>
        public static IProperty<Slider> SliderValue(Source<float> source) =>
            Group<Slider>(
                Set<Slider, float>(
                    "Value",
                    (target, value) =>
                    {
                        target.SetValueWithoutNotify(value);
                        source.Value = target.value;
                    },
                    source
                ),
                On<Slider, float>(target => target.onValueChanged, value => source.Value = value)
            );

        /// <summary>Installs a two-way binding between a string source and TMP_InputField.text.</summary>
        public static IProperty<TMP_InputField> InputValue(Source<string> source) =>
            Group<TMP_InputField>(
                Set<TMP_InputField, string>(
                    "Value",
                    (target, value) => target.SetTextWithoutNotify(value ?? ""),
                    source
                ),
                On<TMP_InputField, string>(
                    target => target.onValueChanged,
                    value => source.Value = value
                )
            );

        /// <summary>Binds TMP text while retaining the existing text component.</summary>
        public static IProperty<TMP_Text> TextProperty(Value<string> text) =>
            Set<TMP_Text, string>("Text", (target, value) => target.text = value, text);

        /// <summary>Binds TMP text while retaining the existing text component.</summary>
        public static IProperty<TMP_Text> TextProperty(Func<string> text) =>
            TextProperty(new Value<string>(text));

        /// <summary>Binds TMP font size in canvas units.</summary>
        public static IProperty<TMP_Text> FontSize(Value<float> size) =>
            Set<TMP_Text, float>("FontSize", (target, value) => target.fontSize = value, size);

        /// <summary>Binds TMP font size in canvas units.</summary>
        public static IProperty<TMP_Text> FontSize(Func<float> size) =>
            FontSize(new Value<float>(size));

        /// <summary>Binds a code-supplied TMP font asset; null resolves the Pine default/fallback.</summary>
        public static IProperty<TMP_Text> Font(Value<TMP_FontAsset> font) =>
            Set<TMP_Text, TMP_FontAsset>(
                "Font",
                (target, value) => target.font = value != null ? value : ResolveFont(),
                font
            );

        /// <summary>Binds a code-supplied TMP font asset; null resolves the Pine default/fallback.</summary>
        public static IProperty<TMP_Text> Font(Func<TMP_FontAsset> font) =>
            Font(new Value<TMP_FontAsset>(font));

        /// <summary>Binds Graphic.color or a Selectable targetGraphic color through a compile-safe shared property.</summary>
        public static GraphicProperty Tint(Value<Color> color) =>
            new(
                target => BindValue(target, color, (item, value) => item.color = value),
                "Graphic.Color"
            );

        /// <summary>Binds Graphic.color or a Selectable targetGraphic color through a compile-safe shared property.</summary>
        public static GraphicProperty Tint(Func<Color> color) => Tint(new Value<Color>(color));

        /// <summary>Binds Selectable.interactable; disabled controls retain their native objects and bindings.</summary>
        public static IProperty<Selectable> Enabled(Value<bool> enabled) =>
            Set<Selectable, bool>(
                "Interactable",
                (target, value) => target.interactable = value,
                enabled
            );

        /// <summary>Binds Selectable.interactable; disabled controls retain their native objects and bindings.</summary>
        public static IProperty<Selectable> Enabled(Func<bool> enabled) =>
            Enabled(new Value<bool>(enabled));

        /// <summary>Binds the native Selectable navigation configuration, including explicit directional links.</summary>
        public static IProperty<Selectable> Navigation(Value<Navigation> navigation) =>
            Set<Selectable, Navigation>(
                "Navigation",
                (target, value) => target.navigation = value,
                navigation
            );

        /// <summary>Binds the native Selectable navigation configuration, including explicit directional links.</summary>
        public static IProperty<Selectable> Navigation(Func<Navigation> navigation) =>
            Navigation(new Value<Navigation>(navigation));

        /// <summary>Selects the native control through the active EventSystem; native navigation maintains visible focus states.</summary>
        public static IProperty<Selectable> Focus() =>
            Action<Selectable>(target => target.Select(), int.MaxValue);

        /// <summary>Adds or reuses CanvasGroup and binds alpha clamped to the range zero through one.</summary>
        public static IProperty<Component> Opacity(Value<float> opacity) =>
            new Property<Component>(
                target =>
                    BindValue(
                        GetOrAdd<CanvasGroup>(target.gameObject),
                        opacity,
                        (item, value) => item.alpha = Mathf.Clamp01(value)
                    ),
                "Opacity"
            );

        /// <summary>Adds or reuses CanvasGroup and binds alpha clamped to the range zero through one.</summary>
        public static IProperty<Component> Opacity(Func<float> opacity) =>
            Opacity(new Value<float>(opacity));

        /// <summary>Binds the sprite of a native Image.</summary>
        public static IProperty<Image> Sprite(Value<Sprite> sprite) =>
            Set<Image, Sprite>("Sprite", (target, value) => target.sprite = value, sprite);

        /// <summary>Binds the sprite of a native Image.</summary>
        public static IProperty<Image> Sprite(Func<Sprite> sprite) =>
            Sprite(new Value<Sprite>(sprite));

        /// <summary>Binds the texture of a native RawImage.</summary>
        public static IProperty<RawImage> Texture(Value<Texture> texture) =>
            Set<RawImage, Texture>("Texture", (target, value) => target.texture = value, texture);

        /// <summary>Binds the texture of a native RawImage.</summary>
        public static IProperty<RawImage> Texture(Func<Texture> texture) =>
            Texture(new Value<Texture>(texture));

        internal static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            foreach (T component in target.GetComponents<T>())
                if (component.GetType() == typeof(T))
                    return component;
            return target.AddComponent<T>();
        }

        internal static T Require<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : throw new InvalidOperationException(
                    $"{target.name} needs {typeof(T).Name} for this property."
                );
        }

        internal static void ApplyProperties<T>(T target, IProperty<T>[] properties)
            where T : Component
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            var ordered = new List<IProperty<T>>();
            Flatten(properties, ordered);
            // Stable ordering preserves declaration order for actions of equal priority.
            foreach (
                IProperty<T> action in ordered
                    .Where(item => item is IOperation op && op.Priority.HasValue)
                    .OrderBy(item => ((IOperation)item).Priority.Value)
            )
                action.Apply(target);
            foreach (IProperty<T> item in ordered)
                if (!(item is IOperation op) || (!op.Priority.HasValue && !op.Parent))
                    item.Apply(target);
            foreach (IProperty<T> item in ordered)
                if (item is IOperation op && !op.Priority.HasValue && op.Parent)
                    item.Apply(target);
        }

        private static void Flatten<T>(
            System.Collections.IEnumerable properties,
            List<IProperty<T>> output
        )
            where T : Component
        {
            var names = Strict ? new HashSet<string>() : null;
            foreach (object value in properties)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(properties));
                var property = (IProperty<T>)value;
                var operation = value as IOperation;
                if (Strict && operation?.Identity != null && !names.Add(operation.Identity))
                    throw new InvalidOperationException(
                        $"Duplicate property {operation.Identity}."
                    );
                if (operation?.Children != null)
                {
                    if (!DeferNestedProperties)
                        Flatten(operation.Children, output);
                }
                else
                    output.Add(property);
            }
            if (DeferNestedProperties)
                foreach (object value in properties)
                    if (value is IOperation op && op.Children != null)
                        Flatten(op.Children, output);
        }

        private static void ApplyDefaults(Component component)
        {
            WireNative(component);
        }
    }
}
