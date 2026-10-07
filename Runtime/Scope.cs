using System;
using System.Collections.Generic;

namespace Pine
{
    /// <summary>A lifetime container for reactive observers, callbacks and native resources.</summary>
    public sealed class Scope : IDisposable
    {
        private List<IDisposable> _resources;
        private readonly bool _owned;
        internal readonly Scope Parent;
        private Dictionary<object, object> _contexts;
        internal Dictionary<object, object> ContextValues => _contexts ??= new();

        internal void CopyContextTo(Scope target)
        {
            Parent?.CopyContextTo(target);
            if (_contexts != null)
                foreach (var pair in _contexts)
                    target.ContextValues[pair.Key] = pair.Value;
        }

        internal bool TryGetContext(object key, out object value)
        {
            value = null;
            return _contexts != null && _contexts.TryGetValue(key, out value);
        }

        /// <summary>Reports whether cleanup has begun/completed for this scope.</summary>
        public bool IsDisposed { get; private set; }

        internal Scope(Scope parent, bool owned)
        {
            Parent = parent;
            _owned = owned;
            if (owned)
                parent?.Own(this);
        }

        /// <summary>Temporarily enters this live scope, preserving ownership and scoped context, and restores the previous scope afterward.</summary>
        public void Run(Action action)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(Scope));
            Scope previous = ReactiveRuntime.Scope;
            ReactiveRuntime.Scope = this;
            try
            {
                action();
            }
            finally
            {
                ReactiveRuntime.Scope = previous;
            }
        }

        /// <summary>Temporarily enters this live scope, preserving ownership and scoped context, and restores the previous scope afterward.</summary>
        public T Run<T>(Func<T> action)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(Scope));
            Scope previous = ReactiveRuntime.Scope;
            ReactiveRuntime.Scope = this;
            try
            {
                return action();
            }
            finally
            {
                ReactiveRuntime.Scope = previous;
            }
        }

        /// <summary>Registers an IDisposable for reverse-order cleanup and returns the same resource.</summary>
        public T Own<T>(T resource)
            where T : IDisposable
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(Scope));
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
            (_resources ??= new()).Add(resource);
            return resource;
        }

        internal void Release(IDisposable resource)
        {
            if (!IsDisposed)
                _resources?.Remove(resource);
        }

        internal void Reset()
        {
            try
            {
                Dispose();
            }
            finally
            {
                IsDisposed = false;
            }
        }

        /// <summary>Ends this owned lifetime idempotently.</summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            if (_owned)
                Parent?.Release(this);
            List<Exception> errors = null;
            if (_resources == null || _resources.Count == 0)
            {
                _contexts?.Clear();
                return;
            }
            ReactiveRuntime.BatchDepth++;
            Observer previous = ReactiveRuntime.Observer;
            ReactiveRuntime.Observer = null;
            try
            {
                while (_resources.Count > 0)
                {
                    int index = _resources.Count - 1;
                    IDisposable resource = _resources[index];
                    _resources.RemoveAt(index);
                    try
                    {
                        resource.Dispose();
                    }
                    catch (Exception error)
                    {
                        (errors ??= new List<Exception>()).Add(error);
                    }
                }
            }
            finally
            {
                ReactiveRuntime.Observer = previous;
                _contexts?.Clear();
                ReactiveRuntime.BatchDepth--;
                try
                {
                    ReactiveRuntime.Flush();
                }
                catch (Exception error)
                {
                    (errors ??= new List<Exception>()).Add(error);
                }
            }
            if (errors != null)
                throw new AggregateException("Pine cleanup failed.", errors);
        }
    }

    /// <summary>A scoped typed dependency with a fallback outside providers.</summary>
    public sealed class Context<T>
    {
        private readonly T _fallback;

        internal Context(T fallback) => _fallback = fallback;

        /// <summary>Returns the nearest scoped provider value or the configured fallback.</summary>
        public T Value
        {
            get
            {
                for (Scope scope = ReactiveRuntime.Scope; scope != null; scope = scope.Parent)
                    if (scope.TryGetContext(this, out object value))
                        return (T)value;
                return _fallback;
            }
        }

        /// <summary>Constructs a parent-owned provider scope with this typed value.</summary>
        public void Provide(T value, Action build)
        {
            Scope scope = new(Core.RequireScope(), true);
            scope.ContextValues[this] = value;
            try
            {
                Core.Untrack(() => scope.Run(build));
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        /// <summary>Constructs a parent-owned provider scope with this typed value.</summary>
        public TResult Provide<TResult>(T value, Func<TResult> build)
        {
            TResult result = default;
            Provide(
                value,
                () =>
                {
                    result = build();
                }
            );
            return result;
        }
    }
}
