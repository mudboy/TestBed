using Dunet;
using static TestBed.Dunet.Option;

namespace TestBed.Dunet;

[Union]
public partial record Option<T>
{
    partial record Some(T Value);
    partial record None;
    
    private static readonly None NoneInst = new();

    public static implicit operator Option<T>(OnlyNone _) => NoneInst;
}

public readonly record struct OnlyNone;

public static class Option
{
    private static readonly OnlyNone inst = new();
    public static Option<T> Some<T>(T value) => new Option<T>.Some(value);
    public static OnlyNone None { get; } = inst;
}

public static class OptionExtensions
{
    public static Option<B> Select<A, B>(this Option<A> opt, Func<A, B> f)
    {
        return opt.Match<Option<B>>(
            some: a => Some(f(a.Value)), 
            none: _ => None);
    }

    public static Option<B> SelectMany<A, B>(this Option<A> opt, Func<A, Option<B>> f)
    {
        return opt.Match<Option<B>>(
            some: a => f(a.Value), 
            none: _ => None);
    }
}