using System;
using System.Collections.Generic;
using System.Linq;
using Pine;
using P = Pine.Pine;

internal static class Program
{
    private static int _checks;
    private static void Check(bool passed, string name)
    {
        _checks++; if (!passed) throw new Exception(name);
    }
    private static void Throws(Action action, string name)
    {
        try { action(); } catch (InvalidOperationException) { Check(true, name); return; }
        throw new Exception(name);
    }
    private static int Main()
    {
        Core(); Lifetimes(); Dynamic(); Animation();
        Console.WriteLine($"PINE_CORE_CHECKS_PASSED ({_checks} assertions)");
        return AuditRegression.Run();
    }
    private static void Core()
    {
        var value = P.Source(1); var choose = P.Source(true); var other = P.Source(10);
        int computes = 0, effects = 0, result = 0;
        using var root = P.Root(() =>
        {
            var parity = P.Derive(() => { computes++; return value.Value % 2; });
            P.Effect(() => { result = parity.Value; effects++; });
            Check(computes == 1 && effects == 1 && result == 1, "Initial eager evaluation");
            value.Value = 3;
            Check(computes == 2 && effects == 1, "Unchanged derived output suppresses consumers");
            value.Value = 3; Check(computes == 2, "Primitive equality");
            value.Value = 4; Check(result == 0 && effects == 2, "Changed derived output");
            var conditional = P.Derive(() => choose.Value ? value.Value : other.Value);
            Check(conditional.Value == 4, "Conditional initial");
            choose.Value = false; Check(conditional.Value == 10, "Conditional changes dependencies");
            value.Value = 6; Check(conditional.Value == 10, "Old dependencies detached");
            other.Value = 20; Check(conditional.Value == 20, "New dependencies tracked");
            var a = P.Derive(() => value.Value + 1);
            var b = P.Derive(() => a.Value * 2);
            var c = P.Derive(() => value.Value * 3);
            var diamond = P.Derive(() => b.Value + c.Value);
            int diamondRuns = 0, diamondValue = 0;
            P.Effect(() => { diamondValue = diamond.Value; diamondRuns++; });
            P.Batch(() => { value.Value = 7; value.Value = 8; });
            Check(diamondValue == 42 && diamondRuns == 2, "Unequal-depth diamond settles before effects");
            int untrackedRuns = 0;
            P.Effect(() => { _ = choose.Value; P.Untrack(() => other.Value); untrackedRuns++; });
            other.Value = 30; Check(untrackedRuns == 1, "Untrack");
            var mutable = P.Source(new List<int>()); int mutableRuns = 0;
            P.Effect(() => { _ = mutable.Value; mutableRuns++; });
            mutable.Value.Add(1); mutable.Value = mutable.Peek();
            Check(mutableRuns == 2, "Same mutable reference notifies");
            var explicitEquality = P.Source(mutable.Peek(), EqualityComparer<List<int>>.Default);
            int explicitRuns = 0; P.Effect(() => { _ = explicitEquality.Value; explicitRuns++; });
            explicitEquality.Value = explicitEquality.Peek(); Check(explicitRuns == 1, "Explicit comparer");
            int accumulator = 0;
            P.Effect<int>(previous => { _ = choose.Value; accumulator = previous + 1; return accumulator; }, 10);
            choose.Value = true; Check(accumulator == 12, "Previous-result effect");
            Check(P.Read(3) == 3 && P.Read(value) == 8 && P.Read(new Value<int>(() => 9)) == 9, "Read variants");
            Throws(() => P.Derive(() => { value.Value = 9; return 0; }), "No source writes from derive");
            Throws(() => P.Effect(() => P.Effect(() => { })), "Stable scope guard");
        });
        Throws(() => P.Effect(() => { }), "Ownership required");
    }
    private static void Lifetimes()
    {
        var value = P.Source(0); int cleaned = 0, runs = 0;
        var context = P.Context("default"); Scope independent = null;
        var root = P.Root(() =>
        {
            Check(context.Value == "default", "Context fallback");
            context.Provide("outer", () =>
            {
                P.Effect(() => { _ = value.Value; Check(context.Value == "outer", "Provider retained in later effect"); runs++; P.Cleanup(() => cleaned++); });
                context.Provide("inner", () => Check(context.Value == "inner", "Nearest provider"));
            });
            independent = P.Root(() => P.Cleanup(() => cleaned += 100));
        });
        value.Value = 1; Check(cleaned == 1 && runs == 2, "Effect cleanup before rerun");
        root.Dispose(); Check(cleaned == 2 && !independent.IsDisposed, "Independent roots");
        value.Value = 2; Check(runs == 2, "Disposed listeners detached");
        root.Dispose(); Check(cleaned == 2, "Idempotent dispose");
        independent.Dispose(); Check(cleaned == 102, "Independent lifetime cleanup");
        int finalCleanup = 0;
        var throwing = P.Root(() => { P.Cleanup(() => finalCleanup++); P.Cleanup(() => throw new Exception("cleanup")); });
        try { throwing.Dispose(); } catch (AggregateException) { }
        Check(finalCleanup == 1, "Cleanup continues after error");
    }
    private static void Dynamic()
    {
        var key = P.Source("a"); int builds = 0, destroys = 0; var presence = new Dictionary<string, Source<bool>>();
        using (var root = P.Root(() =>
        {
            var switched = P.Switch<string, string>(() => key.Value, (name, present) =>
            {
                builds++; presence[name] = present; P.Cleanup(() => destroys++); return new Branch<string>(name, 0.5);
            });
            Check(switched.Value.SequenceEqual(new[] { "a" }), "Switch initial");
            key.Value = "b"; Check(switched.Value.SequenceEqual(new[] { "b", "a" }) && !presence["a"].Value, "Exit retained with false presence");
            P.Step(0.2); key.Value = "a";
            Check(builds == 2 && presence["a"].Value, "Switch reentry reuses branch");
            P.Step(0.31); Check(destroys == 0, "Canceled exit cannot destroy reentered branch");
            P.Step(0.2); Check(destroys == 1 && switched.Value.SequenceEqual(new[] { "a" }), "Exit expires");
            var show = P.Source(true); int showBuilds = 0;
            var shown = P.Show<string>(() => show.Value, present => { showBuilds++; return new Branch<string>("visible", 0.5); });
            show.Value = false; Check(shown.Value.Length == 1, "Show delayed removal");
            show.Value = true; Check(showBuilds == 1, "Show reentry reuse");
            show.Value = false; P.Step(0.6); Check(shown.Value.Length == 0, "Show disposal");
            var text = P.Source("hello"); Source<string> filtered = null;
            P.Show<string, string>(() => text.Value, s => !string.IsNullOrEmpty(s), (s, present) => { filtered = s; return new Branch<string>("value", 1); });
            text.Value = "next"; Check(filtered.Value == "next", "Filtered show value updates");
            text.Value = ""; Check(filtered.Value == "next", "Falsy values retain last truthy");
            var list = P.Source<IReadOnlyList<string>>(new[] { "a", "b" }); int indexedBuilds = 0; Source<string> indexZero = null;
            var indexed = P.Indexes<string, int>(() => list.Value, (index, value, present) => { indexedBuilds++; if (index == 0) indexZero = value; return index; });
            list.Value = new[] { "b", "a" }; Check(indexedBuilds == 2 && indexZero.Value == "b" && indexed.Value.Length == 2, "Indexes preserve keys and change values");
            int valuedBuilds = 0; var indices = new Dictionary<string, Source<int>>();
            var valued = P.Values<string, string>(() => list.Value, (value, index, present) => { valuedBuilds++; indices[value] = index; return new Branch<string>(value, 0.25); });
            list.Value = new[] { "a", "b" }; Check(valuedBuilds == 2 && indices["a"].Value == 0 && indices["b"].Value == 1, "Values preserve identity on reorder");
            list.Value = new[] { "a" }; Check(indices["b"].Value == -1 && valued.Value.Length == 2, "Removed value index and exit");
            list.Value = new[] { "b", "a" }; Check(valuedBuilds == 2, "List reentry reuse");
            list.Value = Array.Empty<string>(); P.Step(0.3); Check(valued.Value.Length == 0, "List delayed cleanup");
            bool rejected = false;
            try { list.Value = new[] { "same", "same" }; } catch (ArgumentException) { rejected = true; }
        catch (AggregateException error) { rejected = error.Flatten().InnerExceptions.All(e => e is ArgumentException); }
            Check(rejected, "Values require unique identity");
            var mappedKey = P.Source(0);
            var mapped = P.Switch<int, string>(() => mappedKey.Value, new Dictionary<int, Func<Source<bool>, Branch<string>>> { [0] = p => "zero" }, p => "fallback");
            mappedKey.Value = 2; Check(mapped.Value.Single() == "fallback", "Switch fallback");
        })) { }
        Check(destroys == 2, "Owner disposes all retained branches");
    }
    private static void Animation()
    {
        Clock.Reset(); var target = P.Source(0d); Spring<double> spring = null;
        using (var root = P.Root(() =>
        {
            spring = P.Spring(() => target.Value, period: new Value<double>(0.4));
            Check(spring.Value == 0, "Spring initial"); target.Value = 10;
            P.Step(0.05); Check(spring.Value > 0 && spring.Value < 10, "Spring follows target");
            for (int i = 0; i < 100; i++) P.Step(0.02);
            Check(Math.Abs(spring.Value - 10) < 0.0001, "Spring settles");
            spring.Value = 3; Check(spring.Value == 3, "Immediate position setter");
            spring.Control(velocity: new Value<double>(5), impulse: new Value<double>(2)); P.Step(0.05);
            Check(spring.Value > 3, "Velocity and impulse");
            double before = spring.Value; Clock.Step(1, false); Check(spring.Value == before, "Manual step disables automatic clock");
            bool invalid = false; try { P.Step(-1); } catch (ArgumentOutOfRangeException) { invalid = true; } Check(invalid, "Negative time rejected");
            invalid = false; try { P.Spring(() => 0d, period: new Value<double>(0)); } catch (ArgumentOutOfRangeException) { invalid = true; } Check(invalid, "Invalid period rejected");
        })) { }
        bool disposed = false; try { _ = spring.Value; } catch (ObjectDisposedException) { disposed = true; } Check(disposed, "Spring owned disposal");
        Check(P.Version == new Version(0, 1, 0), "Pine version");
    }
}
