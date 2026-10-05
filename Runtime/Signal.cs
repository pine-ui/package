using System;
using System.Collections.Generic;

namespace Pine
{
    public readonly struct Value<T>
    {
        private readonly T _literal;
        private readonly Func<T> _read;
        public Value(T literal) { _literal = literal; _read = null; }
        public Value(Func<T> read) { _literal = default; _read = read ?? throw new ArgumentNullException(nameof(read)); }
        public bool IsDynamic => _read != null;
        public T Read() => _read == null ? _literal : _read();
        public static implicit operator Value<T>(T value) => new(value);
        public static implicit operator Value<T>(Func<T> getter) => new(getter);
        public static implicit operator Value<T>(Source<T> source) => new(() => source.Value);
        public static implicit operator Value<T>(Derived<T> source) => new(() => source.Value);
        public static implicit operator Value<T>(Spring<T> source) => new(() => source.Value);
    }

    public sealed class Source<T>
    {
        private readonly Node _node = new();
        private readonly IEqualityComparer<T> _comparer;
        private T _value;
        internal Source(T value, IEqualityComparer<T> comparer) { _value = value; _comparer = comparer; }
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
        public T Peek() => _value;
        public T Set(T value) { Value = value; return value; }
        public void Notify()
        {
            ReactiveRuntime.SourceRevision++;
            _node.Notify();
        }
    }

    public sealed class Derived<T> : IDisposable
    {
        private readonly DerivedObserver<T> _observer;
        internal Derived(Func<T> compute, IEqualityComparer<T> comparer) => _observer = new(compute, comparer);
        internal void Initialize() => _observer.EnsureCurrent();
        public T Value => _observer.Value;
        public void Dispose() => _observer.Dispose();
    }
}
