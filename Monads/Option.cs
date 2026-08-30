using System.Numerics;
using Utils;
using static Monads.Option;

namespace Monads;

public abstract record Option<A>
{

    public abstract bool  HasValue { get; }
    public abstract B Match<B>(Func<A, B> some, Func<B> none);
    public abstract void Match(Action<A> some, Action none);

    private sealed record SomeValue(A Value) : Option<A>
    {
        public override bool HasValue => true;
        public override B Match<B>(Func<A, B> some, Func<B> none) => some(Value);
        public override void Match(Action<A> something, Action nothing) { something(Value); }
        public override string ToString() => $"({Value!.ToString()})";
    }
    
    private sealed record NoValue : Option<A>
    {
        public override bool HasValue => false;
        public override B Match<B>(Func<A, B> some, Func<B> none) => none();
        public override void Match(Action<A> some, Action none) => none();
        public override string ToString() => "()";
    }

    internal static Option<A> CreateSome(A value) => new SomeValue(value);
    internal static Option<A> None() => new NoValue();

    public Option<B> SelectMany<B>(Func<A, Option<B>> bind) => Match(bind, Option<B>.None);

    public Option<C> SelectMany<B, C>(Func<A, Option<B>> bind, Func<A, B, C> project) => 
        SelectMany(x => bind(x).Select(y => project(x, y)));
    
    public Option<B> Select<B>(Func<A, B> selector) =>
        Match(a => Some(selector(a)), Option<B>.None);

    public Option<A> Where(Func<A, bool> predicate) =>
        SelectMany(x => predicate(x) ? Some(x) : None());
    
    public static implicit operator Option<A>(A value) => CreateSome(value);
    public static implicit operator Option<A>(OptionalNone _) => None();
    public static bool operator true(Option<A> value) => value.HasValue;
    public static bool operator false(Option<A> value) => !value.HasValue;
}

public record struct OptionalNone;

public static class Option
{
    public static Option<A> Some<A>(A value) => Option<A>.CreateSome(value);

    public static OptionalNone None => default;
}

public static class OptionExtensions
{
    extension<A, B>(Option<A>)
    {
        public static Option<B> operator >>> (Option<A> a, Func<A, Option<B>> f) => a.SelectMany(f);

        public static Option<B> operator >> (Option<A> a, Func<A, B> f) => a.Select(f);

    }
    
    extension<A, B, C, D>(Func<A, B, C, D>)
    {
        public static Option<Func<B, C, D>> operator %(Func<A, B, C, D> f, Option<A> a) => a.Select(x => f.Curry()(x));
    }
    
    
    extension<A, B, C>(Func<A, B, C>)
    {
        public static Func<A, Func<B, C>> operator !(Func<A, B, C> f) => f.Curry();
        public static Option<Func<B, C>> operator %(Func<A, B, C> f, Option<A> a) => a.Select(x => f.Curry()(x));
    }
    
    extension<A, B>(Option<Func<A, B>>)
    {
        public static Option<B> operator *(Option<Func<A, B>> f, Option<A> a) => f.Apply(a);
    }
    
    extension<A, B, C>(Option<Func<A, B, C>>)
    {
        public static Option<Func<B,C>> operator *(Option<Func<A, B, C>> f, Option<A> a) => f.Apply(a);
    }
    
    public static Option<A> ToOption<A>(this A? value) => value is not null ? Some(value) : None;
     
    // primitive definition of apply not using map2
    extension<A, B>(Option<Func<A, B>> optF)
    {
        public Option<B> Apply(Option<A> optA) =>
            optF.Match(
                some: f => 
                    optA.Match(
                        some: a => Some(f(a)), 
                        none: () => None), 
                none: () => None);

        public Option<B> ApplyViaMap2(Option<A> optA) =>
            optF.Map2(optA, (f, a) => f(a));
    }


    // apply using map2

    // apply right
    extension<A, B, C>(Option<Func<A, B, C>> optF)
    {
        public Option<Func<A, C>> ApplyR(Option<B> optB) =>
            optF.Select(FuncExt.CurryR).Apply(optB);

        public Option<Func<B, C>> Apply(Option<A> optT) 
            => optF.Select(FuncExt.Curry).Apply(optT);
    }

