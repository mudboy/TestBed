namespace Monads;

public abstract partial class Either<A, B> : IEquatable<Either<A, B>>
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

    public abstract Either<A, D> Map2<C, D>(Either<A, C> other, Func<B, C, D> func);
    
    public Either<B, A> Swap() =>
        Match(
            left: Either.Right<B, A>,
            right: Either.Left<B, A>);

    public LeftProjection<A, B> LeftProject => new(this);
    
    public static implicit operator Either<A, B>(B value) => new Right<A, B>(value);
    public static implicit operator Either<A, B>(A value) => new Left<A, B>(value);
    public static implicit operator Either<A, B>(ImplicitLeft<A> imp) => new Left<A, B>(imp.Value);
    public static implicit operator Either<A, B>(ImplicitRight<B> imp) => new Right<A, B>(imp.Value);

    public static explicit operator A(Either<A, B> either) => either.GetLeftValue;
    public static explicit operator B(Either<A, B> either) => either.GetRightValue;
    
    internal abstract A GetLeftValue { get; }
    internal abstract B GetRightValue { get; }

    public abstract bool Equals(Either<A, B>? other);

    public abstract override bool Equals(object? obj);

    public abstract override int GetHashCode();
}

public readonly ref struct ImplicitLeft<A>(A value)
{
    internal A Value { get; } = value;
}

public readonly ref struct ImplicitRight<A>(A value)
{
    internal A Value { get; } = value;
}

public static class Either
{
    public static Either<A, B> Left<A, B>(A value) => new Either<A,B>.Left<A, B>(value);
    public static Either<A, B> Right<A, B>(B value) => new Either<A,B>.Right<A, B>(value);
    public static ImplicitLeft<A> Left<A>(A value) => new(value);
    public static ImplicitRight<A> Right<A>(A value) => new(value);
}