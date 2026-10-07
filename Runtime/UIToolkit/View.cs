using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace Pine.UIToolkit
{
    /// <summary>A deferred native VisualElement declaration.</summary>
    public sealed class View
    {
        internal readonly Type Type;
        private readonly Func<VisualElement> _create;
        private readonly Action<VisualElement, Node> _apply;
        private readonly Action<VisualElement> _reference;
        private readonly Func<View> _component;
        private readonly Scope _declarationScope;
        private static List<Action> _pending;
        private static List<Action> _activation;
        private static List<Action> _bindings;

        internal View(
            Type type,
            Func<VisualElement> create,
            Action<VisualElement, Node> apply,
            Action<VisualElement> reference = null,
            Func<View> component = null
        )
        {
            Type = type;
            _create = create;
            _apply = apply;
            _reference = reference;
            _component = component;
            _declarationScope = ReactiveRuntime.Scope;
        }

        internal static void Construct(Action build)
        {
            if (_pending != null)
            {
                build();
                return;
            }
            var pending = new List<Action>();
            _pending = pending;
            var activation = new List<Action>();
            _activation = activation;
            var bindings = new List<Action>();
            _bindings = bindings;
            try
            {
                Core.Batch(() =>
                {
                    build();
                    for (int i = 0; i < pending.Count; i++)
                        pending[i]();
                    for (int i = 0; i < bindings.Count; i++)
                        bindings[i]();
                });
                _pending = null;
                _activation = null;
                _bindings = null;
                foreach (var action in activation)
                    action();
            }
            finally
            {
                _pending = null;
                _activation = null;
                _bindings = null;
            }
        }

        internal static void Activate(Action action)
        {
            if (_activation == null)
                action();
            else
                _activation.Add(action);
        }

        internal static void BindAfterProperties(Action action)
        {
            if (_bindings == null)
                action();
            else
                _bindings.Add(action);
        }

        internal Built Build(VisualElement existing = null)
        {
            var result = new Built();
            result.Scope = Core.OwnedRoot(() =>
            {
                _declarationScope?.CopyContextTo(Core.RequireScope());
                if (_component != null)
                {
                    View view =
                        _component()
                        ?? throw new InvalidOperationException("A component must return a View.");
                    var child = view.Build(existing);
                    result.Element = child.Element;
                    return;
                }
                if (existing != null && !Type.IsInstanceOfType(existing))
                    throw new InvalidOperationException(
                        $"Target requires {Type.Name}, got {existing.GetType().Name}."
                    );
                result.Element = existing ?? _create();
                if (result.Element == null)
                    throw new InvalidOperationException("A view must create a VisualElement.");
                var node = new Node(result.Element, Core.RequireScope());
                node.IsExisting = existing != null;
                result.Node = node;
                _reference?.Invoke(result.Element);
                _pending.Add(() => node.Run(() => _apply?.Invoke(result.Element, node)));
            });
            return result;
        }
    }

    internal sealed class Built : IDisposable
    {
        internal VisualElement Element;
        internal Node Node;
        internal Scope Scope;

        public void Dispose() => Scope?.Dispose();
    }

    internal sealed class Node
    {
        internal readonly VisualElement Element;
        internal readonly Scope Scope;
        internal static Node Current;
        internal bool IncludeObsoleteValues;
        internal bool IsExisting;
        private static readonly ConditionalWeakTable<VisualElement, HashSet<string>> Writers =
            new();
        private readonly HashSet<string> _writers;

        internal Node(VisualElement element, Scope scope)
        {
            Element = element;
            Scope = scope;
            _writers = element == null ? new HashSet<string>() : Writers.GetOrCreateValue(element);
        }

        internal void Run(Action action)
        {
            Node previous = Current;
            Current = this;
            try
            {
                Core.Untrack(() => Scope.Run(action));
            }
            finally
            {
                Current = previous;
            }
        }

        internal void Claim(string property)
        {
            if (property == "isPasswordField")
                property = "isPassword";
            if (Element is TextElement && property == "value")
                property = "text";
            if (!_writers.Add(property))
                throw new InvalidOperationException(
                    $"Multiple declared writers for {property} on {Element?.GetType().Name}."
                );
            Core.Cleanup(() => _writers.Remove(property));
        }

        internal void ClaimValue(string property)
        {
            if (Element is TextElement && (property == "text" || property == "value"))
            {
                if (Element.GetBinding("text") != null || Element.GetBinding("value") != null)
                    throw new InvalidOperationException("Native binding already owns text/value.");
                property = "text";
            }
            if (Element?.GetBinding(property) != null)
                throw new InvalidOperationException($"Native binding already owns {property}.");
#if UNITY_EDITOR
            if (
                (property == "value" || property == "text")
                && Element is IBindable bindable
                && bindable.binding != null
            )
                throw new InvalidOperationException("A legacy native binding already owns value.");
#endif
            Claim(property);
        }
    }

    /// <summary>A scoped native event subscription.</summary>
    public sealed class Event
    {
        internal readonly Action<Node, VisualElement> Apply;

        internal Event(Action<Node, VisualElement> apply) => Apply = apply;
    }

    /// <summary>A native property binding installed and removed by the declaration lifetime.</summary>
    public sealed class Binding
    {
        internal readonly Action<Node, VisualElement> Apply;

        internal Binding(Action<Node, VisualElement> apply) => Apply = apply;
    }

    /// <summary>A typed declaration applied to a named element inside a UXML instance.</summary>
    public sealed class Target
    {
        internal readonly string Name;
        internal readonly View View;

        internal Target(string name, View view)
        {
            Name = name;
            View = view;
        }
    }

    public static partial class P
    {
        /// <summary>Creates a reusable component with its own setup lifetime.</summary>
        public static View Component(Func<View> create) =>
            new(
                typeof(VisualElement),
                null,
                null,
                component: create ?? throw new ArgumentNullException(nameof(create))
            );

        /// <summary>Declares a custom native element without limiting its native API.</summary>
        public static View Element<T>(
            Func<T> create,
            View[] children = null,
            Value<Style>? style = null,
            Value<string[]>? classes = null,
            Value<StyleSheet[]>? styleSheets = null,
            Event[] events = null,
            Binding[] bindings = null,
            Target[] targets = null,
            Action<T> reference = null,
            Action<T> configure = null,
            Value<bool>? enabled = null,
            IManipulator[] manipulators = null
        )
            where T : VisualElement =>
            BuildView(
                create,
                null,
                children: children,
                style: style,
                classes: classes,
                styleSheets: styleSheets,
                events: events,
                bindings: bindings,
                targets: targets,
                reference: reference,
                configure: configure,
                enabled: enabled,
                manipulators: manipulators
            );

        internal static View BuildView<T>(
            Func<T> create,
            Action<T, Node> props,
            View[] children = null,
            Func<IEnumerable<View>> readChildren = null,
            Value<Style>? style = null,
            Value<string[]>? classes = null,
            Value<StyleSheet[]>? styleSheets = null,
            Event[] events = null,
            Binding[] bindings = null,
            Target[] targets = null,
            Action<T> reference = null,
            Action<T> configure = null,
            Value<bool>? enabled = null,
            IManipulator[] manipulators = null
        )
            where T : VisualElement
        {
            if (children != null && readChildren != null)
                throw new ArgumentException("Declare static or reactive children, once.");
            children = children == null ? null : (View[])children.Clone();
            return new View(
                typeof(T),
                () => create(),
                (element, node) =>
                {
                    props?.Invoke((T)element, node);
                    Prop(element, node, "enabledSelf", enabled, (e, v) => e.SetEnabled(v));
                    if (style.HasValue)
                    {
                        if (!style.Value.IsDynamic)
                            style.Value.Read()?.Apply(node, element);
                        else
                        {
                            Scope owner = Core.RequireScope();
                            Scope current = null;
                            Core.Cleanup(() => current?.Dispose());
                            Core.Effect(() =>
                            {
                                Style next = style.Value.Read();
                                Core.Untrack(() =>
                                    owner.Run(() =>
                                    {
                                        current?.Dispose();
                                        current = Core.OwnedRoot(() => next?.Apply(node, element));
                                    })
                                );
                            });
                        }
                    }
                    if (classes.HasValue)
                    {
                        string[] owned = Array.Empty<string>();
                        Bind(
                            node,
                            () =>
                            {
                                foreach (string name in owned)
                                    element.RemoveFromClassList(name);
                                var next = classes.Value.Read() ?? Array.Empty<string>();
                                var added = new List<string>();
                                foreach (string name in next)
                                    if (!element.ClassListContains(name))
                                    {
                                        element.AddToClassList(name);
                                        added.Add(name);
                                    }
                                owned = added.ToArray();
                            }
                        );
                        Core.Cleanup(() =>
                        {
                            foreach (string name in owned)
                                element.RemoveFromClassList(name);
                        });
                    }
                    if (styleSheets.HasValue)
                    {
                        var owned = new List<StyleSheet>();
                        Bind(
                            node,
                            () =>
                            {
                                foreach (var sheet in owned)
                                    element.styleSheets.Remove(sheet);
                                owned.Clear();
                                foreach (
                                    var sheet in styleSheets.Value.Read()
                                        ?? Array.Empty<StyleSheet>()
                                )
                                    if (sheet != null && !element.styleSheets.Contains(sheet))
                                    {
                                        element.styleSheets.Add(sheet);
                                        owned.Add(sheet);
                                    }
                            }
                        );
                        Core.Cleanup(() =>
                        {
                            foreach (var sheet in owned)
                                element.styleSheets.Remove(sheet);
                        });
                    }
                    if (events != null)
                        foreach (var ev in events)
                            ev?.Apply(node, element);
                    if (bindings != null)
                        View.BindAfterProperties(() =>
                            node.Run(() =>
                            {
                                foreach (var binding in bindings)
                                    binding?.Apply(node, element);
                            })
                        );
                    if (targets != null)
                        foreach (var target in targets)
                        {
                            var native =
                                element.Q<VisualElement>(target.Name)
                                ?? throw new InvalidOperationException(
                                    $"UXML target '{target.Name}' was not found."
                                );
                            target.View.Build(native);
                        }
                    if (manipulators != null)
                        foreach (var manipulator in manipulators)
                        {
                            if (manipulator == null)
                                continue;
                            if (manipulator.target != null)
                                throw new InvalidOperationException(
                                    "Manipulator already has a target."
                                );
                            element.AddManipulator(manipulator);
                            Core.Cleanup(() =>
                            {
                                if (manipulator.target == element)
                                    element.RemoveManipulator(manipulator);
                            });
                        }
                    if (children != null || readChildren != null)
                        Children(node, element, readChildren ?? (() => children));
                    configure?.Invoke((T)element);
                },
                reference == null ? null : e => reference((T)e)
            );
        }

        /// <summary>Declares reactive children inside a custom native element.</summary>
        public static View Element<T>(
            Func<T> create,
            Func<IEnumerable<View>> children,
            Value<Style>? style = null,
            Value<string[]>? classes = null,
            Value<StyleSheet[]>? styleSheets = null,
            Event[] events = null,
            Binding[] bindings = null,
            Target[] targets = null,
            Action<T> reference = null,
            Action<T> configure = null,
            Value<bool>? enabled = null,
            IManipulator[] manipulators = null
        )
            where T : VisualElement =>
            BuildView(
                create,
                null,
                readChildren: children,
                style: style,
                classes: classes,
                styleSheets: styleSheets,
                events: events,
                bindings: bindings,
                targets: targets,
                reference: reference,
                configure: configure,
                enabled: enabled,
                manipulators: manipulators
            );

        private static void Bind(Node node, Action action)
        {
            Node previous = global::Pine.UIToolkit.Node.Current;
            global::Pine.UIToolkit.Node.Current = node;
            try
            {
                Core.Effect(action);
            }
            finally
            {
                global::Pine.UIToolkit.Node.Current = previous;
            }
        }

        internal static void Prop<TTarget, TValue>(
            TTarget target,
            Node node,
            string name,
            Value<TValue>? value,
            Action<TTarget, TValue> assign
        )
            where TTarget : class
        {
            if (!value.HasValue)
                return;
            node ??=
                global::Pine.UIToolkit.Node.Current
                ?? throw new InvalidOperationException("Native declarations require a node.");
            node.ClaimValue(name);
            var input = value.Value;
            void Write(TValue next)
            {
                if (name == "includeObsoleteValues")
                    node.IncludeObsoleteValues = (bool)(object)next;
                assign(target, next);
            }
            if (input.IsDynamic)
                Core.Effect(() => Write(input.Read()));
            else
                Write(input.Read());
        }

        internal static void CallbackProp<TTarget, TDelegate>(
            TTarget target,
            Node node,
            string name,
            Value<TDelegate>? value,
            Func<TTarget, TDelegate> read,
            Action<TTarget, TDelegate> assign
        )
            where TTarget : class
            where TDelegate : Delegate
        {
            if (!value.HasValue)
                return;
            TDelegate previous = read(target);
            TDelegate installed = null;
            Prop(
                target,
                node,
                name,
                value,
                (item, callback) =>
                {
                    assign(item, callback);
                    installed = read(item);
                }
            );
            Core.Cleanup(() =>
            {
                if (ReferenceEquals(read(target), installed))
                    assign(target, previous);
            });
        }

        internal static void InputProp<TValue>(
            VisualElement element,
            Node node,
            Value<TValue>? value,
            Action<TValue> onValueChanged = null
        )
        {
            if (!(element is INotifyValueChanged<TValue> field))
                throw new InvalidOperationException(
                    "The native element has no matching value interface."
                );
            Prop(
                field,
                node,
                "value",
                value,
                (f, v) =>
                {
                    if (v is Enum enumeration)
                    {
                        if (
                            element is EnumField enumField
                            && (
                                enumField.value == null
                                || enumField.value.GetType() != enumeration.GetType()
                            )
                        )
                            enumField.Init(enumeration, node.IncludeObsoleteValues);
#if UNITY_EDITOR
                        if (
                            element is UnityEditor.UIElements.EnumFlagsField enumFlags
                            && (
                                enumFlags.value == null
                                || enumFlags.value.GetType() != enumeration.GetType()
                            )
                        )
                            enumFlags.Init(enumeration, node.IncludeObsoleteValues);
#endif
                    }
                    f.SetValueWithoutNotify(v);
                    if (
                        value?.Writable != null
                        && !EqualityComparer<TValue>.Default.Equals(v, f.value)
                    )
                        value.Value.Writable.Set(f.value);
                }
            );
            if (value?.Writable == null && onValueChanged == null)
                return;
            EventCallback<ChangeEvent<TValue>> callback = ev =>
            {
                if (!ReferenceEquals(ev.target, element))
                    return;
                Scoped(
                    node,
                    () =>
                    {
                        if (value?.Writable != null)
                            value.Value.Writable.Set(ev.newValue);
                        onValueChanged?.Invoke(ev.newValue);
                    }
                );
            };
            element.RegisterCallback(callback);
            Core.Cleanup(() => element.UnregisterCallback(callback));
        }

        internal static void Scoped(Node node, Action action)
        {
            if (!node.Scope.IsDisposed)
                node.Run(() => Core.Batch(action));
        }

        internal static TResult Scoped<TResult>(Node node, Func<TResult> action)
        {
            TResult result = default;
            Scoped(
                node,
                () =>
                {
                    result = action();
                }
            );
            return result;
        }

        internal static void Listen<TDelegate>(
            Node node,
            TDelegate callback,
            Action<TDelegate> add,
            Action<TDelegate> remove
        )
            where TDelegate : Delegate
        {
            if (callback == null)
                return;
            add(callback);
            Core.Cleanup(() => remove(callback));
        }

        private static void Children(Node node, VisualElement parent, Func<IEnumerable<View>> read)
        {
            var entries = new Dictionary<View, Built>();
            Scope owner = Core.RequireScope();
            Core.Cleanup(() =>
            {
                foreach (var built in entries.Values)
                {
                    built.Element.RemoveFromHierarchy();
                    built.Dispose();
                }
                entries.Clear();
            });
            Core.Effect(() =>
            {
                var desired = new List<View>(read() ?? Array.Empty<View>());
                var unique = new HashSet<View>();
                foreach (var view in desired)
                    if (view == null || !unique.Add(view))
                        throw new InvalidOperationException(
                            "Children must contain distinct non-null Views."
                        );
                Core.Untrack(() =>
                    owner.Run(() =>
                        View.Construct(() =>
                        {
                            var removed = new List<View>();
                            foreach (var pair in entries)
                                if (!unique.Contains(pair.Key))
                                    removed.Add(pair.Key);
                            foreach (var view in removed)
                            {
                                var built = entries[view];
                                entries.Remove(view);
                                built.Element.RemoveFromHierarchy();
                                built.Dispose();
                            }
                            foreach (var view in desired)
                                if (!entries.ContainsKey(view))
                                    entries.Add(view, view.Build());
                            foreach (var view in desired)
                                parent.Add(entries[view].Element);
                        })
                    )
                );
            });
        }

        /// <summary>Registers a native event callback with native bubbling and trickle behavior.</summary>
        public static Event On<TEvent>(
            Action<TEvent> callback,
            TrickleDown trickleDown = TrickleDown.NoTrickleDown
        )
            where TEvent : EventBase<TEvent>, new() =>
            new(
                (node, element) =>
                {
                    EventCallback<TEvent> handler = ev => Scoped(node, () => callback(ev));
                    element.RegisterCallback(handler, trickleDown);
                    Core.Cleanup(() => element.UnregisterCallback(handler, trickleDown));
                }
            );

        /// <summary>Registers a native callback with typed user data.</summary>
        public static Event On<TEvent, TArgs>(
            Action<TEvent, TArgs> callback,
            TArgs args,
            TrickleDown trickleDown = TrickleDown.NoTrickleDown
        )
            where TEvent : EventBase<TEvent>, new() =>
            new(
                (node, element) =>
                {
                    EventCallback<TEvent, TArgs> handler = (ev, data) =>
                        Scoped(node, () => callback(ev, data));
                    element.RegisterCallback(handler, args, trickleDown);
                    Core.Cleanup(() => element.UnregisterCallback(handler, trickleDown));
                }
            );

#if PINE_UNITY_6000_3_25_OR_NEWER
        /// <summary>Registers a native callback with patch-specific callback options.</summary>
        public static Event On<TEvent>(Action<TEvent> callback, CallbackOptions options)
            where TEvent : EventBase<TEvent>, new() =>
            new(
                (node, element) =>
                {
                    EventCallback<TEvent> handler = ev => Scoped(node, () => callback(ev));
                    element.RegisterCallback(handler, options);
                    Core.Cleanup(() => element.UnregisterCallback(handler, options));
                }
            );
#endif

        /// <summary>Installs a native runtime binding without taking ownership of unrelated bindings.</summary>
        public static Binding Bind(string property, UnityEngine.UIElements.Binding binding) =>
            new(
                (node, element) =>
                {
                    node.ClaimValue(property);
                    element.SetBinding(property, binding);
                    Core.Cleanup(() =>
                    {
                        if (ReferenceEquals(element.GetBinding(property), binding))
                            element.ClearBinding(property);
                    });
                }
            );

        /// <summary>Declares typed customization of an element in a cloned UXML tree.</summary>
        public static Target Target(string name, View declaration) => new(name, declaration);

        /// <summary>Clones native UXML and applies scoped declarations to its named elements.</summary>
        public static View Template(
            VisualTreeAsset asset,
            Target[] targets = null,
            View[] children = null,
            Value<Style>? style = null,
            Value<string[]>? classes = null,
            Action<TemplateContainer> reference = null,
            Value<StyleSheet[]>? styleSheets = null,
            Event[] events = null,
            Binding[] bindings = null,
            IManipulator[] manipulators = null,
            Action<TemplateContainer> configure = null,
            Value<bool>? enabled = null
        ) =>
            BuildView(
                () => (asset ?? throw new ArgumentNullException(nameof(asset))).Instantiate(),
                null,
                children: children,
                style: style,
                classes: classes,
                targets: targets,
                reference: reference,
                styleSheets: styleSheets,
                events: events,
                bindings: bindings,
                manipulators: manipulators,
                configure: configure,
                enabled: enabled
            );

        /// <summary>Clones native UXML with reactive children and scoped native integrations.</summary>
        public static View Template(
            VisualTreeAsset asset,
            Func<IEnumerable<View>> children,
            Target[] targets = null,
            Value<Style>? style = null,
            Value<string[]>? classes = null,
            Action<TemplateContainer> reference = null,
            Value<StyleSheet[]>? styleSheets = null,
            Event[] events = null,
            Binding[] bindings = null,
            IManipulator[] manipulators = null,
            Action<TemplateContainer> configure = null,
            Value<bool>? enabled = null
        ) =>
            Element(
                () => (asset ?? throw new ArgumentNullException(nameof(asset))).Instantiate(),
                children,
                style,
                classes,
                styleSheets,
                events,
                bindings,
                targets,
                reference,
                configure,
                enabled,
                manipulators
            );
    }
}
