namespace TestBed;

public delegate Task<A> Par<A>(TaskScheduler ts);

//public record struct Par<A>(A a);

public static class Par
{
    public static Task<A> Run<A>(this Par<A> pa, TaskScheduler? ts = null) => pa(ts ?? TaskScheduler.Default);

    public static Par<A> Unit<A>(A a) => _ => Task.FromResult(a);

    public static Par<C> Map2<A, B, C>(this Par<A> a, Par<B> b, Func<A, B, C> f) =>
        ts =>
        {
            var af = a(ts);
            var bf = b(ts);
            C c = f(af.Result, bf.Result);
            return Task.FromResult(c);
        };

    public static Par<A> Fork<A>(Par<A> a) =>
        ts =>
        {
            var t = new Task<A>(() => a(ts).Result);
            t.Start(ts);
            return t;
        };

    public static Par<B> Map<A, B>(this Par<A> a, Func<A, B> f)
    {
        return a.Map2(Unit(0), (a, _) => f(a));
    }

    // public static Par<A> Delay<A>(Par<A> a)
    // {
    //     return async sh => await a(sh);
    // }
}

public static partial class Main
{
    public static void ParExamples()
    {
        Par.Fork(Par.Unit(1));
    }
}