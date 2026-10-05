using System;
using System.Collections.Generic;
using System.Linq;

namespace Pine
{
    /// <summary>A constructed dynamic result and its optional exit retention delay in seconds. Presence becomes false immediately on removal while the result remains alive for ExitDelay; reentry before expiration cancels removal and reuses its scope. Zero delay removes immediately. Delays must be finite and non-negative.</summary>
    /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
    /// <example>
    /// <code><![CDATA[
    /// UI.Show(() => true, present => new Branch<UnityEngine.Component>(
    ///     UI.Label(() => present.Value ? "Present" : "Leaving"), 0.2));
    /// ]]></code>
    /// </example>
    public readonly struct Branch<T>
    {
        /// <summary>The native or custom result retained by this branch&#x27;s scope.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UnityEngine.Component result = branch.Value;
        /// ]]></code>
        /// </example>
        public readonly T Value;
        /// <summary>Finite non-negative seconds to retain an exiting branch after its presence becomes false. Zero removes immediately.</summary>
        /// <example>
        /// <code><![CDATA[
        /// double seconds = branch.ExitDelay;
        /// ]]></code>
        /// </example>
        public readonly double ExitDelay;
        /// <summary>Constructs this value with the supplied typed arguments. A constructed dynamic result and its optional exit retention delay in seconds. Presence becomes false immediately on removal while the result remains alive for ExitDelay; reentry before expiration cancels removal and reuses its scope. Zero delay removes immediately. Delays must be finite and non-negative.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="exitDelay">Finite non-negative seconds to retain a branch after presence becomes false.</param>
        /// <example>
        /// <code><![CDATA[
        /// UI.Show(() => true, present => new Branch<UnityEngine.Component>(
        ///     UI.Label(() => present.Value ? "Present" : "Leaving"), 0.2));
        /// ]]></code>
        /// </example>
        public Branch(T value, double exitDelay = 0)
        {
            if (double.IsNaN(exitDelay) || double.IsInfinity(exitDelay) || exitDelay < 0) throw new ArgumentOutOfRangeException(nameof(exitDelay));
            Value = value; ExitDelay = exitDelay;
        }
        /// <summary>Converts a constructed result into a branch with zero exit delay. Use the explicit constructor for retained exit animation.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <returns>A typed Value adapter that reads this reactive value when evaluated; the conversion does not write to its source.</returns>
        /// <example>
        /// <code><![CDATA[
        /// Branch<string> branch = "Ready";
        /// ]]></code>
        /// </example>
        public static implicit operator Branch<T>(T value) => new(value);
    }

