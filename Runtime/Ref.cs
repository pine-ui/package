using System;
using UnityEngine;

namespace Pine
{
    internal interface INativeReference
    {
        void Bind(UnityEngine.Object value);
    }

    /// <summary>A reactive, single-target native reference cleared by its owner's lifetime.</summary>
    public sealed class Ref<T> : INativeReference
    {
        private readonly Source<T> _value = P.Source<T>();
        private Scope _owner;

        internal Ref() { }

        void INativeReference.Bind(UnityEngine.Object value) => Bind((T)(object)value);

        /// <summary>Reads the current target and tracks its replacement or removal.</summary>
        public T Value => _value.Value;

        internal void Bind(T value)
        {
            var owner = P.RequireScope();
            if (_owner != null && !_owner.IsDisposed)
                throw new InvalidOperationException("A reference can have only one live target.");
            _owner = owner;
            _value.Value = value;
            P.Cleanup(() =>
            {
                if (!ReferenceEquals(_owner, owner))
                    return;
                _owner = null;
                _value.Value = default;
            });
        }

        /// <summary>Captures this reference through a native factory's reference argument.</summary>
        public static implicit operator Action<T>(Ref<T> reference) =>
            reference == null ? null : reference.Bind;

        /// <summary>Uses this reference as a tracked native property value.</summary>
        public static implicit operator Value<T>(Ref<T> reference) =>
            new(() => reference == null ? default : reference.Value);
    }

    public static partial class P
    {
        /// <summary>Creates a typed native reference for forward, cyclic and conditional links.</summary>
        public static Ref<T> Ref<T>()
            where T : UnityEngine.Object => new();
    }
}
