namespace TestBed.Zippers;

public abstract record Tree<A>
{
    public static implicit operator Tree<A>(EmptyTree _) => new Empty<A>();

    public static implicit operator Tree<A>((A value, Tree<A> left, Tree<A> right) p) =>
        new Node<A>(p.value, p.left, p.right);

    public abstract B Match<B>(Func<A, Tree<A>, Tree<A>, B> node, Func<B> empty);
}

internal sealed record Empty<A> : Tree<A>
{
    public override B Match<B>(Func<A, Tree<A>, Tree<A>, B> node, Func<B> empty) => 
        empty();
}

internal sealed record Node<A>(A Value, Tree<A> Left, Tree<A> Right) : Tree<A>
{
    public override B Match<B>(Func<A, Tree<A>, Tree<A>, B> node, Func<B> empty)
    {
        return node(Value, Left, Right);
    }
}

public sealed record EmptyTree;

public static class Tree
{
    public static EmptyTree Empty => new();
    public static Tree<A> Leaf<A>(A value) => new Node<A>(value, Empty, Empty);
    public static Tree<A> Node<A>(A value, Tree<A> left, Tree<A> right) => new Node<A>(value, left, right);
    
}

public abstract record Crumb;

public sealed record CrumbLeft<T>(T Node, Tree<T> RightTree) : Crumb;

public sealed record CrumbRight<T>(T Node, Tree<T> LeftTree) : Crumb;

public record Zipper;

public sealed record TreeZipper<A>(Tree<A> Focus, List<Crumb> Crumbs) : Zipper
{
    public static implicit operator TreeZipper<A>((Tree<A> focus, List<Crumb> crumbs) p) => new(p.focus, p.crumbs);
}
public static class TreeZipper
{
    public static TreeZipper<A> Create<A>() => new(Tree.Empty, []);
    public static TreeZipper<A> Create<A>(Tree<A> tree) => new (tree, []);
    
    private static List<Crumb> Add(List<Crumb> ls, Crumb x)
    {
        return ls.Prepend(x).ToList();
    }
    
    public static TreeZipper<A>? GoLeft<A>(TreeZipper<A> z)
    {
        return z switch
        {
            (Empty<A>, _) => null,
            (Node<A> (var x, var l, var r), var bs) => 
                new TreeZipper<A>(l, Add(bs, new CrumbLeft<A>(x, r))),
            _ => throw new InvalidOperationException()
        };
    }
    
    public static TreeZipper<A>? GoRight<A>(TreeZipper<A> z)
    {
        return z switch
        {
            (Empty<A>, _) => null,
            (Node<A> (var x, var l, var r), var bs) => 
                new TreeZipper<A>(r, Add(bs, new CrumbRight<A>(x, l))),
            _ => throw new InvalidOperationException()
        };
    }

    public static TreeZipper<A>? Modify<A>(TreeZipper<A> z, Func<A, A> f)
    {
        return z switch
        {
            (Empty<A>, _) => null,
            (Node<A> t, var bs) => new TreeZipper<A>(Tree.Node(f(t.Value), t.Left, t.Right), bs),
            _ => throw new InvalidOperationException()
        };
    }

    public static TreeZipper<A>? Topmost<A>(TreeZipper<A> z)
    {
        var g = GoUp(z);
        return g is null ? z : Topmost(g);
    }

    public static TreeZipper<A>? GoUp<A>(TreeZipper<A> z)
    {
        return z switch
        {
            (Empty<A>, _) => null,
            (Node<A>, []) => null,
            (Node<A> t, [var b, .. var bs]) =>
                b switch
                {
                    CrumbLeft<A>(var x, var r) => (Tree.Node(x, t, r), bs),
                    CrumbRight<A>(var x, var l) => (Tree.Node(x, l, t), bs),
                    _ => throw new InvalidOperationException()
                },
            _ => throw new InvalidOperationException()
        };
    } 
}