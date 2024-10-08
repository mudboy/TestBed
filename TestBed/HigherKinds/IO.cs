namespace TestBed.HigherKinds;

public sealed record IO<A>(Func<A> runIO) : K<IO, A>
{
    public IO<B> Map<B>(Func<A,B> f)
    {
        return new IO<B>(() => f(runIO()));
    }

    public IO<S> Fold<S>(S initial, Func<S, A, S> f)
    {
        return new IO<S>(() =>
        {
            var a = runIO();
            return f(initial, a);
        });
    }
}

public class IO : Functor<IO>
{
    public static K<IO, B> Map<A, B>(Func<A, B> f, K<IO, A> ma)
    {
        return new IO<B>(() => f(ma.Run()));
    }
}

public static class IOExtensions
{
    public static IO<A> As<A>(this K<IO, A> ma) =>
        (IO<A>)ma;

    public static A Run<A>(this K<IO, A> ma) =>
        ma.As().runIO();
}

public static class DateTimeIO
{
    public static readonly IO<DateTime> Now =
        new (() => DateTime.Now);
}