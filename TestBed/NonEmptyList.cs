using System.Collections;

namespace TestBed;

public sealed class NonEmptyList<A>(A head, params A[] tail) : IReadOnlyList<A>
{
    private readonly A[] _tail = tail ?? throw new ArgumentNullException(nameof(tail));

    public IEnumerator<A> GetEnumerator()
    {
        yield return head;
        foreach (var a in _tail)
        {
            yield return a;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count { get; } = 1 + tail.Length;

    public A this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return index == 0 ? head : _tail[index - 1];
        }
    }
}

public static class NonEmptyListExt
{
    public static NonEmptyList<A> AsNonEmptyList<A>(this ICollection<A> seq)
    {
        ArgumentNullException.ThrowIfNull(seq);
        switch (seq.Count)
        {
            case < 1:
                throw new ArgumentException("seq must contain at least one element");
            case 1:
                return new NonEmptyList<A>(seq.First());
            default:
            {
                var arr = seq.ToArray();
                return new NonEmptyList<A>(arr[0], arr[1..]);
            }
        }
    } 
    
    public static NonEmptyList<A> AsNonEmptyList<A>(this A[] seq)
    {
        ArgumentNullException.ThrowIfNull(seq);
        return seq.Length switch
        {
            < 1 => throw new ArgumentException("seq must contain at least one element"),
            1 => new NonEmptyList<A>(seq[0]),
            _ => new NonEmptyList<A>(seq[0], seq[1..])
        };
    }
}