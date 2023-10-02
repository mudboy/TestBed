namespace TestBed;

public static class Helpers
{
    public static void Print<T>(this IEnumerable<T> source, string? prefix = null)
    {
        Console.WriteLine(prefix);
        foreach (var v in source)
        {
            Console.WriteLine(v);
        }
    }
    
    // public static void Print<T>(this T source, string? prefix = null)
    // {
    //     Console.Write(prefix);
    //     Console.WriteLine(source);
    // }
}