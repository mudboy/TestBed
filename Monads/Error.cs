using Monads.monoids;

namespace Monads;

public readonly record struct Error(string Message) : Monoid<Error>
{
    public static implicit operator Error(string message) => new(message);
    public Error Combine(Error x, Error y)
    {
        return string.Join("; ", x.Message, y.Message);
    }

    public Error Zero => new("");

    public override string ToString()
    {
        return $"Error '{Message}'";
    }
}