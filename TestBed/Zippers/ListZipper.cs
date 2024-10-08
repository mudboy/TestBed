using System.Collections;

namespace TestBed.Zippers;

// taken from https://blog.ploeh.dk/2024/08/26/a-list-zipper-in-c/
public sealed class ListZipper<T> :IEnumerable<T>
{
    private readonly IEnumerable<T> values;
    public IEnumerable<T> Breadcrumbs { get; }
 
    private ListZipper(IEnumerable<T> values, IEnumerable<T> breadcrumbs)
    {
        this.values = values;
        Breadcrumbs = breadcrumbs;
    }
 
    public ListZipper(IEnumerable<T> values) : this(values, [])
    {
    }
 
    public ListZipper(params T[] values) : this(values.AsEnumerable())
    {
    }

    public ListZipper<T>? GoForward()
    {
        var head = values.Take(1);
        if (!head.Any())
            return null;

        var tail = values.Skip(1);
        return new ListZipper<T>(tail, head.Concat(Breadcrumbs));
    }

    public ListZipper<T>? GoBack()
    {
        var head = Breadcrumbs.Take(1);
        if (!head.Any())
            return null;

        var tail = Breadcrumbs.Skip(1);
        return new ListZipper<T>(head.Concat(values), tail);
    }

    public ListZipper<T> Insert(T item)
    {
        return new ListZipper<T>(values.Prepend(item), Breadcrumbs);
    }

    public ListZipper<T>? Remove()
    {
        if (!values.Any())
            return null;

        return new ListZipper<T>(values.Skip(1), Breadcrumbs);
    }

    public ListZipper<T>? Replace(T item)
    {
        return Remove()?.Insert(item);
    }

    public IEnumerator<T> GetEnumerator() => values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}