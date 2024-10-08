using FluentAssertions;
using TestBed;
using TestBed.Zippers;
using Xunit;
using static TestBed.Zippers.TreeZipper;

namespace Testys.Zippers;

public sealed class ListZipperTests
{
    [Fact]
    public void GoBack1()
    {
        var sut = new ListZipper<int>(1, 2, 3, 4);
 
        var actual = sut.GoForward()?.GoForward()?.GoForward()?.GoBack();
 
        Assert.Equal([3, 4], actual!.AsEnumerable());
        Assert.Equal([2, 1], actual!.Breadcrumbs);
    }
    
    [Fact]
    public void InsertAtFocus()
    {
        var sut = new ListZipper<string>("foo", "bar");
 
        var actual = sut.GoForward()?.Insert("ploeh").GoBack();
 
        Assert.NotNull(actual);
        Assert.Equal(["foo", "ploeh", "bar"], actual.AsEnumerable());
        Assert.Empty(actual.Breadcrumbs);
    }
    
    [Fact]
    public void RemoveAtEnd()
    {
        var sut = new ListZipper<string>("foo", "bar").GoForward()?.GoForward();
 
        var actual = sut?.Remove();
 
        Assert.Null(actual);
        Assert.NotNull(sut);
        Assert.Empty(sut);
        Assert.Equal(["bar", "foo"], sut.Breadcrumbs);
    }
    
    [Fact]
    public void ReplaceAtFocus()
    {
        var sut = new ListZipper<string>("foo", "bar", "baz");
 
        var actual = sut.GoForward()?.Replace("qux")?.GoBack();
 
        Assert.NotNull(actual);
        Assert.Equal(["foo", "qux", "baz"], actual.AsEnumerable());
        Assert.Empty(actual.Breadcrumbs);
    }

    private readonly Tree<string> tree =
        Tree.Node("a",
            Tree.Node("b",
                Tree.Leaf("d"),
                Tree.Leaf("e")),
            Tree.Node("c",
                Tree.Leaf("f"),
                Tree.Leaf("g")));

    [Fact]
    public void TreeGoLeft()
    {
        var sut = Create(tree);

        var r = Ext.Sequence(sut,
            GoLeft,
            GoLeft);
        
        r!.GetValue().Should().Be("d");
    }
    
    [Fact]
    public void TreeGoRight()
    {
        var sut = Create(tree);

        var r = Ext.Sequence(sut,
            GoRight,
            GoRight);
        
        r!.GetValue().Should().Be("g");
    }    
    
    [Fact]
    public void TreeGoUp()
    {
        var sut = Create(tree);

        var actual = Ext.Sequence(sut, GoRight, GoLeft, GoUp, GoLeft);

        actual!.GetValue().Should().Be("f");
    }

    [Fact]
    public void TreeTopmost()
    {
        var sut = Create(tree);

        var r = Ext.Sequence(sut,
            GoLeft,
            GoLeft);
        var actual = Topmost(r);

        actual.GetValue().Should().Be("a");
    }
}

public static class Ext
{

    public static TZip<A>? Sequence<A>(TZip<A> initial, params Func<TZip<A>, TZip<A>?>[] fs)
    {
        var input = initial;
        foreach (var f in fs)
        {
            if (input is null)
                break;
            input = f(input);
        }

        return input;
    }
    public static string? GetValue(this TZip<string>? data)
    {
        return data?.focus.Match((s, _, _) => s, () => "#empty#");
    }
}