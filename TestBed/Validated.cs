namespace TestBed;


public interface ICombine<F>
{
    F Combine(F other);
}

public readonly struct ErrorResult : ICombine<ErrorResult>
{
    private readonly IEnumerable<string> _error = Enumerable.Empty<string>();

    public ErrorResult(string error)
    {
        _error = _error.Append(error);
    }

    public ErrorResult(IEnumerable<string> errors)
    {
        _error = _error.Concat(errors);
    }
    
    public ErrorResult Combine(ErrorResult other)
    {
        return new ErrorResult(_error.Concat(other._error));
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, _error);
    }
}

public sealed class Validated<F, S> where F : ICombine<F>
{
    private interface IValidation
    {
        T Match<T>(Func<F, T> onFailure, Func<S, T> onSuccess);
    }

    private readonly IValidation imp;

    private Validated(IValidation imp)
    {
        this.imp = imp;
    }

    internal static Validated<F, S> Succeed(S success)
    {
        return new Validated<F, S>(new Success(success));
    }

    internal static Validated<F, S> Fail(F failure)
    {
        return new Validated<F, S>(new Failure(failure));
    }

    public T Match<T>(Func<F, T> onFailure, Func<S, T> onSuccess)
    {
        return imp.Match(onFailure, onSuccess);
    }

    public Validated<F1, S1> SelectBoth<F1, S1>(
        Func<F, F1> selectFailure,
        Func<S, S1> selectSuccess) where F1 : ICombine<F1>
    {
        return Match(
            f => Validated.Fail<F1, S1>(selectFailure(f)),
            s => Validated.Succeed<F1, S1>(selectSuccess(s)));
    }

    public Validated<F1, S> SelectFailure<F1>(
        Func<F, F1> selectFailure) where F1 : ICombine<F1>
    {
        return SelectBoth(selectFailure, s => s);
    }

    public Validated<F, S1> SelectSuccess<S1>(
        Func<S, S1> selectSuccess)
    {
        return SelectBoth(f => f, selectSuccess);
    }

    public Validated<F, S1> Select<S1>(
        Func<S, S1> selector)
    {
        return SelectSuccess(selector);
    }

    private sealed class Success : IValidation
    {
        private readonly S success;

        public Success(S success)
        {
            this.success = success;
        }

        public T Match<T>(
            Func<F, T> onFailure,
            Func<S, T> onSuccess)
        {
            return onSuccess(success);
        }
    }

    private sealed class Failure : IValidation
    {
        private readonly F failure;

        public Failure(F failure)
        {
            this.failure = failure;
        }

        public T Match<T>(
            Func<F, T> onFailure,
            Func<S, T> onSuccess)
        {
            return onFailure(failure);
        }
    }
}

public static class Validated
{
    public static Validated<F, S> Succeed<F, S>(
        S success) where F : ICombine<F>
    {
        return Validated<F, S>.Succeed(success);
    }

    public static Validated<F, S> Fail<F, S>(
        F failure) where F : ICombine<F>
    {
        return Validated<F, S>.Fail(failure);
    }

    public static Validated<F, S> Apply<F, T, S>(
        this Validated<F, Func<T, S>> selector,
        Validated<F, T> source) where F : ICombine<F>
    {
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        return selector.Match(
            f1 => source.Match(
                f2 => Fail<F, S>(f1.Combine(f2)),
                _ => Fail<F, S>(f1)),
            map => source.Match(
                Fail<F, S>,
                x => Succeed<F, S>(map(x))));
    }

    public static Validated<F, Func<T2, S>> Apply<F, T1, T2, S>(
        this Validated<F, Func<T1, T2, S>> selector,
        Validated<F, T1> source) where F : ICombine<F>
    {
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        return selector.Match(
            f1 => source.Match(
                f2 => Fail<F, Func<T2, S>>(f1.Combine(f2)),
                _ => Fail<F, Func<T2, S>>(f1)),
            map => source.Match(
                Fail<F, Func<T2, S>>,
                x => Succeed<F, Func<T2, S>>(y => map(x, y))));
    }

    public static Validated<F, Func<T2, T3, S>> Apply<F, T1, T2, T3, S>(
        this Validated<F, Func<T1, T2, T3, S>> selector,
        Validated<F, T1> source) where F : ICombine<F>
    {
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        return selector.Match(
            f1 => source.Match(
                f2 => Fail<F, Func<T2, T3, S>>(f1.Combine(f2)),
                _ => Fail<F, Func<T2, T3, S>>(f1)),
            map => source.Match(
                Fail<F, Func<T2, T3, S>>,
                x => Succeed<F, Func<T2, T3, S>>((y, z) => map(x, y, z))));
    }

    public static Validated<F, Func<T2, S>> Apply<F, T1, T2, S>(
        this Func<T1, T2, S> map,
        Validated<F, T1> source) where F : ICombine<F>
    {
        return Apply(
            Succeed<F, Func<T1, T2, S>>(map), source);   
    }
    
    public static Validated<F, Func<T2, T3, S>> Apply<F, T1, T2, T3, S>(
        this Func<T1, T2, T3, S> map,
        Validated<F, T1> source) where F : ICombine<F>
    {
        return Apply(
            Succeed<F, Func<T1, T2, T3, S>>(map), source);
    }
}

public class DumbObject
{
    public string Name { get; }
    public int Age { get; }

    public DumbObject(string name, int age)
    {
        Name = name;
        Age = age;
    }
    
    private Validated<ErrorResult, string> TryParseName()
    {
        if (string.IsNullOrEmpty(Name))
            return Validated.Fail<ErrorResult, string>(new ErrorResult("name empty"));
        return Validated.Succeed<ErrorResult, string>(Name);
    } 
    
    private Validated<ErrorResult, int> TryParseAge()
    {
        if (Age < 18)
            return Validated.Fail<ErrorResult, int>(new ErrorResult("not old enough"));
        return Validated.Succeed<ErrorResult, int>(Age);
    }

    internal static DumbObject Create(string name, int age)
    {
        return new DumbObject(name, age);
    }
    
    internal Validated<ErrorResult, DumbObject> TryParse()
    {
        var createReservation =
            Create;
 
        return createReservation
            .Apply(TryParseName())
            .Apply(TryParseAge());
    }
}

public static partial class Main
{
    public static void ValidatedExamples()
    {
        var dmo = DumbObject.Create("", 1);

        var v = dmo.TryParse();

        var p = v.Match(e => e.ToString(), x => "");
        
        Console.WriteLine(p);
    }
}
