using System;
using System.Collections.Generic;

namespace Pine
{
    /// <summary>Creates retained Unity UI and reactive state in typed C# declarations.</summary>
    public static partial class P
    {
        /// <summary>Returns the API version.</summary>
        public static Version Version => new(1, 0, 0);

        /// <summary>Global explicit reactive motion preference.</summary>
        public static Source<bool> ReducedMotion { get; } = new(false, null);

        /// <summary>Controls duplicate named-property diagnostics within groups and duplicate child transform diagnostics.</summary>
        public static bool Strict { get; set; } = true;

        /// <summary>Controls required native wiring for the imperative Create API.</summary>
        public static bool Defaults { get; set; } = true;

        /// <summary>Controls whether nested groups are traversed after outer declarations within property ordering phases.</summary>
        public static bool DeferNestedProperties { get; set; } = true;

        /// <summary>Creates mutable typed state, usable outside any ownership scope.</summary>
        public static Source<T> Source<T>(
            T value = default,
            IEqualityComparer<T> comparer = null
        ) => new(value, comparer);

        /// <summary>Reads a typed literal or reactive adapter.</summary>
        public static T Read<T>(Value<T> value) => value.Read();

        /// <summary>Reads a typed literal or reactive adapter.</summary>
        public static T Read<T>(Func<T> getter) => getter();

        /// <summary>Reads a typed literal or reactive adapter.</summary>
        public static T Read<T>(Source<T> source) => source.Value;

        /// <summary>Reads a typed literal or reactive adapter.</summary>
        public static T Read<T>(ReadOnly<T> source) => source.Value;

        /// <summary>Reads a typed literal or reactive adapter.</summary>
        public static T Read<T>(Derived<T> derived) => derived.Value;

        /// <summary>Reads a typed literal or reactive adapter.</summary>
        public static T Read<T>(T value) => value;

        /// <summary>Creates an owned eager cached calculation.</summary>
        public static Derived<T> Derive<T>(Func<T> compute, IEqualityComparer<T> comparer = null)
        {
            RequireStable();
            Derived<T> derived = new(
                compute ?? throw new ArgumentNullException(nameof(compute)),
                comparer
            );
            try
            {
                Batch(() => derived.Initialize());
                return derived;
            }
            catch
            {
                derived.Dispose();
                throw;
            }
        }

        /// <summary>Runs an owned side effect immediately and again after its tracked inputs change.</summary>
        public static IDisposable Effect(Action action)
        {
            RequireStable();
            EffectObserver effect = new(action ?? throw new ArgumentNullException(nameof(action)));
            try
            {
                Batch(effect.Run);
                return effect;
            }
            catch
            {
                effect.Dispose();
                throw;
            }
        }

        /// <summary>Runs an owned side effect immediately and again after its tracked inputs change.</summary>
        public static IDisposable Effect<T>(Func<T, T> action, T initial)
        {
            T previous = initial;
            return Effect(() => previous = action(previous));
        }

        /// <summary>Constructs an independent ownership scope and runs its builder without dependency tracking.</summary>
        public static Scope Root(Action build) => BuildRoot(new Scope(null, false), build);

        /// <summary>Constructs an independent ownership scope and runs its builder without dependency tracking.</summary>
        public static Scope Root(Action<Action> build)
        {
            Scope scope = new(null, false);
            return BuildRoot(scope, () => build(scope.Dispose));
        }

        /// <summary>Constructs an independent ownership scope and runs its builder without dependency tracking.</summary>
        public static (Scope Scope, T Value) Root<T>(Func<Action, T> build)
        {
            T result = default;
            Scope scope = Root(dispose =>
            {
                result = build(dispose);
            });
            return (scope, result);
        }

        internal static Scope OwnedRoot(Action build) =>
            BuildRoot(new Scope(ReactiveRuntime.Scope, true), build);

