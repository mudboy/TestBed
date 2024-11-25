using System.Diagnostics.CodeAnalysis;

namespace Monads;

public abstract class Result<A> : IEquatable<Result<A>>
{
    public abstract bool IsSuccess { get; }
    public abstract B Match<B>(Func<A, B> success, Func<Error, B> failure);
    public abstract void Match(Action<A> success, Action<Error> failure);
    public abstract Unit OnFailure(Action<Error> action);
    public abstract Result<B> SelectMany<B>(Func<A, Result<B>> bind);
    public abstract Result<B> Select<B>(Func<A, B> map);
    public abstract Result<C> Map2<B, C>(Result<B> rb, Func<A, B, C> f);
    public abstract A GetOrElse(A alternative);
    public abstract A GetOrElse(Func<A> alternative);
    public abstract Result<A> OrElse(Result<A> alternative);
    public abstract Result<A> OrElse(Func<Result<A>> alternative);
    public abstract IEnumerable<A> AsEnumerable();

    public static implicit operator Result<A>(A value) => new Success<A>(value);
    public static implicit operator Result<A>(ImplicitFailure failure) => new Failure<A>(failure.Error);
    
    public static Result<A> operator >> (Result<A> lhs, Func<A, Result<A>> rhs) => lhs.SelectMany(rhs);
    
    public bool Equals(Result<A>? other)
    {
        if (other is null) return false;
        if (this is Success<A> a && other is Success<A> b)
            return a.Value.Equals(b.Value);
        if (this is Failure<A> fa && other is Failure<A> fb)
            return fa.Error.Equals(fb.Error);
        return false;
    }

    protected abstract bool IsEqualTo(Result<A>? other);

    public override bool Equals(object? obj) => 
        ReferenceEquals(this, obj) || obj is Result<A> other && Equals(other);

    public override int GetHashCode() =>
        ReturnHashCode();

    protected abstract int ReturnHashCode();

    public static bool operator ==(Result<A>? left, Result<A>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Result<A>? left, Result<A>? right)
    {
        return !Equals(left, right);
    }

    internal sealed class Success<A>(A value) : Result<A>
    {
        [NotNull]
        public readonly A Value = value;

        public override bool IsSuccess => true;
        public override B Match<B>(Func<A, B> success, Func<Error, B> failure) => success(Value);
        public override void Match(Action<A> success, Action<Error> failure) => success(Value);
        public override Unit OnFailure(Action<Error> action) => Unit.Value;
        public override Result<B> SelectMany<B>(Func<A, Result<B>> bind) => bind(Value);
        public override Result<B> Select<B>(Func<A, B> map) => Result.Success(map(Value));
        public override Result<C> Map2<B, C>(Result<B> rb, Func<A, B, C> f) =>
            rb.Match(
                success: b => Result.Success(f(Value, b)),
                failure: er => new Failure<C>(er));
        public override A GetOrElse(A alternative) => Value;
        public override A GetOrElse(Func<A> alternative) => Value;
        public override Result<A> OrElse(Result<A> alternative) => this;
        public override Result<A> OrElse(Func<Result<A>> alternative) => this;
        public override IEnumerable<A> AsEnumerable() => [Value];
        protected override bool IsEqualTo(Result<A>? other) => other is Success<A> success && Value.Equals(success.Value);

        protected override int ReturnHashCode() => Value.GetHashCode();

        public override string ToString() => $"Success({Value})";
    }

    internal sealed class Failure<A>(Error error) : Result<A>
    {
        public  readonly Error Error = error;
        public override bool IsSuccess => false;
        public override B Match<B>(Func<A, B> success, Func<Error, B> failure) => failure(Error);
        public override void Match(Action<A> success, Action<Error> failure) => failure(Error);
        public override Unit OnFailure(Action<Error> action) => action.AsFunc(Error);
        public override Result<B> SelectMany<B>(Func<A, Result<B>> bind) => new Failure<B>(Error);
        public override Result<B> Select<B>(Func<A, B> map) => new Failure<B>(Error);
        public override Result<C> Map2<B, C>(Result<B> rb, Func<A, B, C> f) =>
            rb.Match( // check to see if rb is also a failure 
                success: _ => Result.Failure(Error),
                failure: eb => Result.Failure(string.Join(", ", Error.Message, eb.Message)));
        public override A GetOrElse(A alternative) => alternative;
        public override A GetOrElse(Func<A> alternative) => alternative();
        public override Result<A> OrElse(Result<A> alternative) => alternative;
        public override Result<A> OrElse(Func<Result<A>> alternative) => alternative();
        public override IEnumerable<A> AsEnumerable() => [];
        protected override bool IsEqualTo(Result<A>? other) =>
            other is Failure<A> failure && Error.Equals(failure.Error);

        protected override int ReturnHashCode() => Error.GetHashCode();

        public override string ToString() => $"Failure({Error})";
    }


}

public readonly record struct ImplicitFailure(Error Error);

public static class Result
{
    public static Result<A> Success<A>(A value) => new Result<A>.Success<A>(value);
    public static ImplicitFailure Failure(Error error) => new(error);
    public static Result<A> Failure<A>(Error error) => new Result<A>.Failure<A>(error);
    public static ImplicitFailure Failure(string message) => new(new Error(message));
}
public static class ResultExtensions
{
    public static Unit AsFunc<A>(this Action<A> action, A a)
    {
        action(a);
        return Unit.Value;
    }
    
    public delegate Result<T> Validator<T>(T t);
    public static Validator<T> HarvestErrors<T>(params Validator<T>[] validators) =>
        t => validators
            .Traverse(validate => validate(t))
            .Select(_ => t);

    public static Result<IEnumerable<R>> Traverse<T, R>(this IEnumerable<T> ts, Func<T, Result<R>> f) =>
        ts.Aggregate(
            Result.Success(Enumerable.Empty<R>()),
            (acc, a) =>
                acc.Map2(f(a), (rs, r) => rs.Append(r)));
    
    public static Result<IEnumerable<T>> Sequence<T>(this IEnumerable<Result<T>> results) => 
        results.Traverse(x => x);
    
    public static Result<C> SelectMany<A, B, C>(this Result<A> ra, Func<A, Result<B>> bind, Func<A, B, C> project) =>
        ra.SelectMany(x => bind(x).Select(y => project(x, y)));
}
