namespace TestBed.Zippers;

public abstract record Tree<A>()
{
    public static implicit operator Tree<A>(EmptyTree _) => new Empty<A>();

    public static implicit operator Tree<A>((A value, Tree<A> left, Tree<A> right) p) =>
        new Node<A>(p.value, p.left, p.right);

    public abstract B Match<B>(Func<A, Tree<A>, Tree<A>, B> whenNode, Func<B> whenEmpty);
};

internal sealed record Empty<A> : Tree<A>
{
    public override B Match<B>(Func<A, Tree<A>, Tree<A>, B> whenNode, Func<B> whenEmpty)
    {
        return whenEmpty();
    }
}

internal sealed record Node<A>(A value, Tree<A> left, Tree<A> right) : Tree<A>
{
    public override B Match<B>(Func<A, Tree<A>, Tree<A>, B> whenNode, Func<B> whenEmpty)
    {
        return whenNode(value, left, right);
    }
}

public sealed record EmptyTree;

public static class Tree
{
    public static EmptyTree Empty => new();
    public static Tree<A> Leaf<A>(A value) => new Node<A>(value, Empty, Empty);
    public static Tree<A> Node<A>(A value, Tree<A> left, Tree<A> right) => new Node<A>(value, left, right);
    
}

public abstract record Crumb<T>
{
    public abstract R Match<R>(Func<T, Tree<T>, R> left, Func<T, Tree<T>, R> right);
}
public sealed record CrumbLeft<T>(T x, Tree<T> r) : Crumb<T>
{
    public override R Match<R>(Func<T, Tree<T>, R> left, Func<T, Tree<T>, R> right) => left(x, r);
}

public sealed record CrumbRight<T>(T x, Tree<T> l) : Crumb<T>
{
    public override R Match<R>(Func<T, Tree<T>, R> left, Func<T, Tree<T>, R> right)
    {
        return right(x, l);
    }
}

public record Zipper<A, B>(A focus, List<B> breadcrumbs);

public sealed record TZip<A>(Tree<A> focus, List<Crumb<A>> crumbs) :
    Zipper<Tree<A>, Crumb<A>>(focus, crumbs)
{
    public static implicit operator TZip<A>((Tree<A> focus, List<Crumb<A>> crumbs) p) => new(p.focus, p.crumbs);
}
public static class TreeZipper
{
    public static TZip<A> Create<A>() => new(Tree.Empty, []);
    public static TZip<A> Create<A>(Tree<A> tree) => new (tree, []);
    
    private static List<Crumb<X>> Add<X>(List<Crumb<X>> ls, Crumb<X> x)
    {
        return ls.Prepend(x).ToList();
    }
    
    public static TZip<A>? GoLeft<A>(TZip<A> z)
    {
        return z switch
        {
            (Empty<A>, _) => null,
            (Node<A> (var x, var l, var r), var bs) => 
                new TZip<A>(l, Add(bs, new CrumbLeft<A>(x, r))),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static TZip<A>? GoRight<A>(TZip<A> z)
    {
        return z switch
        {
            (Empty<A>, _) => null,
            (Node<A> (var x, var l, var r), var bs) => 
                new TZip<A>(r, Add(bs, new CrumbRight<A>(x, l))),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static TZip<A>? Modify<A>(TZip<A> z, Func<A, A> f)
    {
        return z switch
        {
            (Empty<A>, _) => null,
            (Node<A> t, var bs) => new TZip<A>(Tree.Node(f(t.value), t.left, t.right), bs),
            _ => throw new ArgumentOutOfRangeException(nameof(z), z, null)
        };
    }

    public static TZip<A>? Topmost<A>(TZip<A> z)
    {
        var g = GoUp(z);
        return g is null ? z : Topmost(g);
    }

    public static TZip<A>? GoUp<A>(TZip<A> z)
    {
        return z switch
        {
            (Empty<A>, _) => null,
            (Node<A>, []) => null,
            (Node<A> t,  [var b, .. var bs]) =>
                b.Match(
                    left: (x, r) => (Tree.Node(x, t, r), bs),
                    right: (x, l) => (Tree.Node(x, l, t), bs)),
            _ => throw new ArgumentOutOfRangeException(nameof(z), z, null)
        };
    } 
}