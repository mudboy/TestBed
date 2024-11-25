namespace Monads;

// see https://github.com/scala/scala/blob/v2.13.14/src/library/scala/util/Try.scala

public abstract record Try<A>
{
    public abstract bool IsSuccess { get; }
    public abstract A GetOrElse(A @default);
    public abstract Try<A> OrElse(Func<Try<A>> @default);
    public abstract A Get();
    public abstract Try<B> SelectMany<B>(Func<A, Try<B>> f);
    public abstract Try<B> Select<B>(Func<A, B> f);
    public abstract Try<A> Where(Func<A, bool> p);
    public abstract Try<A> Recover(Func<Exception, A> pf);
    public abstract Try<A> RecoverWith(Func<Exception, Try<A>> pf);
    public static implicit operator Try<A>(ImplicitException ie) => new Failure<A>(ie.Ex);
    
    internal sealed record Success<A>(A Value) : Try<A>
    {
        public override bool IsSuccess => true;
        public override A GetOrElse(A @default) => Value;
        public override Try<A> OrElse(Func<Try<A>> @default) => this;
        public override A Get() => Value;

        public override Try<B> SelectMany<B>(Func<A, Try<B>> f)
        {
            try
            {
                return f(Value);
            }
            catch (Exception e)
            {
                return new Failure<B>(e);
            }
        }

        public override Try<B> Select<B>(Func<A, B> f)
        {
            try
            {
                return new Success<B>(f(Value));
            }
            catch (Exception e)
            {
                return new Failure<B>(e);
            }
        }

        public override Try<A> Where(Func<A, bool> p)
        {
            try
            {
                if (p(Value))
                    return this;
                return new Failure<A>(new InvalidOperationException("Predicate does not hold for " + Value));
            }
            catch (Exception e)
            {
                return new Failure<A>(e);
            }
        }

        public override Try<A> Recover(Func<Exception, A> pf) => this;
        public override Try<A> RecoverWith(Func<Exception, Try<A>> pf) => this;
    }

    internal sealed record Failure<A>(Exception Ex) : Try<A>
    {
        public override bool IsSuccess => false;
        public override A GetOrElse(A @default) => @default;

        public override Try<A> OrElse(Func<Try<A>> @default)
        {
            try
            {
                return @default();
            }
            catch (Exception e)
            {
                return new Failure<A>(e);
            }
        }

        public override A Get() => throw Ex;

        public override Try<B> SelectMany<B>(Func<A, Try<B>> f) => new Failure<B>(Ex);
        public override Try<B> Select<B>(Func<A, B> f) => new Failure<B>(Ex);
        public override Try<A> Where(Func<A, bool> p) => this;

        public override Try<A> Recover(Func<Exception, A> pf)
        {
            try
            {
                return new Success<A>(pf(Ex));
            }
            catch (Exception e)
            {
                return new Failure<A>(e);
            }
        }

        public override Try<A> RecoverWith(Func<Exception, Try<A>> pf)
        {
            try
            {
                return pf(Ex);
            }
            catch (Exception e)
            {
                return new Failure<A>(e);
            }
        }
    }
}

public readonly record struct ImplicitException(Exception Ex);

public static class Try
{
    public static Try<A> Success<A>(A value) => new Try<A>.Success<A>(value);
    public static ImplicitException Failure(Exception ex) => new(ex);
    public static Try<A> Failure<A>(Exception ex) => new Try<A>.Failure<A>(ex);
}

public static partial class Main
{
    public static void DoEither()
    {
        var fail = Try.Failure<int>(new Exception("Bang!"));

        var xx = fail.Select(x => x + 10);
        
    }
}