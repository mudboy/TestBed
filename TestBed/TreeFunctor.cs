using System.Collections;

namespace TestBed;

public static class TreeFunctor
{
    
    public static class Tree
    {
        public static Tree<T> Leaf<T>(T item)
        {
            return new Tree<T>(item, Array.Empty<Tree<T>>());
        }
 
        public static Tree<T> Create<T>(T item, params Tree<T>[] children)
        {
            return new Tree<T>(item, children);
        }
    }
    
    public sealed record Tree<T>(T Item, IReadOnlyCollection<Tree<T>> Children) : IReadOnlyCollection<Tree<T>>
    {
        //private readonly IReadOnlyCollection<Tree<T>> children;
 
        //public T Item { get; }
 
        /*public Tree(T item, IReadOnlyCollection<Tree<T>> children)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (children == null)
                throw new ArgumentNullException(nameof(children));
 
            Item = item;
            this.children = children;
        }*/
 
        public Tree<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            var mappedItem = selector(Item);
 
            var mappedChildren = new List<Tree<TResult>>();
            foreach (var t in Children)
                mappedChildren.Add(t.Select(selector));
 
            return new Tree<TResult>(mappedItem, mappedChildren);
        }
 
        public int Count => Children.Count;

        public IEnumerator<Tree<T>> GetEnumerator() => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Children.GetEnumerator();
    }
}