    public static partial class UI
    {
        /// <summary>Constructs an owned conditional branch while its condition is true, with an optional fallback. The predicate overload retains the last truthy value for its branch. Results and presence are read-only; exit delays retain native results while presence is false, and reentry reuses unexpired scopes.</summary>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="condition">Reactive visibility predicate; its reads establish the controlling dependencies.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <param name="fallback">Optional construction callback used when the selected primary branch is absent.</param>
        /// <returns>An observable, immutable list of current and retained-exit branch results.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var result = UI.Show(() => visible.Value, () => UI.Label("Visible"));
        /// UI.Frame(UI.Children(() => result.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Show<TResult>(Func<bool> condition, Func<TResult> build, Func<TResult> fallback = null)
            => Show<TResult>(condition, _ => build(), fallback == null ? null : _ => fallback());
        /// <summary>Retains the selected keyed branch and optionally its exiting predecessor. Selection keys must be non-null; a comparer controls identity. Dictionary overloads support a fallback. Branch callbacks run only when creating that keyed scope, and returned presence/output are read-only.</summary>
        /// <typeparam name="TKey">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="select">Native event selector or reactive branch selector, as specified by this overload.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <param name="comparer">Optional equality/identity comparer; null selects the documented default policy.</param>
        /// <returns>An observable, immutable list of selected and retained-exit branch results.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var result = UI.Switch(() => page.Value, key => UI.Label(key));
        /// UI.Frame(UI.Children(() => result.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Switch<TKey, TResult>(Func<TKey> select, Func<TKey, TResult> build, IEqualityComparer<TKey> comparer = null)
            => Switch<TKey, TResult>(select, (key, _) => build(key), comparer);
        /// <summary>Retains rows by index or explicit dictionary key, updating each row&#x27;s read-only reactive value. Use explicit stable item IDs to preserve rows across reordering or immutable item replacement. Keys must be unique and non-null. Presence supports delayed exits through Branch.</summary>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <returns>An observable, immutable ordered list of row results; retained rows keep native identity.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var rows = UI.Indexes(() => items.Value, (index, item) => UI.Label(() => item.Value));
        /// UI.Column(UI.Children(() => rows.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Indexes<TValue, TResult>(Func<IReadOnlyList<TValue>> read, Func<int, ReadOnly<TValue>, TResult> build)
            => Indexes<TValue, TResult>(read, (index, value, _) => build(index, value));
        /// <summary>Retains rows by value identity and exposes each current index as a read-only reactive value. Reordering preserves constructed rows; removed rows report index -1 and false presence during delayed exit. Duplicate or null identity values are rejected, and an optional comparer controls identity.</summary>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <param name="comparer">Optional equality/identity comparer; null selects the documented default policy.</param>
        /// <returns>An observable, immutable ordered list of row results; indices are read-only operator-owned state.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var rows = UI.Values(() => items.Value, (item, index) => UI.Label(() => $"{index.Value}: {item}"));
        /// UI.Column(UI.Children(() => rows.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Values<TValue, TResult>(Func<IReadOnlyList<TValue>> read, Func<TValue, ReadOnly<int>, TResult> build, IEqualityComparer<TValue> comparer = null)
            => Values<TValue, TResult>(read, (value, index, _) => build(value, index), comparer);
        /// <summary>Retains the selected keyed branch and optionally its exiting predecessor. Selection keys must be non-null; a comparer controls identity. Dictionary overloads support a fallback. Branch callbacks run only when creating that keyed scope, and returned presence/output are read-only.</summary>
        /// <typeparam name="TKey">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="select">Native event selector or reactive branch selector, as specified by this overload.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <param name="comparer">Optional equality/identity comparer; null selects the documented default policy.</param>
        /// <returns>An observable, immutable list of selected and retained-exit branch results.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var result = UI.Switch(() => page.Value, key => UI.Label(key));
        /// UI.Frame(UI.Children(() => result.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Switch<TKey, TResult>(Func<TKey> select, Func<TKey, ReadOnly<bool>, Branch<TResult>> build, IEqualityComparer<TKey> comparer = null)
        {
            RequireStable();
            DynamicRows<TKey, TKey, TResult> rows = new((key, value, index, present) => build(key, present), comparer);
            Effect(() => { TKey key = select(); Untrack(() => rows.Update(new[] { new KeyValuePair<TKey, TKey>(key, key) })); });
            return rows.Output;
        }
        /// <summary>Retains the selected keyed branch and optionally its exiting predecessor. Selection keys must be non-null; a comparer controls identity. Dictionary overloads support a fallback. Branch callbacks run only when creating that keyed scope, and returned presence/output are read-only.</summary>
        /// <typeparam name="TKey">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="select">Native event selector or reactive branch selector, as specified by this overload.</param>
        /// <param name="branches">Construction callbacks indexed by non-null identity keys.</param>
        /// <param name="fallback">Optional construction callback used when the selected primary branch is absent.</param>
        /// <returns>An observable, immutable list of selected and retained-exit branch results.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var result = UI.Switch(() => page.Value, key => UI.Label(key));
        /// UI.Frame(UI.Children(() => result.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Switch<TKey, TResult>(Func<TKey> select, IReadOnlyDictionary<TKey, Func<ReadOnly<bool>, Branch<TResult>>> branches, Func<ReadOnly<bool>, Branch<TResult>> fallback = null)
            => Switch(select, (key, present) => branches.TryGetValue(key, out var build) ? build(present) : fallback != null ? fallback(present) : default);

