namespace TestBed.HigherKinds;

// the data types
public abstract record Maybe<A> : K<Maybe, A>;
public sealed record Just<A>(A Value) : Maybe<A>;
public sealed record Nothing<A> : Maybe<A>;

// the trait impl
public class Maybe : 
    Mappable<Maybe>, 
    Foldable<Maybe>,
    Applicative<Maybe>
{
    public static K<Maybe, B> Select<A, B>(K<Maybe, A> list, Func<A, B> f) =>
        list.As() switch
        {
            Just<A> (var x) => new Just<B>(f(x)),
            Nothing<A>      => new Nothing<B>(),
            _ => throw new ArgumentOutOfRangeException()
        };

    public static S Fold<A, S>(K<Maybe, A> fa, S initial, Func<S, A, S> f)
    {
        return fa.As() switch
        {
            Just<A> (var a) => f(initial, a),
            Nothing<A> => initial,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static K<Maybe, B> Map<A, B>(Func<A, B> f, K<Maybe, A> ma) =>
        ma.As() switch
        {
            Just<A>(var a) => new Just<B>(f(a)),
            Nothing<A> => new Nothing<B>(),
            _ => throw new ArgumentOutOfRangeException()
        };

    public static K<Maybe, A> Pure<A>(A value) =>
        new Just<A>(value);

    public static K<Maybe, B> Apply<A, B>(K<Maybe, Func<A, B>> mf, K<Maybe, A> ma) =>
        mf.As() switch
        {
            Just<Func<A, B>> (var f) => ma switch
            {
                Just<A> (var a) => new Just<B>(f(a)),
                Nothing<A> => new Nothing<B>(),
                _ => throw new ArgumentOutOfRangeException(nameof(ma), ma, null)
            },

            Nothing<Func<A, B>> => new Nothing<B>(),
            _ => throw new ArgumentOutOfRangeException()
        };
}

// the down caster
public static class MaybeExtensions
{
    public static Maybe<A> As<A>(this K<Maybe, A> ma) =>
        (Maybe<A>)ma;
}