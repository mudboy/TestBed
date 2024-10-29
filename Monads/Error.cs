namespace Monads;

public readonly record struct Error(string Message) 
{
    public override string ToString()
    {
        return $"Error '{Message}'";
    }
}