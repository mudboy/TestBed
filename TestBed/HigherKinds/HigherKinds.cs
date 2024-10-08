namespace TestBed.HigherKinds;

public static class Main 
{
    public static Func<T1, Func<T2, R>> curry<T1, T2, R>(Func<T1, T2, R> f) =>
        a => b => f(a, b);
    
    static int Multiply(int x, int y) =>
        x * y;    
    
    static int Multiply3(int x, int y, int z) =>
        x * y * z;
    
    public static void Do()
    {
        var j = new Just<int>(5);
        j.IsEmpty();
        

        var r = j.Fold(1, (i, x) => i + x);

        var lst = new List2<int>([1, 2, 3]);
        var bl = lst.IsEmpty();
        var jl = j.IsEmpty();

        var multiply = curry<int, int, int>(Multiply);
        var mx = new Just<int>(2);
        var my = new Just<int>(3);
        var mz = new Just<int>(4);
        var x = multiply.Map(mx).Apply(my);
        var xx = Multiply3;
        var xxx = xx.Map(mx).Apply(my).Apply(mz);
        var dd = multiply.Map(lst);

        var nio = DateTimeIO.Now.Map(dt => dt.AddDays(1));
    }
}
public interface SemiGroup<A> where A : SemiGroup<A>
{
    public static abstract A operator+ (A x, A y);
}

public record MyMoStr(string value) : Monoid<MyMoStr>
{
    public static MyMoStr operator +(MyMoStr x, MyMoStr y)
    {
        return new (x.value + y.value);
    }

    public static MyMoStr Empty => new("");
}

public interface Addable<SELF> where SELF : Addable<SELF>
{
    public static abstract SELF Empty { get; }
    public static abstract SELF Add(SELF x, SELF y);
}

public record MyString(string value) : Addable<MyString>
{
    public static MyString Empty => new("");
    public static MyString Add(MyString x, MyString y)
    {
        return new(x.value + y.value);
    }
}

public record MyList<A>(A[] values) : 
    Addable<MyList<A>>
{
    public static MyList<A> Empty { get; } = new ([]);
    
    public static MyList<A> Add(MyList<A> x, MyList<A> y) => 
        new (x.values.Concat(y.values).ToArray());
}

public static class Funcs
{
    public static B FoldMap<A, B>(this IEnumerable<A> xs, Func<A, B> f) where B : Monoid<B>
    {
        return xs.Aggregate(B.Empty, (current, x) => current + f(x));
    }
}

