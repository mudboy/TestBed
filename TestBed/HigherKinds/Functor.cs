namespace TestBed.HigherKinds;

public interface Functor<F>  
    where F : Functor<F>
{
    public static abstract K<F, B> Map<A, B>(Func<A, B> f, K<F, A> ma);
}

public static class FunctorExtensions
{
    public static Func<T1, Func<T2, R>> curry<T1, T2, R>(Func<T1, T2, R> f) =>
        a => b => f(a, b);
    public static Func<T1, Func<T2, Func<T3, R>>> curry<T1, T2, T3, R>(Func<T1, T2, T3, R> f) =>
        a => b => c => f(a, b, c);
    
    public static K<F, B> Map<F, A, B>(this Func<A, B> f, K<F, A> ma) 
        where F : Functor<F> =>
        F.Map(f, ma);
    
    public static K<F, Func<B, C>> Map<F, A, B, C>(
        this Func<A, B, C> f, K<F, A> ma) 
        where F : Functor<F> =>
        F.Map(x => curry(f)(x), ma);    
    
    public static K<F, Func<B, Func<C, D>>> Map<F, A, B, C, D>(
        this Func<A, B, C, D> f, K<F, A> ma) 
        where F : Functor<F> =>
        F.Map(x => curry(f)(x), ma);

    public static K<F, B> Map<F, A, B>(this K<F, A> ma, Func<A, B> f)
    where F : Functor<F>
    {
        return F.Map(f, ma);
    }
}