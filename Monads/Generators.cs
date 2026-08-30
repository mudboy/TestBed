using System.Text;
using Utils;
using static Monads.Gen;
using static Utils.EnumerableExtensions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Monads;

// converted from
// https://github.com/fpinscala/fpinscala/blob/second-edition/src/main/scala/fpinscala/answers/testing/Gen.scala

public delegate (T, IRng) Gen<T>(IRng r);

public delegate Gen<T> SGen<T>(int n);


public class IdxGen<A>(Func<IRng, (A, IRng)> f)
{
    public (A, IRng) this[IRng r] => f(r);

    public static IdxGen<B> Return<B>(B value) => new(r => (value, r));
    public static IdxGen<int> Int => new (r => r.NextInt());

    public static implicit operator IdxGen<A>(A a) => Return(a);
}

public static class Gen
{
    public static Gen<int> Int => Rng.Int;
    public static Gen<int> NaturalInt => Rng.NaturalNumber;
    public static Gen<int> NonNegativeInt => Rng.NonNegativeInt;
    public static Gen<double> Double => Rng.Double;
    public static Gen<bool> Bool => Rng.Bool;
    
    public static Gen<char> Digit => 
        Choose(48, 58).Select(x => (char)x);
    
    public static Gen<char> Char => 
        Choose(65, 91)
            .Union(Choose(97, 123))
            .Select(x => (char)x);

    public static Gen<char> AlphaNumeric => Char.Union(Digit);

    public static Gen<IEnumerable<A>> ListOfN<A>(int n, Gen<A> g) =>
        Fill(n, g).Sequence();

    extension<A>(Gen<A> self)
    {
        public Gen<List<A>> ListOfN(int n) => 
            ListOfN(n, self).Select(l => l.ToList());

        public Gen<List<A>> ListOfN(Gen<int> size) => 
            size.SelectMany(x => self.ListOfN(Math.Max(0, x)));

        public SGen<List<A>> List() => 
            n => ListOfN(n, self).Select(l => l.ToList());

        public SGen<List<A>> NonEmptyList() => 
            n => ListOfN(Math.Max(n, 1), self).Select(l => l.ToList());

        public SGen<A> UnSized() => 
            _ => self;

        public Gen<IEnumerable<A>> NonEmptyEnumerable() =>
            Choose(1, 127).SelectMany(i => ListOfN(i, self));

        public Gen<R> Select<R>(Func<A, R> f) =>
            self.SelectMany(a => Return(f(a)));

        public Gen<R> SelectMany<R>(Func<A, Gen<R>> f) =>
            r =>
            {
                var (v, rng) = self(r);
                return f(v)(rng);
            };

        public Gen<C> SelectMany<B, C>(Func<A, Gen<B>> fromFirst, Func<A, B, C> project) =>
            self.SelectMany(x => fromFirst(x).Select(y => project(x, y)));
        
        public Gen<A> Union(Gen<A> b) =>
            Bool.SelectMany(x => x ? self : b);
    }
    
    public static Gen<T> Return<T>(T value) => 
        rng => (value, rng);
    

    
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

    public static Gen<A> Weighted2<A>(params (A a, int weight)[] items) => 
        Weighted2(items.Select(tuple => (Return(tuple.a), tuple.weight)).ToArray());
    
    // map(select) can be defined in terms of BiMap(map2) and Unit
    extension<A>(Gen<A> ga)
    {
        public Gen<B> Map<B>(Func<A, B> f) =>
            ga.Map2(Return<B>(default!), (a, _) => f(a));

        // BiMap(map2) can be defined directly for this type and is applicative
        public Gen<C> Map2<B, C>(Gen<B> gb, Func<A, B, C> f) =>
            r =>
            {
                var (a, rng1) = ga(r);
                var (b, rng2) = gb(rng1);
                return (f(a,b), rng2);
            };

        // or it can be defined with selectMany/bind (and this is general for all monads)
        public Gen<C> BiMapM<B, C>(Gen<B> gb, Func<A, B, C> fc) =>
            ga.SelectMany(a => gb.Select(b => fc(a, b)));

        public A Run(IRng r) => ga(r).Item1;
    }
    
    
    /// <param name="actions">the list of Gen{A}s</param>
    /// <typeparam name="A">the type contained in the Gen{A}</typeparam>
    extension<A>(IEnumerable<Gen<A>> actions)
    {
        /// <summary>
        /// Convert an IEnumerable[Gen[A]] to Gen[IEnumerable[A]] 
        /// </summary>
        /// <returns></returns>
        public Gen<IEnumerable<A>> Sequence() =>
            actions.Traverse(x => x);

        public Gen<IEnumerable<A>> Sequence2() => 
            actions.Aggregate(
                Return(Nil<A>()),
                (acc, a) => 
                    acc.SelectMany(xs => a.Select(xs.Append)));

        public Gen<IEnumerable<A>> Sequence3() =>
            actions.Aggregate(
                Return(Nil<A>()), 
                (acc, a) => 
                    acc.Map2(a, (xs, x) => xs.Append(x)));

        public Gen<IEnumerable<A>> Sequence4() =>
            actions.Aggregate(
                Return(Nil<A>()),
                (acc, a) =>
                    Return(Append<A>()) 
                        .Apply(acc)
                        .Apply(a));
    }

    // Sequence can be defined as a SelectMany then select this version is monadic i.e. each step depended on the last

    // Here we use and actual lifted function and apply method

    public static async Task<S> Fold<A, S>(this Task<A> task, S initial, Func<S, A, S> folder)
    {
        var val = await task;
        return folder(initial, val);
    }
    
    private static Func<IEnumerable<T>, T, IEnumerable<T>> Append<T>()
        => (ts, t) => ts.Append(t);
    
    // direct implementation, like sequence but with the added function call.
    extension<A>(IEnumerable<A> las)
    {
        public Gen<IEnumerable<B>> Traverse<B>(Func<A, Gen<B>> f) =>
            las.Aggregate(
                Return(Nil<B>()), 
                (acc, a) => 
                    f(a).Map2(acc, (b, xs) => xs.Append(b)));

        public Gen<IEnumerable<B>> Traverse2<B>(Func<A, Gen<B>> f) =>
            las.Select(f).Sequence3();
    }

    // or can be just as simple as a map then sequence

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
            var seed = (builder, rng);
            var res = pattern.Aggregate(seed, (acc, c) =>
            {
                var v = c switch
                {
                    'A' => Char(acc.rng),
                    '9' => Digit(acc.rng),
                    _ => (c, acc.rng)
                };
                return (acc.builder.Append(v.Item2), v.Item2);
            });

            return (res.builder.ToString(), res.rng);
        };
    }

    public static Gen<int>  Choose(int start, int stopExclusive) =>
        Select(Rng.NonNegativeInt, n => start + n % (stopExclusive - start));

    public static readonly Gen<string> Postcode = 
        from pattern in OneOf("??##", "??#", "?##", "?#", "?#?", "??#?")
        from outCode in FromPattern(pattern)
        from inCode in FromPattern("#??")
        select $"{outCode} {inCode}".ToUpper();
}

public static class SGen
{
    extension<A>(SGen<A> self)
    {
        public SGen<B> Select<B>(Func<A, B> f) => n => self(n).Select(f);

        public SGen<B> SelectMany<B>(Func<A, SGen<B>> f) =>
            n => self(n).SelectMany(x => f(x)(n));

        public Gen<A> Apply(int n) => self(n);
    }

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

        var taintingFunctions = OneOf(EssBefore, QueAfter, BothEssAndQue);
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