using System.Collections;

namespace TestBed;

public sealed class NonEmptyList<A> : IReadOnlyList<A> 
{
    private readonly A _head;
    private readonly IReadOnlyList<A> _tail;

    public NonEmptyList(A head, params A[] tail)
    {
        _head = head;
        _tail = tail ?? throw new ArgumentNullException(nameof(tail));
        Count = 1 + tail.Length;
    }
    
    public IEnumerator<A> GetEnumerator()
    {
        yield return _head;
        foreach (var a in _tail)
        {
            yield return a;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count { get; }

    public A this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return index == 0 ? _head : _tail[index - 1];
        }
    }
}

public static class NonEmptyListExt
{
    public static NonEmptyList<A> AsNonEmptyList<A>(this ICollection<A> seq)
    {
        if (seq is null)
            throw new ArgumentNullException(nameof(seq));
        if (seq.Count < 1)
            throw new ArgumentException("seq must contain at least one element");
        if (seq.Count == 1)
            return new NonEmptyList<A>(seq.First());

        var arr = seq.ToArray();
        return new NonEmptyList<A>(arr[0], arr[1..]);
    } 
    
    public static NonEmptyList<A> AsNonEmptyList<A>(this A[] seq)
    {
        if (seq is null)
            throw new ArgumentNullException(nameof(seq));
        if (seq.Length < 1)
            throw new ArgumentException("seq must contain at least one element");
        if (seq.Length == 1)
            return new NonEmptyList<A>(seq[0]);

        return new NonEmptyList<A>(seq[0], seq[1..]);
    }
}