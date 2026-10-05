using System;
using System.Collections.Generic;
using System.Buffers;

namespace Pine
{
    internal static class Clock
    {
        private static readonly List<ClockTask> _tasks = new();
        internal static Action EnsureHost = null;
        internal static bool Manual;
        internal static double Time;

        internal static IDisposable Listen(Action<double> update)
        {
            ClockTask task = new(update, double.PositiveInfinity, null);
            _tasks.Add(task); EnsureHost?.Invoke(); return task;
        }
        internal static IDisposable Delay(double seconds, Action action)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            ClockTask task = new(null, Time + seconds, action);
            _tasks.Add(task); EnsureHost?.Invoke(); return task;
        }
        internal static void Step(double dt, bool manual)
        {
            if (double.IsNaN(dt) || double.IsInfinity(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
            if (manual) Manual = true;
            else if (Manual) return;
            Time += dt;
            if (_tasks.Count == 0) return;
            int count = _tasks.Count;
            ClockTask[] snapshot = ArrayPool<ClockTask>.Shared.Rent(count);
            _tasks.CopyTo(snapshot);
            ReactiveRuntime.BatchDepth++;
            List<Exception> errors = null;
            try
            {
                for (int index = 0; index < count; index++)
                {
                    ClockTask task = snapshot[index];
                    if (task.Disposed) continue;
                    try
                    {
                        if (task.Update != null) task.Update(dt);
                        else if (task.Deadline <= Time)
                        {
                            task.Dispose(); task.Complete();
                        }
                    }
                    catch (Exception error) { (errors ??= new()).Add(error); }
                }
            }
            finally
            {
                ArrayPool<ClockTask>.Shared.Return(snapshot, clearArray: true);
                ReactiveRuntime.BatchDepth--;
                try { ReactiveRuntime.Flush(); }
                catch (Exception error) { (errors ??= new()).Add(error); }
            }
            if (errors != null) throw new AggregateException("Pine clock updates failed.", errors);
        }
        internal static void Reset()
        {
            foreach (ClockTask task in _tasks.ToArray()) task.Dispose();
            Time = 0; Manual = false;
        }
        private sealed class ClockTask : IDisposable
        {
            internal readonly Action<double> Update;
            internal readonly double Deadline;
            internal readonly Action Complete;
            internal bool Disposed;
            internal ClockTask(Action<double> update, double deadline, Action complete) { Update = update; Deadline = deadline; Complete = complete; }
            public void Dispose() { if (Disposed) return; Disposed = true; _tasks.Remove(this); }
        }
    }

    public sealed class SpringSpace<T>
    {
        public readonly Func<T, double[]> Pack;
        public readonly Func<double[], T> Unpack;
        public SpringSpace(Func<T, double[]> pack, Func<double[], T> unpack) { Pack = pack; Unpack = unpack; }
    }

    public static class SpringSpaces
    {
        internal static readonly Dictionary<Type, object> Registered = new();
        public static readonly SpringSpace<float> Float = new(value => new[] { (double)value }, value => (float)value[0]);
        public static readonly SpringSpace<double> Double = new(value => new[] { value }, value => value[0]);
        public static readonly SpringSpace<double[]> Array = new(value => (double[])value.Clone(), value => (double[])value.Clone());
        internal static SpringSpace<T> Default<T>()
        {
            if (typeof(T) == typeof(float)) return (SpringSpace<T>)(object)Float;
            if (typeof(T) == typeof(double)) return (SpringSpace<T>)(object)Double;
            if (typeof(T) == typeof(double[])) return (SpringSpace<T>)(object)Array;
            if (Registered.TryGetValue(typeof(T), out object space)) return (SpringSpace<T>)space;
            throw new NotSupportedException($"Supply a SpringSpace<{typeof(T).Name}> for this type.");
        }
    }

    public static partial class Pine
    {
        static partial void ConfigureSpringSpaces();
        public static Spring<T> Spring<T>(Func<T> target, Value<double>? period = null, Value<double>? dampingRatio = null, SpringSpace<T> space = null)
        {
            RequireStable(); ConfigureSpringSpaces();
            return new Spring<T>(target, period ?? new Value<double>(1), dampingRatio ?? new Value<double>(1), space ?? SpringSpaces.Default<T>());
        }
    }

    public sealed class Spring<T> : IDisposable
    {
        private readonly SpringSpace<T> _space;
        private readonly Source<T> _output;
        private readonly IDisposable _watch;
        private IDisposable _clock;
        private readonly Scope _owner;
        private double[] _position;
        private double[] _velocity;
        private double[] _target;
        private double _period;
        private double _damping;
        private bool _disposed;
        private double _elapsed;
        private bool _active;
        private double _xx, _xv, _vx, _vv;

        internal Spring(Func<T> target, Value<double> period, Value<double> damping, SpringSpace<T> space)
        {
            _owner = Pine.RequireScope(); _space = space;
            T initial = Pine.Untrack(target);
            _position = space.Pack(initial); _target = (double[])_position.Clone(); _velocity = new double[_position.Length];
            _output = Pine.Source(initial);
            _watch = Pine.Effect(() =>
            {
                double nextPeriod = period.Read(); double nextDamping = damping.Read();
                if (nextPeriod <= 0 || double.IsNaN(nextPeriod) || double.IsInfinity(nextPeriod)) throw new ArgumentOutOfRangeException(nameof(period));
                if (nextDamping < 0 || double.IsNaN(nextDamping) || double.IsInfinity(nextDamping)) throw new ArgumentOutOfRangeException(nameof(damping));
                double[] next = space.Pack(target());
                Validate(next);
                if (_period != nextPeriod || _damping != nextDamping)
                    Coefficients(2 * Math.PI / nextPeriod, nextDamping, 1d / 120d, out _xx, out _xv, out _vx, out _vv);
                _target = next; _period = nextPeriod; _damping = nextDamping; Activate();
            });
            _owner.Own(this);
        }
        public T Value
        {
            get { if (_disposed) throw new ObjectDisposedException(nameof(Spring<T>)); return _output.Value; }
            set { Control(position: new Value<T>(value)); System.Array.Clear(_velocity, 0, _velocity.Length); _output.Value = value; }
        }
        public void Control(Value<T>? position = null, Value<T>? velocity = null, Value<T>? impulse = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Spring<T>));
            if (position.HasValue) { double[] next = _space.Pack(position.Value.Read()); Validate(next); _position = next; }
            if (velocity.HasValue) { double[] next = _space.Pack(velocity.Value.Read()); Validate(next); _velocity = next; }
            if (impulse.HasValue)
            {
                double[] next = _space.Pack(impulse.Value.Read()); Validate(next);
                for (int lane = 0; lane < next.Length; lane++) _velocity[lane] += next[lane];
            }
            Activate();
        }
        private void Activate()
        {
            _active = true; _clock ??= Clock.Listen(Update);
        }
        private void Validate(double[] value)
        {
            if (value.Length != _position.Length) throw new ArgumentException("Spring component count cannot change.");
            foreach (double lane in value) if (double.IsNaN(lane) || double.IsInfinity(lane)) throw new ArgumentException("Spring components must be finite.");
        }
        private void Update(double dt)
        {
            if (!_active || _disposed) return;
            _elapsed += dt;
            const double h = 1d / 120d;
            int steps = (int)Math.Min(120000, Math.Floor(_elapsed / h));
            _elapsed -= steps * h;
            for (int step = 0; step < steps; step++)
            {
                for (int lane = 0; lane < _position.Length; lane++)
                {
                    double x = _position[lane] - _target[lane]; double v = _velocity[lane];
                    _position[lane] = _target[lane] + _xx * x + _xv * v;
                    _velocity[lane] = _vx * x + _vv * v;
                }
            }
            bool settled = true;
            for (int lane = 0; lane < _position.Length; lane++)
            {
                double tolerance = 0.00001 * Math.Max(1, Math.Abs(_target[lane]));
                if (Math.Abs(_position[lane] - _target[lane]) > tolerance || Math.Abs(_velocity[lane]) > tolerance) settled = false;
            }
            if (settled)
            {
                System.Array.Copy(_target, _position, _target.Length); System.Array.Clear(_velocity, 0, _velocity.Length);
                _active = false; _clock?.Dispose(); _clock = null;
            }
            _output.Value = _space.Unpack(_position);
        }
        private static void Coefficients(double w, double damping, double h, out double xx, out double xv, out double vx, out double vv)
        {
            if (Math.Abs(damping - 1) < 0.000001)
            {
                double decay = Math.Exp(-w * h);
                xx = decay * (1 + w * h); xv = decay * h; vx = -decay * w * w * h; vv = decay * (1 - w * h);
            }
            else if (damping < 1)
            {
                double alpha = w * damping; double beta = w * Math.Sqrt(1 - damping * damping);
                double decay = Math.Exp(-alpha * h); double cosine = Math.Cos(beta * h); double sine = Math.Sin(beta * h);
                double factor = decay * sine / beta;
                xx = decay * cosine + alpha * factor; xv = factor;
                vx = -w * w * factor; vv = decay * cosine - alpha * factor;
            }
            else
            {
                double root = Math.Sqrt(damping * damping - 1); double r1 = -w * (damping - root); double r2 = -w * (damping + root);
                double e1 = Math.Exp(r1 * h); double e2 = Math.Exp(r2 * h);
                double denominator = r1 - r2;
                xx = (r1 * e2 - r2 * e1) / denominator; xv = (e1 - e2) / denominator;
                vx = r1 * r2 * (e2 - e1) / denominator; vv = (r1 * e1 - r2 * e2) / denominator;
            }
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; _watch.Dispose(); _clock?.Dispose(); _owner.Release(this);
        }
    }
}
