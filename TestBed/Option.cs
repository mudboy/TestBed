using System.Runtime.InteropServices.ComTypes;
using static TestBed.Option;

namespace TestBed;

public readonly struct Option<A>
{

    private static readonly IOptional OnlyNone = new NoValue();
    private readonly IOptional _impl;

    public Option()
    {
        _impl = OnlyNone;
    }
    
    private Option(IOptional imple)
    {
        _impl = imple;
    }

    interface IOptional
    {
        bool HasValue { get; }
        B Match<B>(Func<A, B> something, Func<B> nothing);
        void Match(Action<A> something, Action nothing);
    }

    private sealed record SomeValue(A Value) : IOptional
    {
        public bool HasValue => true;

        public B Match<B>(Func<A, B> something, Func<B> nothing)
        {
            return something(Value);
        }

        public void Match(Action<A> something, Action nothing)
        {
            something(Value);
        }

        public override string ToString() => $"({Value!.ToString()})";
    }
    private sealed record NoValue : IOptional
    {
        public bool HasValue => false;

        public B Match<B>(Func<A, B> something, Func<B> nothing)
        {
            return nothing();
        }

        public void Match(Action<A> something, Action nothing)
        {
            nothing();
        }

        public override string ToString() => "()";
    }

    internal static Option<A> CreateSome(A value) => new(new SomeValue(value));
    internal static Option<A> CreateNothing() => new(OnlyNone);

    public bool HasValue => _impl.HasValue;

    public B Match<B>(Func<A, B> some, Func<B> none) => _impl.Match(some, none);
    public void Match(Action<A> some, Action none) => _impl.Match(some, none);
    
    public Option<B> SelectMany<B>(Func<A, Option<B>> bind) => _impl.Match(bind, Option<B>.CreateNothing);

    public Option<C> SelectMany<B, C>(Func<A, Option<B>> bind, Func<A, B, C> project)
    {
        return SelectMany(x => bind(x).Select(y => project(x, y)));
    }
    
    public Option<B> Select<B>(Func<A, B> selector) => SelectMany(x => Option<B>.CreateSome(selector(x)));
    
    public static implicit operator Option<A>(A value) => CreateSome(value);
    public static implicit operator Option<A>(OptionalNone _) => CreateNothing();
    public static bool operator true(Option<A> value) => value.HasValue;
    public static bool operator false(Option<A> value) => !value.HasValue;

    public override string ToString() => _impl.ToString()!;
}

public record struct OptionalNone;

public static class Option
{
    public static Option<A> Some<A>(A value) => Option<A>.CreateSome(value);

    public static OptionalNone None => default;
}

public static class OptionExtensions
{
    public static Option<B> Apply<A, B>(this Option<Func<A, B>> optF, Option<A> optA) =>
        optF.Match(
            some: f => 
                optA.Match(
                    some: a => Some(f(a)), 
                    none: () => None), 
            none: () => None);

    public static Option<Func<A, C>> ApplyR<A, B, C>(this Option<Func<A, B, C>> optF, Option<B> optB) =>
        optF.Select(CurryR).Apply(optB);

    public static Option<Func<T2, R>> Apply<T1, T2, R>(this Option<Func<T1, T2, R>> optF, Option<T1> optT) 
        => optF.Select(Curry).Apply(optT);
    
    public static Option<Func<T2, T3, R>> Apply<T1, T2, T3, R>(this Option<Func<T1, T2, T3, R>> optF, Option<T1> optT) 
        => Apply(optF.Select(Curry), optT);
    
    public static Func<B, Func<A, C>> CurryR<A, B, C>(this Func<A, B, C> f) =>
        x => y => f(y, x);

    
    public static Func<T1, Func<T2, R>> Curry<T1, T2, R>(this Func<T1, T2, R> f) =>
        x => y => f(x, y);    
    public static Func<T1, Func<T2, T3, R>> Curry<T1, T2, T3, R>(this Func<T1, T2, T3, R> f) =>
        x => (y, z) => f(x, y, z);

    public static Option<IEnumerable<B>> TraverseM<A, B>(this IEnumerable<A> list, Func<A, Option<B>> f)
        => list.Aggregate(
            seed: Some(Enumerable.Empty<B>()),
            func: (optBs, a) =>
                from bs in optBs
                from b in f(a)
                select bs.Append(b)
        );
    
    private static Func<IEnumerable<T>, T, IEnumerable<T>> Append<T>()
        => (ts, t) => ts.Append(t);

    public static Option<IEnumerable<B>> TraverseA<A, B>(this IEnumerable<A> list, Func<A, Option<B>> f)
        => list.Aggregate(
            seed: Some(Enumerable.Empty<B>()),
            func: (optBs, a) =>
                Some(Append<B>())
                    .Apply(optBs)
                    .Apply(f(a))
        );

    public static Option<IEnumerable<B>> Traverse<A, B>(this IEnumerable<A> ts, Func<A, Option<B>> f)
        => TraverseA(ts, f);

    public static Option<C> Map2<A, B, C>(this Option<A> oa, Option<B> ob, Func<A, B, C> f) =>
        oa.SelectMany(a => ob.Select(b => f(a, b)));

    public static Option<IEnumerable<B>> Traverse2<A, B>(this IEnumerable<A> ts, Func<A, Option<B>> f) =>
        ts.Aggregate(
            seed: Some(Enumerable.Empty<B>()),
            func: (acc, a) =>
                f(a).Map2(acc, (b, bs) => bs.Append(b))
        );
}

public static partial class Main
{
    public static Option<double> DoubleParse(string value) =>
        Double.TryParse(value, out var result) ? Some(result) : None;

    public static string StringTrim(string value) => value.Trim();
    
    public static void OptionExamples()
    {
        var input = Console.ReadLine();

        // when you map(select) a world crossing function
        // you get a list of the container type
        // which is usually not what you want 
        input.Split(',')
            .Select(StringTrim)
            .Select(DoubleParse) // IEnumerable<Option<double>> 😒
            .Print("Uh what? ");

        // so use Traverse to flip the order of the types
        input.Split(',')
            .Select(StringTrim)
            .Traverse(DoubleParse) // Option<IEnumerable<double>> 😍
            .Match(x => x.Print("Numbers are "), () => Console.WriteLine($"input {input} is not valid")); 
        
        
        var dobl = (int x) => x * 2;

        var four = Some(2).Select(dobl);

        if (four)
        {
            Console.WriteLine("have four");
        }

        var mul = (int x) => x * x;
        var mul2 = (int x, int y) => x * y;
        var mul3 = (int x, int y, int z) => x * y * z;
        var remainder = (int dividend, int divisor) => dividend % divisor;

        var x = Some(2).Select(mul2.Curry()).Apply(Some(3));
        x.Match(i => Console.WriteLine($"i is {i}"), () => Console.WriteLine("None"));
        Some(mul2)
            .Apply(Some(2))
            .Apply(Some(3));

        Some(mul3)
            .Apply(Some(1))
            .Apply(Some(3))
            .Apply(Some(5));

        var twoRemainder = Some(remainder)
            .ApplyR(Some(2));

        var res = twoRemainder.Apply(Some(4));

    }
}