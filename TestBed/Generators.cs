using System.Text;
using static TestBed.Gen;
using static TestBed.IEnumExt;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace TestBed;

// converted from
// https://github.com/fpinscala/fpinscala/blob/second-edition/src/main/scala/fpinscala/answers/testing/Gen.scala

public delegate (T, IRng) Gen<T>(IRng r);

public delegate Gen<T> SGen<T>(int n);

public static class Gen
{
    public static Gen<int> Int => Rng.Int;
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
            var (v, rng) = self(r);
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
    
    public static Gen<T> Return<T>(T value) => 
        rng => (value, rng);
    
    public static Gen<A> Union<A>(this Gen<A> a, Gen<A> b) =>
        Bool.SelectMany(x => x ? a : b);
    
    public static Gen<A> Weighted<A>((Gen<A>, double) g1, (Gen<A>, double) g2)
    {
        var g1Threshold = Math.Abs(g1.Item2) / (Math.Abs(g1.Item2) + Math.Abs(g2.Item2));
        return Double.SelectMany(d => d < g1Threshold ? g1.Item1 : g2.Item1);
    }
    
    // map(select) can be defined in terms of BiMap(map2) and Unit
    public static Gen<B> Map<A, B>(this Gen<A> ga, Func<A, B> f) =>
        ga.BiMap(Return<B>(default!), (a, _) => f(a));
    
    // BiMap(map2) can be defined directly for this type and is applicative
    public static Gen<C> BiMap<A, B, C>(this Gen<A> ga, Gen<B> gb, Func<A, B, C> f) =>
        r =>
        {
            var (a, rng1) = ga(r);
            var (b, rng2) = gb(rng1);
            return (f(a,b), rng2);
        };

    // or it can be defined with selectMany/bind (and this is general for all monads)
    public static Gen<C> BiMapM<A, B, C>(this Gen<A> @ga, Gen<B> gb, Func<A, B, C> fc) =>
        ga.SelectMany(a => gb.Select(b => fc(a, b)));

    /// <summary>
    /// Convert an IEnumerable{Gen{A}} to Gen{IEnumerable{A}} 
    /// </summary>
    /// <param name="actions">the list of Gen{A}s</param>
    /// <typeparam name="A">the type contained in the Gen{A}</typeparam>
    /// <returns></returns>
    public static Gen<IEnumerable<A>> Sequence<A>(this IEnumerable<Gen<A>> actions) =>
        actions.Traverse(x => x);
    
    public static Gen<IEnumerable<B>> Traverse<A,B>(this IEnumerable<A> las, Func<A, Gen<B>> f) =>
        las.Aggregate(Return(Nil<B>()), (acc, a) => f(a).BiMap(acc, (b, xs) => xs.Append(b)));

    public static Gen<TResult> Apply<T, TResult>(this Gen<Func<T, TResult>> self, Gen<T> source) =>
        self.BiMap(source, (f, a) => f(a));
    
    public static Gen<TResult> ApplyM<T, TResult>(this Gen<Func<T, TResult>> self, Gen<T> source) => 
        self.SelectMany(source.Select);
    
    public static Gen<string> StringN(int n) =>
        ListOfN(n, Char).Select(string.Concat);

    public static SGen<string> String => StringN;  
    
    public static Gen<string> AlphaNumericStringN(int n) => 
        ListOfN(n, Char.Union(Digit)).Select(string.Concat);

    public static Gen<T> OneOf<T>(params T[] choices) => 
        Choose(0, choices.Length).Select(x => choices[x]);
    public static Gen<T> OneOf<T>(params Gen<T>[] choices) => 
        Choose(0, choices.Length).SelectMany(x => choices[x]);
    

    /// <summary>
    /// Generates a random string based a simple pattern made of
    /// single character symbols
    /// A -> character a-zA-z
    /// 9 -> digit 0-9
    /// e.g AA99 -> xH83
    /// all other character in the pattern map to themselves
    /// </summary>
    /// <param name="pattern">the pattern</param>
    /// <returns>a random string based on the pattern</returns>
    public static Gen<string> FromPattern(string pattern)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        if (pattern.Length == 0)
            return Return("");
        
        // the easy way let the HOF do the work
        return pattern.Select(c => c switch
        {
            '?' => Char,
            '#' => Digit,
            _ => Return(c)
        }).Sequence().Select(string.Concat);
    }

    public static Gen<string> FromPatternAlternative(string pattern)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        /* the hard way, manually managing the state (rng) */
        return rng =>
        {
            var builder = new StringBuilder(pattern.Length);
            var seed = (builder, rng);
            var res = pattern.Aggregate(seed, (acc, c) =>
            {
                var v = c switch
                {
                    'A' => Char(acc.rng),
                    '9' => Digit(acc.rng),
                    _ => (c, acc.rng)
                };
                return (acc.builder.Append(v.Item1), v.Item2);
            });

            return (res.builder.ToString(), res.rng);
        };
    }

    public static Gen<int> Choose(int start, int stopExclusive) =>
        Select(Rng.NonNegativeInt, n => start + n % (stopExclusive - start));

    public static readonly Gen<string> Postcode = 
        from pattern in OneOf("??##", "??#", "?##", "?#", "?#?", "??#?")
        from outCode in FromPattern(pattern)
        from inCode in FromPattern("#??")
        select $"{outCode} {inCode}".ToUpper();

    public static A Run<A>(this IRng rng, Gen<A> gen) => gen(rng).Item1;
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
        var postcode = Rng.Simple(1234).Run(Postcode);
        var g = ListOfN(10, Digit)(Rng.Simple(42));
        g.Item1.Print("10 random digits");
        var doubles = Gen.Double.ListOfN(100)(Rng.Simple(57));
        doubles.Item1.Print("10 random doubles");

        var weighted = Weighted((Return("A"), 0.25), (Return("B"), 0.75)).ListOfN(1000)(Rng.Simple(43566754));
        weighted.Item1.Print("100 weighted a&bs");
        Console.WriteLine("A count = " + weighted.Item1.Count(s => s == "A"));
        
        var r = Rng.Simple(420);

        var sg = AlphaNumericStringN(10);
        var s = sg(r);
        
        var tree = TreeFunctor.Tree.Create(42,
            TreeFunctor.Tree.Create(123, TreeFunctor.Tree.Leaf(1), TreeFunctor.Tree.Leaf(2)),
            TreeFunctor.Tree.Create(234));
        
        var mappedTree = tree.Select(i => i.ToString());

        Console.WriteLine(mappedTree);

        string EssBefore(string i) => $"s-{i}";
        string QueAfter(string s) => s + "-q";
        string BothEssAndQue(string s) => EssBefore(QueAfter(s));

        var taintingFunctions = Gen.OneOf(EssBefore, QueAfter, BothEssAndQue);
        var taintedPostcodes = taintingFunctions.Apply(Postcode);

        var listOfTaintedPostcodes = ListOfN(25, taintedPostcodes)(r);

        listOfTaintedPostcodes.Item1.Print();

// lift a function to the Gen<> world apply an argument (i.e call it)
        var genF = Return(EssBefore).Apply(Return("123"))(r);
        Console.WriteLine("lift and apply = " + genF);

        Return(EssBefore).BiMap(Return("123"), (a, b) => a(b));

// choose example
        var lostOfInts = Choose(1, 10).ListOfN(25); //(new Random(2));
//lostOfInts.Print();
    }
} 