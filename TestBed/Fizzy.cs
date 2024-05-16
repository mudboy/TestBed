
namespace TestBed;

public sealed class Fizzy
{
    public static string FizzBuzz(int number)
    {
        if (number % 3 == 0 && number % 5 == 0)
            return "FizzBuzz";
        if (number % 3 == 0)
            return "Fizz";
        if (number % 5 == 0)
            return "Buzz";

        return number.ToString();
    }
}

public static partial class Main
{
    public static void GetFizzy()
    {
        var buzzys = Enumerable.Range(1, 100)
            .Select(Fizzy.FizzBuzz)
            .Aggregate(Join(Environment.NewLine));

        Console.WriteLine(buzzys);

    }

    private static Func<string, string, string> Join(string sep) => 
        (a, b) => $"{a}{sep}{b}";
}