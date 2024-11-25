namespace Utils;

public static class Helpers
{
    public static void Print<T>(this IEnumerable<T> source, string? prefix = null)
    {
        Console.WriteLine(prefix);
        var str = string.Join(", ", source);
        Console.WriteLine("[" + str + "]");
    }

    public static void Print<T>(this List<T> source, string? prefix = null) =>
        source.AsEnumerable().Print(prefix);
    
    public static void Print<T>(this T source, string? prefix = null)
    {
        Console.Write(prefix);
        Console.WriteLine(source);
    }
}