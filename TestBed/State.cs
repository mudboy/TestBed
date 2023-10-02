namespace TestBed;


public delegate (A, S) State<S, A>(S s);

public static class State
{
    public static (A, S) Run<S, A>(this State<S, A> underlying, S s) => underlying(s);

    public static State<S, T> Unit<S, T>(T a) => s => (a, s);

    public static State<S, B> Select<S, A, B>(this State<S, A> underlying, Func<A, B> f) =>
        underlying.SelectMany(a => Unit<S, B>(f(a)));
    
    public static State<S, B> SelectMany<S, A, B>(this State<S, A> underlying, Func<A, State<S, B>> f) =>
        s => {
            var (a, s1) = underlying(s);
            return f(a)(s1);
        };

    public static State<S, C> SelectMany<S, A, B, C>(this State<S, A> underlying, Func<A, State<S, B>> f,
        Func<A, B, C> project) =>
        underlying.SelectMany(a => f(a).Select(b => project(a, b)));

    public static State<S, C> BiMap<S, A, B, C>(this State<S, A> underlying, State<S, B> sb, Func<A, B, C> f) =>
        from a in underlying
        from b in sb
        select f(a, b);

    public static State<S, A> Apply<S, A>(State<S, A> f) =>  f;

    private static IEnumerable<A> Nil<A>() => Enumerable.Empty<A>();

    public static State<S, IEnumerable<A>> Sequence<S, A>(IEnumerable<State<S, A>> actions) =>
        actions.Aggregate(Unit<S, IEnumerable<A>>(Nil<A>()), (acc, f) => f.BiMap(acc, (a, xs) => xs.Append(a)));

}