using Utils;

namespace TestBed;

public sealed class Foo<T>
{
    internal readonly T _value;

    public Foo(T value)
    {
        _value = value;
    }
    
    public Foo<B> FlatMap<B>(Func<T, Foo<B>> f) => f(_value);
    
    public Foo<B> Map<B>(Func<T, B> f) => FlatMap(x => FooExt.Return(f(x)));

    public Foo<C> Map2<B, C>(Foo<B> fb, Func<T, B, C> f) => FlatMap(a => fb.Map(b => f(a, b)));

    public Foo<C> Map2a<B, C>(Foo<B> fb, Func<T, B, C> f) => FooExt.Return(f(_value, fb._value));
    
    public static void Main()
    {
        var x = FooExt.Return(1);

        var d = x.Map(FooExt.Return);
        var dd = d.FlatMap(x => x);
    }
}

public static class FooExt
{
    public static Foo<IEnumerable<B>> Traverse<A,B>(this IEnumerable<A> ts, Func<A, Foo<B>> f) => 
        ts.Aggregate(Return(Enumerable.Empty<B>()), 
            (bs, a) => f(a).Map2a(bs, (b, acc) => acc.Append(b)));

    public static Foo<IEnumerable<T>> Sequence<T>(this IEnumerable<Foo<T>> ts) => 
        ts.Traverse(x => x);
    
    public static Foo<A> Return<A>(A value) => new(value);
    public static Foo<B> Apply<A, B>(this Foo<Func<A, B>> f, Foo<A> a) => Return(f._value(a._value));
    public static Foo<Func<B, C>> Apply<A, B, C>(this Foo<Func<A, B, C>> f, Foo<A> a) => Apply(f.Map(FuncExt.Curry), a);
    public static Foo<Func<B, C, D>> Apply<A, B, C, D>(this Foo<Func<A, B, C, D>> f, Foo<A> a) => Apply(f.Map(FuncExt.CurryFirst), a);

    public static Foo<C> Map2ViaApply<A, B, C>(this Foo<A> fa, Foo<B> fb, Func<A, B, C> f) =>
        Return(f).Apply(fa).Apply(fb);
    
    public static Foo<D> Map3ViaApply<A, B, C, D>(this Foo<A> fa, Foo<B> fb, Foo<C> fc, Func<A, B, C, D> f) =>
        Return(f).Apply(fa).Apply(fb).Apply(fc);
    

}