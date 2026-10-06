using System;
using System.Collections.Generic;

namespace Pine
{
    /// <summary>A typed literal-or-getter adapter for mutable UI inputs.</summary>
    public readonly struct Value<T>
    {
        private readonly T _literal;
        private readonly Func<T> _read;
        internal readonly Source<T> Writable;
        /// <summary>Constructs this value with the supplied typed arguments.</summary>
        public Value(T literal) { _literal = literal; _read = null; Writable = null; }
        /// <summary>Constructs this value with the supplied typed arguments.</summary>
        public Value(Func<T> read) { _literal = default; _read = read ?? throw new ArgumentNullException(nameof(read)); Writable = null; }

        private Value(Source<T> source)
        {
            Writable = source ?? throw new ArgumentNullException(nameof(source));
            _literal = default; _read = () => source.Value;
        }
        /// <summary>Reports whether this adapter wraps a getter.</summary>
        public bool IsDynamic => _read != null;
        /// <summary>Returns the literal or invokes its getter with normal dependency tracking.</summary>
        public T Read() => _read == null ? _literal : _read();
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter.</summary>
        public static implicit operator Value<T>(T value) => new(value);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter.</summary>
        public static implicit operator Value<T>(Func<T> getter) => new(getter);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter.</summary>
        public static implicit operator Value<T>(Source<T> source) => new(source);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter.</summary>
        public static implicit operator Value<T>(ReadOnly<T> source) => new(() => source.Value);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter.</summary>
        public static implicit operator Value<T>(Derived<T> source) => new(() => source.Value);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter.</summary>
        public static implicit operator Value<T>(Spring<T> source) => new(() => source.Value);
    }

    /// <summary>A framework-owned reactive value that callers can observe without replacing it.</summary>
    public sealed class ReadOnly<T>
    {
        private readonly Source<T> _source;
        internal ReadOnly(Source<T> source) => _source = source;
        /// <summary>Reads the framework-owned value with dependency tracking.</summary>
        public T Value => _source.Value;
        /// <summary>Reads the framework-owned snapshot without collecting a dependency.</summary>
        public T Peek() => _source.Peek();
    }

    /// <summary>Mutable typed reactive state.</summary>
    public sealed class Source<T>
    {
        private readonly Node _node = new();
        private readonly IEqualityComparer<T> _comparer;
        private T _value;
        internal Source(T value, IEqualityComparer<T> comparer) { _value = value; _comparer = comparer; }
        /// <summary>Reads with dependency tracking and writes with notifications under the source equality policy.</summary>
        public T Value
        {
            get { _node.Track(); return _value; }
            set
            {
                if (ReactiveRuntime.Observer is DerivedObserver) throw new InvalidOperationException("Derived calculations must not write sources.");
                if (ValueEquality<T>.Same(_value, value, _comparer)) return;
                _value = value; Notify();
            }
        }
        /// <summary>Returns a source snapshot without collecting a dependency.</summary>
        public T Peek() => _value;
        /// <summary>Assigns the typed source value and returns the value supplied.</summary>
        public T Set(T value) { Value = value; return value; }
        /// <summary>Explicitly notifies observers after mutating a referenced value in place.</summary>
        public void Notify()
        {
            ReactiveRuntime.SourceRevision++;
            _node.Notify();
        }
    }

    /// <summary>An owned cached pure calculation with dynamically tracked dependencies.</summary>
    public sealed class Derived<T> : IDisposable
    {
        private readonly DerivedObserver<T> _observer;
        internal Derived(Func<T> compute, IEqualityComparer<T> comparer) => _observer = new(compute, comparer);
        internal void Initialize() => _observer.EnsureCurrent();
        /// <summary>Returns the current cached pure result and tracks downstream reads.</summary>
        public T Value => _observer.Value;
        /// <summary>Ends this owned lifetime idempotently.</summary>
        public void Dispose() => _observer.Dispose();
    }
}
