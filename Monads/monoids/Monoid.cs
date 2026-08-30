namespace Monads.monoids;

public interface SemiGroup<A>
{
    A Combine(A a, A b);
}

public interface Monoid<A> : SemiGroup<A>
{
    A Zero { get; }
}

public class StringMonoid : Monoid<string>
{
    public string Combine(string a, string b) => string.Concat(a, b);
    public string Zero => string.Empty;
}

public class IntAddMonoid : Monoid<int>
{
    public int Combine(int a, int b) => a + b;
    public int Zero => 0;
}

public class ListMonoid<A> : Monoid<List<A>>
{
    public List<A> Combine(List<A> a, List<A> b) => a.Concat(b).ToList();
    public List<A> Zero => [];
}

public class BoolAndMonoid : Monoid<bool>
{
    public bool Combine(bool a, bool b) => a && b;
    public bool Zero => true;
}