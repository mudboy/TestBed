using System.Collections.Immutable;
using System.Diagnostics;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Monads;

// converted from
// https://github.com/fpinscala/fpinscala/blob/second-edition/src/main/scala/fpinscala/answers/parallelism/Par.scala
//
// Scala: opaque type Par[A] = ExecutorService => Future[A]
// C#   : a delegate gives us a distinct nominal type over the same function shape.
//        It is not *opaque* (anyone can build one from a lambda), but combined with
//        the extension block below it reads the same at every call site.
public delegate IFuture<A> Par<A>(ExecutorService es);

public static class Par
{
    // Scala: extension [A](pa: Par[A]) def run(s: ExecutorService): Future[A] = pa(s)
    extension<A>(Par<A> pa)
    {
        public IFuture<A> Run(ExecutorService s) => pa(s);

        // `Map2` doesn't evaluate the call to `f` in a separate logical thread, in accord
        // with our design choice of having `Fork` be the sole function in the API for
        // controlling parallelism. We can always do `Fork(() => a.Map2(b, f))` if we want
        // the evaluation of `f` to occur in a separate thread.
        public Par<C> Map2<B, C>(Par<B> pb, Func<A, B, C> f) =>
            es =>
            {
                var af = pa(es);
                var bf = pb(es);
                // Does _not_ respect timeouts: it waits on both futures and wraps the
                // result in a UnitFuture. See Map2Timeouts for the version that does.
                return new UnitFuture<C>(f(af.Get(), bf.Get()));
            };

        public Par<C> Map2Timeouts<B, C>(Par<B> pb, Func<A, B, C> f) =>
            es => new Map2Future<A, B, C>(pa(es), pb(es), f);

        public Par<B> Map<B>(Func<A, B> f) =>
            pa.Map2(Unit(Monads.Unit.Value), (a, _) => f(a));

        public Par<B> Chooser<B>(Func<A, Par<B>> choices) =>
            es => choices(pa.Run(es).Get()).Run(es);

        // `Chooser` is usually called `flatMap` or `bind`.
        public Par<B> FlatMap<B>(Func<A, Par<B>> choices) =>
            es => choices(pa.Run(es).Get()).Run(es);

        public Par<B> FlatMapViaJoin<B>(Func<A, Par<B>> f) => Join(pa.Map(f));

        // Not in the Scala original: the LINQ names, so that
        // `from a in pa from b in pb select ...` works. The C# analogue of a
        // for-comprehension, which Scala gets for free from map/flatMap.
        public Par<B> Select<B>(Func<A, B> f) => pa.Map(f);

        public Par<C> SelectMany<B, C>(Func<A, Par<B>> bind, Func<A, B, C> project) =>
            pa.FlatMap(a => bind(a).Map(b => project(a, b)));
    }

    // `Unit` is a function that returns a `UnitFuture`, a simple implementation of
    // IFuture that just wraps a constant value. It doesn't use the ExecutorService at
    // all. It's always done and can't be cancelled.
    public static Par<A> Unit<A>(A a) => _ => new UnitFuture<A>(a);

    private sealed record UnitFuture<A>(A Value) : IFuture<A>
    {
        public bool IsDone => true;
        public bool IsCancelled => false;
        public A Get() => Value;
        public A Get(TimeSpan timeout) => Value;
        public bool Cancel(bool evenIfRunning) => false;
    }

    // Scala's `new Future[C] { ... }` anonymous class has no C# equivalent, so the
    // inline instance becomes a named private type.
    private sealed class Map2Future<A, B, C>(IFuture<A> futureA, IFuture<B> futureB, Func<A, B, C> f)
        : IFuture<C>
    {
        // Scala: @volatile private var cache: Option[C] = None
        // C# can't mark a generic or struct-typed field `volatile`, so the cache lives in
        // a reference-typed holder, which volatile does allow.
        private sealed record Cached(C Value);

        private volatile Cached? cache;

        public bool IsDone => cache is not null;

        public C Get() => Get(Timeout.InfiniteTimeSpan);

        public C Get(TimeSpan timeout)
        {
            var started = Stopwatch.GetTimestamp();
            var a = futureA.Get(timeout);
            var elapsed = Stopwatch.GetElapsedTime(started);
            var b = futureB.Get(timeout == Timeout.InfiniteTimeSpan ? timeout : timeout - elapsed);
            var c = f(a, b);
            cache = new Cached(c);
            return c;
        }

        public bool IsCancelled => futureA.IsCancelled || futureB.IsCancelled;

        public bool Cancel(bool evenIfRunning) =>
            futureA.Cancel(evenIfRunning) || futureB.Cancel(evenIfRunning);
    }

    // Scala: def fork[A](a: => Par[A]): Par[A]
    // C# has no by-name parameters, so laziness is spelled Func<Par<A>> and callers must
    // write Fork(() => ...). This is the one place the port is genuinely noisier.
    //
    // This is the simplest and most natural implementation of `Fork`, but the outer
    // callable blocks waiting for the inner task to complete. Since that blocking
    // occupies a thread in the pool, we're using two threads where one should suffice.
    public static Par<A> Fork<A>(Func<Par<A>> a) =>
        es => es.Submit(() => a()(es).Get());

