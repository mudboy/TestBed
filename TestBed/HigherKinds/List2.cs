namespace TestBed.HigherKinds;

// data type
public sealed record List2<A>(A[] Items) : K<List2, A>;

// the trait impl 
public class List2 : 
    Mappable<List2>, 
    Traversable<List2>
{
    public static K<List2, B> Select<A, B>(K<List2, A> list, Func<A, B> f) =>
        new List2<B>(list.As().Items.Select(f).ToArray());

    public static S Fold<A, S>(K<List2, A> fa, S initial, Func<S, A, S> f) => 
        fa.As().Items.Aggregate(initial, f);

    public static bool IsEmpty<A>(K<List2, A> ta) => 
        ta.As().Items.Length == 0;

    public static int Count<A>(K<List2, A> ta) =>
        ta.As().Items.Length;

    public static int Sum<A>(K<List2, int> ta) =>
        ta.As().Items.Sum();

    public static bool All<A>(K<List2, A> ta, Func<A, bool> f) =>
        ta.As().Items.All(f);

    public static bool Any<A>(K<List2, A> ta, Func<A, bool> f) =>
        ta.As().Items.Any(f);

    public static bool Contains<A>(K<List2, A> ta, A value) where A : IEquatable<A> =>
        ta.As().Items.Contains(value);

    public static IEnumerable<A> AsEnumerable<A>(K<List2, A> ta) =>
        ta.As().Items;

    public static K<List2, B> Map<A, B>(Func<A, B> f, K<List2, A> ma)
    {
        return new List2<B>(ma.As().Items.Select(f).ToArray());
    }

    public static K<F, K<List2, B>> Traverse<F, A, B>(Func<A, K<F, B>> f, K<List2, A> ta) where F : Applicative<F>
    {
        throw new NotImplementedException();
    }

    private static K<F, List2<B>> cons<F, B, A>(K<F, List2<B>> arg1, A arg2) where F : Applicative<F>
    {
        throw new NotImplementedException();
    }
}

public static class Prelude
{
    public static List2<A> Cons<A>(this A head, List2<A> tail) => new(new[] { head }.Concat(tail.Items).ToArray());
}
// down caster
public static class List2Extensions
{
    public static List2<A> As<A>(this K<List2, A> ma) =>
        (List2<A>)ma;
}