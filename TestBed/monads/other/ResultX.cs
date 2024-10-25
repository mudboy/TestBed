using static TestBed.monads.other.ResultX;

namespace TestBed.monads.other;

using static ResultX; 

/// <summary>
/// A result that can either be in the success state or failure state.
/// </summary>
/// <remarks>
/// <para>
/// Create a successful instance
/// <code>
/// var r = Result.Success("a value");
/// </code>
/// </para>
/// <para>
/// Create a failed instance
/// <code>
/// var r = Result.Failure("Error message");
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="A">the type of the successful value</typeparam>
public readonly struct ResultX<A> : IEquatable<ResultX<A>>, IEquatable<FailedResult>
{
    private readonly IResult _resultImpl;

    public ResultX()
    {
        _resultImpl = new Failure(new ErrorResult("Default Error"));
    }
    
    private ResultX(IResult resultImpl)
    {
        _resultImpl = resultImpl;
    }

    private interface IResult
    {
        B Match<B>(Func<A, B> onSuccess, Func<ErrorResult, B> onFailure);
        void Match(Action<A> success, Action<ErrorResult> fail);
        bool IsSuccess { get; }
    }
    
    private sealed record Success(A Value) : IResult, IEquatable<ResultX<A>>
    {
        public B Match<B>(Func<A, B> onSuccess, Func<ErrorResult, B> onFailure)
        {
            return onSuccess(Value);
        }

        public void Match(Action<A> success, Action<ErrorResult> fail)
        {
            success(Value);
        }

        public bool IsSuccess => true;
        
        public override string ToString()
        {
            return $"Success({Value})";
        }
        
        public bool Equals(Success? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value!.Equals(other.Value);
        }
        
        public bool Equals(ResultX<A> other)
        {
            return other._resultImpl is Success success && success.Equals(this);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<A>.Default.GetHashCode(Value!);
        }
    }

    private sealed record Failure(ErrorResult Error) : IResult, IEquatable<ResultX<A>>
    {
        public B Match<B>(Func<A, B> onSuccess, Func<ErrorResult, B> onFailure)
        {
            return onFailure(Error);
        }

        public void Match(Action<A> success, Action<ErrorResult> fail)
        {
            fail(Error);
        }

        public bool IsSuccess => false;
        
        public override string ToString()
        {
            return $"Failure({Error})";
        }

        public bool Equals(Failure? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Error.Equals(other.Error);
        }

        public bool Equals(ResultX<A> other)
        {
            return other._resultImpl is Failure failure && failure.Equals(this);
        }

        public override int GetHashCode()
        {
            return Error.GetHashCode();
        }
    }
    
    /// <summary>
    /// internal constructor for successful results
    /// </summary>
    /// <param name="success">the successful value</param>
    /// <returns>a new instance of <see cref="Result{A}"/></returns>
    internal static ResultX<A> Successful(A success) => new(new Success(success));
    
    /// <summary>
    /// 
    /// internal constructor for failed results
    /// </summary>
    /// <param name="errorResult">the failed error message</param>
    /// <returns>a new instance of <see cref="Result{A}"/></returns>
    internal static ResultX<A> Failed(ErrorResult errorResult) => new(new Failure(errorResult));
    
    /// <summary>
    /// Returns true if the result is successful
    /// </summary>
    public bool IsSuccess => _resultImpl.IsSuccess;
    
    /// <summary>
    /// Pattern match the <see cref="Result{A}"/>.
    /// Calls <paramref name="success"/> function when in success state and
    /// calls the <paramref name="fail"/> function when in the failure state.
    /// </summary>
    /// <param name="success">the <see cref="Func{A,B}"/> to call when in the success state</param>
    /// <param name="fail">the <see cref="Func{A,B}"/> to call when in the failure state</param>
    /// <typeparam name="B">the result type of the match</typeparam>
    /// <returns>the result of calling either function</returns>
    public B Match<B>(Func<A, B> success, Func<ErrorResult, B> fail) => _resultImpl.Match(success, fail);
    
    /// <summary>
    /// Pattern match the <see cref="Result{A}"/>. Calls the success action when in the success state
    /// and the failure action when in the failed state.
    /// </summary>
    /// <param name="success">the <see cref="Action{T}"/> to call when successful</param>
    /// <param name="fail">the <see cref="Action{T}"/> to call when failed</param>
    public void Match(Action<A> success, Action<ErrorResult> fail) => _resultImpl.Match(success, fail);


    /// <summary>
    /// Calls the given action on when in a failure state
    /// </summary>
    /// <param name="action"></param>
    public Unit OnFailure(Action<ErrorResult> action) => _resultImpl.Match(_ => Unit.Value, err =>
    {
        action.Invoke(err);
        return Unit.Value;
    });
    
    /// <summary>
    /// Maps the <see cref="Result{A}"/> to another type if in the successful state, else returns the failure state.
    /// </summary>
    /// <param name="selector">A <see cref="Func{A,B}"/> that maps the inner value</param>
    /// <typeparam name="B">the type returned by <paramref name="selector"/></typeparam>
    /// <returns>a new <see cref="Result{A}"/></returns>
    public ResultX<B> Select<B>(Func<A, B> selector) => this.SelectMany(x => Success(selector(x)));

    /// <summary>
    /// Allows functions that return <see cref="Result{A}"/>s to be composed together. <paramref name="bind"/> is only
    /// called in the successful state otherwise the failure state is returned
    /// </summary>
    /// <param name="bind">a <see cref="Func{A,B}"/> that returns a <see cref="Result{A}"/></param>
    /// <typeparam name="B">the return type of <paramref name="bind"/></typeparam>
    /// <returns>a new <see cref="Result{A}"/> </returns>
    public ResultX<B> SelectMany<B>(Func<A, ResultX<B>> bind) => _resultImpl.Match(bind, ResultX<B>.Failed);
    

    /// <summary>
    /// Special override to allow using LINQ Syntax
    /// <code>
    /// var r = from a in Result.Success(3)
    ///         from b in Result.Success(4)
    ///         select a + b;
    /// r == Result.Success(7)
    /// </code>
    /// </summary>
    /// <param name="bind">a function that takes an A returns a <see cref="Result{B}"/></param>
    /// <param name="project">a function that takes <typeparamref name="A"/> and <typeparamref name="B"/> and returns a <typeparamref name="C"/></param>
    /// <typeparam name="B">the type of the result returned by <see cref="bind"/></typeparam>
    /// <typeparam name="C">the type returned by <see cref="project"/></typeparam>
    /// <returns>a mapped <see cref="Result{C}"/></returns>
    public ResultX<C> SelectMany<B, C>(Func<A, ResultX<B>> bind, Func<A, B, C> project)
    {
        return SelectMany(x => bind(x).Select(y => project(x, y)));
    }

    /// <summary>
    /// Converts a non-generic <see cref="FailedResult"/> to a <see cref="Result{A}"/> of the right type 
    /// </summary>
    /// <param name="failure">the <see cref="FailedResult"/> to convert</param>
    /// <returns>a new <see cref="Result{A}"/></returns>
    public static implicit operator ResultX<A>(FailedResult failure) => Failed(failure.Error);
    
    /// <summary>
    /// Converts any <typeparamref name="A"/> to a successful <see cref="Result{A}"/>
    /// </summary>
    /// <param name="value">the value to wrap in a <see cref="Result{A}"/></param>
    /// <returns>a new <see cref="Result{A}"/></returns>
    public static implicit operator ResultX<A>(A value) => Successful(value);

    public override string ToString()
    {
        return _resultImpl.ToString()!;
    }

    public bool Equals(ResultX<A> other)
    {
        return _resultImpl.Equals(other._resultImpl);
    }

    public bool Equals(FailedResult other)
    {
        return _resultImpl is Failure failure && _resultImpl.Equals(failure);
    }

    public override bool Equals(object? obj)
    {
        if (obj is FailedResult failed)
            return Equals(failed);
        return obj is ResultX<A> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_resultImpl);
    }

    public static bool operator ==(ResultX<A> left, ResultX<A> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ResultX<A> left, ResultX<A> right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Helper to all allow type inference when using the <see cref="Result.Fail(string)"/> static constructor.
/// </summary>
/// <remarks>Not to be used publicly</remarks>
/// <param name="Error">the error payload</param>
public readonly record struct FailedResult(ErrorResult Error);

/// <summary>
/// Static constructors for creating <see cref="Result{A}"/> instances
/// </summary>
public static class ResultX
{
    
    /// <summary>
    /// Create a new successful <see cref="Result{A}"/> instance
    /// </summary>
    /// <param name="value">the value to wrap</param>
    /// <typeparam name="A">the type of the wrapped value</typeparam>
    /// <returns>a new <see cref="Result{A}"/></returns>
    public static ResultX<A> Success<A>(A value) => ResultX<A>.Successful(value);
    
    /// <summary>
    /// Create a new failed <see cref="Result{A}"/> instance
    /// </summary>
    /// <param name="message">the error message</param>
    /// <returns>a new <see cref="Result{A}"/></returns>
    public static FailedResult Fail(string message) => new(new ErrorResult(message));
}

/// <summary>
/// Adds support to <see cref="Result{A}"/> when wrapped in a <see cref="Task{A}"/>.
/// Allows mapping (Select) and binding (SelectMany). If calling a function that doesn't
/// return Task as part of a chain you can wrap it with Async().
/// </summary>
public static class TaskResultExtensions
{
    /// <summary>
    /// Maps <see cref="Result{A}"/> that are wrapped in <see cref="Task{TResult}"/>
    /// </summary>
    /// <param name="taskResult"></param>
    /// <param name="f"></param>
    /// <typeparam name="A"></typeparam>
    /// <typeparam name="B"></typeparam>
    /// <returns></returns>
    public static async Task<ResultX<B>> Select<A, B>(this Task<ResultX<A>> taskResult, Func<A, B> f)
    {
        return (await taskResult).Select(f);
    }
    
    /// <summary>
    /// Composes <see cref="Result{A}"/> that are wrapped in <see cref="Task{TResult}"/>
    /// </summary>
    /// <param name="result">the wrapped ResultX</param>
    /// <param name="bind">the compose function which returns a task wrapped ResultX</param>
    /// <typeparam name="A">the type of the wrapped ResultX</typeparam>
    /// <typeparam name="B">the return type of the new ResultX</typeparam>
    /// <returns>a new <see cref="Result{A}"/></returns>
    public static async Task<ResultX<B>> SelectMany<A, B>(this Task<ResultX<A>> result, Func<A, Task<ResultX<B>>> bind)
    {
        var r = await result;
        return await r.Select(bind).Match(x => x, e => Async(ResultX<B>.Failed(e)));
    }

    public static Task<ResultX<C>> SelectMany<A, B, C>(this Task<ResultX<A>> a, Func<A, Task<ResultX<B>>> bind,
        Func<A, B, C> project)
    {
        return a.SelectMany(x => bind(x).Select(y => project(x, y)));
    }
    
    /*
    public ResultX<C> SelectMany<B, C>(Func<A, ResultX<B>> bind, Func<A, B, C> project)
    {
        return SelectMany(x => bind(x).Select(y => project(x, y)));
    }
    */

    public static Task<A> Async<A>(A a) => Task.FromResult(a);

    public static ResultX<A> When<A>(this ResultX<A> bind, Func<A, bool> condition, Func<A, ResultX<A>> isTrue, Func<A, ResultX<A>> isFalse)
    {
        return bind.Match(
            s => condition(s) ? isTrue(s) : isFalse(s),
            ResultX<A>.Failed);
    }
    
    public static async Task<ResultX<B>> When<A, B>(this Task<ResultX<A>> result, Func<A, bool> condition, Func<A, Task<ResultX<B>>> isTrue, Func<A, Task<ResultX<B>>> isFalse)
    {
        return await result.SelectMany(x =>  condition(x) ? isTrue(x) : isFalse(x));
    }
    
}

public static class ResultXExtensions
{
    public delegate ResultX<T> Validator<T>(T t);

    /// <summary>
    /// Apply(call) a two parameter function with Result{T} values
    /// </summary>
    /// <param name="ra">first param wrapped in a Result{T}</param>
    /// <param name="rb">second param wrapped in a Result{T}</param>
    /// <param name="f">the function to apply(call)</param>
    /// <typeparam name="A">Type of the rest param</typeparam>
    /// <typeparam name="B">Type of the second param</typeparam>
    /// <typeparam name="C">Type of the ResultX</typeparam>
    /// <returns>Result{C}</returns>
    public static ResultX<C> Map2<A, B, C>(this ResultX<A> ra, ResultX<B> rb, Func<A, B, C> f) =>
        ra.Match( // try and get the a param
            success: a => rb.Match( // got a now try and get b
                success: b => ResultX.Success(f(a, b)), // got a & b so call the func
                fail: eb => Fail(eb.Message)), // failed to get b report failure
            fail: ea => rb.Match( // failed to get a try and get b
                success: _ => Fail(ea.Message), // got b report a's failure
                // both failed combine both error messages
                fail: eb => Fail(string.Join(", ", ea.Message, eb.Message)))); 
    
    public static Validator<T> HarvestErrors<T>(params Validator<T>[] validators) =>
        t => validators
                .TraverseA(validate => validate(t))
                .Select(_ => t);
    
    public static ResultX<IEnumerable<R>> Traverse<T, R>(this IEnumerable<T> ts, Func<T, ResultX<R>> f) 
        => ts.TraverseA(f);

    public static ResultX<IEnumerable<T>> Sequence<T>(this IEnumerable<ResultX<T>> results) => 
        results.Traverse(x => x);

    public static async Task<ResultX<T>> Sequence<T>(this ResultX<Task<T>> x)
    {
        var message = "Failed to get task contents";
        var task = x.Match(t => t,
            e =>
            {
                message = e.Message;
                return Task.FromResult<T>(default!);
            });

        var r = await task;
        return r == null ? Fail(message) : ResultX.Success(r);
    }

    private static ResultX<IEnumerable<R>> TraverseA<T, R>(this IEnumerable<T> ts, Func<T, ResultX<R>> f) =>
        ts.Aggregate(
            Success(Enumerable.Empty<R>()),
            (acc, a) =>
                acc.Map2(f(a), (dd, cc) => dd.Append(cc)));
                //f(a).Map2(acc, (b, xs) => xs.Append(b)));
                
    private static ResultX<IEnumerable<R>> TraverseM<T, R>(this IEnumerable<T> ts, Func<T, ResultX<R>> f) =>
        ts.Aggregate(Success(Enumerable.Empty<R>()),
            (acc, a) => acc.SelectMany(x => f(a).Select(x.Append)));

    /// <summary>
    /// Perform a side effect action on successful results
    /// </summary>
    /// <param name="source"></param>
    /// <param name="onSuccess"></param>
    /// <typeparam name="A"></typeparam>
    /// <returns></returns>
    public static ResultX<A> Tap<A>(this ResultX<A> source, Action<A> onSuccess)
    {
        source.Match(onSuccess, _ => { });
        return source;
    }
    
    /// <summary>
    /// Perform a side effect action on successful results
    /// </summary>
    /// <param name="source"></param>
    /// <param name="onSuccess"></param>
    /// <typeparam name="A"></typeparam>
    /// <returns></returns>
    public static async Task<ResultX<A>> Tap<A>(this Task<ResultX<A>> source, Action<A> onSuccess)
    {
        var r = await source;
        r.Match(onSuccess, _ => { });
        return r;
    }
}

public static class TaskExtensions {

    public static async Task<B> Map<A, B>(this Task<A> ta, Func<A, B> f)
    {
        var a = await ta;
        return f(a);
    }
    
    public static async Task<C> Map2<A, B, C>(this Task<A> ra, Task<B> rb, Func<A, B, C> f)
    {
        var a = await ra.ConfigureAwait(false);
        var b = await rb.ConfigureAwait(false);
        return f(a, b);
    }
    
    public static Task<IEnumerable<R>> Traverse<T, R>(this IEnumerable<T> ts, Func<T, Task<R>> f) =>
        ts.Aggregate(
            Task.FromResult(Enumerable.Empty<R>()),
            (acc, a) =>
                acc.Map2(f(a), (dd, cc) => dd.Append(cc)));
}
