using System.Diagnostics;
using System.Security.Cryptography;

namespace TestBed;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

// converted from
// https://github.com/fpinscala/fpinscala/blob/second-edition/src/main/scala/fpinscala/answers/state/State.scala

public interface IRng
{
    (IRng, int) NextInt();
}

public static class Rng
{
    private sealed record SimpleImpl(long Seed) : IRng
    {
        public (IRng, int) NextInt()
        {
            var newSeed = (Seed * 0x5DEECE66DL + 0xBL) & 0xFFFFFFFFFFFFL;
            var nextRng = new SimpleImpl(newSeed);
            var v = (newSeed >>> 16);
            var n = (int)v;
            return (nextRng, n);
        }
    }

    private sealed record PseudoImpl(int seed) : IRng
    {
        private readonly Random _rng = new(seed);
        public (IRng, int) NextInt() => (this, _rng.Next());
    }
    
    private sealed record SecureImpl : IRng
    {
        public (IRng, int) NextInt()
        {
            var rg = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
            return (this, rg);
        }
    }

    private sealed record ReallyDumbImpl : IRng
    {
        private int _next = 1;
        public (IRng, int) NextInt() => (this, _next++);
    }

    public static IRng Simple(long seed) => new SimpleImpl(seed);
    public static IRng Default() => new SimpleImpl(Stopwatch.GetTimestamp());
    public static IRng Pseudo(int seed) => new PseudoImpl(seed);
    public static IRng Secure() => new SecureImpl();
    public static IRng ReallyDumb() => new ReallyDumbImpl();
    
    
    public static (IRng, int) NonNegativeInt(this IRng rng)
    {
        var (r, i) = rng.NextInt();
        return (r, i < 0 ? -(i + 1) : i);
    }    
    
    public static (IRng, int) NaturalNumber(this IRng rng)
    {
        var (r, i) = rng.NonNegativeInt();
        return (r, 1 + i % (int.MaxValue - 1));
    }
    
    public static (B, IRng) Select<A, B>(this (A value, IRng next) rng, Func<A,B> f) =>
            (f(rng.value), rng.next);
    
    public static (IRng, int) Int(this IRng rng) => 
        rng.NextInt();
    
    public static (IRng, double) Double(this IRng rng)
    {
        var (r,i) = rng.NonNegativeInt();
        return (r, i * (1.0 / int.MaxValue));
    }

    public static (IRng, bool) Bool(this IRng rng) =>
        rng.NextInt() switch
        {
            var (r2, i) => (r2, i % 2 == 0)
        };
}

public delegate (A, IRng) Rand<A>(IRng rng);

public static class Rand
{
    public static Rand<A> Unit<A>(A a) => r => (a, r);
}