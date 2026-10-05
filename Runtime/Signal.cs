using System;
using System.Collections.Generic;

namespace Pine
{
    /// <summary>A typed literal-or-getter adapter for mutable UI inputs. A literal is applied once; a getter is evaluated in an owned reactive effect and tracks the sources it reads. Source, ReadOnly, Derived and Spring convert implicitly. Structural native types and identity keys are declared separately.</summary>
    /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
    /// <example>
    /// <code><![CDATA[
    /// var width = UI.Source(240f);
    /// Value<UnityEngine.Vector2> size = new(() => new UnityEngine.Vector2(width.Value, 48));
    /// UI.Frame(UI.Size(size));
    /// ]]></code>
    /// </example>
    public readonly struct Value<T>
    {
        private readonly T _literal;
        private readonly Func<T> _read;
        /// <summary>Constructs this value with the supplied typed arguments. A typed literal-or-getter adapter for mutable UI inputs. A literal is applied once; a getter is evaluated in an owned reactive effect and tracks the sources it reads. Source, ReadOnly, Derived and Spring convert implicitly. Structural native types and identity keys are declared separately.</summary>
        /// <param name="literal">The typed literal input (T); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <example>
        /// <code><![CDATA[
        /// var width = UI.Source(240f);
        /// Value<UnityEngine.Vector2> size = new(() => new UnityEngine.Vector2(width.Value, 48));
        /// UI.Frame(UI.Size(size));
        /// ]]></code>
        /// </example>
        public Value(T literal) { _literal = literal; _read = null; }
        /// <summary>Constructs this value with the supplied typed arguments. A typed literal-or-getter adapter for mutable UI inputs. A literal is applied once; a getter is evaluated in an owned reactive effect and tracks the sources it reads. Source, ReadOnly, Derived and Spring convert implicitly. Structural native types and identity keys are declared separately.</summary>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <example>
        /// <code><![CDATA[
        /// var width = UI.Source(240f);
        /// Value<UnityEngine.Vector2> size = new(() => new UnityEngine.Vector2(width.Value, 48));
        /// UI.Frame(UI.Size(size));
        /// ]]></code>
        /// </example>
        public Value(Func<T> read) { _literal = default; _read = read ?? throw new ArgumentNullException(nameof(read)); }
        /// <summary>Reports whether this adapter wraps a getter. Sources, derived values, springs and read-only values convert to dynamic adapters.</summary>
        /// <example>
        /// <code><![CDATA[
        /// bool reactive = value.IsDynamic;
        /// ]]></code>
        /// </example>
        public bool IsDynamic => _read != null;
        /// <summary>Returns the literal or invokes its getter with normal dependency tracking.</summary>
        /// <returns>The literal or getter result; getter reads participate in the active observer’s dependency tracking.</returns>
        /// <example>
        /// <code><![CDATA[
        /// var current = value.Read();
        /// ]]></code>
        /// </example>
        public T Read() => _read == null ? _literal : _read();
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter. Conversions preserve the value type and reactive dependency reads.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <returns>A typed Value adapter that reads this reactive value when evaluated; the conversion does not write to its source.</returns>
        /// <example>
        /// <code><![CDATA[
        /// Value<int> value = UI.Source(0);
        /// ]]></code>
        /// </example>
        public static implicit operator Value<T>(T value) => new(value);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter. Conversions preserve the value type and reactive dependency reads.</summary>
        /// <param name="getter">The typed getter to evaluate with normal dependency tracking.</param>
        /// <returns>A typed Value adapter that reads this reactive value when evaluated; the conversion does not write to its source.</returns>
        /// <example>
        /// <code><![CDATA[
        /// Value<int> value = UI.Source(0);
        /// ]]></code>
        /// </example>
        public static implicit operator Value<T>(Func<T> getter) => new(getter);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter. Conversions preserve the value type and reactive dependency reads.</summary>
        /// <param name="source">The source, derived, spring or read-only value to observe through this adapter. This conversion preserves reads and does not grant write access.</param>
        /// <returns>A typed Value adapter that reads this reactive value when evaluated; the conversion does not write to its source.</returns>
        /// <example>
        /// <code><![CDATA[
        /// Value<int> value = UI.Source(0);
        /// ]]></code>
        /// </example>
        public static implicit operator Value<T>(Source<T> source) => new(() => source.Value);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter. Conversions preserve the value type and reactive dependency reads.</summary>
        /// <param name="source">The source, derived, spring or read-only value to observe through this adapter. This conversion preserves reads and does not grant write access.</param>
        /// <returns>A typed Value adapter that reads this reactive value when evaluated; the conversion does not write to its source.</returns>
        /// <example>
        /// <code><![CDATA[
        /// Value<int> value = UI.Source(0);
        /// ]]></code>
        /// </example>
        public static implicit operator Value<T>(ReadOnly<T> source) => new(() => source.Value);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter. Conversions preserve the value type and reactive dependency reads.</summary>
        /// <param name="source">The source, derived, spring or read-only value to observe through this adapter. This conversion preserves reads and does not grant write access.</param>
        /// <returns>A typed Value adapter that reads this reactive value when evaluated; the conversion does not write to its source.</returns>
        /// <example>
        /// <code><![CDATA[
        /// Value<int> value = UI.Source(0);
        /// ]]></code>
        /// </example>
        public static implicit operator Value<T>(Derived<T> source) => new(() => source.Value);
        /// <summary>Converts a typed literal, getter or supported reactive value into a literal-or-getter adapter. Conversions preserve the value type and reactive dependency reads.</summary>
        /// <param name="source">The source, derived, spring or read-only value to observe through this adapter. This conversion preserves reads and does not grant write access.</param>
        /// <returns>A typed Value adapter that reads this reactive value when evaluated; the conversion does not write to its source.</returns>
        /// <example>
        /// <code><![CDATA[
        /// Value<int> value = UI.Source(0);
        /// ]]></code>
        /// </example>
        public static implicit operator Value<T>(Spring<T> source) => new(() => source.Value);
    }

