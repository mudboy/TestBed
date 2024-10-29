namespace Monads;

public abstract class Either<A, B>
{
    public abstract bool IsLeft { get; }
    public abstract bool IsRight { get; }

    public abstract C Match<C>(Func<A, C> left, Func<B, C> right);
    public abstract void Match(Action<A> left, Action<B> right);

    public abstract Either<A, C> Select<C>(Func<B, C> f);
    public abstract Either<A, C> SelectMany<C>(Func<B, Either<A,C>> f);
    
    public abstract bool Contains(B elem);
    public abstract bool Exists(Func<B, bool> predicate);
    public abstract Either<A, B> FilterOrElse(Func<B, bool> predicate, Func<A> zero);
    public abstract B GetOrElse(B alternative);
    public abstract B GetOrElse(Func<B> alternative);
    
    public Either<B, A> Swap() =>
        Match(
            left: Either.Right<B, A>,
            right: Either.Left<B, A>);

    public LeftProjection<A, B> Left => new(this);
    
    public static implicit operator Either<A, B>(B value) => new Right<A, B>(value);
    public static implicit operator Either<A, B>(A e) => new Left<A, B>(e);
}

public static class Either
{
    public static Either<A, B> Left<A, B>(A value) => new Left<A, B>(value);
    public static Either<A, B> Right<A, B>(B value) => new Right<A, B>(value);
}

public class Left<A, B>(A value) : Either<A, B>
{
    public override bool IsLeft => true;
    public override bool IsRight => false;
    public override C Match<C>(Func<A, C> left, Func<B, C> right) => left(value);
    public override void Match(Action<A> left, Action<B> right) => left(value);
    public override Either<A, C> Select<C>(Func<B, C> f) => new Left<A, C>(value);
    public override Either<A, C> SelectMany<C>(Func<B, Either<A, C>> f) => new Left<A, C>(value);
    public override bool Contains(B elem) => false;
    public override bool Exists(Func<B, bool> predicate) => false;
    public override Either<A, B> FilterOrElse(Func<B, bool> predicate, Func<A> zero) => this;
    public override B GetOrElse(B alternative) => alternative;
    public override B GetOrElse(Func<B> alternative) => alternative();
} 

public class Right<A, B>(B value) : Either<A, B>
{
    public override bool IsLeft => false;
    public override bool IsRight => true;
    public override C Match<C>(Func<A, C> left, Func<B, C> right) => right(value);
    public override void Match(Action<A> left, Action<B> right) => right(value);
    public override Either<A, C> Select<C>(Func<B, C> f) => new Right<A, C>(f(value));
    public override Either<A, C> SelectMany<C>(Func<B, Either<A, C>> f) => f(value);
    public override bool Contains(B elem) => elem.Equals(value);
    public override bool Exists(Func<B, bool> predicate) => predicate(value);
    public override Either<A, B> FilterOrElse(Func<B, bool> predicate, Func<A> zero) =>
        !predicate(value) ? new Left<A, B>(zero()) : this;
    public override B GetOrElse(B alternative) => value;
    public override B GetOrElse(Func<B> alternative) => value;
}

public class LeftProjection<A, B>(Either<A, B> e)
{
    public Either<C, B> Select<C>(Func<A, C> f) =>
        e.Match(
            left: l => Either.Left<C, B>(f(l)),
            right: r => Either.Right<C, B>(r));

    public Either<C, B> SelectMany<C>(Func<A, Either<C, B>> f) =>
        e.Match(
            left: l => f(l),
            right: r => Either.Right<C, B>(r));

    public bool Exists(Func<A, bool> predicate) =>
        e.Match(
            left: predicate,
            right: _ => false);

    public Either<A, B> FilterOrElse(Func<A, bool> predicate, Func<B> zero)
    {
        return e.Match(
            left: l => !predicate(l) ? zero() : e,
            right: r => e);
    }
}