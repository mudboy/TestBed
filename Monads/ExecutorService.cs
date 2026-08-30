using System.Collections.Concurrent;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Monads;

// .NET has no java.util.concurrent, so Par.cs needs a minimal stand-in for the two
// types it is written against. Task<A> is *not* a drop-in for java.util.concurrent
// Future: a Task is already running by the time you hold one, and it has no
// `Get(timeout)` that leaves the task alive on expiry. Par leans on both, so we model
// Future directly and back the pool with real threads — which also preserves the
// chapter's point that `Fork` burns two threads per task and will deadlock a
// single-threaded pool.

/// <summary>java.util.concurrent.Future</summary>
public interface IFuture<out A>
{
    bool IsDone { get; }
    bool IsCancelled { get; }
    A Get();
    A Get(TimeSpan timeout);
    bool Cancel(bool evenIfRunning);
}

/// <summary>java.util.concurrent.ExecutorService</summary>
public abstract class ExecutorService : IDisposable
{
    public abstract IFuture<A> Submit<A>(Func<A> callable);

    public virtual void Dispose() => GC.SuppressFinalize(this);

    public static ExecutorService FixedThreadPool(int nThreads) => new FixedThreadPool(nThreads);

    /// <summary>Runs every submission on the calling thread. Deadlocks under `Fork`.</summary>
    public static ExecutorService Immediate { get; } = new ImmediateExecutor();
}

file sealed class FixedThreadPool : ExecutorService
{
    private readonly BlockingCollection<Action> queue = new();
    private readonly Thread[] threads;

    public FixedThreadPool(int nThreads)
    {
        threads = new Thread[nThreads];
        for (var i = 0; i < nThreads; i++)
        {
            threads[i] = new Thread(Worker) { IsBackground = true, Name = $"par-{i}" };
            threads[i].Start();
        }
    }

    private void Worker()
    {
        foreach (var work in queue.GetConsumingEnumerable()) work();
    }

    public override IFuture<A> Submit<A>(Func<A> callable)
    {
        var future = new PromiseFuture<A>();
        queue.Add(() => future.Complete(callable));
        return future;
    }

    public override void Dispose()
    {
        queue.CompleteAdding();
        foreach (var t in threads) t.Join(TimeSpan.FromSeconds(5));
        queue.Dispose();
        base.Dispose();
    }
}

file sealed class ImmediateExecutor : ExecutorService
{
    public override IFuture<A> Submit<A>(Func<A> callable)
    {
        var future = new PromiseFuture<A>();
        future.Complete(callable);
        return future;
    }
}

file sealed class PromiseFuture<A> : IFuture<A>
{
    private const int Pending = 0, Running = 1, Cancelled = 2;

    private readonly TaskCompletionSource<A> promise =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int state = Pending;

    public void Complete(Func<A> callable)
    {
        if (Interlocked.CompareExchange(ref state, Running, Pending) != Pending) return;
        try
        {
            promise.TrySetResult(callable());
        }
        catch (Exception e)
        {
            promise.TrySetException(e);
        }
    }

    public bool IsDone => promise.Task.IsCompleted;

    public bool IsCancelled => Volatile.Read(ref state) == Cancelled;

    // Only cancels work that has not started; `evenIfRunning` is accepted for parity
    // with the Java signature but .NET has no Thread.Interrupt equivalent worth using.
    public bool Cancel(bool evenIfRunning) =>
        Interlocked.CompareExchange(ref state, Cancelled, Pending) == Pending &&
        promise.TrySetCanceled();

    public A Get() => promise.Task.GetAwaiter().GetResult();

    public A Get(TimeSpan timeout)
    {
        if (timeout == Timeout.InfiniteTimeSpan) return Get();
        // Map2Timeouts can hand us a negative budget once the first future has eaten it.
        if (timeout < TimeSpan.Zero || !promise.Task.Wait(timeout)) throw new TimeoutException();
        return promise.Task.GetAwaiter().GetResult();
    }
}
