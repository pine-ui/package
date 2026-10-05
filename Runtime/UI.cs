using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pine
{
    /// <summary>A contravariant declaration operation restricted to compatible native component types. Text properties accept TMP text; control properties accept their corresponding controls. Invalid property/component combinations fail compilation. Implement this interface for typed custom properties; Apply runs in the caller&#x27;s active ownership scope.</summary>
    /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
    /// <example>
    /// <code><![CDATA[
    /// IProperty<TMPro.TMP_Text> text = UI.Text("Hello");
    /// UI.Label("Initial", text);
    /// ]]></code>
    /// </example>
    public interface IProperty<in T> where T : Component
    {
        /// <summary>Applies this compatible property to the supplied native target in the current ownership scope. Custom implementations may create owned reactive bindings via UI.Effect and UI.Cleanup.</summary>
        /// <param name="target">The existing native component or tracked target getter, as specified by this overload.</param>
        /// <returns>The typed result described above; reactive reads participate in the active observer.</returns>
        /// <example>
        /// <code><![CDATA[
        /// UI.Apply(label, UI.Text("Applied"));
        /// ]]></code>
        /// </example>
        void Apply(T target);
    }

    internal interface IOperation
    {
        string Identity { get; }
        int? Priority { get; }
        bool Parent { get; }
        System.Collections.IEnumerable Children { get; }
    }
    internal sealed class Property<T> : IProperty<T>, IOperation where T : Component
    {
        private readonly Action<T> _apply;
        public string Identity { get; }
        public int? Priority { get; }
        public bool Parent { get; }
        public System.Collections.IEnumerable Children { get; }
        internal Property(Action<T> apply, string identity = null, int? priority = null, bool parent = false, System.Collections.IEnumerable children = null)
        { _apply = apply; Identity = identity; Priority = priority; Parent = parent; Children = children; }
        public void Apply(T target) => _apply(target);
    }

    /// <summary>A color binding shared by native Graphics and Selectables. On a Selectable it configures targetGraphic rather than requiring the control itself to inherit Graphic. The factory uses this intersection contract to keep Tint available on text, images and controls while rejecting plain frames.</summary>
    /// <example>
    /// <code><![CDATA[
    /// UI.Button("Save", () => {}, UI.Tint(UnityEngine.Color.green));
    /// ]]></code>
    /// </example>
    public sealed class GraphicProperty : IProperty<Graphic>, IProperty<Selectable>, IOperation
    {
        private readonly Action<Graphic> _apply;
        internal GraphicProperty(Action<Graphic> apply, string identity) { _apply = apply; Identity = identity; }
        /// <summary>The named native operation used by strict duplicate diagnostics. Obtain instances from UI.Tint; the operation is shared across graphic and selectable targets.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var tint = UI.Tint(UnityEngine.Color.white);
        /// string name = tint.Identity;
        /// ]]></code>
        /// </example>
        public string Identity { get; }
        int? IOperation.Priority => null;
        bool IOperation.Parent => false;
        System.Collections.IEnumerable IOperation.Children => null;
        void IProperty<Graphic>.Apply(Graphic target) => _apply(target);
        void IProperty<Selectable>.Apply(Selectable target) => _apply(target.targetGraphic != null ? target.targetGraphic : UI.Require<Graphic>(target.gameObject));
    }

    /// <summary>An explicit mounted interface lifetime. Scope owns bindings and created native objects; Root identifies the returned interface and Canvas identifies its containing canvas. Dispose removes the interface; destroying Root also disposes its scope. The result is optional for scene-lived UI; scene unload or root destruction ends the scope. Retain it for early disposal. Disabling the creating component does not remove or remount the tree.</summary>
    /// <example>
    /// <code><![CDATA[
    /// Mount mount = UI.Mount(() => UI.Label("Hello"));
    /// mount.Dispose();
    /// ]]></code>
    /// </example>
    public sealed class Mount : IDisposable
    {
        /// <summary>The scope owning this mounted interface, including bindings and native objects. Enter it with Run to apply further declarations after construction.</summary>
        /// <example>
        /// <code><![CDATA[
        /// mount.Scope.Run(() => UI.Apply(label, UI.Text("Updated")));
        /// ]]></code>
        /// </example>
        public Scope Scope { get; internal set; }
        /// <summary>The native root returned by the mount builder. Destroying it also ends the mount scope; disposal destroys owned roots.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UnityEngine.GameObject root = mount.Root;
        /// ]]></code>
        /// </example>
        public GameObject Root { get; internal set; }
        /// <summary>The Pine-created canvas or nearest ancestor canvas of an explicit parent. Existing external canvases remain externally owned.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UnityEngine.Canvas canvas = mount.Canvas;
        /// ]]></code>
        /// </example>
        public Canvas Canvas { get; internal set; }
        /// <summary>Ends this owned lifetime idempotently. Dependencies and native event/clock registrations are released; Scope/Mount cleanup attempts all resources and aggregates failures. Application code disposes a mount when its owner ends.</summary>
        /// <example>
        /// <code><![CDATA[
        /// mount.Dispose();
        /// ]]></code>
        /// </example>
        public void Dispose() => Scope?.Dispose();
    }

    public static partial class UI
    {
        /// <summary>Registers a callback, disposable or Unity object with the active scope. Cleanup occurs in reverse registration order when the scope ends or an effect reruns. Unity objects are destroyed at the end of the frame in Play Mode and immediately in Edit Mode; callback failures do not skip other resources.</summary>
        /// <param name="target">The existing native component or tracked target getter, as specified by this overload.</param>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Cleanup(() => UnityEngine.Debug.Log("Interface removed"));
        /// ]]></code>
        /// </example>
        public static void Cleanup(UnityEngine.Object target) => Cleanup(() => DestroyObject(target));
        internal static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
        /// <summary>Creates and owns a GameObject, RectTransform and the native component T, then applies compatible typed properties. Creation requires a scope. Defaults configure text and controls unless disabled. The native result can be saved, configured directly or passed into Children.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var label = UI.Create<TMPro.TextMeshProUGUI>(UI.Text("Created"), UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static T Create<T>(params IProperty<T>[] properties) where T : Component
        {
            RequireScope();
            var target = new GameObject(typeof(T).Name, typeof(RectTransform));
            Cleanup(target);
            T component = GetOrAdd<T>(target);
            if (Defaults) ApplyDefaults(component);
            ApplyProperties(component, properties);
            return component;
        }
        /// <summary>Owns a native clone of the supplied component&#x27;s GameObject, preserves its serialized native configuration, and applies compatible properties. Existing template bindings are not copied: declare new reactive bindings for the clone. The template itself remains externally owned.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <param name="template">The typed template input (T); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var copy = UI.Clone(label, UI.Text("Copy"));
        /// ]]></code>
        /// </example>
        public static T Clone<T>(T template, params IProperty<T>[] properties) where T : Component
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            RequireScope();
            var target = UnityEngine.Object.Instantiate(template.gameObject);
            Cleanup(target);
            T component = target.GetComponent<T>();
            ApplyProperties(component, properties); return component;
        }
        /// <summary>Applies compatible declarations to an existing native component in the active scope. New bindings and handlers belong to that scope; the component remains externally owned unless Pine created it or cleanup explicitly owns it. Applying later requires entering the original live scope.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <param name="target">The existing native component or tracked target getter, as specified by this overload.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The same supplied native component. Its external ownership is preserved; only bindings and resources added by these declarations belong to the active scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Apply(label, UI.Text(() => count.Value.ToString()));
        /// ]]></code>
        /// </example>
        public static T Apply<T>(T target, params IProperty<T>[] properties) where T : Component
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            RequireScope(); ApplyProperties(target, properties); return target;
        }
        /// <summary>Composes reusable properties for one explicit native target type. Contravariance allows a group targeting a base component to configure compatible derived components. Nested group processing follows DeferNestedProperties; strict duplicate diagnostics apply within each declaration group.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var textStyle = UI.Group<TMPro.TMP_Text>(UI.FontSize(24), UI.Text("Styled"));
        /// UI.Label("Initial", textStyle);
        /// ]]></code>
        /// </example>
        public static IProperty<T> Group<T>(params IProperty<T>[] properties) where T : Component
            => new Property<T>(_ => { }, children: properties ?? throw new ArgumentNullException(nameof(properties)));
        /// <summary>Runs a one-time typed action before ordinary properties and parenting. Lower numeric priorities run first; equal priorities retain declaration order. Use Configure for the ordinary-property phase. The action runs within construction ownership and context.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <param name="action">Callback/action executed in the documented phase or event scope.</param>
        /// <param name="priority">Ordering before ordinary assignments: lower runs first, equal values retain declaration order.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Action<UnityEngine.RectTransform>(frame => frame.name = "Action", priority: 0));
        /// ]]></code>
        /// </example>
        public static IProperty<T> Action<T>(Action<T> action, int priority = 1) where T : Component
            => new Property<T>(action ?? throw new ArgumentNullException(nameof(action)), priority: priority);
        /// <summary>Runs a one-time typed native configuration in the ordinary-property phase. T is the actual compatible target, so configuring another component type is rejected at compilation. Use Set or Bind when the native assignment must remain reactive.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <param name="configure">One-time typed native configuration, performed in the ordinary-property phase.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Centered", UI.Configure<TMPro.TextMeshProUGUI>(label => label.alignment = TMPro.TextAlignmentOptions.Center));
        /// ]]></code>
        /// </example>
        public static IProperty<T> Configure<T>(Action<T> configure) where T : Component
            => new Property<T>(configure ?? throw new ArgumentNullException(nameof(configure)));
        /// <summary>Declares a named typed native assignment. Literals assign once; reactive values/getters install an owned effect. Getter dependencies are tracked, but the setter is untracked so native work cannot add accidental dependencies. Names identify duplicates within strict groups and must be nonempty.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="name">Native name or nonempty property identity used by strict diagnostics.</param>
        /// <param name="set">Typed native setter run without dependency tracking.</param>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Value", UI.Set<TMPro.TMP_Text, float>("Spacing", (label, value) => label.characterSpacing = value, 2f));
        /// ]]></code>
        /// </example>
        public static IProperty<T> Set<T, TValue>(string name, Action<T, TValue> set, Value<TValue> value) where T : Component
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("A property name is required.", nameof(name));
            if (set == null) throw new ArgumentNullException(nameof(set));
            return new Property<T>(target => BindValue(target, value, set), typeof(T).FullName + "." + name);
        }
        /// <summary>Declares a named typed native assignment. Literals assign once; reactive values/getters install an owned effect. Getter dependencies are tracked, but the setter is untracked so native work cannot add accidental dependencies. Names identify duplicates within strict groups and must be nonempty.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="name">Native name or nonempty property identity used by strict diagnostics.</param>
        /// <param name="set">Typed native setter run without dependency tracking.</param>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Value", UI.Set<TMPro.TMP_Text, float>("Spacing", (label, value) => label.characterSpacing = value, 2f));
        /// ]]></code>
        /// </example>
        public static IProperty<T> Set<T, TValue>(string name, Action<T, TValue> set, Func<TValue> read) where T : Component
            => Set(name, set, new Value<TValue>(read));
        internal static void BindValue<T, TValue>(T target, Value<TValue> value, Action<T, TValue> set)
        {
            if (!value.IsDynamic) { set(target, value.Read()); return; }
            Effect(() =>
            {
                TValue next = value.Read();
                Untrack(() => set(target, next));
            });
        }
        /// <summary>Adds an owned reactive getter/setter binding to a saved native component and returns the same component. Reads collect dependencies; native assignment runs untracked. Call during construction or from a live Scope.Run; ending that scope stops updates without destroying an external component.</summary>
        /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="component">Builder returning the live native root in the mount scope.</param>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <param name="apply">The typed apply input (Action&lt;T, TValue&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>The typed result described above; reactive reads participate in the active observer.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// label.Bind(() => count.Value.ToString(), (target, value) => target.text = value);
        /// ]]></code>
        /// </example>
        public static T Bind<T, TValue>(this T component, Func<TValue> read, Action<T, TValue> apply) where T : Component
        { RequireScope(); BindValue(component, new Value<TValue>(read), apply); return component; }
        /// <summary>Binds the native GameObject name. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="name">Native name or nonempty property identity used by strict diagnostics.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Name("Panel"));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Name(Value<string> name) => Set<Component, string>("Name", (target, value) => target.name = value, name);
        /// <summary>Binds the native GameObject name. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="name">Native name or nonempty property identity used by strict diagnostics.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Name("Panel"));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Name(Func<string> name) => Name(new Value<string>(name));
        /// <summary>Binds GameObject activeSelf; disabling a native object does not dispose its construction scope. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="active">The typed active input (Value&lt;bool&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Active(() => visible.Value));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Active(Value<bool> active) => Set<Component, bool>("Active", (target, value) => target.gameObject.SetActive(value), active);
        /// <summary>Binds GameObject activeSelf; disabling a native object does not dispose its construction scope. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="active">The typed active input (Func&lt;bool&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Active(() => visible.Value));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Active(Func<bool> active) => Active(new Value<bool>(active));
        /// <summary>Binds the native parent Transform while preserving local UI transforms. Parenting runs after ordinary property application, validates uniform grid conflicts, and does not take ownership of an external parent. Sources/getters can reparent an existing native result without rebuilding it.</summary>
        /// <param name="parent">Optional external native parent; null creates a Pine-owned canvas.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Parent(canvas.transform));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Parent(Value<Transform> parent) => new Property<Component>(target =>
            BindValue(target.transform, parent, (item, next) => { if (next != null && next.TryGetComponent<PineGrid>(out var grid)) grid.ValidateChild(item); item.SetParent(next, false); }), "Parent", parent: true);
        /// <summary>Binds the native parent Transform while preserving local UI transforms. Parenting runs after ordinary property application, validates uniform grid conflicts, and does not take ownership of an external parent. Sources/getters can reparent an existing native result without rebuilding it.</summary>
        /// <param name="parent">Optional external native parent; null creates a Pine-owned canvas.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Parent(canvas.transform));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Parent(Func<Transform> parent) => Parent(new Value<Transform>(parent));
        /// <summary>Composes native child components explicitly. Fixed children attach once; getter children reconcile retained instances and sibling order as tracked inputs change. Removed external children detach; creation scopes own destruction. Duplicate native transforms are rejected in strict mode. Parenting preserves local UI transforms.</summary>
        /// <param name="children">Fixed native children or tracked getter of retained child instances; duplicates are rejected in strict mode.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Column(UI.Children(UI.Label("One"), UI.Label("Two")));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Children(params Component[] children) => new Property<Component>(target =>
        {
            if (children == null) throw new ArgumentNullException(nameof(children));
            var seen = Strict ? new HashSet<Transform>() : null;
            int index = 0;
            foreach (Component child in children)
            {
                if (child == null) continue;
                if (seen != null && !seen.Add(child.transform)) throw new InvalidOperationException("A child can only occur once.");
                AttachChild(target.transform, child.transform); child.transform.SetSiblingIndex(index++);
            }
        }, "Children", parent: true);
        internal static void AttachChild(Transform parent, Transform child)
        {
            if (parent != null && parent.TryGetComponent<PineGrid>(out var grid)) grid.ValidateChild(child);
            child.SetParent(parent, false);
            Cleanup(() => { if (child != null && child.parent == parent) child.SetParent(null, false); });
        }
        /// <summary>Composes native child components explicitly. Fixed children attach once; getter children reconcile retained instances and sibling order as tracked inputs change. Removed external children detach; creation scopes own destruction. Duplicate native transforms are rejected in strict mode. Parenting preserves local UI transforms.</summary>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Column(UI.Children(UI.Label("One"), UI.Label("Two")));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Children(Func<IEnumerable<Component>> read) => Children(() => ComponentsToObjects(read()));
        private static IEnumerable<GameObject> ComponentsToObjects(IEnumerable<Component> components)
        { foreach (Component component in components) if (component != null) yield return component.gameObject; }
        /// <summary>Composes native child components explicitly. Fixed children attach once; getter children reconcile retained instances and sibling order as tracked inputs change. Removed external children detach; creation scopes own destruction. Duplicate native transforms are rejected in strict mode. Parenting preserves local UI transforms.</summary>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Column(UI.Children(UI.Label("One"), UI.Label("Two")));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Children(Func<IEnumerable<GameObject>> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            return new Property<Component>(target =>
            {
                var previous = new List<Transform>(); var next = new List<Transform>(); var active = new HashSet<Transform>();
                Cleanup(() => { foreach (Transform child in previous) if (child != null && child.parent == target.transform) child.SetParent(null, false); });
                Effect(() =>
                {
                    next.Clear(); active.Clear();
                    foreach (GameObject item in read())
                    {
                        if (item == null) continue;
                        if (!active.Add(item.transform))
                        { if (Strict) throw new InvalidOperationException("A child can only occur once."); continue; }
                        next.Add(item.transform);
                    }
                    Untrack(() =>
                    {
                        if (target.TryGetComponent<PineGrid>(out var grid))
                            foreach (Transform child in next) grid.ValidateChild(child);
                        foreach (Transform child in previous) if (child != null && child.parent == target.transform && !active.Contains(child)) child.SetParent(null, false);
                        for (int index = 0; index < next.Count; index++)
                        {
                            Transform child = next[index];
                            if (child.parent != target.transform) child.SetParent(target.transform, false);
                            if (child.GetSiblingIndex() != index) child.SetSiblingIndex(index);
                        }
                        var swap = previous; previous = next; next = swap;
                    });
                });
            }, "Children", parent: true);
        }
        /// <summary>Registers a native UnityEvent handler for the compatible target type and removes it when the scope ends. Callbacks enter their captured construction scope and context, then batch source writes without dependency tracking. Use the generic value overload for native event payloads.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <param name="select">Native event selector or reactive branch selector, as specified by this overload.</param>
        /// <param name="action">Callback/action executed in the documented phase or event scope.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Save", () => {}, UI.On<UnityEngine.UI.Button>(b => b.onClick, () => UnityEngine.Debug.Log("Clicked")));
        /// ]]></code>
        /// </example>
        public static IProperty<T> On<T>(Func<T, UnityEvent> select, Action action) where T : Component
            => Action<T>(target =>
            {
                UnityEvent evt = select(target); Scope scope = RequireScope();
                UnityAction handler = () => { if (!scope.IsDisposed) scope.Run(() => Batch(() => Untrack(action))); };
                evt.AddListener(handler); Cleanup(() => evt.RemoveListener(handler));
            });
        /// <summary>Registers a native UnityEvent handler for the compatible target type and removes it when the scope ends. Callbacks enter their captured construction scope and context, then batch source writes without dependency tracking. Use the generic value overload for native event payloads.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="select">Native event selector or reactive branch selector, as specified by this overload.</param>
        /// <param name="action">Callback/action executed in the documented phase or event scope.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Save", () => {}, UI.On<UnityEngine.UI.Button>(b => b.onClick, () => UnityEngine.Debug.Log("Clicked")));
        /// ]]></code>
        /// </example>
        public static IProperty<T> On<T, TValue>(Func<T, UnityEvent<TValue>> select, Action<TValue> action) where T : Component
            => Action<T>(target =>
            {
                UnityEvent<TValue> evt = select(target); Scope scope = RequireScope();
                UnityAction<TValue> handler = value => { if (!scope.IsDisposed) scope.Run(() => Batch(() => Untrack(() => action(value)))); };
                evt.AddListener(handler); Cleanup(() => evt.RemoveListener(handler));
            });
        /// <summary>Registers an owned Button click handler. The callback batches writes, retains construction scope/context and is detached on cleanup. The native Button supplies mouse, touch and navigation-submit behavior through the compatible EventSystem.</summary>
        /// <param name="action">Callback/action executed in the documented phase or event scope.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Save", () => UnityEngine.Debug.Log("Saved"));
        /// ]]></code>
        /// </example>
        public static IProperty<Button> OnClick(Action action) => On<Button>(target => target.onClick, action);
        /// <summary>Observes a native value, calls the callback initially, and reports distinct changes under the supplied comparer. A supplied UnityEvent delivers changes directly; otherwise the shared clock polls the native getter. Cleanup removes the handler or polling listener. Callbacks retain their construction scope/context.</summary>
        /// <typeparam name="T">Native component target compatible with these declarations.</typeparam>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <param name="changed">Callback for the initial native value and subsequent distinct values.</param>
        /// <param name="events">Optional native event source; null uses shared-clock polling.</param>
        /// <param name="comparer">Optional equality/identity comparer; null selects the documented default policy.</param>
        /// <returns>An owned subscription that invokes the callback for the initial native value and subsequent distinct values.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Create<UnityEngine.UI.Toggle>(UI.Changed<UnityEngine.UI.Toggle, bool>(t => t.isOn, value => UnityEngine.Debug.Log(value), t => t.onValueChanged));
        /// ]]></code>
        /// </example>
        public static IProperty<T> Changed<T, TValue>(Func<T, TValue> read, Action<TValue> changed, Func<T, UnityEvent<TValue>> events = null, IEqualityComparer<TValue> comparer = null) where T : Component
        {
            return Action<T>(target =>
            {
                TValue previous = read(target); changed(previous); Scope scope = RequireScope();
                UnityAction<TValue> handler = value =>
                {
                    if ((comparer ?? EqualityComparer<TValue>.Default).Equals(previous, value) || scope.IsDisposed) return;
                    previous = value; scope.Run(() => Batch(() => Untrack(() => changed(value))));
                };
                if (events != null)
                { UnityEvent<TValue> evt = events(target); evt.AddListener(handler); Cleanup(() => evt.RemoveListener(handler)); }
                else Cleanup(Clock.Listen(_ => { if (target != null) handler(read(target)); }));
            });
        }
        /// <summary>Installs a two-way binding between Source&lt;bool&gt; and native Toggle.isOn. Source updates use SetIsOnWithoutNotify to prevent event feedback; user-generated native changes write the source. Prefer UI.Toggle(source, ...) for a completely wired control.</summary>
        /// <param name="source">Mutable typed source used by this two-way native binding.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Toggle(isEnabled, "Enabled");
        /// ]]></code>
        /// </example>
        public static IProperty<Toggle> ToggleValue(Source<bool> source) => Group<Toggle>(
            Set<Toggle, bool>("Value", (target, value) => target.SetIsOnWithoutNotify(value), source),
            On<Toggle, bool>(target => target.onValueChanged, value => source.Value = value));
        /// <summary>Installs a two-way slider binding and normalizes the source to the native slider&#x27;s clamped/rounded value. Source updates avoid synthetic value-change events; native changes update the source. Prefer UI.Slider(source, ...) for built-in graphics and handles.</summary>
        /// <param name="source">Mutable typed source used by this two-way native binding.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Slider(volume, 0f, 1f);
        /// ]]></code>
        /// </example>
        public static IProperty<Slider> SliderValue(Source<float> source) => Group<Slider>(
            Set<Slider, float>("Value", (target, value) => { target.SetValueWithoutNotify(value); source.Value = target.value; }, source),
            On<Slider, float>(target => target.onValueChanged, value => source.Value = value));
        /// <summary>Installs a two-way binding between a string source and TMP_InputField.text. Source writes use the native non-notifying setter; user edits write the source. Prefer UI.TextField(source, ...) for the complete viewport, text and placeholder hierarchy.</summary>
        /// <param name="source">Mutable typed source used by this two-way native binding.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.TextField(playerName, "Player name");
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_InputField> InputValue(Source<string> source) => Group<TMP_InputField>(
            Set<TMP_InputField, string>("Value", (target, value) => target.SetTextWithoutNotify(value ?? ""), source),
            On<TMP_InputField, string>(target => target.onValueChanged, value => source.Value = value));
        /// <summary>Binds TMP text while retaining the existing text component. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="text">The typed text input (Value&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Initial", UI.Text(() => count.Value.ToString()));
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_Text> Text(Value<string> text) => Set<TMP_Text, string>("Text", (target, value) => target.text = value, text);
        /// <summary>Binds TMP text while retaining the existing text component. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="text">The typed text input (Func&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Initial", UI.Text(() => count.Value.ToString()));
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_Text> Text(Func<string> text) => Text(new Value<string>(text));
        /// <summary>Binds TMP font size in canvas units. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="size">The typed size input (Value&lt;float&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Heading", UI.FontSize(32));
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_Text> FontSize(Value<float> size) => Set<TMP_Text, float>("FontSize", (target, value) => target.fontSize = value, size);
        /// <summary>Binds TMP font size in canvas units. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="size">The typed size input (Func&lt;float&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Heading", UI.FontSize(32));
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_Text> FontSize(Func<float> size) => FontSize(new Value<float>(size));
        /// <summary>Binds a code-supplied TMP font asset; null resolves the Pine default/fallback. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="font">The typed font input (Value&lt;TMP_FontAsset&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Localized", UI.Font(fontAsset));
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_Text> Font(Value<TMP_FontAsset> font) => Set<TMP_Text, TMP_FontAsset>("Font", (target, value) => target.font = value != null ? value : ResolveFont(), font);
        /// <summary>Binds a code-supplied TMP font asset; null resolves the Pine default/fallback. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="font">The typed font input (Func&lt;TMP_FontAsset&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Localized", UI.Font(fontAsset));
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_Text> Font(Func<TMP_FontAsset> font) => Font(new Value<TMP_FontAsset>(font));
        /// <summary>Binds Graphic.color or a Selectable targetGraphic color through a compile-safe shared property. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="color">The typed color input (Value&lt;Color&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Image(UI.Tint(UnityEngine.Color.green));
        /// ]]></code>
        /// </example>
        public static GraphicProperty Tint(Value<Color> color) => new(target => BindValue(target, color, (item, value) => item.color = value), "Graphic.Color");
        /// <summary>Binds Graphic.color or a Selectable targetGraphic color through a compile-safe shared property. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="color">The typed color input (Func&lt;Color&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Image(UI.Tint(UnityEngine.Color.green));
        /// ]]></code>
        /// </example>
        public static GraphicProperty Tint(Func<Color> color) => Tint(new Value<Color>(color));
        /// <summary>Binds Selectable.interactable; disabled controls retain their native objects and bindings. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="enabled">The typed enabled input (Value&lt;bool&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Save", () => {}, UI.Enabled(() => canSave.Value));
        /// ]]></code>
        /// </example>
        public static IProperty<Selectable> Enabled(Value<bool> enabled) => Set<Selectable, bool>("Interactable", (target, value) => target.interactable = value, enabled);
        /// <summary>Binds Selectable.interactable; disabled controls retain their native objects and bindings. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="enabled">The typed enabled input (Func&lt;bool&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Save", () => {}, UI.Enabled(() => canSave.Value));
        /// ]]></code>
        /// </example>
        public static IProperty<Selectable> Enabled(Func<bool> enabled) => Enabled(new Value<bool>(enabled));
        /// <summary>Binds the native Selectable navigation configuration, including explicit directional links. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="navigation">The typed navigation input (Value&lt;Navigation&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Next", () => {}, UI.Navigation(new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Automatic }));
        /// ]]></code>
        /// </example>
        public static IProperty<Selectable> Navigation(Value<Navigation> navigation) => Set<Selectable, Navigation>("Navigation", (target, value) => target.navigation = value, navigation);
        /// <summary>Binds the native Selectable navigation configuration, including explicit directional links. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="navigation">The typed navigation input (Func&lt;Navigation&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Next", () => {}, UI.Navigation(new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Automatic }));
        /// ]]></code>
        /// </example>
        public static IProperty<Selectable> Navigation(Func<Navigation> navigation) => Navigation(new Value<Navigation>(navigation));
        /// <summary>Selects the native control through the active EventSystem; native navigation maintains visible focus states.</summary>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Start", () => {}, UI.Focus());
        /// ]]></code>
        /// </example>
        public static IProperty<Selectable> Focus() => Action<Selectable>(target => target.Select(), int.MaxValue);
        /// <summary>Adds or reuses CanvasGroup and binds alpha clamped to the range zero through one. It does not disable interaction or destroy children. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="opacity">The typed opacity input (Value&lt;float&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Opacity(0.5f));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Opacity(Value<float> opacity) => new Property<Component>(target => BindValue(GetOrAdd<CanvasGroup>(target.gameObject), opacity, (item, value) => item.alpha = Mathf.Clamp01(value)), "Opacity");
        /// <summary>Adds or reuses CanvasGroup and binds alpha clamped to the range zero through one. It does not disable interaction or destroy children. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="opacity">The typed opacity input (Func&lt;float&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Opacity(0.5f));
        /// ]]></code>
        /// </example>
        public static IProperty<Component> Opacity(Func<float> opacity) => Opacity(new Value<float>(opacity));
        /// <summary>Creates an owned plain RectTransform for child composition and positioning. Overflow remains visible unless explicitly clipped.</summary>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Frame(UI.Size(300, 100), UI.Children(UI.Label("Panel")));
        /// ]]></code>
        /// </example>
        public static RectTransform Frame(params IProperty<RectTransform>[] properties) => Create<RectTransform>(properties);
        /// <summary>Creates an owned frame with native vertical layout and an eight-unit default gap. Use Size, Fill and Auto to state sizing intent; children compose explicitly.</summary>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Column(UI.Vertical(12), UI.Children(UI.Label("First"), UI.Label("Second")));
        /// ]]></code>
        /// </example>
        public static RectTransform Column(params IProperty<RectTransform>[] properties)
        { var frame = Frame(Vertical()); Apply(frame, properties); return frame; }
        /// <summary>Creates an owned vertical container with the supplied fixed gap and ordered native children. Children may be returned by ordinary custom component functions; they inherit the current mount or dynamic branch scope. Each factory runs once when its instance is constructed. Use the property overload for reactive spacing, sizing or dynamic child lists.</summary>
        /// <param name="gap">Fixed spacing between children in canvas units; negative spacing overlaps adjacent children.</param>
        /// <param name="children">Native child components, including results of nested custom component factories, in display order.</param>
        /// <returns>The live native RectTransform, owned by the current scope. No additional mount is created.</returns>
        /// <remarks>Call inside UI.Mount, a dynamic branch builder or a live Scope.Run. Hiding keeps the instance; removing a Pine-owned branch ends its bindings. Supply state from outside a removable branch to preserve it across reconstruction.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Mount(() =>
        /// {
        ///     var count = UI.Source(0);
        ///     return UI.Column(12,
        ///         UI.Label(() => $"Count: {count.Value}"),
        ///         UI.Button("Increment", () => count.Value++));
        /// });
        /// ]]></code>
        /// </example>
        public static RectTransform Column(float gap, params Component[] children) => Column(Vertical(gap), Children(children));
        /// <summary>Creates an owned frame with native horizontal layout and an eight-unit default gap. Child exact sizes are preserved; flexible and content-driven sizing are explicit.</summary>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Row(UI.Horizontal(12), UI.Children(UI.Label("Left"), UI.Label("Right")));
        /// ]]></code>
        /// </example>
        public static RectTransform Row(params IProperty<RectTransform>[] properties)
        { var frame = Frame(Horizontal()); Apply(frame, properties); return frame; }
        /// <summary>Creates an owned horizontal container with the supplied fixed gap and ordered native children. Custom component functions compose synchronously under the enclosing ownership scope without additional mounts. Use the property overload for reactive spacing, sizing or dynamic child lists.</summary>
        /// <param name="gap">Fixed spacing between children in canvas units; negative spacing overlaps adjacent children.</param>
        /// <param name="children">Native child components, including results of nested custom component factories, in display order.</param>
        /// <returns>The live native RectTransform, owned by the current scope. No additional mount is created.</returns>
        /// <remarks>Call inside UI.Mount, a dynamic branch builder or a live Scope.Run. Retained children update through their bindings; ordinary state changes do not rebuild the complete factory.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Row(8,
        ///     UI.Button("Save", () => Save()),
        ///     UI.Button("Cancel", () => Cancel()));
        /// ]]></code>
        /// </example>
        public static RectTransform Row(float gap, params Component[] children) => Row(Horizontal(gap), Children(children));
        /// <summary>Creates owned TextMeshProUGUI with typed literal or reactive text, bundled/default font, white text and nonblocking raycasts. Compatible text, graphic and common properties are accepted at compilation.</summary>
        /// <param name="text">The typed text input (Value&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label(() => count.Value.ToString(), UI.FontSize(24), UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static TextMeshProUGUI Label(Value<string> text, params IProperty<TextMeshProUGUI>[] properties)
        { var label = Create<TextMeshProUGUI>(Text(text)); Apply(label, properties); return label; }
        /// <summary>Creates owned TextMeshProUGUI with typed literal or reactive text, bundled/default font, white text and nonblocking raycasts. Compatible text, graphic and common properties are accepted at compilation.</summary>
        /// <param name="text">The typed text input (Func&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label(() => count.Value.ToString(), UI.FontSize(24), UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static TextMeshProUGUI Label(Func<string> text, params IProperty<TextMeshProUGUI>[] properties) => Label(new Value<string>(text), properties);
        /// <summary>Creates an owned native Image with nonblocking decorative raycasts and typed sprite/color properties. Use Progress for a native filled image.</summary>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Image(UI.Sprite(icon), UI.Size(48, 48));
        /// ]]></code>
        /// </example>
        public static Image Image(params IProperty<Image>[] properties) => Create<Image>(properties);
        /// <summary>Creates an owned native RawImage with typed texture/color properties. Supply a code-configured Texture or RenderTexture without Inspector wiring.</summary>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.RawImage(UI.Texture(texture), UI.Size(320, 180));
        /// ]]></code>
        /// </example>
        public static RawImage RawImage(params IProperty<RawImage>[] properties) => Create<RawImage>(properties);
        /// <summary>Binds the sprite of a native Image. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="sprite">The typed sprite input (Value&lt;Sprite&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Image(UI.Sprite(icon));
        /// ]]></code>
        /// </example>
        public static IProperty<Image> Sprite(Value<Sprite> sprite) => Set<Image, Sprite>("Sprite", (target, value) => target.sprite = value, sprite);
        /// <summary>Binds the sprite of a native Image. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="sprite">The typed sprite input (Func&lt;Sprite&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Image(UI.Sprite(icon));
        /// ]]></code>
        /// </example>
        public static IProperty<Image> Sprite(Func<Sprite> sprite) => Sprite(new Value<Sprite>(sprite));
        /// <summary>Binds the texture of a native RawImage. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="texture">The typed texture input (Value&lt;Texture&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.RawImage(UI.Texture(texture));
        /// ]]></code>
        /// </example>
        public static IProperty<RawImage> Texture(Value<Texture> texture) => Set<RawImage, Texture>("Texture", (target, value) => target.texture = value, texture);
        /// <summary>Binds the texture of a native RawImage. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="texture">The typed texture input (Func&lt;Texture&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.RawImage(UI.Texture(texture));
        /// ]]></code>
        /// </example>
        public static IProperty<RawImage> Texture(Func<Texture> texture) => Texture(new Value<Texture>(texture));
        internal static T GetOrAdd<T>(GameObject target) where T : Component
        { T component = target.GetComponent<T>(); return component != null ? component : target.AddComponent<T>(); }
        internal static T Require<T>(GameObject target) where T : Component
        { T component = target.GetComponent<T>(); return component != null ? component : throw new InvalidOperationException($"{target.name} needs {typeof(T).Name} for this property."); }
        internal static void ApplyProperties<T>(T target, IProperty<T>[] properties) where T : Component
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            var ordered = new List<IProperty<T>>(); Flatten(properties, ordered);
            // Stable ordering preserves declaration order for actions of equal priority.
            var actions = new List<IProperty<T>>();
            foreach (IProperty<T> item in ordered) if (item is IOperation op && op.Priority.HasValue) actions.Add(item);
            actions.Sort((a, b) => { int priority = ((IOperation)a).Priority.Value.CompareTo(((IOperation)b).Priority.Value); return priority != 0 ? priority : ordered.IndexOf(a).CompareTo(ordered.IndexOf(b)); });
            foreach (IProperty<T> action in actions) action.Apply(target);
            foreach (IProperty<T> item in ordered) if (!(item is IOperation op) || (!op.Priority.HasValue && !op.Parent)) item.Apply(target);
            foreach (IProperty<T> item in ordered) if (item is IOperation op && !op.Priority.HasValue && op.Parent) item.Apply(target);
        }
        private static void Flatten<T>(System.Collections.IEnumerable properties, List<IProperty<T>> output) where T : Component
        {
            var names = Strict ? new HashSet<string>() : null;
            foreach (object value in properties)
            {
                if (value == null) throw new ArgumentNullException(nameof(properties));
                var property = (IProperty<T>)value; var operation = value as IOperation;
                if (Strict && operation?.Identity != null && !names.Add(operation.Identity)) throw new InvalidOperationException($"Duplicate property {operation.Identity}.");
                if (operation?.Children != null) { if (!DeferNestedProperties) Flatten(operation.Children, output); }
                else output.Add(property);
            }
            if (DeferNestedProperties) foreach (object value in properties) if (value is IOperation op && op.Children != null) Flatten(op.Children, output);
        }
        private static void ApplyDefaults(Component component)
        {
            Require<RectTransform>(component.gameObject).sizeDelta = new Vector2(160, 40);
            if (component is TMP_Text text)
            { text.font = ResolveFont(); text.fontSize = 24; text.color = Color.white; text.raycastTarget = false; }
            if (component is Selectable selectable) SetupSelectable(selectable);
            if (component is Graphic graphic && !(component is TMP_Text)) graphic.raycastTarget = component is Selectable;
        }
    }
}