    /// <summary>A framework-owned reactive value that callers can observe without replacing it. Dynamic operators expose their output, row values, indices and presence through this type. Value tracks dependency reads; Peek reads without tracking. Change the source collection or selection to update these values.</summary>
    /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
    /// <example>
    /// <code><![CDATA[
    /// var rows = UI.Values(() => new[] { "A" }, (value, index) =>
    ///     UI.Label(() => $"{index.Value}: {value}"));
    /// UI.Column(UI.Children(() => rows.Value));
    /// ]]></code>
    /// </example>
    public sealed class ReadOnly<T>
    {
        private readonly Source<T> _source;
        internal ReadOnly(Source<T> source) => _source = source;
        /// <summary>Reads the framework-owned value with dependency tracking. There is no public setter; update the controlling collection or selector instead.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label(() => index.Value.ToString());
        /// ]]></code>
        /// </example>
        public T Value => _source.Value;
        /// <summary>Reads the framework-owned snapshot without collecting a dependency.</summary>
        /// <returns>The current value without registering a dependency on this read.</returns>
        /// <example>
        /// <code><![CDATA[
        /// int snapshot = index.Peek();
        /// ]]></code>
        /// </example>
        public T Peek() => _source.Peek();
    }

    /// <summary>Mutable typed reactive state. Reading Value inside an observer registers a dependency; assigning Value notifies observers under the configured equality policy. Sources can be stored on an owner outside any UI lifetime. Equal values and strings are suppressed by default, while mutable reference assignments notify unless a comparer changes that behavior.</summary>
    /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
    /// <example>
    /// <code><![CDATA[
    /// var count = UI.Source(0);
    /// UI.Label(() => count.Value.ToString());
    /// count.Value++;
    /// ]]></code>
    /// </example>
    public sealed class Source<T>
    {
        private readonly Node _node = new();
        private readonly IEqualityComparer<T> _comparer;
        private T _value;
        internal Source(T value, IEqualityComparer<T> comparer) { _value = value; _comparer = comparer; }
        /// <summary>Reads with dependency tracking and writes with notifications under the source equality policy. Writes from a derived calculation are rejected.</summary>
        /// <example>
        /// <code><![CDATA[
        /// count.Value++;
        /// ]]></code>
        /// </example>
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
        /// <summary>Returns a source snapshot without collecting a dependency. Use Value when a reactive binding should follow changes.</summary>
        /// <returns>The current value without registering a dependency on this read.</returns>
        /// <example>
        /// <code><![CDATA[
        /// int snapshot = count.Peek();
        /// ]]></code>
        /// </example>
        public T Peek() => _value;
        /// <summary>Assigns the typed source value and returns the value supplied. The same equality and notification policy as the Value setter applies.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <returns>The assigned value, after synchronously notifying observers outside a batch.</returns>
        /// <example>
        /// <code><![CDATA[
        /// int next = count.Set(10);
        /// ]]></code>
        /// </example>
        public T Set(T value) { Value = value; return value; }
        /// <summary>Explicitly notifies observers after mutating a referenced value in place. It increments the source revision and settles tracked observers synchronously outside a batch.</summary>
        /// <example>
        /// <code><![CDATA[
        /// items.Peek().Add("New");
        /// items.Notify();
        /// ]]></code>
        /// </example>
        public void Notify()
        {
            ReactiveRuntime.SourceRevision++;
            _node.Notify();
        }
    }

    /// <summary>An owned cached pure calculation with dynamically tracked dependencies. Reading Value tracks downstream consumers; equal outputs suppress their reruns. Create in a stable ownership scope and keep the calculation free of source writes. Disposal releases dependencies and is idempotent.</summary>
    /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
    /// <example>
    /// <code><![CDATA[
    /// var count = UI.Source(2);
    /// var doubled = UI.Derive(() => count.Value * 2);
    /// UI.Label(() => doubled.Value.ToString());
    /// ]]></code>
    /// </example>
    public sealed class Derived<T> : IDisposable
    {
        private readonly DerivedObserver<T> _observer;
        internal Derived(Func<T> compute, IEqualityComparer<T> comparer) => _observer = new(compute, comparer);
        internal void Initialize() => _observer.EnsureCurrent();
        /// <summary>Returns the current cached pure result and tracks downstream reads. Within a batch it first settles stale upstream derived calculations.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Label(() => total.Value.ToString());
        /// ]]></code>
        /// </example>
        public T Value => _observer.Value;
        /// <summary>Ends this owned lifetime idempotently. Dependencies and native event/clock registrations are released; Scope/Mount cleanup attempts all resources and aggregates failures. Application code disposes a mount when its owner ends.</summary>
        /// <example>
        /// <code><![CDATA[
        /// total.Dispose();
        /// ]]></code>
        /// </example>
        public void Dispose() => _observer.Dispose();
    }
}
