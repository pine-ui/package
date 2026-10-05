using System;
using System.Collections.Generic;
using System.Linq;

namespace Pine
{
    public readonly struct Branch<T>
    {
        public readonly T Value;
        public readonly double ExitDelay;
        public Branch(T value, double exitDelay = 0)
        {
            if (double.IsNaN(exitDelay) || double.IsInfinity(exitDelay) || exitDelay < 0) throw new ArgumentOutOfRangeException(nameof(exitDelay));
            Value = value; ExitDelay = exitDelay;
        }
        public static implicit operator Branch<T>(T value) => new(value);
    }

    public static partial class Pine
    {
        public static Source<TResult[]> Show<TResult>(Func<bool> condition, Func<TResult> build, Func<TResult> fallback = null)
            => Show<TResult>(condition, _ => build(), fallback == null ? null : _ => fallback());
        public static Source<TResult[]> Switch<TKey, TResult>(Func<TKey> select, Func<TKey, TResult> build, IEqualityComparer<TKey> comparer = null)
            => Switch<TKey, TResult>(select, (key, _) => build(key), comparer);
        public static Source<TResult[]> Indexes<TValue, TResult>(Func<IReadOnlyList<TValue>> read, Func<int, Source<TValue>, TResult> build)
            => Indexes<TValue, TResult>(read, (index, value, _) => build(index, value));
        public static Source<TResult[]> Values<TValue, TResult>(Func<IReadOnlyList<TValue>> read, Func<TValue, Source<int>, TResult> build, IEqualityComparer<TValue> comparer = null)
            => Values<TValue, TResult>(read, (value, index, _) => build(value, index), comparer);
        public static Source<TResult[]> Switch<TKey, TResult>(Func<TKey> select, Func<TKey, Source<bool>, Branch<TResult>> build, IEqualityComparer<TKey> comparer = null)
        {
            RequireStable();
            DynamicRows<TKey, TKey, TResult> rows = new((key, value, index, present) => build(key, present), comparer);
            Effect(() => { TKey key = select(); Untrack(() => rows.Update(new[] { new KeyValuePair<TKey, TKey>(key, key) })); });
            return rows.Output;
        }
        public static Source<TResult[]> Switch<TKey, TResult>(Func<TKey> select, IReadOnlyDictionary<TKey, Func<Source<bool>, Branch<TResult>>> branches, Func<Source<bool>, Branch<TResult>> fallback = null)
            => Switch(select, (key, present) => branches.TryGetValue(key, out var build) ? build(present) : fallback != null ? fallback(present) : default);

        public static Source<TResult[]> Show<TResult>(Func<bool> condition, Func<Source<bool>, Branch<TResult>> build, Func<Source<bool>, Branch<TResult>> fallback = null)
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
        public static Source<TResult[]> Show<T, TResult>(Func<T> read, Predicate<T> truthy, Func<Source<T>, Source<bool>, Branch<TResult>> build, Func<Source<bool>, Branch<TResult>> fallback = null)
        {
            Source<T> filtered = Source<T>();
            return Show(() =>
            {
                T value = read(); bool visible = truthy(value);
                if (visible) Untrack(() => filtered.Value = value);
                return visible;
            }, present => build(filtered, present), fallback);
        }
        public static Source<TResult[]> Indexes<TKey, TValue, TResult>(Func<IEnumerable<KeyValuePair<TKey, TValue>>> read, Func<TKey, Source<TValue>, Source<bool>, Branch<TResult>> build, IEqualityComparer<TKey> comparer = null)
        {
            RequireStable();
            DynamicRows<TKey, TValue, TResult> rows = new((key, value, index, present) => build(key, value, present), comparer);
            Effect(() => { var next = read().ToArray(); Untrack(() => rows.Update(next)); });
            return rows.Output;
        }
        public static Source<TResult[]> Indexes<TValue, TResult>(Func<IReadOnlyList<TValue>> read, Func<int, Source<TValue>, Source<bool>, Branch<TResult>> build)
            => Indexes(() => read().Select((value, index) => new KeyValuePair<int, TValue>(index, value)), build);
        public static Source<TResult[]> Values<TValue, TResult>(Func<IReadOnlyList<TValue>> read, Func<TValue, Source<int>, Source<bool>, Branch<TResult>> build, IEqualityComparer<TValue> comparer = null)
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
        private readonly Func<TKey, Source<TValue>, Source<int>, Source<bool>, Branch<TResult>> _build;
        private readonly Scope _owner;
        private readonly List<Row> _order = new();
        private readonly List<Row> _next = new();
        private readonly HashSet<TKey> _seen;
        private bool _outputChanged;
        private bool _disposed;
        internal readonly Source<TResult[]> Output = Pine.Source(Array.Empty<TResult>());
        internal DynamicRows(Func<TKey, Source<TValue>, Source<int>, Source<bool>, Branch<TResult>> build, IEqualityComparer<TKey> comparer = null)
        {
            _rows = new Dictionary<TKey, Row>(comparer); _seen = new HashSet<TKey>(_rows.Comparer);
            _build = build; _owner = Pine.RequireScope(); _owner.Own(this);
        }
        internal void Update(KeyValuePair<TKey, TValue>[] items)
        {
            if (_disposed) return;
            _seen.Clear();
            foreach (var item in items)
                if (item.Key == null || !_seen.Add(item.Key)) throw new ArgumentException("Dynamic keys must be non-null and unique.");
            Pine.Batch(() => _owner.Run(() =>
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
            Row row = new() { Key = key, Value = Pine.Source(value), Index = Pine.Source(index), Present = Pine.Source(true) };
            try { row.Scope = Pine.OwnedRoot(() => row.Branch = _build(row.Key, row.Value, row.Index, row.Present)); }
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
            Output.Value = snapshot;
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
