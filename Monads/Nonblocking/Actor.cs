// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Monads.Nonblocking;

/*
 * Translated from fpinscala's Actor.scala, which is itself taken from the scalaz
 * library with only minor changes. See:
 *
 * https://github.com/fpinscala/fpinscala/blob/second-edition/src/main/scala/fpinscala/answers/parallelism/Actor.scala
 * https://github.com/scalaz/scalaz/blob/scalaz-seven/concurrent/src/main/scala/scalaz/concurrent/Actor.scala
 *
 * That code is copyright Andriy Plokhotnyuk, Runar Bjarnason, and other contributors,
 * and is licensed under the 3-clause BSD licence:
 * https://github.com/scalaz/scalaz/blob/scalaz-seven/etc/LICENCE
 */

/// <summary>
/// Processes messages of type <typeparamref name="A"/>, one at a time. Messages are
/// submitted with <see cref="Tell"/>. Processing happens asynchronously on the supplied
/// executor.
///
/// Memory consistency guarantee: when each message is processed by the handler, any
/// memory it mutates is guaranteed visible to the handler when it processes the next
/// message, even if the executor runs the handler on different threads. This holds
/// because the actor reads a volatile location before entering its event loop and writes
/// the same location before suspending.
///
/// Implementation is the non-intrusive MPSC node-based queue described by Dmitriy Vyukov:
/// https://www.1024cores.net/home/lock-free-algorithms/queues/non-intrusive-mpsc-node-based-queue
/// </summary>
public sealed class Actor<A>
{
    private readonly ExecutorService executor;
    private readonly Action<A> handler;
    private readonly Action<Exception> onError;

    private Node tail;
    private Node head;
    private int suspended = 1;

    public Actor(ExecutorService executor, Action<A> handler, Action<Exception>? onError = null)
    {
        this.executor = executor;
        this.handler = handler;
        // Scala's default is `throw(_)`; rethrowing on a pool thread is the same thing.
        this.onError = onError ?? (e => throw e);
        tail = new Node();
        head = tail;
    }

    /// <summary>
    /// Scala spells this <c>!</c>. C# does not allow a custom binary <c>!</c>, so the
    /// message send gets a name — the same one Akka.NET uses.
    /// </summary>
    public void Tell(A a)
    {
        var n = new Node { Value = a };
        // getAndSet then lazySet: the release store is what publishes the node.
        Interlocked.Exchange(ref head, n).SetNext(n);
        TrySchedule();
    }

    public Actor<B> Contramap<B>(Func<B, A> f) => new(executor, b => Tell(f(b)), onError);

    private void TrySchedule()
    {
        if (Interlocked.CompareExchange(ref suspended, 0, 1) == 1) Schedule();
    }

    private void Schedule() => executor.Execute(Act);

    private void Act()
    {
        var t = Volatile.Read(ref tail);
        var n = BatchHandle(t, 1024);
        if (!ReferenceEquals(n, t))
        {
            n.Value = default!; // release the message for collection
            Volatile.Write(ref tail, n);
            Schedule();
        }
        else
        {
            Volatile.Write(ref suspended, 1);
            if (n.Next is not null) TrySchedule();
        }
    }

    // Scala relies on @tailrec here; C# has no tail-call guarantee, so it is a loop.
    private Node BatchHandle(Node t, int i)
    {
        while (true)
        {
            var n = t.Next;
            if (n is null) return t;
            try
            {
                handler(n.Value!);
            }
            catch (Exception ex)
            {
                onError(ex);
            }

            if (i <= 0) return n;
            t = n;
            i--;
        }
    }

    // Scala: `private class Node[A](var a: A) extends AtomicReference[Node[A]]`.
    // C# can't extend an atomic reference, so the link is a field with explicit
    // acquire/release accessors.
    private sealed class Node
    {
        public A? Value;
        private Node? next;

        public Node? Next => Volatile.Read(ref next);

        public void SetNext(Node n) => Volatile.Write(ref next, n);
    }
}
