namespace TestBed;

public interface IRng
{
    (int, IRng) NextInt();
}

public delegate (A, IRng) Rand<A>(IRng r);

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

    public static IRng Simple(long seed) => new SimpleImpl(seed);
    
    public static (int, IRng) NonNegativeInt(this IRng rng)
    {
        var (i, r) = rng.NextInt();
        return (i < 0 ? -(i + 1) : i, r);
    }
    
    public static (B, IRng) Select<A, B>(this (A, IRng) rng, Func<A,B> f) =>
            (f(rng.Item1), rng.Item2);
    
    public static (double, IRng) Double(this IRng rng)
    {
        var (i, r) = rng.NonNegativeInt();
        return (i * (1.0 / int.MaxValue), r);
    }

    public static (bool, IRng) Bool(this IRng rng)
    {
        var (i, r) = rng.NextInt();
        return (i % 2 == 0, r);
    }
    
    public static Rand<B> Select<A, B>(this Rand<A> s, Func<A, B> f) =>
        rng =>
        {
            var (a, r) = s(rng);
            return (f(a), r);
        };
}