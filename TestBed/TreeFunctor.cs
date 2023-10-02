using System.Collections;

namespace TestBed;

public static class TreeFunctor
{
    
    public static class Tree
    {
        public static Tree<T> Leaf<T>(T item)
        {
            return new Tree<T>(item, new Tree<T>[0]);
        }
 
        public static Tree<T> Create<T>(T item, params Tree<T>[] children)
        {
            return new Tree<T>(item, children);
        }
    }
    public sealed class Tree<T> : IReadOnlyCollection<Tree<T>>
    {
        private readonly IReadOnlyCollection<Tree<T>> children;
 
        public T Item { get; }
 
        public Tree(T item, IReadOnlyCollection<Tree<T>> children)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (children == null)
                throw new ArgumentNullException(nameof(children));
 
            Item = item;
            this.children = children;
        }
 
        public Tree<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            var mappedItem = selector(Item);
 
            var mappedChildren = new List<Tree<TResult>>();
            foreach (var t in children)
                mappedChildren.Add(t.Select(selector));
 
            return new Tree<TResult>(mappedItem, mappedChildren);
        }
 
        public int Count
        {
            get { return children.Count; }
        }
 
        public IEnumerator<Tree<T>> GetEnumerator()
        {
            return children.GetEnumerator();
        }
 
        IEnumerator IEnumerable.GetEnumerator()
        {
            return children.GetEnumerator();
        }
 
        public override bool Equals(object obj)
        {
            if (!(obj is Tree<T> other))
                return false;
 
            return Equals(Item, other.Item)
                   && this.SequenceEqual(other);
        }
 
        public override int GetHashCode()
        {
            return Item.GetHashCode() ^ children.GetHashCode();
        }
    }
}