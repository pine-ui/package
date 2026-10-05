using System;
using System.Collections.Generic;

namespace Pine
{
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
        public bool IsDisposed { get; private set; }
        internal Scope(Scope parent, bool owned)
        {
            Parent = parent; _owned = owned;
            if (owned) parent?.Own(this);
        }
        public void Run(Action action)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Scope));
            Scope previous = ReactiveRuntime.Scope;
            ReactiveRuntime.Scope = this;
            try { action(); } finally { ReactiveRuntime.Scope = previous; }
        }
        public T Run<T>(Func<T> action)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Scope));
            Scope previous = ReactiveRuntime.Scope;
            ReactiveRuntime.Scope = this;
            try { return action(); }
            finally { ReactiveRuntime.Scope = previous; }
        }
        public T Own<T>(T resource) where T : IDisposable
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Scope));
            (_resources ??= new()).Add(resource); return resource;
        }
        internal void Release(IDisposable resource) => _resources?.Remove(resource);
        internal void Reset()
        {
            try { Dispose(); }
            finally { IsDisposed = false; }
        }
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
                    try { Pine.Untrack(resource.Dispose); }
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

    public sealed class Context<T>
    {
        private readonly T _fallback;
        internal Context(T fallback) => _fallback = fallback;
        public T Value
        {
            get
            {
                for (Scope scope = ReactiveRuntime.Scope; scope != null; scope = scope.Parent)
                    if (scope.TryGetContext(this, out object value)) return (T)value;
                return _fallback;
            }
        }
        public void Provide(T value, Action build)
        {
            Scope scope = new(Pine.RequireScope(), true);
            scope.ContextValues[this] = value;
            try { Pine.Untrack(() => scope.Run(build)); }
            catch { scope.Dispose(); throw; }
        }
        public TResult Provide<TResult>(T value, Func<TResult> build)
        {
            TResult result = default;
            Provide(value, () => { result = build(); });
            return result;
        }
    }
}

