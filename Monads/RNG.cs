using System.Diagnostics;
using System.Security.Cryptography;

namespace Monads;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

// converted from
// https://github.com/fpinscala/fpinscala/blob/second-edition/src/main/scala/fpinscala/answers/state/State.scala

public interface IRng
{
    (int, IRng) NextInt();
}

public static class Rng
{
    private sealed record SimpleImpl(long Seed) : IRng
    {
        public (int, IRng) NextInt()
        {
            var newSeed = (Seed * 0x5DEECE66DL + 0xBL) & 0xFFFFFFFFFFFFL;
            var nextRng = new SimpleImpl(newSeed);
            var v = (newSeed >>> 16);
            var n = (int)v;
            return (n, nextRng);
        }
    }

    private sealed record PseudoImpl(int seed) : IRng
    {
        public int seed { get; init; } = seed;
        private readonly Random _rng = new(seed);
        public (int, IRng) NextInt() => (_rng.Next(), this);
    }
    
    private sealed record SecureImpl : IRng
    {
        public (int, IRng) NextInt()
        {
            var rg = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
            return (rg, this);
        }
    }

    private sealed record ReallyDumbImpl : IRng
    {
        private int _next = 1;
        public (int, IRng) NextInt() => (_next++, this);
    }

    public static IRng Simple(long seed) => new SimpleImpl(seed);
    public static IRng Default() => new SimpleImpl(Stopwatch.GetTimestamp());
    public static IRng Pseudo(int seed) => new PseudoImpl(seed);
    public static IRng Secure() => new SecureImpl();
    public static IRng ReallyDumb() => new ReallyDumbImpl();
    
    
    public static (int, IRng) NonNegativeInt(this IRng rng)
    {
        var (i, r) = rng.NextInt();
        return (i < 0 ? -(i + 1) : i, r);
    }    
    
    public static (int, IRng) NaturalNumber(this IRng rng)
    {
        var (i, r) = rng.NonNegativeInt();
        return (1 + i % (int.MaxValue - 1), r);
    }
    
    public static (B, IRng) Select<A, B>(this (A value, IRng next) rng, Func<A,B> f) =>
            (f(rng.value), rng.next);
    
    public static (int, IRng) Int(this IRng rng) => 
        rng.NextInt();
    
    public static (double, IRng) Double(this IRng rng)
    {
        var (i, r) = rng.NonNegativeInt();
        return (i * (1.0 / int.MaxValue), r);
    }

    public static (bool, IRng) Bool(this IRng rng) =>
        rng.NextInt() switch
        {
            var (i, r2) => (i % 2 == 0, r2)
        };
}

public delegate (A Value, IRng Rng) Rand<A>(IRng rng);

public static class Rand
{
    public static Rand<A> Unit<A>(A a) => r => (a, r);

    public static Rand<int> Int => Rng.Int;
    public static Rand<int> NonNegativeInt => Rng.NonNegativeInt;
    public static Rand<int> NaturalNumber => Rng.NaturalNumber;
    public static Rand<double> Double => Rng.Double;
    public static Rand<bool> Bool => Rng.Bool;

    extension<A>(Rand<A> underlying)
    {
        public (A Value, IRng Rng) Run(IRng rng) => underlying(rng);

        public A Eval(IRng rng) => underlying(rng).Value;

        public IRng Exec(IRng rng) => underlying(rng).Rng;

        public Rand<B> Select<B>(Func<A, B> f) =>
            underlying.SelectMany(a => Unit(f(a)));

        public Rand<B> SelectMany<B>(Func<A, Rand<B>> f) =>
            r =>
            {
                var (a, r1) = underlying(r);
                return f(a)(r1);
            };

        public Rand<C> SelectMany<B, C>(Func<A, Rand<B>> f, Func<A, B, C> project) =>
            underlying.SelectMany(a => f(a).Select(b => project(a, b)));

        public Rand<C> Map2<B, C>(Rand<B> rb, Func<A, B, C> f) =>
            from a in underlying
            from b in rb
            select f(a, b);
    }

    public static Rand<IEnumerable<A>> Sequence<A>(this IEnumerable<Rand<A>> actions) =>
        actions.Aggregate(Unit(Enumerable.Empty<A>()),
            (acc, a) => acc.Map2(a, (xs, x) => xs.Append(x)));

    public static Rand<IEnumerable<B>> Traverse<A, B>(this IEnumerable<A> input, Func<A, Rand<B>> f) =>
        input.Select(f).Sequence();

    /// <summary>Runs the same action the given number of times, collecting each result.</summary>
    public static Rand<IReadOnlyList<A>> Repeat<A>(this Rand<A> action, int times) =>
        Enumerable.Repeat(action, times)
            .Sequence()
            .Select(results => (IReadOnlyList<A>)results.ToList());
}
