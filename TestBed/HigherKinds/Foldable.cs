namespace TestBed.HigherKinds;

public interface Foldable<F> where F : Foldable<F>
{
    // method
    public static abstract S Fold<A, S>(K<F, A> fa, S initial, Func<S, A, S> f);
    
    // useful extensions with default impl
    public static virtual bool IsEmpty<A>(K<F, A> ta) =>
        F.Fold(ta, true, (_, _) => false);

    public static virtual int Count<A>(K<F, A> ta) =>
        F.Fold(ta, 0, (s, _) => s + 1);

    public static virtual int Sum<A>(K<F, int> ta) =>
        F.Fold(ta, 0, (s, x) => s + x);

    public static virtual A Fold<A>(K<F, A> ta)
        where A : Monoid<A> =>
        F.Fold(ta, A.Empty, (s, x) => s + x);

    public static virtual IEnumerable<A> AsEnumerable<A>(K<F, A> ta) => 
        ta.Fold(new List<A>(), Add);

    private static List<A> Add<A>(List<A> xs, A x)
    {
        xs.Add(x);
        return xs;
    }

    public static virtual bool All<A>(K<F, A> ta, Func<A, bool> f) =>
        F.Fold(ta, true, (s, x) => s && f(x));

    public static virtual bool Any<A>(K<F, A> ta, Func<A, bool> f) =>
        F.Fold(ta, false, (s, x) => s || f(x));

    public static virtual bool Contains<A>(K<F, A> ta, A value)
        where A : IEquatable<A> =>
        F.Any(ta, x => x.Equals(value)); 
}

public static class Foldable
{
    public static K<F, B> map<F, A, B>(Func<A, B> f, K<F, A> ma) 
        where F : Functor<F> =>
        F.Map(f, ma);
    
    public static S foldBack<T, A, S>(Func<S, A, S> f, S initialState, K<T, A> ta) 
        where T : Foldable<T> =>
        T.Fold(ta, initialState, f);
}

public static class FoldableExtensions
{
    public static S Fold<F, A, S>(this K<F, A> ta, S initial, Func<S, A, S> f)
        where F : Foldable<F> =>
        F.Fold(ta, initial, f);
    
    public static bool IsEmpty<F, A>(this K<F, A> ta) where F : Foldable<F> =>
        F.IsEmpty(ta);
}