        /// <summary>Constructs an owned conditional branch while its condition is true, with an optional fallback. The predicate overload retains the last truthy value for its branch. Results and presence are read-only; exit delays retain native results while presence is false, and reentry reuses unexpired scopes.</summary>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="condition">Reactive visibility predicate; its reads establish the controlling dependencies.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <param name="fallback">Optional construction callback used when the selected primary branch is absent.</param>
        /// <returns>An observable, immutable list of current and retained-exit branch results.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var result = UI.Show(() => visible.Value, () => UI.Label("Visible"));
        /// UI.Frame(UI.Children(() => result.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Show<TResult>(Func<bool> condition, Func<ReadOnly<bool>, Branch<TResult>> build, Func<ReadOnly<bool>, Branch<TResult>> fallback = null)
        {
            RequireStable();
            DynamicRows<bool, bool, TResult> rows = new((key, value, index, present) => key ? build(present) : fallback(present));
            Effect(() =>
            {
                bool visible = condition();
                Untrack(() => rows.Update(visible || fallback != null ? new[] { new KeyValuePair<bool, bool>(visible, visible) } : Array.Empty<KeyValuePair<bool, bool>>()));
            });
            return rows.Output;
        }
        /// <summary>Constructs an owned conditional branch while its condition is true, with an optional fallback. The predicate overload retains the last truthy value for its branch. Results and presence are read-only; exit delays retain native results while presence is false, and reentry reuses unexpired scopes.</summary>
        /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <param name="truthy">Predicate determining whether the read value belongs to the visible branch.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <param name="fallback">Optional construction callback used when the selected primary branch is absent.</param>
        /// <returns>An observable, immutable list of current and retained-exit branch results.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var result = UI.Show(() => visible.Value, () => UI.Label("Visible"));
        /// UI.Frame(UI.Children(() => result.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Show<T, TResult>(Func<T> read, Predicate<T> truthy, Func<ReadOnly<T>, ReadOnly<bool>, Branch<TResult>> build, Func<ReadOnly<bool>, Branch<TResult>> fallback = null)
        {
            Source<T> filtered = Source<T>();
            return Show(() =>
            {
                T value = read(); bool visible = truthy(value);
                if (visible) Untrack(() => filtered.Value = value);
                return visible;
            }, present => build(new ReadOnly<T>(filtered), present), fallback);
        }
        /// <summary>Retains rows by index or explicit dictionary key, updating each row&#x27;s read-only reactive value. Use explicit stable item IDs to preserve rows across reordering or immutable item replacement. Keys must be unique and non-null. Presence supports delayed exits through Branch.</summary>
        /// <typeparam name="TKey">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <param name="comparer">Optional equality/identity comparer; null selects the documented default policy.</param>
        /// <returns>An observable, immutable ordered list of row results; retained rows keep native identity.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var rows = UI.Indexes(() => items.Value, (index, item) => UI.Label(() => item.Value));
        /// UI.Column(UI.Children(() => rows.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Indexes<TKey, TValue, TResult>(Func<IEnumerable<KeyValuePair<TKey, TValue>>> read, Func<TKey, ReadOnly<TValue>, ReadOnly<bool>, Branch<TResult>> build, IEqualityComparer<TKey> comparer = null)
        {
            RequireStable();
            DynamicRows<TKey, TValue, TResult> rows = new((key, value, index, present) => build(key, value, present), comparer);
            Effect(() => { var next = read().ToArray(); Untrack(() => rows.Update(next)); });
            return rows.Output;
        }
        /// <summary>Retains rows by index or explicit dictionary key, updating each row&#x27;s read-only reactive value. Use explicit stable item IDs to preserve rows across reordering or immutable item replacement. Keys must be unique and non-null. Presence supports delayed exits through Branch.</summary>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <returns>An observable, immutable ordered list of row results; retained rows keep native identity.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var rows = UI.Indexes(() => items.Value, (index, item) => UI.Label(() => item.Value));
        /// UI.Column(UI.Children(() => rows.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Indexes<TValue, TResult>(Func<IReadOnlyList<TValue>> read, Func<int, ReadOnly<TValue>, ReadOnly<bool>, Branch<TResult>> build)
            => Indexes(() => read().Select((value, index) => new KeyValuePair<int, TValue>(index, value)), build);
        /// <summary>Retains rows by value identity and exposes each current index as a read-only reactive value. Reordering preserves constructed rows; removed rows report index -1 and false presence during delayed exit. Duplicate or null identity values are rejected, and an optional comparer controls identity.</summary>
        /// <typeparam name="TValue">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <typeparam name="TResult">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="read">The getter whose source reads establish reactive dependencies; supply a stable native result where required.</param>
        /// <param name="build">Construction callback executed in its documented ownership scope; declare owned resources here.</param>
        /// <param name="comparer">Optional equality/identity comparer; null selects the documented default policy.</param>
        /// <returns>An observable, immutable ordered list of row results; indices are read-only operator-owned state.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var rows = UI.Values(() => items.Value, (item, index) => UI.Label(() => $"{index.Value}: {item}"));
        /// UI.Column(UI.Children(() => rows.Value));
        /// ]]></code>
        /// </example>
        public static ReadOnly<IReadOnlyList<TResult>> Values<TValue, TResult>(Func<IReadOnlyList<TValue>> read, Func<TValue, ReadOnly<int>, ReadOnly<bool>, Branch<TResult>> build, IEqualityComparer<TValue> comparer = null)
        {
            RequireStable();
            DynamicRows<TValue, TValue, TResult> rows = new((value, source, index, present) => build(value, index, present), comparer);
            Effect(() => { var next = read().Select(value => new KeyValuePair<TValue, TValue>(value, value)).ToArray(); Untrack(() => rows.Update(next)); });
            return rows.Output;
        }
    }

