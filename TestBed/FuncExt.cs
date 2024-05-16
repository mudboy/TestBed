using System.Diagnostics.Contracts;

namespace TestBed;

public static class FuncExt
{
    public static Func<A, A> Compose<A>(params Func<A, A>[] fs) =>
        fs.Aggregate((g, f) => x => f(g(x)));
    
    [Pure]
    public static B Apply<A, B>(this A a, Func<A, B> f) =>
        f(a);
    
    public static Func<B, Func<A, C>> CurryR<A, B, C>(this Func<A, B, C> f) =>
        x => y => f(y, x);

    
    public static Func<T1, Func<T2, R>> Curry<T1, T2, R>(this Func<T1, T2, R> f) =>
        x => y => f(x, y);    
    public static Func<T1, Func<T2, T3, R>> Curry<T1, T2, T3, R>(this Func<T1, T2, T3, R> f) =>
        x => (y, z) => f(x, y, z);
    
    public static Func<T1, Func<T2, T3, R>> CurryFirst<T1, T2, T3, R>
        (this Func<T1, T2, T3, R> @this) => t1 => (t2, t3) => @this(t1, t2, t3);
}

public static partial class Main
{
    public static void Examples()
    {
        Func<string, string> exclaim = x => $"{x}!";
        Func<string, string> toUpperCase = x => x.ToUpper();
        Func<string, string> back = x => String.Concat(x.Reverse());
        
        Func<string, string> a = x => x + "A";
        Func<string, string> b = x => x + "B";
        Func<string, string> c = x => x + "C";

        Console.WriteLine(FuncExt.Compose(a, b, c)(""));

        Func<string, string> shout = FuncExt.Compose(exclaim);//, toUpperCase, back);
        
        Console.WriteLine(shout("send in the clowns"));

        var xx = new[] { 1, 2 }.Aggregate((acc, x) => x + acc);

        var x = 5.0.Apply(TimeSpan.FromHours)
                      .Apply(WithDuration);
    }

    private static int WithDuration(TimeSpan ts) => 10;
}