using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using static TestBed.LinkedList.ListXtn;

namespace TestBed.LinkedList;

public abstract record List<T> : IEnumerable<T>
{
    public static implicit operator List<T>(NilList _) => Empty<T>.Instance;
    public IEnumerator<T> GetEnumerator()
    {
        return ListXtn.GetEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal sealed record Empty<T> : List<T>
{
    public static List<T> Instance => new Empty<T>();
}

internal sealed record Cons<T>(T Head, List<T> Tail) : List<T>;

public readonly record struct NilList;

public static class ListXtn
{
    public static List<T> List<T>()
        => Empty<T>.Instance;

    public static List<T> List<T>(params T[] items)
        => items.Reverse().Aggregate(Empty<T>.Instance
            , (tail, head) => Cons(head, tail));

    public static List<T> List<T>(IEnumerable<T> items)
        => items.Reverse().Aggregate(Empty<T>.Instance,
            (tail, h) => Cons(h, tail));

    public static NilList Nil => default;
    
    public static List<T> Cons<T>(T h, List<T> t)
        => new Cons<T>(h, t);

    public static R Match<T, R>(this List<T> list, Func<R> Empty, Func<T, List<T>, R> Cons)
        => list switch
        {
            Empty<T> => Empty(),
            Cons<T>(var t, var ts) => Cons(t, ts),
            _ => throw new ArgumentException("List can only be Empty or Cons")
        };



    public static int Length<T>(this List<T> list) => list.LengthImpl(0);
    
    private static int LengthImpl<T>(this List<T> list, int count)
        => list switch
        {
            Empty<T> => count,
            Cons<T>(_, var ts) => ts.LengthImpl(count + 1),
            _ => throw new ArgumentException("List can only be Empty or Cons")
        };
    
    public static List<B> Select2<A, B>(this List<A> source, Func<A, B> f)
        => source.SelectImpl(f, Nil).Reverse();

    public static List<B> SelectImpl<A, B>(this List<A> list, Func<A, B> f, List<B> acc)
        => list switch
        {
            Empty<A> => acc,
            Cons<A>(var h, var ts) => ts.SelectImpl(f, Cons(f(h), acc)),
            _ => throw new ArgumentException("List can only be Empty or Cons")
        };

    
    // this is just map with the id func
    public static List<T> Reverse<T>(this List<T> list)
        => list.SelectImpl(x => x, Nil);

        
    public static IEnumerator<T> GetEnumerator<T>(this List<T> source)
    {
        var list = source;
        while (true)
        {
            var (current, stop) = list.Match(
                () => (default, true)!,
                (h, t) => { list = t; return (h, false); }
            );
            
            if (stop)
                yield break;

            yield return current;
        }
    }

    public static List<T> ToLinkedList<T>(this IEnumerable<T> source) => List(source);

    public static void Print<T>(this List<T> source)
        => Console.WriteLine(string.Join(", ", source));
}

public static class Main
{
    public static void ListExamples()
    {
        var x = List(1, 2, 3);
        var len = x.Length();
        Console.WriteLine(len);

        var s = x.Select2(i => i+1);

        Console.WriteLine("Select will reverse the input");
        s.Print();

        Console.WriteLine("Select id will do?");
        var r = x.Select2(x => x);
        r.Print();        
        
        Console.WriteLine("So select followed by select id will restore order");
        var t = x.Select2(i => i+1).Select2(x => x);
        t.Print();

        var f = t.Where(x => x > 2).ToLinkedList();
        f.Print();
    }
    
    
}
