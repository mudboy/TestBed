namespace TestBed;

public delegate Task<A> Par<A>();

public static class Par
{
    public static void Run<A>(Par<A> p, TaskScheduler scheduler)
    {
        var task = p();
        task.Start();
    }

    public static Par<A> Unit<A>(A a) => () => Task.FromResult(a);

    /*
    public static Par<A> Fork<A>(Func<Par<A>> a)
    {
    }
    */

    public static Par<C> Map2<A, B, C>(Par<A> a, Par<B> b, Func<A, B, C> f) =>
        async () =>
        {
            var af = await a();
            var bf = await b();
            return f(af, bf);
        };

    public static Par<A> Fork<A>(Par<A> a) =>
        () =>
        {

            return Task.Run(() => a().Result);
        };

    // public static Par<A> Delay<A>(Par<A> a)
    // {
    //     return async sh => await a(sh);
    // }
}