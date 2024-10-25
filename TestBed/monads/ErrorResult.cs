namespace TestBed.monads;

public readonly record struct ErrorResult(string Message) 
{
    public override string ToString()
    {
        return $"Error '{Message}'";
    }
}