    internal sealed class DynamicRows<TKey, TValue, TResult> : IDisposable
    {
        private sealed class Row
        {
            internal TKey Key;
            internal Source<TValue> Value;
            internal Source<int> Index;
            internal Source<bool> Present;
            internal Scope Scope;
            internal Branch<TResult> Branch;
            internal IDisposable Timer;
        }
        private readonly Dictionary<TKey, Row> _rows;
        private readonly Func<TKey, ReadOnly<TValue>, ReadOnly<int>, ReadOnly<bool>, Branch<TResult>> _build;
        private readonly Scope _owner;
        private readonly List<Row> _order = new();
        private readonly List<Row> _next = new();
        private readonly HashSet<TKey> _seen;
        private bool _outputChanged;
        private bool _disposed;
        private readonly Source<IReadOnlyList<TResult>> _output = UI.Source<IReadOnlyList<TResult>>(Array.Empty<TResult>());
        internal readonly ReadOnly<IReadOnlyList<TResult>> Output;
        internal DynamicRows(Func<TKey, ReadOnly<TValue>, ReadOnly<int>, ReadOnly<bool>, Branch<TResult>> build, IEqualityComparer<TKey> comparer = null)
        {
            _rows = new Dictionary<TKey, Row>(comparer); _seen = new HashSet<TKey>(_rows.Comparer);
            Output = new ReadOnly<IReadOnlyList<TResult>>(_output);
            _build = build; _owner = UI.RequireScope(); _owner.Own(this);
        }
        internal void Update(KeyValuePair<TKey, TValue>[] items)
        {
            if (_disposed) return;
            _seen.Clear();
            foreach (var item in items)
                if (item.Key == null || !_seen.Add(item.Key)) throw new ArgumentException("Dynamic keys must be non-null and unique.");
            UI.Batch(() => _owner.Run(() =>
            {
                List<Exception> errors = null;
                _next.Clear();
                for (int index = 0; index < items.Length; index++)
                {
                    var item = items[index];
                    if (!_rows.TryGetValue(item.Key, out Row row))
                    {
                        row = CreateRow(item.Key, item.Value, index);
                    }
                    else
                    {
                        row.Timer?.Dispose(); row.Timer = null;
                        row.Value.Value = item.Value; row.Index.Value = index; row.Present.Value = true;
                    }
                    _next.Add(row);
                }
                for (int index = 0; index < _order.Count;)
                {
                    Row row = _order[index];
                    if (_seen.Contains(row.Key)) { index++; continue; }
                    if (row.Present.Peek())
                    {
                        row.Present.Value = false; row.Index.Value = -1;
                        if (row.Branch.ExitDelay == 0)
                        {
                            try { Remove(row); }
                            catch (Exception error) { (errors ??= new()).Add(error); }
                            continue;
                        }
                        ScheduleExit(row);
                    }
                    _next.Add(row); index++;
                }
                if (_order.Count != _next.Count) _outputChanged = true;
                else for (int index = 0; index < _order.Count; index++)
                    if (_order[index] != _next[index]) { _outputChanged = true; break; }
                _order.Clear(); _order.AddRange(_next); _next.Clear(); _seen.Clear(); Publish();
                if (errors != null) throw new AggregateException("Pine branch cleanup failed.", errors);
            }));
        }
        private Row CreateRow(TKey key, TValue value, int index)
        {
            Row row = new() { Key = key, Value = UI.Source(value), Index = UI.Source(index), Present = UI.Source(true) };
            try { row.Scope = UI.OwnedRoot(() => row.Branch = _build(row.Key, new ReadOnly<TValue>(row.Value), new ReadOnly<int>(row.Index), new ReadOnly<bool>(row.Present))); }
            catch { Dispose(); throw; }
            _rows.Add(key, row); _outputChanged = true; return row;
        }
        private void ScheduleExit(Row row) => row.Timer = Clock.Delay(row.Branch.ExitDelay, () =>
        {
            try { Remove(row); }
            finally { Publish(); }
        });
        private void Remove(Row row)
        {
            row.Timer?.Dispose(); row.Timer = null; _rows.Remove(row.Key); _order.Remove(row); _outputChanged = true; row.Scope.Dispose();
        }
        private void Publish()
        {
            if (!_outputChanged) return;
            _outputChanged = false;
            TResult[] snapshot = new TResult[_order.Count];
            for (int index = 0; index < snapshot.Length; index++) snapshot[index] = _order[index].Branch.Value;
            _output.Value = Array.AsReadOnly(snapshot);
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; _owner.Release(this);
            List<Exception> errors = null;
            foreach (Row row in _rows.Values.ToArray())
            {
                row.Timer?.Dispose();
                try { row.Scope.Dispose(); }
                catch (Exception error) { (errors ??= new()).Add(error); }
            }
            _rows.Clear(); _order.Clear(); _next.Clear(); _seen.Clear();
            if (errors != null) throw new AggregateException("Pine branch cleanup failed.", errors);
        }
    }
}
