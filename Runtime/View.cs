using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pine
{
    /// <summary>A deferred native UI declaration.</summary>
    public sealed class View
    {
        internal readonly Type Type;
        internal readonly Func<GameObject, Component> Add;
        internal readonly Action<Component> Configure;
        internal readonly Action<Component> Reference;
        internal readonly Func<Transform, (Component Native, Value<bool>? Active)> Factory;
        internal readonly Func<IEnumerable<View>> ReadChildren;
        internal readonly Scope DeclarationScope;
        internal readonly bool Modifier;
        internal readonly bool SameObject;
        internal readonly Value<bool>? Active;
        internal readonly View[] Entries;

        internal View(
            Type type,
            Func<GameObject, Component> add,
            Action<Component> configure,
            Action<Component> reference,
            bool modifier,
            Value<bool>? active,
            View[] entries = null,
            bool sameObject = false,
            Func<Transform, (Component Native, Value<bool>? Active)> factory = null,
            Func<IEnumerable<View>> readChildren = null,
            Scope declarationScope = null
        )
        {
            Type = type;
            Add = add;
            Configure = configure;
            Reference = reference;
            Modifier = modifier;
            Active = active;
            Entries = entries ?? Array.Empty<View>();
            SameObject = sameObject;
            Factory = factory;
            ReadChildren = readChildren;
            DeclarationScope = declarationScope ?? ReactiveRuntime.Scope;
        }

        /// <summary>Returns a declaration with the supplied entries appended in order.</summary>
        public View With(params View[] entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            var combined = new View[Entries.Length + entries.Length];
            Array.Copy(Entries, combined, Entries.Length);
            for (int i = 0; i < entries.Length; i++)
                combined[Entries.Length + i] =
                    entries[i] ?? throw new ArgumentNullException(nameof(entries));
            return new View(
                Type,
                Add,
                Configure,
                Reference,
                Modifier,
                Active,
                combined,
                SameObject,
                Factory,
                ReadChildren,
                DeclarationScope
            );
        }

        /// <summary>Appends tracked children.</summary>
        public View With(Func<IEnumerable<View>> children)
        {
            if (children == null)
                throw new ArgumentNullException(nameof(children));
            if (Modifier || SameObject || Factory != null)
                throw new InvalidOperationException(
                    "Declare tracked children on the containing visual view, inside Create for owned behaviours."
                );
            if (ReadChildren != null)
                throw new InvalidOperationException("Declare tracked children once per view.");
            return new View(
                Type,
                Add,
                Configure,
                Reference,
                Modifier,
                Active,
                Entries,
                SameObject,
                Factory,
                children,
                DeclarationScope
            );
        }

        internal Component Build(Transform parent) => Build(parent, true, out _);

        internal Component Build(Transform parent, bool activate, out Value<bool>? resolvedActive)
        {
            if (Modifier || SameObject)
                throw new InvalidOperationException(
                    "A modifier or P.Self declaration needs a containing visual view."
                );
            Component result = null;
            Value<bool>? active = Active;
            Scope scope = P.OwnedRoot(() =>
            {
                DeclarationScope?.CopyContextTo(P.RequireScope());
                GameObject root;
                if (Factory != null)
                {
                    var built = Factory(parent);
                    result = built.Native;
                    if (active.HasValue && built.Active.HasValue)
                        throw new InvalidOperationException("Declare active once per GameObject.");
                    active ??= built.Active;
                    if (result == null)
                        throw new InvalidOperationException(
                            "A view factory must return a live native component."
                        );
                    root = result.gameObject;
                }
                else
                {
                    root = new GameObject(Type.Name, typeof(RectTransform));
                    root.SetActive(false);
                    P.Cleanup(root);
                    if (parent != null)
                        root.transform.SetParent(parent, false);
                    result = Add(root);
                }
                var parts = new List<(View View, Component Native)>();
                var types = new HashSet<Type>();
                if (result == null)
                    throw new InvalidOperationException(
                        "Unity could not create the declared component."
                    );
                types.Add(result.GetType());
                Prepare(Entries, root, parts, types, visual: true);
                Prepare(Entries, root, parts, types, visual: false);
                P.WireNative(result);
                foreach (var part in parts)
                    P.WireNative(part.Native);
                BuildChildren(Entries, root.transform);
                BindChildren(ReadChildren, root.transform);
                foreach (var part in parts)
                    part.View.Configure?.Invoke(part.Native);
                Configure?.Invoke(result);
                foreach (var part in parts)
                    part.View.Reference?.Invoke(part.Native);
                Reference?.Invoke(result);
                foreach (var part in parts)
                {
                    if (!part.View.Active.HasValue)
                        continue;
                    if (active.HasValue)
                        throw new InvalidOperationException("Declare active once per GameObject.");
                    active = part.View.Active;
                }
                if (activate)
                {
                    if (active.HasValue)
                        P.Prop(root, active, (g, value) => g.SetActive(value));
                    else
                        root.SetActive(true);
                }
            });
            resolvedActive = active;
            try
            {
                result.gameObject.AddComponent<MountLifetime>().Scope = scope;
                RuntimeHost.ObserveView(result.gameObject, scope);
                return result;
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        private static void Prepare(
            View[] entries,
            GameObject root,
            List<(View View, Component Native)> parts,
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
                        "An owned behaviour view cannot be attached with P.Self."
                    );
                if (visual != !entry.Modifier)
                {
                    Prepare(entry.Entries, root, parts, types, visual);
                    continue;
                }
                if (!types.Add(entry.Type))
                    throw new InvalidOperationException(
                        "Only one declaration of "
                            + entry.Type.Name
                            + " can configure the same GameObject."
                    );
                if (typeof(UnityEngine.UI.Graphic).IsAssignableFrom(entry.Type))
                {
                    var graphic = root.GetComponent<UnityEngine.UI.Graphic>();
                    if (graphic != null && graphic.GetType() != entry.Type)
                        throw new InvalidOperationException(
                            "Unity permits one Graphic per GameObject. Declare this graphic as a child."
                        );
                }
                var component = entry.Add(root);
                if (component == null)
                    throw new InvalidOperationException(
                        "Unity could not attach "
                            + entry.Type.Name
                            + ". Check its required components."
                    );
                parts.Add((entry, component));
                Prepare(entry.Entries, root, parts, types, visual);
            }
        }

        private static void BuildChildren(View[] entries, Transform parent)
        {
            foreach (var entry in entries)
            {
                if (entry.Modifier || entry.SameObject)
                {
                    BuildChildren(entry.Entries, parent);
                    BindChildren(entry.ReadChildren, parent);
                }
                else
                    entry.Build(parent);
            }
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
                            "Tracked children must be unique visual declarations."
                        );
                P.Untrack(() =>
                    owner.Run(() =>
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
                );
            });
        }
    }

    public static partial class P
    {
        /// <summary>Places a visual declaration's component on its containing GameObject.</summary>
        public static View Self(View view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (view.ReadChildren != null)
                throw new InvalidOperationException(
                    "Declare tracked children on the containing visual view, not P.Self."
                );
            return new View(
                view.Type,
                view.Add,
                view.Configure,
                view.Reference,
                view.Modifier,
                view.Active,
                view.Entries,
                sameObject: true,
                factory: view.Factory,
                readChildren: view.ReadChildren,
                declarationScope: view.DeclarationScope
            );
        }

        /// <summary>Declares a custom native component using the same ownership and composition rules as built-in factories.</summary>
        public static View Declare<T>(
            Action<T> configure = null,
            Action<T> reference = null,
            bool modifier = false,
            Value<bool>? active = null
        )
            where T : Component =>
            new View(
                typeof(T),
                root => GetOrAdd<T>(root),
                target => configure?.Invoke((T)target),
                target => reference?.Invoke((T)target),
                modifier,
                active
            );

        /// <summary>Builds and mounts a deferred view once.</summary>
        public static Mount Mount(
            Func<View> component,
            Transform parent = null,
            CanvasOptions options = null
        )
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            return Mount(
                (Func<Component>)(
                    () =>
                        (
                            component()
                            ?? throw new InvalidOperationException("A mount must return a view.")
                        ).Build(null)
                ),
                parent,
                options
            );
        }

        /// <summary>Builds and mounts an existing reusable declaration once.</summary>
        public static Mount Mount(
            View view,
            Transform parent = null,
            CanvasOptions options = null
        ) => Mount(() => view ?? throw new ArgumentNullException(nameof(view)), parent, options);

        /// <summary>Composes vertical children without a settings block.</summary>
        public static View Vertical(View first, params View[] rest) =>
            Vertical().With(first).With(rest);

        /// <summary>Composes horizontal children without a settings block.</summary>
        public static View Horizontal(View first, params View[] rest) =>
            Horizontal().With(first).With(rest);
    }
}
