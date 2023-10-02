namespace TestBed;

public static class LazyExt
{
    public static Lazy<R> Select<T, R>(this Lazy<T> source, Func<T, R> f)
    {
        return new Lazy<R>(() => f(source.Value));
    }

    public static Lazy<R> SelectMany<T, R>(this Lazy<T> source, Func<T, Lazy<R>> f)
    {
        return new Lazy<R>(() => f(source.Value).Value);
    }

    public static Lazy<R> SelectMany<A, B, R>(this Lazy<A> first, Func<A, Lazy<B>> second, Func<A, B, R> project)
    {
        return new Lazy<R>(() =>
        {
            var a = first.Value;
            var b = second(a).Value;
            return project(a, b);
        });
    }

    public static Lazy<R> Apply<T, R>(this Lazy<T> source, Lazy<Func<T, R>> f)
    {
        return new Lazy<R>(() =>
        {
            var fs = f.Value;
            var v = source.Value;
            return fs(v);
        });
        //return f.Select(ff => ff(source.Value));
        //return f.SelectMany(source.Select);
    }
}

public static partial class Main
{
    public static void LazyExamples()
    {
        var x = new Lazy<int>(() => 1);
        var y = new Lazy<int>(() => 2);

        var xx = from a in x
            from b in y
            select a + b;

        var doubl = (int x) => x * 2;

        var r = x.Select(doubl);
        
    }
    
    public static void ApplyExamples()
    {
        var itoa = (int i) => $">{i}>";
        var itob = (int i) => $"<{i}<";
        var lazyItos = new Lazy<Func<int, string>>(() => itoa);

        var lv = new Lazy<int>(() => 123);

        var lr = lv.Apply(lazyItos);

        var fs = new List<Func<int, string>> { itoa, itob };
        var fv = new List<int> { 1, 2, 3 };

        fv.Apply(fs).Print("apply style");
        fv.ApplyM(fs).Print("monadic style");
    }
}