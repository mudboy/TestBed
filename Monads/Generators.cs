using System.Text;
using TestBed;
using Utils;
using static Monads.Gen;
using static Utils.EnumerableExtensions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Monads;

// converted from
// https://github.com/fpinscala/fpinscala/blob/second-edition/src/main/scala/fpinscala/answers/testing/Gen.scala

public delegate (IRng, T) Gen<T>(IRng r);

public delegate Gen<T> SGen<T>(int n);

public static class Gen
{
    public static A Run<A>(this Gen<A> gen, IRng r) => gen(r).Item2;
    public static Gen<int> Int => Rng.Int;
    public static Gen<int> NaturalInt => Rng.NaturalNumber;
    public static Gen<int> NonNegativeInt => Rng.NonNegativeInt;
    public static Gen<double> Double => Rng.Double;
    public static Gen<bool> Bool => Rng.Bool;

    public static Gen<IEnumerable<A>> ListOfN<A>(int n, Gen<A> g) =>
        Fill(n, g).Sequence().Select(x => x);

    public static Gen<List<A>> ListOfN<A>(this Gen<A> self, int n) => 
        ListOfN(n, self).Select(l => l.ToList());

    public static Gen<List<A>> ListOfN<A>(this Gen<A> self, Gen<int> size) => 
        size.SelectMany(x => self.ListOfN(Math.Max(0, x)));
    
    public static SGen<List<A>> List<A>(this Gen<A> self) => 
        n => ListOfN(n, self).Select(l => l.ToList());
    
    public static SGen<List<A>> NonEmptyList<A>(this Gen<A> self) => 
        n => ListOfN(Math.Max(n, 1), self).Select(l => l.ToList());
    
    public static SGen<A> UnSized<A>(this Gen<A> self) => 
        _ => self;
    
    public static Gen<IEnumerable<A>> NonEmptyEnumerable<A>(this Gen<A> g) =>
        Choose(1, 127).SelectMany(i => ListOfN(i, g));

    public static Gen<R> Select<T, R>(this Gen<T> self, Func<T, R> f) =>
        self.SelectMany(a => Return(f(a)));
    

    public static Gen<R> SelectMany<T, R>(this Gen<T> self, Func<T, Gen<R>> f) =>
        r =>
        {
            var (rng, v) = self(r);
            return f(v)(rng);
        };

    public static Gen<C> SelectMany<A, B, C>(this Gen<A> self, Func<A, Gen<B>> fromFirst, Func<A, B, C> project) =>
        self.SelectMany(x => fromFirst(x).Select(y => project(x, y))); 

    
    public static Gen<char> Digit => 
        Choose(48, 58).Select(x => (char)x);
    
    public static Gen<char> Char => 
        Choose(65, 91)
            .Union(Choose(97, 123))
            .Select(x => (char)x);

    public static Gen<char> AlphaNumeric => Char.Union(Digit);
    
    public static Gen<T> Return<T>(T value) => 
        rng => (rng, value);
    
    public static Gen<A> Union<A>(this Gen<A> a, Gen<A> b) =>
        Bool.SelectMany(x => x ? a : b);
    
    public static Gen<A> Weighted<A>((Gen<A> gen, double weight) g1, (Gen<A> gen, double weight) g2)
    {
        var g1Threshold = Math.Abs(g1.weight) / (Math.Abs(g1.weight) + Math.Abs(g2.weight));
        return Double.SelectMany(d => d < g1Threshold ? g1.gen : g2.gen);
    }

    public static Gen<A> Weighted2<A>(params (Gen<A> gen, int weight)[] items) =>
        NaturalInt.Select(i => i % items.Sum(x => x.weight))
            .SelectMany(pick =>
            {
                (int next, Gen<A>? gen, bool found) seed = (0, null, false); 
                var selection = items.Aggregate(seed, (acc, item) =>
                {
                    var next = acc.next + item.weight;
                    if (next > pick && !acc.found)
                    {
                        acc.gen = item.gen;
                        acc.found = true;
                    }
                
                    acc.next = next;
                    return acc;
                });
                return selection.gen!;
            });

    // map(select) can be defined in terms of BiMap(map2) and Unit
    public static Gen<B> Map<A, B>(this Gen<A> ga, Func<A, B> f) =>
        ga.Map2(Return<B>(default!), (a, _) => f(a));
    
