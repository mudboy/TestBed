using OneOf;
using OneOf.Types;

namespace TestBed;

public sealed class Result<T> : OneOfBase<T, string>
{
    internal Result(OneOf<T, string> input) : base(input)
    {
    }
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new(value);
    public static Result<T> Fail<T>(string error) => new(error);
}