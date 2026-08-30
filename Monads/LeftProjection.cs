namespace Monads;

public class LeftProjection<A, B>(Either<A, B> e)
{
    public Either<C, B> Select<C>(Func<A, C> f) =>
        e.Match(
            left: l => Either.Left<C, B>(f(l)),
            right: Either.Right<C, B>);

    public Either<C, B> SelectMany<C>(Func<A, Either<C, B>> f) =>
        e.Match(
            left: f,
            right: Either.Right<C, B>);

    public bool Exists(Func<A, bool> predicate) =>
        e.Match(
            left: predicate,
            right: _ => false);

    public Either<A, B> FilterOrElse(Func<A, bool> predicate, Func<B> zero)
    {
        return e.Match(
            left: l => !predicate(l) ? zero() : e,
            right: _ => e);
    }
}