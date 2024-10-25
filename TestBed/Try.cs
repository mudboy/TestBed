namespace TestBed;

public abstract record Try<T>
{
    public abstract Try<U> Select<U>(Func<T, U> f);
}

public sealed record Success<T>(T Value) : Try<T>
{
    public override Try<U> Select<U>(Func<T, U> f) => new Success<U>(f(Value));
}

public sealed record Failure<T>(Exception Ex): Try<T>
{
    public override Try<U> Select<U>(Func<T, U> f)
    {
        return this.AsInstanceOf<Try<U>>();
    }
}

public static class Extens
{
    public static T AsInstanceOf<T>(this object self)
    {
        return (T) self;
    }
}

public static partial class Main
{
    public static void DoEither()
    {
        var fail = new Failure<int>(new Exception("Bang!"));

        var xx = fail.Select(x => x + 10);
        
    }
}