    public static Option<Func<T2, T3, R>> Apply<T1, T2, T3, R>(this Option<Func<T1, T2, T3, R>> optF, Option<T1> optT) 
        => Apply(optF.Select(FuncExt.Curry), optT);
    
    // Traverse allows mapping a world crossing function to a functor
    // so you get M[List[A]] not List[M[a]] i.e. it flips the order of the types 
    // this is the monadic version (will short circuit on error)
    public static Option<IEnumerable<B>> TraverseM<A, B>(this IEnumerable<A> list, Func<A, Option<B>> f)
        => list.Aggregate(
            seed: Some(Enumerable.Empty<B>()),
            func: (optBs, a) =>
                from bs in optBs
                from b in f(a)
                select bs.Append(b)
        );

    // while lists of things are traversable so is option as you can think of it
    // as a list of 1 or 0 items (this is true for all types that are alternatives, Either, Result, Validation, etc...) 
    public static Task<Option<B>> Traverse<A, B>(this Option<A> opt, Func<A, Task<B>> f) =>
        opt.Match(
            some: async a => Some(await f(a)),
            none: () => Task.FromResult(Option<B>.None()));

    // helper function
    private static Func<IEnumerable<T>, T, IEnumerable<T>> Append<T>()
        => (ts, t) => ts.Append(t);

    // this is the applicative version using apply
    extension<A, B>(IEnumerable<A> list)
    {
        public Option<IEnumerable<B>> TraverseA(Func<A, Option<B>> f) => 
            list.Aggregate(
                seed: Some(Enumerable.Empty<B>()),
                func: (optBs, a) =>
                    Some(Append<B>())
                        .Apply(optBs)
                        .Apply(f(a))
            );

        public Option<IEnumerable<B>> Traverse2(Func<A, Option<B>> f) =>
            list.Aggregate(
                seed: Some(Enumerable.Empty<B>()),
                func: (acc, a) =>
                    f(a).Map2(acc, (b, bs) => bs.Append(b))
            );

        public Option<IEnumerable<B>> TraverseA2(Func<A, Option<B>> f) => 
            list.Aggregate(
                seed: Some(Enumerable.Empty<B>()), 
                func: (acc, x) => 
                    f(x).Map2(acc, (b, xs) => xs.Append(b)));

        public Option<IEnumerable<B>> Traverse(Func<A, Option<B>> f) =>
            list.TraverseA(f);

        public static IEnumerable<B> operator >> (IEnumerable<A> a, Func<A, B> f) => a.Select(f);

        public static Option<IEnumerable<B>> operator <<(IEnumerable<A> a, Func<A, Option<B>> f) => a.Traverse(f);
    }
    
    // Traverse can also be defined with Map2

    // default to the applicative version

    // Sequence can flip the type on collections of M[A]
    // so List[M[A]] -> M[List[A]]
    // it can be defined as Traverse with the identity function
    public static Option<IEnumerable<A>> Sequence<A>(this IEnumerable<Option<A>> ts)
        => ts.Traverse2(x => x);

    // 
    public static Option<IEnumerable<B>> TraverseViaSequence<A, B>(this IEnumerable<A> ts, Func<A, Option<B>> f)
        => ts.Select(f).Sequence();

    // default to the direct version
    public static Option<C> Map2<A, B, C>(this Option<A> oa, Option<B> ob, Func<A, B, C> f) =>
        oa.Map2Directly(ob, f);

    // map2 can be defined via bind for all monads
    public static Option<C> Map2General<A, B, C>(this Option<A> oa, Option<B> ob, Func<A, B, C> f) =>
        oa.SelectMany(a => ob.Select(b => f(a, b)));

    // or can be defined directly for types that support that and thus is a primative
    public static Option<C> Map2Directly<A, B, C>(this Option<A> oa, Option<B> ob, Func<A, B, C> f) =>
        oa.Match(
            a => ob.Match(
                b => Some(f(a, b)), 
                Option<C>.None), 
            Option<C>.None);
    
    // Map2 can also be defined with the primitive apply and unit
    public static Option<C> Map2WithApply<A, B, C>(this Option<A> oa, Option<B> ob, Func<A, B, C> f) =>
        //Some(f).Apply(oa).Apply(ob);
        f % oa * ob;
    

    // join can be defined be bind
    public static Option<A> Join<A>(this Option<Option<A>> ooa) =>
        ooa.SelectMany(x => x);