    public static Par<A> LazyUnit<A>(Func<A> a) => Fork(() => Unit(a()));

    public static Func<A, Par<B>> AsyncF<A, B>(Func<A, B> f) => a => LazyUnit(() => f(a));

    public static Par<ImmutableList<int>> SortPar(Par<ImmutableList<int>> parList) =>
        parList.Map(l => l.Sort());

    public static Par<ImmutableList<A>> SequenceSimple<A>(ImmutableList<Par<A>> pas) =>
        pas.Reverse().Aggregate(
            Unit(ImmutableList<A>.Empty),
            (acc, pa) => pa.Map2(acc, (a, tail) => tail.Insert(0, a)));

    // Forks the recursive step off to a new logical thread, making it effectively
    // tail-recursive. But it builds a right-nested parallel program; SequenceBalanced
    // below gets better performance by halving the sequence.
    // No `case h :: t` here: ImmutableList has no Slice, so it can't drive a C# list
    // pattern, and it isn't a cons list anyway.
    public static Par<ImmutableList<A>> SequenceRight<A>(ImmutableList<Par<A>> pas) =>
        pas.IsEmpty
            ? Unit(ImmutableList<A>.Empty)
            : pas[0].Map2(Fork(() => SequenceRight(pas.RemoveAt(0))), (a, tail) => tail.Insert(0, a));

    // Scala uses IndexedSeq for its efficient splitAt; ImmutableArray is the C# analogue.
    public static Par<ImmutableArray<A>> SequenceBalanced<A>(ImmutableArray<Par<A>> pas) =>
        pas switch
        {
            [] => Unit(ImmutableArray<A>.Empty),
            [var single] => single.Map(ImmutableArray.Create),
            _ => SequenceBalanced(pas[..(pas.Length / 2)])
                .Map2(SequenceBalanced(pas[(pas.Length / 2)..]), (l, r) => l.AddRange(r))
        };

    public static Par<ImmutableList<A>> Sequence<A>(ImmutableList<Par<A>> pas) =>
        SequenceBalanced([..pas]).Map(arr => arr.ToImmutableList());

    public static Par<ImmutableList<B>> ParMap<A, B>(ImmutableList<A> ps, Func<A, B> f) =>
        Fork(() =>
        {
            var fbs = ps.Select(AsyncF(f)).ToImmutableList();
            return Sequence(fbs);
        });

    public static Par<ImmutableList<A>> ParFilter<A>(ImmutableList<A> l, Func<A, bool> f) =>
        Fork(() =>
        {
            var pars = l
                .Select(AsyncF<A, ImmutableList<A>>(a => f(a) ? [a] : []))
                .ToImmutableList();
            return Sequence(pars).Map(ls => ls.SelectMany(x => x).ToImmutableList());
        });

    // Scala's `==` is structural, so the faithful C# equivalent is the default equality
    // comparer, not reference `==`.
    public static bool Equal<A>(ExecutorService e, Par<A> p, Par<A> p2) =>
        EqualityComparer<A>.Default.Equals(p(e).Get(), p2(e).Get());

    public static Par<A> Delay<A>(Func<Par<A>> fa) => es => fa()(es);

    public static Par<A> Choice<A>(Par<bool> cond, Par<A> t, Par<A> f) =>
        es => cond.Run(es).Get() ? t(es) : f(es); // Notice we are blocking on `cond`.

    public static Par<A> ChoiceN<A>(Par<int> n, ImmutableList<Par<A>> choices) =>
        es =>
        {
            var ind = n.Run(es).Get() % choices.Count;
            return choices[ind].Run(es);
        };

    public static Par<A> ChoiceViaChoiceN<A>(Par<bool> cond, Par<A> t, Par<A> f) =>
        ChoiceN(cond.Map(b => b ? 0 : 1), [t, f]);

    public static Par<V> ChoiceMap<K, V>(Par<K> key, ImmutableDictionary<K, Par<V>> choices)
        where K : notnull =>
        es => choices[key.Run(es).Get()].Run(es);

    public static Par<A> ChoiceViaFlatMap<A>(Par<bool> p, Par<A> f, Par<A> t) =>
        p.FlatMap(b => b ? t : f);

    public static Par<A> ChoiceNViaFlatMap<A>(Par<int> p, ImmutableList<Par<A>> choices) =>
        p.FlatMap(i => choices[i]);

    // see nonblocking implementation in `Nonblocking.scala`
    public static Par<A> Join<A>(Par<Par<A>> ppa) =>
        es => ppa.Run(es).Get().Run(es);

    public static Par<A> JoinViaFlatMap<A>(Par<Par<A>> ppa) => ppa.FlatMap(pa => pa);
}

public static class ParExamples
{
    // Scala uses IndexedSeq for its efficient splitAt; ReadOnlySpan is the zero-copy C#
    // equivalent (it can't be captured in a lambda, but plain recursion is fine).
    public static int Sum(ReadOnlySpan<int> ints)
    {
        if (ints.Length <= 1) return ints.IsEmpty ? 0 : ints[0];
        var half = ints.Length / 2;
        return Sum(ints[..half]) + Sum(ints[half..]);
    }
}
