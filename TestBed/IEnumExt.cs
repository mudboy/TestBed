namespace TestBed;

public static class IEnumExt
{
    public static IEnumerable<R> Apply<T, R>(this IEnumerable<T> source, IEnumerable<Func<T, R>> fs)
    {
        return from func in fs from v in source select func(v);
        //return fs.SelectMany(source.Select); // monadic style
    }    
    public static IEnumerable<R> ApplyM<T, R>(this IEnumerable<T> source, IEnumerable<Func<T, R>> fs)
    {
        return fs.SelectMany(source.Select); // monadic style
    }

    public static IEnumerable<T> Join<T>(this IEnumerable<IEnumerable<T>> source)
    {
        //...?
        return source.Aggregate((acc, x) => acc.Concat(x));
    }

    // I named it flatMap not selectMany (which it is)
    public static IEnumerable<R> FlatMap<T, R>(this IEnumerable<T> source, Func<T, IEnumerable<R>> selector)
    {
        return source.Select(selector).Join();
    }
}