    // BiMap(map2) can be defined directly for this type and is applicative
    public static Gen<C> Map2<A, B, C>(this Gen<A> ga, Gen<B> gb, Func<A, B, C> f) =>
        r =>
        {
            var (rng1, a) = ga(r);
            var (rng2, b) = gb(rng1);
            return (rng2, f(a,b));
        };

    // or it can be defined with selectMany/bind (and this is general for all monads)
    public static Gen<C> BiMapM<A, B, C>(this Gen<A> ga, Gen<B> gb, Func<A, B, C> fc) =>
        ga.SelectMany(a => gb.Select(b => fc(a, b)));

    /// <summary>
    /// Convert an IEnumerable{Gen{A}} to Gen{IEnumerable{A}} 
    /// </summary>
    /// <param name="actions">the list of Gen{A}s</param>
    /// <typeparam name="A">the type contained in the Gen{A}</typeparam>
    /// <returns></returns>
    public static Gen<IEnumerable<A>> Sequence<A>(this IEnumerable<Gen<A>> actions) =>
        actions.Traverse(x => x);

    // Sequence can be defined as a SelectMany then select this version is monadic i.e. each step depended on the last
    public static Gen<IEnumerable<A>> Sequence2<A>(this IEnumerable<Gen<A>> actions) => 
        actions.Aggregate(Return(Nil<A>()), (acc, a) => acc.SelectMany(xs => a.Select(x => xs.Append(x))));

    public static Gen<IEnumerable<A>> Sequence3<A>(this IEnumerable<Gen<A>> actions) =>
        actions.Aggregate(Return(Nil<A>()), (acc, a) => acc.Map2(a, (xs, x) => xs.Append(x)));

    // Here we use and actual lifted function and apply method
    public static Gen<IEnumerable<A>> Sequence4<A>(this IEnumerable<Gen<A>> actions) =>
        actions.Aggregate(
            Return(Nil<A>()),
            (acc, a) =>
                Return(Append<A>())
                    .Apply(acc)
                    .Apply(a));
    
    public static async Task<S> Fold<A, S>(this Task<A> task, S initial, Func<S, A, S> folder)
    {
        var val = await task;
        return folder(initial, val);
    }
    
    
    private static Func<IEnumerable<T>, T, IEnumerable<T>> Append<T>()
        => (ts, t) => ts.Append(t);
    
    
    // direct implementation, like sequence but with the added function call.
    public static Gen<IEnumerable<B>> Traverse<A,B>(this IEnumerable<A> las, Func<A, Gen<B>> f) =>
        las.Aggregate(
            Return(Nil<B>()), 
            (acc, a) => 
                f(a).Map2(acc, (b, xs) => xs.Append(b)));

    // or can be just as simple as a map then sequence
    public static Gen<IEnumerable<B>> Traverse2<A, B>(this IEnumerable<A> las, Func<A, Gen<B>> f) =>
        las.Select(f).Sequence3();
    
    
    public static Gen<Func<T2, R>> Apply<T1, T2, R>(this Gen<Func<T1, T2, R>> optF, Gen<T1> optT) 
        => optF.Select(FuncExt.Curry).Apply(optT);      
    
    public static Gen<Func<T2, T3, R>> Apply<T1, T2, T3, R>(this Gen<Func<T1, T2, T3, R>> optF, Gen<T1> optT) 
        => optF.Select(FuncExt.CurryFirst).Apply(optT); 

    public static Gen<TResult> Apply<T, TResult>(this Gen<Func<T, TResult>> self, Gen<T> source) =>
        self.Map2(source, (f, a) => f(a));
    
    public static Gen<TResult> ApplyM<T, TResult>(this Gen<Func<T, TResult>> self, Gen<T> source) => 
        self.SelectMany<Func<T, TResult>, TResult>(source.Select);
    
    public static Gen<string> StringN(int n) =>
        ListOfN(n, Char).Select(string.Concat);

    public static SGen<string> String => StringN;  
    
    public static Gen<string> AlphaNumericStringN(int n) => 
        ListOfN(n, AlphaNumeric).Select(string.Concat);

    public static Gen<char> SpecialCharacter => OneOf("!£$%^&*+-=@#~?");
    public static Gen<string> PasswordStringN(int n) =>
        ListOfN(n, Weighted2(
                (SpecialCharacter, 20), 
                (Char, 40),
                (Digit, 40)))
            .Select(string.Concat);
    
    public static Gen<char> OneOf(string input) =>
        Choose(0, input.Length).Select(i => input[i]);

