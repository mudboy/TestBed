namespace TestBed.HigherKinds;

public interface Mappable<F>
    where F : Mappable<F>
{
    public static abstract K<F, B> Select<A, B>(K<F, A> list, Func<A, B> f);
}

public static class MappableExtensions
{
    public static K<F, B> Select<F, A, B>(this K<F, A> fa, Func<A, B> f)
        where F : Mappable<F> =>
        F.Select(fa, f);
}