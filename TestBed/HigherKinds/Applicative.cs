namespace TestBed.HigherKinds;

public interface Applicative<F> : Functor<F>
    where F : Applicative<F>
{
    public static abstract K<F, A> Pure<A>(A value);

    public static abstract K<F, B> Apply<A, B>(K<F, Func<A, B>> mf, K<F, A> ma);
}

public static class ApplicativeExtensions
{
    public static K<F, B> Apply<F, A, B>(this K<F, Func<A, B>> mf, K<F, A> ma) 
        where F : Applicative<F> => 
        F.Apply(mf, ma);
}

public static class Applicative
{
    public static K<F, C> lift<F, A, B, C>(Func<A, B, C> f, K<F, A> fa, K<F, B> fb)
        where F : Applicative<F> =>
        f.Map(fa).Apply(fb);
}

public interface Traversable<T> : Functor<T>, Foldable<T>
    where T : Traversable<T>, Foldable<T>, Functor<T>
{
    public static abstract K<F, K<T, B>> Traverse<F, A, B>(
        Func<A, K<F, B>> f,
        K<T, A> ta)
        where F : Applicative<F>;
    
    public static virtual K<F, K<T, A>> Sequence<F, A>(
        K<T, K<F, A>> ta)
        where F : Applicative<F> =>
        Traversable.Traverse(x => x, ta);
}

public static class Traversable
{
    public static K<F, K<T, B>> Traverse<T, F, A, B>(
        Func<A, K<F, B>> f,
        K<T, A> ta)
        where T : Traversable<T>
        where F : Applicative<F> =>
        T.Traverse(f, ta);
}