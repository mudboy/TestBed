namespace TestBed.HigherKinds;

public interface Monoid<A> : SemiGroup<A> where A : Monoid<A>
{
    public static abstract A Empty { get; }
}