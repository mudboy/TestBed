namespace TestBed;

public delegate T Io<out T>();

public static class IoExt
{
    public static Io<T> Pure<T>(T item) => 
        () => item;

    public static Io<T> ToIo<T>(this T item) => 
        () => item;

    public static Io<R> Select<T, R>(this Io<T> source, Func<T, R> f) => 
        () => f(source());

    public static Io<R> SelectMany<T, R>(this Io<T> source, Func<T, Io<R>> f) => 
        () => f(source())();

    public static Io<C> SelectMany<A, B, C>(this Io<A> source, Func<A, Io<B>> k, Func<A, B, C> s) => 
        () => source.SelectMany(a => k(a).Select(b => s(a, b)))();
}