    public static Gen<T> OneOf<T>(params T[] choices) => 
        Choose(0, choices.Length).Select(x => choices[x]);
    public static Gen<T> OneOf<T>(params Gen<T>[] choices) => 
        Choose(0, choices.Length).SelectMany(x => choices[x]);

    public static Gen<(A, B)> Both<A, B>(Gen<A> ga, Gen<B> gb) =>
        Map2(ga, gb, (a, b) => (a, b));
    

    /// <summary>
    /// Generates a random string based a simple pattern made of
    /// single character symbols
    /// ? -> character a-zA-z
    /// # -> digit 0-9
    /// e.g ??## -> xH83
    /// all other character in the pattern map to themselves
    /// </summary>
    /// <param name="pattern">the pattern</param>
    /// <returns>a random string based on the pattern</returns>
    public static Gen<string> FromPattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (pattern.Length == 0)
            return Return("");
        
        // the easy way let the HOF do the work
        return pattern.Traverse(c => c switch
        {
            '?' => Char,
            '#' => Digit,
            _ => Return(c)
        }).Select(string.Concat);
    }

    public static Gen<string> FromPatternAlternative(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        /* the hard way, manually managing the state (rng) */
        return rng =>
        {
            var builder = new StringBuilder(pattern.Length);
            var seed = (rng, builder);
            var res = pattern.Aggregate(seed, (acc, c) =>
            {
                var v = c switch
                {
                    'A' => Char(acc.rng),
                    '9' => Digit(acc.rng),
                    _ => (acc.rng, c)
                };
                return (v.Item1, acc.builder.Append(v.Item2));
            });

            return (res.rng, res.builder.ToString());
        };
    }

    public static Gen<int> Choose(int start, int stopExclusive) =>
        Select(Rng.NonNegativeInt, n => start + n % (stopExclusive - start));

    public static readonly Gen<string> Postcode = 
        from pattern in OneOf("??##", "??#", "?##", "?#", "?#?", "??#?")
        from outCode in FromPattern(pattern)
        from inCode in FromPattern("#??")
        select $"{outCode} {inCode}".ToUpper();
}

public static class SGen
{
    public static SGen<B> Select<A, B>(this SGen<A> self, Func<A, B> f) => n => self(n).Select(f);

    public static SGen<B> SelectMany<A, B>(this SGen<A> self, Func<A, SGen<B>> f) =>
        n => self(n).SelectMany(x => f(x)(n));

    public static Gen<A> Apply<A>(this SGen<A> self, int n) => self(n);
    public static SGen<A> Apply<A>(Func<int, Gen<A>> f) => n => f(n);
}

public static partial class Main
{
    public static void GenExamples()
    {
        var r = Rng.Default();
        //var r = Rng.Simple(42);
        //var r = Rng.Simple(4980234);
        //var r = Rng.Simple(10);
        
        var postcode = Postcode.Run(r);
        postcode.Print("a postcode: ");
        var g = ListOfN(10, Digit).Run(r);
        g.Print("10 random digits ");
        var doubles = Gen.Double.ListOfN(100).Run(r);
        doubles.Print("100 random doubles ");

        var weighted = Weighted((Return("A"), 0.25), (Return("B"), 0.75)).ListOfN(100).Run(r);
        weighted.Print("100 weighted a&bs ");
        Console.WriteLine("A count = " + weighted.Count(s => s == "A"));
        
        var sg = AlphaNumericStringN(10).Run(r);
        sg.Print("length 10 alpha numeric string: ");

        var password = PasswordStringN(14).Run(r);
        password.Print("A generated password: ");
        
        string EssBefore(string i) => $"s-{i}";
        string QueAfter(string s) => s + "-q";
        string BothEssAndQue(string s) => EssBefore(QueAfter(s));

        var taintingFunctions = Gen.OneOf(EssBefore, QueAfter, BothEssAndQue);
        var taintedPostcodes = taintingFunctions.Apply(Postcode);

        var listOfTaintedPostcodes = ListOfN(25, taintedPostcodes).Run(r);

        listOfTaintedPostcodes.Print();

// lift a function to the Gen<> world apply an argument (i.e call it)
        var genF = Return(EssBefore).Apply(Return("123"))(r);
        Console.WriteLine("lift and apply = " + genF);

        Return(EssBefore).Map2(Return("123"), (a, b) => a(b));

// choose example
        var lostOfInts = Choose(1, 10).ListOfN(25); //(new Random(2));
//lostOfInts.Print();
    }
}