    // or directly for the type 
    public static Option<A> Join2<A>(this Option<Option<A>> ooa) =>
        ooa.Match(oa => oa, Option<A>.None);

    // bind can be defined with return/map/join as an alternative to
    // bind/return
    public static Option<B> SelectMany_Join<A, B>(this Option<A> oa, Func<A, Option<B>> f) =>
        oa.Select(f).Join();
}

public static partial class Main
{

    
    public static void OptionExamples()
    {
        var input = "";

        // when you map(select) with a world crossing function a -> M a
        // you get a list of the container type
        // which is usually not what you want
        // i.e. [a] map a -> M a = [M a] and not M [a] 
        (input.Split(',') >> StringEx.Trim >> DoubleEx.MaybeParse // IEnumerable<Option<double>> 😒
            ).Print("Uh what? ");

        // so use Traverse to flip the order of the types
        // Traverse is usually applicative so will combine "errors" if M supports that
        input.Split(',')
            .Select(StringEx.Trim)
            .Traverse(DoubleEx.MaybeParse) // Option<IEnumerable<double>> 😍
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

        var xxx = DoProcessing2;

        var x = Some(2).Select(!xxx).Apply(Some(3));
        x.Match(i => Console.WriteLine($"i is {i}"), () => Console.WriteLine("None"));
        Some(mul2)
            .Apply(Some(2))
            .Apply(Some(3));

        Some(mul3)
            .Apply(Some(1))
            .Apply(Some(3))
            .Apply(Some(5));
        
        //with cryptic symbols
        var cs = mul3 % Some(1) * Some(2) * Some(3);

        var twoRemainder = Some(remainder)
            .ApplyR(Some(2));

        var res = twoRemainder.Apply(Some(4));

        var xx = Some(3);
        
        var rr = xx >> (xy => xy + 2) >>> DoProcessing;

        var res1 = mul2 % Some(3) * Some(3);

        res1.Select(ConsoleEx.WriteLine);
        
        
        var x1 = Consume([1,2,3], 1);
        Console.WriteLine(x1.ToList());
        var x2 = Consume([1,2,3], 2);
        Console.WriteLine(x2.ToList());
        var x3 = Consume([1,2,3], 3);
        Console.WriteLine(x3.ToList());
        var x4 = Consume([1,2,3], 4);
        Console.WriteLine(x4.ToList());

        string workDir = "work";
        var tempPath = Path.GetTempPath() / workDir / "file.jpg";
        var fi = new FileInfo(tempPath);

        var di = new DirectoryInfo(Path.GetTempPath());
        
        var nn = fi.FullName;
            
        Console.WriteLine(tempPath);
    }

    public static Option<string> DoProcessing(int a) => Some("");
    public static Option<string> DoProcessing2(int a, int b) => Some("");

    public static IEnumerable<T> Consume<T>(
        this IEnumerable<T> source, T quantity) 
        where T : IComparisonOperators<T, T, bool>, INumberBase<T>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        return ConsumeIterator(source, quantity);
    }

    private static IEnumerable<T> ConsumeIterator<T>(IEnumerable<T> source, T quantity)
        where T : IAdditiveIdentity<T, T>,
        IAdditionOperators<T, T, T>,
        IComparisonOperators<T, T, bool>
    {
        var acc = T.AdditiveIdentity;
        foreach (var i in source)
        {
            if (quantity <= acc)
                yield return i;
            acc += i;
        }
    }
}

public static class DoubleEx
{
    extension(double)
    {
        public static Option<double> MaybeParse(string value) =>
            double.TryParse(value, out var result) ? Some(result) : None;
    }
}

public static class StringEx
{
    extension(string value)
    {
        public string Trim() => value.Trim();
    }
}

public static class ConsoleEx
{
    extension(Console)
    {
        public static Unit WriteLine(int x)
        {
            Console.WriteLine(x);
            return Unit.Value;
        }
    }
}

public static class PathEx
{
    extension(string)
    {
        public static string operator /(string a, string b) => Path.Combine(a, b);
    }

    extension(Uri)
    {
        public static UriBuilder operator /(Uri a, string b)
        {
            var bb = new UriBuilder(a);
            bb.Path += b;
            return bb;
        }
    }
}