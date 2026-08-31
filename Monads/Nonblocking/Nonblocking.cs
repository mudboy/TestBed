using System.Collections.Immutable;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Monads.Nonblocking;

// converted from
// https://github.com/fpinscala/fpinscala/blob/second-edition/src/main/scala/fpinscala/answers/parallelism/Nonblocking.scala
//
// Scala's `object Nonblocking:` scopes the shadowing Future/Par names; the C# analogue
// is this child namespace, so Monads.Par (blocking) and Monads.Nonblocking.Par coexist.
//
// Scala: opaque type Future[+A] = (A => Unit) => Unit
// The whole point of the chapter: a "future" here is not a handle you wait on, it is a
// function you hand a callback to. Nothing blocks except `Run`.
public delegate void Future<out A>(Action<A> cb);

// Scala: opaque type Par[+A] = ExecutorService => Future[A]
public delegate Future<A> Par<out A>(ExecutorService es);

public static class Par
{
    extension<A>(Par<A> p)
    {
        // The one place that blocks, and only because it has to hand a value back to
        // ordinary imperative code.
        public A Run(ExecutorService es)
        {
            var result = new Box<A>();                  // stands in for AtomicReference
            using var latch = new CountdownEvent(1); // stands in for CountDownLatch(1)
            p(es)(a =>
            {
                result.Value = a;
                latch.Signal();
            });
            latch.Wait(); // Signal/Wait carries the happens-before that publishes Value
            return result.Value!;
        }

        public Par<C> Map2<B, C>(Par<B> p2, Func<A, B, C> f) =>
            es => cb =>
            {
                Option<A> ar = Option.None;
                Option<B> br = Option.None;
                // A little too liberal in forking threads: one logical thread for the
                // actor, and another because `cb` is scheduled rather than called. The
                // actor serialises access to ar/br, which is why plain locals are safe.
                var combiner = new Actor<Either<A, B>>(es, msg => msg.Match(
                    left: a => br.Match(
                        some: b => Eval(es, () => cb(f(a, b))),
                        none: () => ar = a),
                    right: b => ar.Match(
                        some: a => Eval(es, () => cb(f(a, b))),
                        none: () => br = b)));

                p(es)(a => combiner.Tell(Either.Left<A, B>(a)));
                p2(es)(b => combiner.Tell(Either.Right<A, B>(b)));
            };

        public Par<B> Map<B>(Func<A, B> f) =>
            es => cb => p(es)(a => Eval(es, () => cb(f(a))));

        public Par<B> FlatMap<B>(Func<A, Par<B>> f)
        {
            // Fork isn't strictly necessary but lets us avoid stack overflows when
            // chaining lots of FlatMap calls.
            //
            // Worth being precise about what that buys: Fork defers the *descent*, so
            // monadic recursion (p.FlatMap(i => loop(i - 1))) is stack safe to any depth.
            // The *ascent* is not — a left-nested chain, p.FlatMap(f).FlatMap(f)...,
            // returns through every continuation synchronously, because Unit invokes its
            // callback directly. 50k of those overflow. The Scala has the same shape and
            // the same limit; adding Eval(es, () => f(a)(es)(cb)) below would trampoline
            // the ascent too, at the cost of a scheduled task per bind.
            Par<B> inner = es => cb => p(es)(a => f(a)(es)(cb));
            return Fork(() => inner);
        }

        public Par<(A, B)> Zip<B>(Par<B> b) => p.Map2(b, (a, bb) => (a, bb));

        // `Chooser` is usually called `flatMap` or `bind`.
        public Par<B> Chooser<B>(Func<A, Par<B>> f) => p.FlatMap(f);

        public Par<B> FlatMapViaJoin<B>(Func<A, Par<B>> f) => Join(p.Map(f));

        // Not in the Scala original: LINQ names, so query syntax stands in for a
        // for-comprehension.
        public Par<B> Select<B>(Func<A, B> f) => p.Map(f);

        public Par<C> SelectMany<B, C>(Func<A, Par<B>> bind, Func<A, B, C> project) =>
            p.FlatMap(a => bind(a).Map(b => project(a, b)));
    }

    // Stands in for AtomicReference; extension blocks can't declare nested types, so it
    // lives out here.
    private sealed class Box<T>
    {
        public T? Value;
    }

    public static Par<A> Unit<A>(A a) => _ => cb => cb(a);

    /// <summary>A non-strict version of <see cref="Unit{A}"/>.</summary>
    public static Par<A> Delay<A>(Func<A> a) => _ => cb => cb(a());

