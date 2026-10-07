using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pine.uGUI
{
    /// <summary>A declared native part, supplied target, or tracked native relationship.</summary>
    public readonly struct Part<T>
        where T : UnityEngine.Object
    {
        internal readonly View Declaration;
        internal readonly Value<T> Value;

        private Part(View declaration, Value<T> value)
        {
            Declaration = declaration;
            Value = value;
        }

        /// <summary>Declares and owns a custom part inside the control.</summary>
        public static implicit operator Part<T>(View view) =>
            new(view ?? throw new ArgumentNullException(nameof(view)), default);

        /// <summary>Uses an externally owned native target.</summary>
        public static implicit operator Part<T>(T target) => new(null, new Value<T>(target));

        /// <summary>Uses a tracked native target.</summary>
        public static implicit operator Part<T>(Value<T> value) => new(null, value);

        /// <summary>Uses a lifetime-aware typed reference.</summary>
        public static implicit operator Part<T>(Ref<T> reference) =>
            new(null, new Value<T>(() => reference == null ? null : reference.Value));

        /// <summary>Uses explicit mutable native state as a relationship.</summary>
        public static implicit operator Part<T>(Source<T> source) => new(null, source);

        /// <summary>Uses a retained read-only native target.</summary>
        public static implicit operator Part<T>(ReadOnly<T> source) => new(null, source);

        /// <summary>Uses a derived native target.</summary>
        public static implicit operator Part<T>(Derived<T> source) => new(null, source);
    }

    internal sealed class NativePart
    {
        internal readonly string Name;
        internal readonly View Declaration;
        internal readonly Action<Component, Component, Dictionary<string, Component>> Assign;
        internal readonly Action<Component, Dictionary<string, Component>> Bind;

        internal NativePart(
            string name,
            View declaration,
            Action<Component, Component, Dictionary<string, Component>> assign,
            Action<Component, Dictionary<string, Component>> bind
        )
        {
            Name = name;
            Declaration = declaration;
            Assign = assign;
            Bind = bind;
        }
    }

    public static partial class P
    {
        internal static NativePart NativePart<TTarget, T>(
            string name,
            Part<T>? part,
            Action<TTarget, T> assign
        )
            where TTarget : Component
            where T : UnityEngine.Object
        {
            if (!part.HasValue)
                return null;
            var value = part.Value;
            return new NativePart(
                name,
                value.Declaration,
                (target, native, parts) =>
                {
                    var resolved = ResolvePart<T>(native);
                    assign?.Invoke((TTarget)target, resolved);
                    parts[name] = resolved as Component;
                },
                value.Declaration != null
                    ? null
                    : (target, parts) =>
                        Prop(
                            (TTarget)target,
                            (Value<T>?)value.Value,
                            (item, next) =>
                            {
                                assign?.Invoke(item, next);
                                parts[name] = next as Component;
                            }
                        )
            );
        }

        private static T ResolvePart<T>(Component native)
            where T : UnityEngine.Object
        {
            if (typeof(T) == typeof(GameObject))
                return native.gameObject as T;
            var result = native as T ?? native.GetComponent(typeof(T)) as T;
            return result != null
                ? result
                : throw new InvalidOperationException(
                    "The declared part needs " + typeof(T).Name + "."
                );
        }
    }
}
