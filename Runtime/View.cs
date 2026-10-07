using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pine
{
    /// <summary>A deferred native UI declaration.</summary>
    public sealed class View
    {
        internal readonly Type Type;
        internal readonly Func<GameObject, Component> Add;
        internal readonly Action<Component, Dictionary<string, Component>> Configure;
        internal readonly Action<Component> Reference,
            Publish;
        internal readonly Func<Transform, (Component Native, Value<bool>? Active)> Factory;
        internal readonly Func<IEnumerable<View>> ReadChildren;
        internal readonly Scope DeclarationScope;
        internal readonly bool Modifier,
            SameObject;
        internal readonly Value<bool>? Active;
        internal readonly View[] Entries;
        internal readonly NativePart[] Parts;
        private static Construction _construction;

        internal View(
            Type type,
            Func<GameObject, Component> add,
            Action<Component, Dictionary<string, Component>> configure,
            Action<Component> reference,
            bool modifier,
            Value<bool>? active,
            View[] entries = null,
            bool sameObject = false,
            Func<Transform, (Component Native, Value<bool>? Active)> factory = null,
            Func<IEnumerable<View>> readChildren = null,
            Scope declarationScope = null,
            Action<Component> publish = null,
            NativePart[] parts = null
        )
        {
            Type = type;
            Add = add;
            Configure = configure;
            Reference = reference;
            Publish = publish;
            Modifier = modifier;
            Active = active;
            Entries = entries == null ? Array.Empty<View>() : (View[])entries.Clone();
            foreach (var entry in Entries)
                if (entry == null)
                    throw new ArgumentNullException(nameof(entries));
            SameObject = sameObject;
            Factory = factory;
            ReadChildren = readChildren;
            DeclarationScope = declarationScope ?? ReactiveRuntime.Scope;
            Parts = parts ?? Array.Empty<NativePart>();
            if (readChildren != null && (modifier || sameObject || factory != null))
                throw new InvalidOperationException(
                    "Declare reactive children on the containing visual view, inside Create for owned behaviours."
                );
        }

        internal static bool IsConstructing => _construction != null;

        internal static void Construct(Action action)
        {
            if (_construction != null)
            {
                action();
                return;
            }
            var construction = new Construction();
            _construction = construction;
            try
            {
                P.Batch(() =>
                {
                    try
                    {
                        action();
                        construction.Complete();
                    }
                    finally
                    {
                        _construction = null;
                    }
                });
                construction.Activate();
            }
            catch
            {
                foreach (var node in construction.Nodes)
                    node.Scope.Dispose();
                throw;
            }
            finally
            {
                _construction = null;
            }
        }

        internal Component Build(Transform parent) => Build(parent, true, out _);

        internal Component Build(Transform parent, bool activate, out Value<bool>? resolvedActive)
        {
            Component result = null;
            Value<bool>? active = Active;
            Construct(() => result = BuildCore(parent, activate, out active));
            resolvedActive = active;
            return result;
        }

        private Component BuildCore(
            Transform parent,
            bool activate,
            out Value<bool>? resolvedActive
        )
        {
            if (Modifier || SameObject)
                throw new InvalidOperationException(
                    "A modifier or P.Self needs a containing visual view."
                );
            Component result = null;
            Value<bool>? active = Active;
            var nodes = new List<Node>();
            Scope scope = P.OwnedRoot(() =>
            {
                DeclarationScope?.CopyContextTo(P.RequireScope());
                if (Factory != null)
                {
                    var built = Factory(parent);
                    result = built.Native;
                    if (active.HasValue && built.Active.HasValue)
                        throw new InvalidOperationException("Declare active once per GameObject.");
                    active ??= built.Active;
                }
                else
                {
                    var root = new GameObject(Type.Name, typeof(RectTransform));
                    root.SetActive(false);
                    P.Cleanup(root);
                    if (parent != null)
                        root.transform.SetParent(parent, false);
                    result = Add(root);
                }
                if (result == null)
                    throw new InvalidOperationException(
                        "A view must create a live native component."
                    );
                var types = new HashSet<Type> { result.GetType() };
                Prepare(Entries, result.gameObject, nodes, types, true);
                Prepare(Entries, result.gameObject, nodes, types, false);
                foreach (var node in nodes)
                {
                    if (!node.View.Active.HasValue)
                        continue;
                    if (active.HasValue)
                        throw new InvalidOperationException("Declare active once per GameObject.");
                    active = node.View.Active;
                }
                nodes.Add(new Node(this, result, P.RequireScope(), activate, active));
                foreach (var node in nodes)
                {
                    _construction.Nodes.Add(node);
                    node.View.Publish?.Invoke(node.Native);
                    foreach (var part in node.View.Parts)
                        if (part?.Declaration != null)
                            node.Parts.Add(
                                part.Name,
                                part.Declaration.Build(
                                    result.transform,
                                    part.Name != "template",
                                    out _
                                )
                            );
                }
                BuildChildren(Entries, result.transform);
                BindChildren(ReadChildren, result.transform);
            });
            resolvedActive = active;
            result.gameObject.AddComponent<MountLifetime>().Scope = scope;
            RuntimeHost.ObserveView(result.gameObject, scope);
            return result;
        }

        private static bool CanRepeat(Type type) =>
            !typeof(Transform).IsAssignableFrom(type)
            && !typeof(Graphic).IsAssignableFrom(type)
            && !typeof(LayoutGroup).IsAssignableFrom(type)
            && !Attribute.IsDefined(type, typeof(DisallowMultipleComponent), true);

        private static void Prepare(
            View[] entries,
            GameObject root,
            List<Node> nodes,
            HashSet<Type> types,
            bool visual
        )
        {
            foreach (var entry in entries)
            {
                if (!entry.Modifier && !entry.SameObject)
                    continue;
                if (entry.Factory != null)
                    throw new InvalidOperationException(
                        "An owned behaviour cannot be attached with P.Self."
                    );
                if (visual != !entry.Modifier)
                {
                    Prepare(entry.Entries, root, nodes, types, visual);
                    continue;
                }
                bool repeated = !types.Add(entry.Type);
                if (repeated && !CanRepeat(entry.Type))
                    throw new InvalidOperationException(
                        "Unity permits one " + entry.Type.Name + " on this object."
                    );
                if (typeof(Graphic).IsAssignableFrom(entry.Type))
                {
                    var graphic = root.GetComponent<Graphic>();
                    if (graphic != null && graphic.GetType() != entry.Type)
                        throw new InvalidOperationException(
                            "Unity permits one Graphic per GameObject."
                        );
                }
                var component = repeated ? root.AddComponent(entry.Type) : entry.Add(root);
                if (component == null)
                    throw new InvalidOperationException(
                        "Unity could not attach " + entry.Type.Name + "."
                    );
                nodes.Add(new Node(entry, component, P.RequireScope(), false, null));
                Prepare(entry.Entries, root, nodes, types, visual);
            }
        }

        private static void BuildChildren(View[] entries, Transform parent)
        {
            foreach (var entry in entries)
                if (entry.Modifier || entry.SameObject)
                    BuildChildren(entry.Entries, parent);
                else
                    entry.Build(parent);
        }

        private static void BindChildren(Func<IEnumerable<View>> read, Transform parent)
        {
            if (read == null)
                return;
            Scope owner = P.RequireScope();
            int offset = parent.childCount;
            var mounted = new Dictionary<View, Component>();
            P.Effect(() =>
            {
                var next = new List<View>(
                    read()
                        ?? throw new InvalidOperationException(
                            "A child getter must return a collection."
                        )
                );
                var unique = new HashSet<View>();
                foreach (var view in next)
                    if (view == null || view.Modifier || view.SameObject || !unique.Add(view))
                        throw new InvalidOperationException(
                            "Reactive children must be unique visual declarations."
                        );
                P.Untrack(() =>
                    owner.Run(() =>
                        Construct(() =>
                        {
                            foreach (var old in new List<View>(mounted.Keys))
                            {
                                if (unique.Contains(old))
                                    continue;
                                var native = mounted[old];
                                if (native != null)
                                {
                                    var lifetimes = native.GetComponents<MountLifetime>();
                                    lifetimes[lifetimes.Length - 1].Scope.Dispose();
                                }
                                mounted.Remove(old);
                            }
                            for (int i = 0; i < next.Count; i++)
                            {
                                var view = next[i];
                                if (!mounted.TryGetValue(view, out var native) || native == null)
                                    mounted[view] = native = view.Build(parent);
                                native.transform.SetSiblingIndex(offset + i);
                            }
                        })
                    )
                );
            });
        }

        private sealed class Node
        {
            internal readonly View View;
            internal readonly Component Native;
            internal readonly Scope Scope;
            internal readonly bool Activate;
            internal readonly Value<bool>? Active;
            internal readonly Dictionary<string, Component> Parts = new();

            internal Node(
                View view,
                Component native,
                Scope scope,
                bool activate,
                Value<bool>? active
            )
            {
                View = view;
                Native = native;
                Scope = scope;
                Activate = activate;
                Active = active;
            }
        }

        private sealed class Construction
        {
            internal readonly List<Node> Nodes = new();

            internal void Complete()
            {
                for (int i = 0; i < Nodes.Count; i++)
                {
                    var node = Nodes[i];
                    node.Scope.Run(() =>
                    {
                        foreach (var part in node.View.Parts)
                            if (part != null)
                                if (part.Declaration != null)
                                    part.Assign?.Invoke(
                                        node.Native,
                                        node.Parts[part.Name],
                                        node.Parts
                                    );
                                else
                                    part.Bind?.Invoke(node.Native, node.Parts);
                    });
                }
                for (int i = Nodes.Count - 1; i >= 0; i--)
                {
                    var node = Nodes[i];
                    node.Scope.Run(() => P.WireNative(node.Native, node.Parts, node.View.Parts));
                }
                for (int i = Nodes.Count - 1; i >= 0; i--)
                {
                    var node = Nodes[i];
                    node.Scope.Run(() => node.View.Configure?.Invoke(node.Native, node.Parts));
                }

                for (int i = 0; i < Nodes.Count; i++)
                {
                    var node = Nodes[i];
                    node.Scope.Run(() => node.View.Reference?.Invoke(node.Native));
                }
            }

            internal void Activate()
            {
                for (int i = Nodes.Count - 1; i >= 0; i--)
                {
                    var node = Nodes[i];
                    if (!node.Activate || node.Scope.IsDisposed || node.Native == null)
                        continue;
                    node.Scope.Run(() =>
                    {
                        if (node.Active.HasValue)
                            P.Prop(
                                node.Native.gameObject,
                                node.Active,
                                (g, value) => g.SetActive(value)
                            );
                        else
                            node.Native.gameObject.SetActive(true);
                    });
                }
            }
        }
    }

    public static partial class P
    {
        /// <summary>Places a visual declaration's component on its containing GameObject.</summary>
        public static View Self(View view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            return new View(
                view.Type,
                view.Add,
                view.Configure,
                view.Reference,
                view.Modifier,
                view.Active,
                view.Entries,
                true,
                view.Factory,
                view.ReadChildren,
                view.DeclarationScope,
                view.Publish,
                view.Parts
            );
        }

        /// <summary>Declares a custom native component with owned children and optional reference capture.</summary>
        public static View Declare<T>(
            Action<T> configure = null,
            Action<T> reference = null,
            bool modifier = false,
            Value<bool>? active = null,
            View[] children = null,
            View[] components = null
        )
            where T : Component =>
            DeclareNative<T>(
                (target, _) => configure?.Invoke(target),
                reference,
                modifier,
                active,
                children,
                null,
                null,
                components
            );

        /// <summary>Declares a custom native component with retained reactive children.</summary>
        public static View Declare<T>(
            Func<IEnumerable<View>> children,
            Action<T> configure = null,
            Action<T> reference = null,
            Value<bool>? active = null,
            View[] components = null
        )
            where T : Component =>
            DeclareNative<T>(
                (target, _) => configure?.Invoke(target),
                reference,
                false,
                active,
                null,
                children ?? throw new ArgumentNullException(nameof(children)),
                null,
                components
            );

        private static View[] ComposeEntries(View[] children, View[] components)
        {
            if (components == null)
                return children;
            foreach (var component in components)
                if (component == null || (!component.Modifier && !component.SameObject))
                    throw new InvalidOperationException(
                        "components accepts only modifiers and P.Self declarations."
                    );
            var entries = new View[(children?.Length ?? 0) + components.Length];
            components.CopyTo(entries, 0);
            children?.CopyTo(entries, components.Length);
            return entries;
        }

        internal static View DeclareNative<T>(
            Action<T, Dictionary<string, Component>> configure,
            Action<T> reference,
            bool modifier,
            Value<bool>? active,
            View[] children,
            Func<IEnumerable<View>> readChildren,
            NativePart[] parts,
            View[] components = null
        )
            where T : Component
        {
            var owned = reference?.Target as INativeReference;
            return new View(
                typeof(T),
                root => GetOrAdd<T>(root),
                (target, partsMap) => configure?.Invoke((T)target, partsMap),
                owned != null ? null : target => reference?.Invoke((T)target),
                modifier,
                active,
                ComposeEntries(children, components),
                readChildren: readChildren,
                publish: owned == null ? null : target => owned.Bind(target),
                parts: parts
            );
        }

        /// <summary>Builds, wires and mounts a tree before activating its native objects.</summary>
        public static Mount Mount(
            Func<View> component,
            Transform parent = null,
            CanvasOptions options = null
        )
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            Mount mount = null;
            try
            {
                View.Construct(() =>
                    mount = Mount(
                        (Func<Component>)(
                            () =>
                                (
                                    component()
                                    ?? throw new InvalidOperationException(
                                        "A mount must return a view."
                                    )
                                ).Build(null)
                        ),
                        parent,
                        options
                    )
                );
                RuntimeHost.Ensure();
                return mount;
            }
            catch
            {
                mount?.Dispose();
                throw;
            }
        }

        /// <summary>Builds and mounts a reusable declaration once.</summary>
        public static Mount Mount(
            View view,
            Transform parent = null,
            CanvasOptions options = null
        ) => Mount(() => view ?? throw new ArgumentNullException(nameof(view)), parent, options);
    }
}
