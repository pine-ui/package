using System;
using System.Collections.Generic;
using System.Linq;
using Pine;

internal static class AuditRegression
{
    private static int _passed;
    private static int _failed;

    public static int Run()
    {
        Run(
            "source value adapters track without allocating getter delegates",
            () =>
            {
                var source = P.Source(1);
                Value<int> warm = source;
                long before = GC.GetAllocatedBytesForCurrentThread();
                int total = 0;
                for (int index = 0; index < 1000; index++)
                {
                    Value<int> value = source;
                    if (!value.IsDynamic)
                        throw new Exception("A source adapter must stay reactive.");
                    total += value.Read();
                }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Equal(1000, total);
                if (allocated > 4096)
                    throw new Exception("Source adapters allocated " + allocated + " bytes.");
                int observed = 0;
                using var root = P.Root(() => P.Effect(() => observed = warm.Read()));
                source.Value = 2;
                Equal(2, observed);
            }
        );
        Run(
            "spring position control publishes immediately",
            () =>
            {
                using var root = P.Root(() =>
                {
                    var spring = P.Spring(() => 0d);
                    int updates = 0;
                    P.Effect(() =>
                    {
                        _ = spring.Value;
                        updates++;
                    });
                    spring.Control(position: 5d);
                    Equal(5d, spring.Value);
                    Equal(2, updates);
                    spring.Value = 3d;
                    Equal(3d, spring.Value);
                    Equal(3, updates);
                });
            }
        );
        Run(
            "spring rejects overflowing accumulated impulses without changing velocity",
            () =>
            {
                using var root = P.Root(() =>
                {
                    var spring = P.Spring(() => 0d);
                    spring.Control(impulse: double.MaxValue);
                    Throws(() => spring.Control(impulse: double.MaxValue));
                    spring.Control(velocity: 0d);
                    P.Step(1d / 120d);
                    Equal(0d, spring.Value);
                });
            }
        );
        Run(
            "unrepresentable spring coefficients fail instead of snapping",
            () =>
            {
                using var root = P.Root(() =>
                {
                    Throws(() => P.Spring(() => 0d, dampingRatio: 1e200));
                    Throws(() => P.Spring(() => 0d, period: double.Epsilon));
                });
            }
        );
        Run(
            "spring integration overflow cannot publish nonfinite output",
            () =>
            {
                using var root = P.Root(() =>
                {
                    var spring = P.Spring(() => double.MaxValue);
                    spring.Value = -double.MaxValue;
                    Throws(() => P.Step(1d / 120d));
                    Equal(-double.MaxValue, spring.Value);
                    spring.Control(position: 0d, velocity: 0d);
                    Equal(0d, spring.Value);
                });
            }
        );
        Run(
            "bulk disposal avoids per-resource delegate allocation",
            () =>
            {
                var root = P.Root(() =>
                {
                    for (int index = 0; index < 1000; index++)
                        P.Effect(() => { });
                });
                long before = GC.GetAllocatedBytesForCurrentThread();
                root.Dispose();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                if (allocated > 4096)
                    throw new Exception("Bulk cleanup allocated " + allocated + " bytes.");
            }
        );
        Run(
            "cleanup and resulting update failures are both reported",
            () =>
            {
                var source = P.Source(0);
                using var observer = P.Root(() =>
                    P.Effect(() =>
                    {
                        if (source.Value != 0)
                            throw new InvalidOperationException("update");
                    })
                );
                var root = P.Root(() =>
                {
                    P.Cleanup(() => source.Value = 1);
                    P.Cleanup(() => throw new InvalidOperationException("cleanup"));
                });
                var errors = Throws(root.Dispose).Flatten().InnerExceptions;
                Equal("cleanup,update", string.Join(",", errors.Select(error => error.Message)));
                Equal(true, root.IsDisposed);
            }
        );
        Run(
            "effect failures do not drop independent updates",
            () =>
            {
                var bad = P.Source(0);
                var good = P.Source(0);
                int displayed = -1;
                using var root = P.Root(() =>
                {
                    P.Effect(() =>
                    {
                        if (bad.Value == 1)
                            throw new InvalidOperationException("binding");
                    });
                    P.Effect(() => displayed = good.Value);
                });
                Throws(() =>
                    P.Batch(() =>
                    {
                        bad.Value = 1;
                        good.Value = 1;
                    })
                );
                Equal(1, displayed);
                good.Value = 2;
                Equal(2, displayed);
            }
        );
        Run(
            "multiple update errors are reported together",
            () =>
            {
                var source = P.Source(0);
                using var root = P.Root(() =>
                {
                    P.Effect(() =>
                    {
                        if (source.Value != 0)
                            throw new InvalidOperationException("first");
                    });
                    P.Effect(() =>
                    {
                        if (source.Value != 0)
                            throw new InvalidOperationException("second");
                    });
                });
                Equal(2, Throws(() => source.Value = 1).Flatten().InnerExceptions.Count);
            }
        );
        Run(
            "derived failures do not drop independent updates",
            () =>
            {
                var bad = P.Source(0);
                var good = P.Source(0);
                int displayed = -1;
                using var root = P.Root(() =>
                {
                    P.Derive(() =>
                        bad.Value == 1 ? throw new InvalidOperationException("derive") : bad.Value
                    );
                    P.Effect(() => displayed = good.Value);
                });
                Throws(() =>
                    P.Batch(() =>
                    {
                        bad.Value = 1;
                        good.Value = 1;
                    })
                );
                Equal(1, displayed);
            }
        );
        Run(
            "derived chains recover on a later valid update",
            () =>
            {
                var source = P.Source(0);
                int displayed = -1;
                using var root = P.Root(() =>
                {
                    var middle = P.Derive(() =>
                        source.Value == 1
                            ? throw new InvalidOperationException("derive")
                            : source.Value
                    );
                    var end = P.Derive(() => middle.Value * 2);
                    P.Effect(() => displayed = end.Value);
                });
                Throws(() => source.Value = 1);
                source.Value = 2;
                Equal(4, displayed);
            }
        );
        Run(
            "effect can recover after throwing cleanup",
            () =>
            {
                var source = P.Source(0);
                int displayed = -1;
                using var root = P.Root(() =>
                    P.Effect(() =>
                    {
                        displayed = source.Value;
                        if (displayed == 0)
                            P.Cleanup(() => throw new InvalidOperationException("cleanup"));
                    })
                );
                Throws(() => source.Value = 1);
                source.Value = 2;
                Equal(2, displayed);
            }
        );
        Run(
            "effect replaces conditional dependencies after cleanup recovery",
            () =>
            {
                var select = P.Source(false);
                var a = P.Source(0);
                var b = P.Source(0);
                int runs = 0;
                using var root = P.Root(() =>
                    P.Effect(() =>
                    {
                        _ = select.Value ? b.Value : a.Value;
                        if (runs++ == 0)
                            P.Cleanup(() => throw new InvalidOperationException("cleanup"));
                    })
                );
                Throws(() => select.Value = true);
                a.Value = 1;
                Equal(2, runs);
                a.Value = 2;
                Equal(2, runs);
                b.Value = 1;
                Equal(3, runs);
            }
        );
        Run(
            "cleanup writes do not reschedule the effect being reset",
            () =>
            {
                var source = P.Source(0);
                int runs = 0;
                using var root = P.Root(() =>
                    P.Effect(() =>
                    {
                        _ = source.Value;
                        runs++;
                        P.Cleanup(() => source.Value++);
                    })
                );
                source.Value = 1;
                Equal(2, source.Value);
                Equal(2, runs);
            }
        );
        Run(
            "chained derived reads are current within a batch",
            () =>
            {
                var source = P.Source(1);
                Derived<int> end = null;
                using var root = P.Root(() =>
                {
                    var middle = P.Derive(() => source.Value * 2);
                    end = P.Derive(() => middle.Value * 2);
                });
                P.Batch(() =>
                {
                    source.Value = 2;
                    Equal(8, end.Value);
                    source.Value = 3;
                    Equal(12, end.Value);
                });
            }
        );
        Run(
            "batched derived reads keep effects deferred",
            () =>
            {
                var source = P.Source(0);
                Derived<int> end = null;
                int runs = 0;
                using var root = P.Root(() =>
                {
                    var middle = P.Derive(() => source.Value * 2);
                    end = P.Derive(() => middle.Value * 2);
                    P.Effect(() =>
                    {
                        _ = end.Value;
                        runs++;
                    });
                });
                P.Batch(() =>
                {
                    source.Value = 1;
                    Equal(4, end.Value);
                    source.Value = 2;
                    Equal(8, end.Value);
                    Equal(1, runs);
                });
                Equal(2, runs);
            }
        );
        Run(
            "unchanged derived outputs suppress effects",
            () =>
            {
                var source = P.Source(1);
                Derived<int> end = null;
                int runs = 0;
                using var root = P.Root(() =>
                {
                    var parity = P.Derive(() => source.Value % 2);
                    end = P.Derive(() => parity.Value * 2);
                    P.Effect(() =>
                    {
                        _ = end.Value;
                        runs++;
                    });
                });
                P.Batch(() =>
                {
                    source.Value = 3;
                    Equal(2, end.Value);
                });
                Equal(1, runs);
            }
        );
        Run(
            "deep shared dependency graphs retain cached reads",
            () =>
            {
                var source = P.Source(1);
                Derived<int> end = null;
                using var root = P.Root(() =>
                {
                    end = P.Derive(() => source.Value);
                    for (int layer = 0; layer < 24; layer++)
                    {
                        var previous = end;
                        var left = P.Derive(() => previous.Value);
                        var right = P.Derive(() => previous.Value);
                        end = P.Derive(() => left.Value + right.Value);
                    }
                });
                P.Batch(() =>
                {
                    source.Value = 2;
                    Equal(2 << 24, end.Value);
                });
                Equal(2 << 24, end.Value);
            }
        );
        Run(
            "explicit source notifications refresh chained derived reads",
            () =>
            {
                var values = new[] { 1 };
                var source = P.Source(values);
                Derived<int> end = null;
                using var root = P.Root(() =>
                {
                    var middle = P.Derive(() => source.Value[0] * 2);
                    end = P.Derive(() => middle.Value * 2);
                });
                P.Batch(() =>
                {
                    values[0] = 2;
                    source.Notify();
                    Equal(8, end.Value);
                });
            }
        );
        Run(
            "diamond graph settles once per batch",
            () =>
            {
                var source = P.Source(1);
                int displayed = 0,
                    runs = 0;
                using var root = P.Root(() =>
                {
                    var left = P.Derive(() => source.Value * 2);
                    var right = P.Derive(() => source.Value * 3);
                    var total = P.Derive(() => left.Value + right.Value);
                    P.Effect(() =>
                    {
                        displayed = total.Value;
                        runs++;
                    });
                });
                P.Batch(() =>
                {
                    source.Value = 2;
                    source.Value = 3;
                });
                Equal(15, displayed);
                Equal(2, runs);
            }
        );
        foreach (double delay in new[] { 0d, 0.1d })
        {
            Run(
                $"failed branch cleanup publishes removal (delay {delay})",
                () =>
                {
                    var visible = P.Source(true);
                    ReadOnly<IReadOnlyList<string>> rows = null;
                    using var root = P.Root(() =>
                        rows = P.Show(
                            () => visible.Value,
                            present =>
                            {
                                P.Cleanup(() => throw new InvalidOperationException("exit"));
                                return new Branch<string>("row", delay);
                            }
                        )
                    );
                    if (delay == 0)
                        Throws(() => visible.Value = false);
                    else
                    {
                        visible.Value = false;
                        Throws(() => P.Step(0.2));
                    }
                    Equal(0, rows.Value.Count);
                    visible.Value = true;
                    Equal(1, rows.Value.Count);
                    // Remove the second row too so test disposal does not hide its assertions.
                    if (delay == 0)
                        Throws(() => visible.Value = false);
                    else
                    {
                        visible.Value = false;
                        Throws(() => P.Step(0.2));
                    }
                }
            );
        }
        Run(
            "immediate removal continues after multiple cleanup failures",
            () =>
            {
                var items = P.Source(new[] { 1, 2 });
                ReadOnly<IReadOnlyList<int>> rows = null;
                using var root = P.Root(() =>
                    rows = P.Values<int, int>(
                        () => items.Value,
                        (value, index, present) =>
                        {
                            P.Cleanup(() => throw new InvalidOperationException("row " + value));
                            return value;
                        }
                    )
                );
                Equal(
                    2,
                    Throws(() => items.Value = Array.Empty<int>()).Flatten().InnerExceptions.Count
                );
                Equal(0, rows.Value.Count);
            }
        );
        Run(
            "simultaneous delayed exits complete despite cleanup failures",
            () =>
            {
                var items = P.Source(new[] { 1, 2 });
                ReadOnly<IReadOnlyList<int>> rows = null;
                using var root = P.Root(() =>
                    rows = P.Values<int, int>(
                        () => items.Value,
                        (value, index, present) =>
                        {
                            P.Cleanup(() => throw new InvalidOperationException("row " + value));
                            return new Branch<int>(value, 0.1);
                        }
                    )
                );
                items.Value = Array.Empty<int>();
                Equal(2, Throws(() => P.Step(0.2)).Flatten().InnerExceptions.Count);
                Equal(0, rows.Value.Count);
            }
        );
        Run(
            "delayed exit reentry retains the branch",
            () =>
            {
                var visible = P.Source(true);
                ReadOnly<IReadOnlyList<string>> rows = null;
                int builds = 0,
                    disposed = 0;
                using var root = P.Root(() =>
                    rows = P.Show(
                        () => visible.Value,
                        present =>
                        {
                            builds++;
                            P.Cleanup(() => disposed++);
                            return new Branch<string>("row", 0.1);
                        }
                    )
                );
                visible.Value = false;
                P.Step(0.05);
                visible.Value = true;
                P.Step(0.2);
                Equal(1, builds);
                Equal(0, disposed);
                Equal(1, rows.Value.Count);
                visible.Value = false;
                P.Step(0.2);
                Equal(1, disposed);
                Equal(0, rows.Value.Count);
            }
        );
        Run(
            "scope disposal attempts all cleanup in reverse order",
            () =>
            {
                var calls = new List<int>();
                var root = P.Root(() =>
                {
                    P.Cleanup(() => calls.Add(1));
                    P.Cleanup(() =>
                    {
                        calls.Add(2);
                        throw new InvalidOperationException("cleanup");
                    });
                    P.Cleanup(() => calls.Add(3));
                });
                Throws(root.Dispose);
                Equal("3,2,1", string.Join(",", calls));
                root.Dispose();
                Equal(3, calls.Count);
            }
        );
        Run(
            "spring and delayed exits share the clock",
            () =>
            {
                var target = P.Source(0d);
                Spring<double> spring = null;
                using var root = P.Root(() => spring = P.Spring(() => target.Value, period: 0.5));
                target.Value = 10;
                P.Step(0.1);
                if (spring.Value <= 0 || spring.Value >= 10)
                    throw new Exception("Spring did not advance smoothly.");
                P.Step(5);
                Equal(10d, spring.Value);
            }
        );
        Run(
            "derived writes and feedback loops still fail",
            () =>
            {
                var source = P.Source(0);
                using var root = P.Root(() => { });
                Throws(() => root.Run(() => P.Derive(() => source.Value = 1)));
                Throws(() => root.Run(() => P.Effect(() => source.Value++)));
            }
        );
        Console.WriteLine($"{_passed} passed; {_failed} failed.");
        return _failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine("PASS " + name);
        }
        catch (Exception error)
        {
            _failed++;
            Console.Error.WriteLine("FAIL " + name + ": " + error);
        }
        finally
        {
            Clock.Reset();
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected}, got {actual}.");
    }

    private static AggregateException Throws(Action action)
    {
        try
        {
            action();
        }
        catch (AggregateException error)
        {
            return error;
        }
        catch (Exception error)
        {
            return new AggregateException(error);
        }
        throw new Exception("Expected an exception.");
    }
}
