using System.Collections;
using System.Diagnostics.Tracing;

namespace TestBed;

public static class IEnumExt
{
    public static (IEnumerable<A>, IEnumerable<A>) Break<A>(this IEnumerable<A> source, Func<A, bool> predicate)
    {
        var a = source.SkipWhile(x => !predicate(x));
        var b = source.TakeWhile(predicate);
        return (a, b);
    } 
    
    public static IEnumerable<A> Nil<A>() => Enumerable.Empty<A>();
    public static IEnumerable<A> Fill<A>(int n, A e) => Enumerable.Repeat(e, n);

    public static IEnumerable<R> Apply<T, R>(this IEnumerable<T> source, IEnumerable<Func<T, R>> fs)
    {
        return from func in fs from v in source select func(v);
        //return fs.SelectMany(source.Select); // monadic style
    }    
    public static IEnumerable<R> ApplyM<T, R>(this IEnumerable<T> source, IEnumerable<Func<T, R>> fs)
    {
        return fs.SelectMany(source.Select); // monadic style
    }

    // [[A]] -> [A]
    public static IEnumerable<T> Join<T>(this IEnumerable<IEnumerable<T>> source)
    {
        //...?
        return source.Aggregate((acc, x) => acc.Concat(x));
    }

    // I named it flatMap not selectMany (which it is)
    public static IEnumerable<R> FlatMap<T, R>(this IEnumerable<T> source, Func<T, IEnumerable<R>> selector) 
        => source.Select(selector).Join();

    public static IEnumerable<B> SelectViaSelectMany<A, B>(this IEnumerable<A> source, Func<A, B> f) 
        => source.SelectMany(a => new[] { f(a) });
        //=> source.SelectMany(a => Enumerable.Repeat(f(a), 1));

    public static IEnumerable<A> WhereViaSelectMany<A>(this IEnumerable<A> source, Func<A, bool> p)
        //=> source.SelectMany(a => p(a) ? new[] { a } : Enumerable.Empty<A>());
        => source.SelectMany(a => Enumerable.Repeat(a, p(a) ? 1 : 0));
        
    // you can sort of define Aggregate/Reduce via SelectMany/bind/flatMap
    // public static A Aggregate<A>(this IEnumerable<A> source, A seed, Func<A,A,A> f)
    // {
    //     var acc = seed;
    //     return source.SelectMany(a =>
    //     {
    //         acc = f(a, acc);
    //         return Enumerable.Repeat(acc, 1);
    //     }).FirstOrDefault(seed);
    // }

    // But you can easily define SelectMany/bind/flatMap with Aggregate/Reduce and Select/Map
    public static IEnumerable<B> SelectManyAgg<A, B>(this IEnumerable<A> source, Func<A, IEnumerable<B>> f)
        //=> source.Aggregate(Enumerable.Empty<B>(), (acc, a) => acc.Concat(f(a)));
        => source.Select(f).Aggregate(Enumerable.Empty<B>(), (acc, a) => acc.Concat(a));
    
    public static IEnumerable<B> SelectAgg<A, B>(this IEnumerable<A> source, Func<A, B> f)
        => source.Aggregate(Enumerable.Empty<B>(), (acc, a) => acc.Append(f(a)));

    public static IEnumerable<A> FilterAgg<A, B>(this IEnumerable<A> source, Func<A, bool> p)
        => source.Aggregate(Enumerable.Empty<A>(), (acc, a) => acc.Concat(Enumerable.Repeat(a, p(a) ? 1 : 0)));

    private static IEnumerable<int> Remove(int a, IEnumerable<int> arr)  => arr.Where(x => a != x);
    
    //
    // Lazy List applicative (non monadic)
    //

    public static IEnumerable<T> Continually<T>(T a)
    {
        for (;;)
        {
            yield return a;
        }
    }

    public static IEnumerable<C> Map2<A, B, C>(this IEnumerable<A> fa, IEnumerable<B> fb, Func<A, B, C> f) =>
        fa.Zip(fb).Select(a => f(a.First, a.Second));
    
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        return source.Shuffle(new Random());
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (rng == null) throw new ArgumentNullException(nameof(rng));

        return source.ShuffleIterator(rng);
    }

    private static IEnumerable<T> ShuffleIterator<T>(
        this IEnumerable<T> source, Random rng)
    {
        var buffer = source.ToList();
        for (int i = 0; i < buffer.Count; i++)
        {
            int j = rng.Next(i, buffer.Count);
            yield return buffer[j];

            buffer[j] = buffer[i];
        }
    }
}