        private static Scope BuildRoot(Scope scope, Action build)
        {
            try
            {
                Untrack(() => scope.Run(build));
                return scope;
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        /// <summary>A scoped typed dependency with a fallback outside providers.</summary>
        public static Context<T> Context<T>(T fallback = default) => new(fallback);

        /// <summary>Registers a callback, disposable or Unity object with the active scope.</summary>
        public static void Cleanup(Action cleanup) =>
            RequireScope().Own(new CleanupAction(cleanup));

        /// <summary>Registers a callback, disposable or Unity object with the active scope.</summary>
        public static void Cleanup(IDisposable disposable) => RequireScope().Own(disposable);

        /// <summary>Runs several writes as one synchronous update transaction.</summary>
        public static void Batch(Action action)
        {
            ReactiveRuntime.BatchDepth++;
            try
            {
                action();
            }
            finally
            {
                ReactiveRuntime.BatchDepth--;
                ReactiveRuntime.Flush();
            }
        }

        /// <summary>Runs a read or action with dependency collection temporarily suspended.</summary>
        public static T Untrack<T>(Func<T> read)
        {
            Observer previous = ReactiveRuntime.Observer;
            ReactiveRuntime.Observer = null;
            try
            {
                return read();
            }
            finally
            {
                ReactiveRuntime.Observer = previous;
            }
        }

        /// <summary>Runs a read or action with dependency collection temporarily suspended.</summary>
        public static void Untrack(Action action)
        {
            Observer previous = ReactiveRuntime.Observer;
            ReactiveRuntime.Observer = null;
            try
            {
                action();
            }
            finally
            {
                ReactiveRuntime.Observer = previous;
            }
        }

        /// <summary>Advances the shared spring/polling/exit-delay clock manually by a finite non-negative number of seconds.</summary>
        public static void Step(double deltaTime) => Clock.Step(deltaTime, true);

        internal static Scope RequireScope() =>
            ReactiveRuntime.Scope
            ?? throw new InvalidOperationException("Use P.Root or P.Mount to own this operation.");

        internal static void RequireStable()
        {
            RequireScope();
            if (ReactiveRuntime.Observer != null)
                throw new InvalidOperationException(
                    "Create reactive scopes inside a stable root or P.Untrack, not another reactive calculation."
                );
        }
    }

    internal sealed class CleanupAction : IDisposable
    {
        private Action _action;

        internal CleanupAction(Action action) =>
            _action = action ?? throw new ArgumentNullException(nameof(action));

        public void Dispose()
        {
            Action action = _action;
            _action = null;
            action?.Invoke();
        }
    }

    internal static class ValueEquality<T>
    {
        internal static bool Same(T left, T right, IEqualityComparer<T> comparer)
        {
            if (comparer != null)
                return comparer.Equals(left, right);
            if (typeof(T).IsValueType || left is string || left == null)
                return EqualityComparer<T>.Default.Equals(left, right);
            return false;
        }
    }

    internal static class ReactiveRuntime
    {
        internal static Observer Observer;
        internal static Scope Scope;
        internal static int BatchDepth;
        internal static int NotificationDepth;
        internal static long SourceRevision;
        private static readonly Queue<DerivedObserver> _derived = new();
        private static readonly HashSet<DerivedObserver> _derivedSet = new();
        private static readonly Queue<EffectObserver> _effects = new();
        private static readonly HashSet<EffectObserver> _effectSet = new();
        private static bool _flushing;
#if UNITY_5_3_OR_NEWER
        private static readonly Unity.Profiling.ProfilerMarker BindingMarker = new("Pine.Bindings");
#endif

        internal static void Schedule(DerivedObserver node)
        {
            if (_derivedSet.Add(node))
                _derived.Enqueue(node);
        }

        internal static void Schedule(EffectObserver node)
        {
            if (_effectSet.Add(node))
                _effects.Enqueue(node);
        }

        internal static void Flush()
        {
            if (_flushing || BatchDepth > 0 || NotificationDepth > 0)
                return;
#if UNITY_5_3_OR_NEWER
            using var measurement = BindingMarker.Auto();
#endif
            _flushing = true;
            int evaluations = 0;
            List<Exception> errors = null;
            try
            {
                while (_derived.Count > 0 || _effects.Count > 0)
                {
                    while (_derived.Count > 0)
                    {
                        Guard(ref evaluations);
                        DerivedObserver node = _derived.Dequeue();
                        _derivedSet.Remove(node);
                        try
                        {
                            if (!node.IsDisposed)
                                node.EnsureCurrent();
                        }
                        catch (Exception error)
                        {
                            (errors ??= new()).Add(error);
                        }
                    }
                    if (_effects.Count == 0)
                        continue;
                    Guard(ref evaluations);
                    EffectObserver effect = _effects.Dequeue();
                    _effectSet.Remove(effect);
                    try
                    {
                        if (!effect.IsDisposed)
                            effect.Run();
                    }
                    catch (Exception error)
                    {
                        (errors ??= new()).Add(error);
                    }
                }
            }
            finally
            {
                _derived.Clear();
                _derivedSet.Clear();
                _effects.Clear();
                _effectSet.Clear();
                _flushing = false;
            }
            if (errors != null)
                throw new AggregateException("Pine updates failed.", errors);
        }

        private static void Guard(ref int count)
        {
            if (++count > 10000)
                throw new InvalidOperationException(
                    "Pine updates did not settle. Check for a feedback loop."
                );
        }
    }

    internal class Node
    {
        private readonly HashSet<Observer> _observers = new();

        internal void Track()
        {
            Observer observer = ReactiveRuntime.Observer;
            if (observer != null && observer.Dependencies.Add(this))
                _observers.Add(observer);
        }

        internal void Remove(Observer observer) => _observers.Remove(observer);

        internal void Notify()
        {
            ReactiveRuntime.NotificationDepth++;
            try
            {
                foreach (Observer observer in _observers)
                    observer.Invalidate();
            }
            finally
            {
                ReactiveRuntime.NotificationDepth--;
                ReactiveRuntime.Flush();
            }
        }
    }

    internal abstract class Observer : Node, IDisposable
    {
        internal readonly HashSet<Node> Dependencies = new();
        protected readonly Scope Owner = ReactiveRuntime.Scope;
        internal bool IsDisposed { get; private set; }

        protected Observer() => Owner?.Own(this);

        internal abstract void Invalidate();

        protected void ClearDependencies()
        {
            foreach (Node node in Dependencies)
                node.Remove(this);
            Dependencies.Clear();
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            ClearDependencies();
            Owner?.Release(this);
        }
    }

    internal abstract class DerivedObserver : Observer
    {
        internal abstract void EnsureCurrent();
    }

    internal sealed class DerivedObserver<T> : DerivedObserver
    {
        private readonly Func<T> _compute;
        private readonly IEqualityComparer<T> _comparer;
        private T _value;
        private bool _dirty = true;
        private bool _computing;
        private bool _initialized;
        private long _settledRevision = -1;
        private readonly List<Node> _settling = new();

        internal DerivedObserver(Func<T> compute, IEqualityComparer<T> comparer)
        {
            _compute = compute;
            _comparer = comparer;
        }

        internal T Value
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(Derived<T>));
                EnsureCurrent();
                Track();
                return _value;
            }
        }

