using Dunet;

namespace TestBed.Dunet;

[Union]
public partial record Option<T>
{
    partial record Some(T Value);
    partial record None();
}

public static class OptionExtensions
{
    public static Option<B> Select<A, B>(this Option<A> opt, Func<A, B> f)
    {
        return opt.Match<Option<B>>(
            some: a => new Option<B>.Some(f(a.Value)), 
            none: _ => new Option<B>.None());
    }

    public static Option<B> SelectMany<A, B>(this Option<A> opt, Func<A, Option<B>> f)
    {
        return opt.Match<Option<B>>(
            some: a => f(a.Value), 
            none: _ => new Option<B>.None());
    }
}