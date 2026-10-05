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

    /// <summary>A typed mapping between a custom value and a fixed number of finite double lanes. Pack and Unpack must agree on component order and lane count; the lane count cannot change during a spring lifetime. Pass an explicit space for a custom struct rather than relying on reflection.</summary>
    /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
    /// <example>
    /// <code><![CDATA[
    /// var space = new SpringSpace<float>(v => new[] { (double)v }, lanes => (float)lanes[0]);
    /// UI.Spring(() => 10f, space: space);
    /// ]]></code>
    /// </example>
    public sealed class SpringSpace<T>
    {
        /// <summary>Maps the typed value into a fixed number of finite double lanes. Lane order must agree with Unpack.</summary>
        /// <example>
        /// <code><![CDATA[
        /// double[] lanes = space.Pack(10f);
        /// ]]></code>
        /// </example>
        public readonly Func<T, double[]> Pack;
        /// <summary>Reconstructs the typed value from the fixed lane order produced by Pack.</summary>
        /// <example>
        /// <code><![CDATA[
        /// float value = space.Unpack(new[] { 10d });
        /// ]]></code>
        /// </example>
        public readonly Func<double[], T> Unpack;
        /// <summary>Constructs this value with the supplied typed arguments. A typed mapping between a custom value and a fixed number of finite double lanes. Pack and Unpack must agree on component order and lane count; the lane count cannot change during a spring lifetime. Pass an explicit space for a custom struct rather than relying on reflection.</summary>
        /// <param name="pack">Mapping to a fixed number of finite double lanes.</param>
        /// <param name="unpack">Mapping from those same ordered lanes back to the typed value.</param>
        /// <example>
        /// <code><![CDATA[
        /// var space = new SpringSpace<float>(v => new[] { (double)v }, lanes => (float)lanes[0]);
        /// UI.Spring(() => 10f, space: space);
        /// ]]></code>
        /// </example>
        public SpringSpace(Func<T, double[]> pack, Func<double[], T> unpack) { Pack = pack; Unpack = unpack; }
    }

    /// <summary>Built-in lane mappings for scalar floats, doubles and fixed-length double arrays. Arrays are copied when packed/unpacked to protect solver storage. Use UnitySpringSpaces for Unity vector, color, rectangle, rotation and pose values.</summary>
    /// <example>
    /// <code><![CDATA[
    /// UI.Spring(() => 1f, space: SpringSpaces.Float);
    /// ]]></code>
    /// </example>
    public static class SpringSpaces
    {
        internal static readonly Dictionary<Type, object> Registered = new();
        /// <summary>Built-in fixed-lane mapping for Float. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: SpringSpaces.Float);
        /// ]]></code>
        /// </example>
        public static readonly SpringSpace<float> Float = new(value => new[] { (double)value }, value => (float)value[0]);
        /// <summary>Built-in fixed-lane mapping for Double. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: SpringSpaces.Double);
        /// ]]></code>
        /// </example>
        public static readonly SpringSpace<double> Double = new(value => new[] { value }, value => value[0]);
        /// <summary>Built-in fixed-lane mapping for Array. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: SpringSpaces.Array);
        /// ]]></code>
        /// </example>
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

    public static partial class UI
    {
        static partial void ConfigureSpringSpaces();
        /// <summary>An owned reactive analytic spring whose output moves toward a tracked target. Period and damping accept typed reactive inputs. Automatic runtime updates use unscaled time; UI.Step selects explicit manual clock advancement. ReducedMotion snaps changing targets without animated travel. Dispose releases its watch and clock listener.</summary>
        /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
        /// <param name="target">The existing native component or tracked target getter, as specified by this overload.</param>
        /// <param name="period">Reactive positive finite spring period in seconds; null uses the default.</param>
        /// <param name="dampingRatio">Reactive finite non-negative damping; null uses the default.</param>
        /// <param name="space">Optional fixed-lane mapping for the spring value type.</param>
        /// <returns>An owned animated value in the selected fixed-lane space; its output tracks reactive reads.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var target = UI.Source(0f);
        /// var motion = UI.Spring(() => target.Value, period: 0.4, dampingRatio: 0.8);
        /// UI.Image(UI.Position(() => new UnityEngine.Vector2(motion.Value, 0)));
        /// ]]></code>
        /// </example>
        public static Spring<T> Spring<T>(Func<T> target, Value<double>? period = null, Value<double>? dampingRatio = null, SpringSpace<T> space = null)
        {
            RequireStable(); ConfigureSpringSpaces();
            return new Spring<T>(target, period ?? new Value<double>(1), dampingRatio ?? new Value<double>(1), space ?? SpringSpaces.Default<T>());
        }
    }

    /// <summary>An owned reactive analytic spring whose output moves toward a tracked target. Period and damping accept typed reactive inputs. Automatic runtime updates use unscaled time; UI.Step selects explicit manual clock advancement. ReducedMotion snaps changing targets without animated travel. Dispose releases its watch and clock listener.</summary>
    /// <typeparam name="T">Typed value, native result or identity contract; see the summary for its role.</typeparam>
    /// <example>
    /// <code><![CDATA[
    /// var target = UI.Source(0f);
    /// var motion = UI.Spring(() => target.Value, period: 0.4, dampingRatio: 0.8);
    /// UI.Image(UI.Position(() => new UnityEngine.Vector2(motion.Value, 0)));
    /// ]]></code>
    /// </example>
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
            _owner = UI.RequireScope(); _space = space;
            T initial = UI.Untrack(target);
            _position = space.Pack(initial); _target = (double[])_position.Clone(); _velocity = new double[_position.Length];
            _output = UI.Source(initial);
            _watch = UI.Effect(() =>
            {
                double nextPeriod = period.Read(); double nextDamping = damping.Read();
                if (nextPeriod <= 0 || double.IsNaN(nextPeriod) || double.IsInfinity(nextPeriod)) throw new ArgumentOutOfRangeException(nameof(period));
                if (nextDamping < 0 || double.IsNaN(nextDamping) || double.IsInfinity(nextDamping)) throw new ArgumentOutOfRangeException(nameof(damping));
                double[] next = space.Pack(target());
                Validate(next);
                bool reduced = UI.ReducedMotion.Value;
                if (reduced) { _position = (double[])next.Clone(); System.Array.Clear(_velocity, 0, _velocity.Length); _output.Value = _space.Unpack(_position); }
                if (_period != nextPeriod || _damping != nextDamping)
                    Coefficients(2 * Math.PI / nextPeriod, nextDamping, 1d / 120d, out _xx, out _xv, out _vx, out _vv);
                _target = next; _period = nextPeriod; _damping = nextDamping; Activate();
            });
            _owner.Own(this);
        }
        /// <summary>Reads reactive spring output. Assigning sets an immediate position and clears velocity; subsequent target changes can resume motion. Access after disposal throws.</summary>
        /// <example>
        /// <code><![CDATA[
        /// motion.Value = 20f;
        /// ]]></code>
        /// </example>
        public T Value
        {
            get { if (_disposed) throw new ObjectDisposedException(nameof(Spring<T>)); return _output.Value; }
            set { Control(position: new Value<T>(value)); System.Array.Clear(_velocity, 0, _velocity.Length); _output.Value = value; }
        }
        /// <summary>Sets position and/or velocity and adds an impulse using the spring&#x27;s fixed typed space. Lane counts must agree and values must be finite. Reduced motion applies explicit positions without velocity animation.</summary>
        /// <param name="position">Typed immediate position or reactive native position, as specified by this overload.</param>
        /// <param name="velocity">Optional finite velocity input expressed through the same fixed spring space.</param>
        /// <param name="impulse">Optional finite velocity increment expressed through the same fixed spring space.</param>
        /// <example>
        /// <code><![CDATA[
        /// motion.Control(impulse: new Value<float>(10f));
        /// ]]></code>
        /// </example>
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
            if (UI.ReducedMotion.Peek())
            {
                System.Array.Clear(_velocity, 0, _velocity.Length);
                _output.Value = _space.Unpack(_position); _active = false;
                _clock?.Dispose(); _clock = null; return;
            }
            Activate();
        }
        private void Activate()
        {
            if (UI.ReducedMotion.Peek()) { _active = false; _clock?.Dispose(); _clock = null; return; }
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
        /// <summary>Ends this owned lifetime idempotently. Dependencies and native event/clock registrations are released; Scope/Mount cleanup attempts all resources and aggregates failures. Application code disposes a mount when its owner ends.</summary>
        /// <example>
        /// <code><![CDATA[
        /// motion.Dispose();
        /// ]]></code>
        /// </example>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; _watch.Dispose(); _clock?.Dispose(); _owner.Release(this);
        }
    }
}