        internal override void EnsureCurrent()
        {
            if (_computing)
                throw new InvalidOperationException("Circular derived dependency.");
            if (IsDisposed)
                return;
            if (!_dirty && _settledRevision == ReactiveRuntime.SourceRevision)
                return;
            _computing = true;
            // Upstream invalidation may not have reached this node yet inside a batch.
            // Delay flushing until the dependency walk and this calculation finish.
            ReactiveRuntime.BatchDepth++;
            Observer previous = ReactiveRuntime.Observer;
            try
            {
                _settling.Clear();
                _settling.AddRange(Dependencies);
                foreach (Node dependency in _settling)
                    if (dependency is DerivedObserver derived)
                        derived.EnsureCurrent();
                if (!_dirty)
                {
                    _settledRevision = ReactiveRuntime.SourceRevision;
                    return;
                }
                ClearDependencies();
                ReactiveRuntime.Observer = this;
                T next = Owner.Run(_compute);
                bool changed = !_initialized || !ValueEquality<T>.Same(_value, next, _comparer);
                _value = next;
                _dirty = false;
                _initialized = true;
                _settledRevision = ReactiveRuntime.SourceRevision;
                if (changed)
                    Notify();
            }
            finally
            {
                _settling.Clear();
                _computing = false;
                ReactiveRuntime.Observer = previous;
                ReactiveRuntime.BatchDepth--;
                ReactiveRuntime.Flush();
            }
        }

        internal override void Invalidate()
        {
            if (IsDisposed)
                return;
            _dirty = true;
            ReactiveRuntime.Schedule(this);
        }
    }

    internal sealed class EffectObserver : Observer
    {
        private readonly Action _action;
        private Scope _execution;
        private bool _resetting;

        internal EffectObserver(Action action) => _action = action;

        internal void Run()
        {
            if (IsDisposed)
                return;
            if (_execution == null)
                _execution = new Scope(Owner, false);
            else
            {
                _resetting = true;
                try
                {
                    _execution.Reset();
                }
                finally
                {
                    _resetting = false;
                }
            }
            // A failed reset must retain the previous subscriptions for a later retry.
            ClearDependencies();
            Observer previous = ReactiveRuntime.Observer;
            ReactiveRuntime.Observer = this;
            try
            {
                _execution.Run(_action);
            }
            finally
            {
                ReactiveRuntime.Observer = previous;
            }
        }

        internal override void Invalidate()
        {
            if (!IsDisposed && !_resetting)
                ReactiveRuntime.Schedule(this);
        }

        public override void Dispose()
        {
            if (IsDisposed)
                return;
            base.Dispose();
            _execution?.Dispose();
        }
    }
}
