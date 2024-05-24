using System.Collections.Specialized;

namespace TestBed;

public static class WordNumbers
{

    private static readonly string?[] Units = { null, "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };

    private static readonly string[] Teens =
        { "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };

    private static readonly string?[] Tens =
        { null, null, "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

    private static List<(long, string)> magnitudes = new()
    {
        (1_000_000_000_000_000_000, "quintillion"),
        (1_000_000_000_000_000, "quadrillion"),
        (1_000_000_000_000, "trillion"),
        (1_000_000_000, "billion"),
        (1_000_000, "million"),
        (1_000, "thousand"),
        (1, "")
    };


    public static string NumberToWords(long input)
    {
        if (input < 0)
            throw new ArgumentOutOfRangeException();
        if (input == 0)
            return "zero";

        var finalWords = new List<string>();
        foreach (var (mag, title) in magnitudes)
        {
            var xs = (long)Math.Floor((decimal)(input / mag));
            if (xs <= 0) continue;
            HundredsToWords(finalWords, xs);
            finalWords.Add(title);
            input -= xs * mag;
        }
        
        return string.Join(" ", finalWords);
    }

    private static void HundredsToWords(ICollection<string> words, long input)
    {
        var hundreds = (long)Math.Floor(input / 100.0);
        input -= hundreds * 100;
        var tens = (long)Math.Floor(input / 10.0);
        input -= tens * 10;
        var u = input;
        
        if (hundreds != 0)
        {
            words.Add(Units[hundreds]!);
            words.Add("hundred");
        }

        words.Add(tens == 1 ? Teens[u] : Hypenate(Tens[tens], Units[u]));
    }
    
    private static string Hypenate(string? first, string? second)
    {
        if (first is not null && second is not null)
            return $"{first}-{second}";

        if (first is not null) return first;
        return second ?? "";
    }
}

public static partial class Main
{
    public static void ExamplesWords()
    {
        Console.WriteLine($"Enter a number between 0 and {long.MaxValue}");
        for (;;)
        {
            Console.Write(":> ");
            var value = Console.ReadLine();
            if (long.TryParse(value, out var number))
            {
                var words = WordNumbers.NumberToWords(number);
                Console.WriteLine(words);
            }
            else
            {
                Console.WriteLine($"'{value}' was not a valid long");
            }
        }
    }
}