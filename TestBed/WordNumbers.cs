using System.Text;

namespace TestBed;

public sealed class WordNumbers
{

    private static string[] units = { "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };

    private static string[] teens =
        { "", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };

    private static string[] tens =
        { "", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

    private static List<(double, string)> magnitudes = new()
    {
        (1_000_000_000.0, "billion"),
        (1_000_000.0, "million"),
        (1_000.0, "thousand"),
        (1.0, "")
    };


    public static string NumberToWords(int input)
    {
        if (input < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (input == 0)
            return "zero";

        var finalWords = new List<string>();
        foreach (var magnitude in magnitudes)
        {
            var xs = (int)Math.Floor(input / magnitude.Item1);
            if (xs > 0)
            {
                var words = HundredsToWords(xs);
                finalWords.Add(words);
                finalWords.Add(magnitude.Item2);

                input -= (int)(xs * magnitude.Item1);
            }
        }
        
        return string.Join(" ", finalWords);
    }

    private static string HundredsToWords(int input)
    {
        var words = new List<string>();
        var hasTens = false;

        // hundreds
        var hundreds = (int)Math.Floor(input / 100.0);
        if (hundreds > 0)
        {
            words.Add(units[hundreds]);
            words.Add("hundred");
            input -= hundreds * 100;
        }

        // teens
        var tens = (int)Math.Floor(input / 10.0);
        if (input >= 11 & input <= 19)
        {
            words.Add(teens[input - 10]);
            input -= input;
        }
        else // tens
        {
            if (tens > 0)
            {
                words.Add(WordNumbers.tens[tens]);
                input -= tens * 10;
                hasTens = true;
            }
        }

        // units
        if (input > 0)
        {
            if (hasTens)
            {
                var tenword = words.Last();
                words.Remove(tenword);
                words.Add(tenword + "-" + units[input]);
            }
            else
            {
                words.Add(units[input]);
            }
        }

        return string.Join(" ", words);
    }
}

public static partial class Main
{
    public static void ExamplesWords()
    {
        Console.WriteLine($"Enter a number between 0 and {int.MaxValue}");
        for (;;)
        {
            Console.Write(":> ");
            var value = Console.ReadLine();
            if (int.TryParse(value, out var number))
            {
                var words = WordNumbers.NumberToWords(number);
                Console.WriteLine(words);
            }
            else
            {
                Console.WriteLine($"'{value}' was not a valid int");
            }
        }
    }
}