    // Scala: def fork[A](a: => Par[A]). As in the blocking version, by-name becomes
    // Func<Par<A>> and callers write Fork(() => ...).
    public static Par<A> Fork<A>(Func<Par<A>> a) =>
        es => cb => Eval(es, () => a()(es)(cb));

    /// <summary>
    /// Constructs a Par from a non-blocking continuation-passing-style API. Handy in
    /// chapter 13 — and the seam where a .NET Task would be adapted in.
    /// </summary>
    public static Par<A> Async<A>(Action<Action<A>> f) => _ => cb => f(cb);

    /// <summary>Evaluates an action asynchronously on the given executor.</summary>
    public static void Eval(ExecutorService es, Action r) => es.Execute(r);

    public static Par<A> LazyUnit<A>(Func<A> a) => Fork(() => Unit(a()));

    public static Func<A, Par<B>> AsyncF<A, B>(Func<A, B> f) => a => LazyUnit(() => f(a));

    // Note the recursive call is Sequence, not SequenceRight — that is what the Scala
    // answer does, kept as-is.
    public static Par<ImmutableList<A>> SequenceRight<A>(ImmutableList<Par<A>> pas) =>
        pas.IsEmpty
            ? Unit(ImmutableList<A>.Empty)
            : pas[0].Map2(Fork(() => Sequence(pas.RemoveAt(0))), (a, t) => t.Insert(0, a));

    // Unlike the blocking version, this one forks, so the halves really do run in
    // parallel rather than the left completing before the right is submitted.
    public static Par<ImmutableArray<A>> SequenceBalanced<A>(ImmutableArray<Par<A>> pas) =>
        Fork(() => pas switch
        {
            [] => Unit(ImmutableArray<A>.Empty),
            [var single] => single.Map(a => ImmutableArray.Create(a)),
            _ => SequenceBalanced(pas[..(pas.Length / 2)])
                .Map2(SequenceBalanced(pas[(pas.Length / 2)..]), (l, r) => l.AddRange(r))
        });

    public static Par<ImmutableList<A>> Sequence<A>(ImmutableList<Par<A>> pas) =>
        SequenceBalanced([..pas]).Map(arr => arr.ToImmutableList());

    public static Par<ImmutableList<B>> ParMap<A, B>(ImmutableList<A> pas, Func<A, B> f) =>
        Sequence(pas.Select(AsyncF(f)).ToImmutableList());

    public static Par<ImmutableArray<B>> ParMap<A, B>(ImmutableArray<A> pas, Func<A, B> f) =>
        SequenceBalanced([..pas.Select(AsyncF(f))]);

    // exercise answers

    /*
     * `p(es)(result => ...)` is the idiom for running `p` and registering a callback to
     * be invoked when its result is available. If this is hard to follow, write down the
     * type of each subexpression: what is the type of `p(es)`? Of `t(es)`? Of `t(es)(cb)`?
     */
    public static Par<A> Choice<A>(Par<bool> cond, Par<A> t, Par<A> f) =>
        es => cb => cond(es)(b =>
        {
            if (b) Eval(es, () => t(es)(cb));
            else Eval(es, () => f(es)(cb));
        });

    /* The code here is very similar. */
    public static Par<A> ChoiceN<A>(Par<int> p, ImmutableList<Par<A>> ps) =>
        es => cb => p(es)(ind => Eval(es, () => ps[ind % ps.Count](es)(cb)));

    public static Par<A> ChoiceViaChoiceN<A>(Par<bool> cond, Par<A> t, Par<A> f) =>
        ChoiceN(cond.Map(b => b ? 0 : 1), [t, f]);

    public static Par<V> ChoiceMap<K, V>(Par<K> key, ImmutableDictionary<K, Par<V>> choices)
        where K : notnull =>
        es => cb => key(es)(k => choices[k](es)(cb));

    public static Par<A> ChoiceViaFlatMap<A>(Par<bool> p, Par<A> f, Par<A> t) =>
        p.FlatMap(b => b ? t : f);

    public static Par<A> ChoiceNViaFlatMap<A>(Par<int> p, ImmutableList<Par<A>> choices) =>
        p.FlatMap(i => choices[i]);

    public static Par<A> Join<A>(Par<Par<A>> ppa) =>
        es => cb => ppa(es)(pa => Eval(es, () => pa(es)(cb)));

    public static Par<A> JoinViaFlatMap<A>(Par<Par<A>> ppa) => ppa.FlatMap(pa => pa);
}
