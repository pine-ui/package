using System;
using System.Collections.Generic;

namespace Pine
{
    /// <summary>A lifetime container for reactive observers, callbacks and native resources. Run temporarily enters this scope so new operations inherit its ownership and context. Dispose is idempotent, attempts resources in reverse registration order, and aggregates cleanup failures. Independent Root scopes require explicit disposal.</summary>
    /// <example>
    /// <code><![CDATA[
    /// using var scope = UI.Root(() => UI.Effect(() => UnityEngine.Debug.Log("Ready")));
    /// scope.Run(() => UI.Cleanup(() => UnityEngine.Debug.Log("Disposed")));
    /// ]]></code>
    /// </example>
    public sealed class Scope : IDisposable
    {
        private List<IDisposable> _resources;
        private readonly bool _owned;
        internal readonly Scope Parent;
        private Dictionary<object, object> _contexts;
        internal Dictionary<object, object> ContextValues => _contexts ??= new();
        internal bool TryGetContext(object key, out object value)
        {
            value = null;
            return _contexts != null && _contexts.TryGetValue(key, out value);
        }
        /// <summary>Reports whether cleanup has begun/completed for this scope. Disposal is idempotent.</summary>
        /// <example>
        /// <code><![CDATA[
        /// if (!mount.Scope.IsDisposed) mount.Scope.Run(() => UI.Apply(label, UI.Text("Live")));
        /// ]]></code>
        /// </example>
        public bool IsDisposed { get; private set; }
        internal Scope(Scope parent, bool owned)
        {
            Parent = parent; _owned = owned;
            if (owned) parent?.Own(this);
        }
        /// <summary>Temporarily enters this live scope, preserving ownership and scoped context, and restores the previous scope afterward. Operations throw after disposal. The generic overload returns the callback result.</summary>
        /// <param name="action">Callback/action executed in the documented phase or event scope.</param>
        /// <example>
        /// <code><![CDATA[
        /// mount.Scope.Run(() => UI.Apply(label, UI.Text("Updated")));
        /// ]]></code>
        /// </example>
        public void Run(Action action)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Scope));
            Scope previous = ReactiveRuntime.Scope;
            ReactiveRuntime.Scope = this;
            try { action(); } finally { ReactiveRuntime.Scope = previous; }
        }
        /// <summary>Temporarily enters this live scope, preserving ownership and scoped context, and restores the previous scope afterward. Operations throw after disposal. The generic overload returns the callback result.</summary>
        /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="action">Callback/action executed in the documented phase or event scope.</param>
        /// <returns>The typed result described above; reactive reads participate in the active observer.</returns>
        /// <example>
        /// <code><![CDATA[
        /// mount.Scope.Run(() => UI.Apply(label, UI.Text("Updated")));
        /// ]]></code>
        /// </example>
        public T Run<T>(Func<T> action)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Scope));
            Scope previous = ReactiveRuntime.Scope;
            ReactiveRuntime.Scope = this;
            try { return action(); }
            finally { ReactiveRuntime.Scope = previous; }
        }
        /// <summary>Registers an IDisposable for reverse-order cleanup and returns the same resource. A disposed scope rejects further ownership.</summary>
        /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="resource">The typed resource input (T); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>The same disposable resource, now registered for reverse-order cleanup by this scope.</returns>
        /// <example>
        /// <code><![CDATA[
        /// scope.Own(subscription);
        /// ]]></code>
        /// </example>
        public T Own<T>(T resource) where T : IDisposable
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Scope));
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            (_resources ??= new()).Add(resource); return resource;
        }
        internal void Release(IDisposable resource) => _resources?.Remove(resource);
        internal void Reset()
        {
            try { Dispose(); }
            finally { IsDisposed = false; }
        }
        /// <summary>Ends this owned lifetime idempotently. Dependencies and native event/clock registrations are released; Scope/Mount cleanup attempts all resources and aggregates failures. Application code disposes a mount when its owner ends.</summary>
        /// <example>
        /// <code><![CDATA[
        /// scope.Dispose();
        /// ]]></code>
        /// </example>
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            if (_owned) Parent?.Release(this);
            List<Exception> errors = null;
            if (_resources == null || _resources.Count == 0) { _contexts?.Clear(); return; }
            ReactiveRuntime.BatchDepth++;
            try
            {
                while (_resources.Count > 0)
                {
                    int index = _resources.Count - 1;
                    IDisposable resource = _resources[index]; _resources.RemoveAt(index);
                    try { UI.Untrack(resource.Dispose); }
                    catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
                }
            }
            finally
            {
                _contexts?.Clear(); ReactiveRuntime.BatchDepth--; ReactiveRuntime.Flush();
            }
            if (errors != null) throw new AggregateException("Pine cleanup failed.", errors);
        }
    }

    /// <summary>A scoped typed dependency with a fallback outside providers. Provide creates a parent-owned scope whose value is available to declarations and later effects or native callbacks created inside it. The nearest provider wins; context values are not reactive by themselves. Supply reactive state as the context value when needed.</summary>
    /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
    /// <example>
    /// <code><![CDATA[
    /// var theme = UI.Context(UnityEngine.Color.white);
    /// theme.Provide(UnityEngine.Color.green, () => UI.Label("Theme", UI.Tint(theme.Value)));
    /// ]]></code>
    /// </example>
    public sealed class Context<T>
    {
        private readonly T _fallback;
        internal Context(T fallback) => _fallback = fallback;
        /// <summary>Returns the nearest scoped provider value or the configured fallback. It does not independently track reactive dependencies; a reactive context value can expose its own tracked state.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label("Theme", UI.Tint(theme.Value));
        /// ]]></code>
        /// </example>
        public T Value
        {
            get
            {
                for (Scope scope = ReactiveRuntime.Scope; scope != null; scope = scope.Parent)
                    if (scope.TryGetContext(this, out object value)) return (T)value;
                return _fallback;
            }
        }
        /// <summary>Constructs a parent-owned provider scope with this typed value. The nearest provider is retained by created effects and native callbacks. An exception during construction disposes the provider scope.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <example>
        /// <code><![CDATA[
        /// theme.Provide(UnityEngine.Color.green, () => UI.Label("Theme", UI.Tint(theme.Value)));
        /// ]]></code>
        /// </example>
        public void Provide(T value, Action build)
        {
            Scope scope = new(UI.RequireScope(), true);
            scope.ContextValues[this] = value;
            try { UI.Untrack(() => scope.Run(build)); }
            catch { scope.Dispose(); throw; }
        }
        /// <summary>Constructs a parent-owned provider scope with this typed value. The nearest provider is retained by created effects and native callbacks. An exception during construction disposes the provider scope.</summary>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <returns>The typed result described above; reactive reads participate in the active observer.</returns>
        /// <example>
        /// <code><![CDATA[
        /// theme.Provide(UnityEngine.Color.green, () => UI.Label("Theme", UI.Tint(theme.Value)));
        /// ]]></code>
        /// </example>
        public TResult Provide<TResult>(T value, Func<TResult> build)
        {
            TResult result = default;
            Provide(value, () => { result = build(); });
            return result;
        }
    }
}

