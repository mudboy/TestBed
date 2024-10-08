using System.Net.WebSockets;
using System.Text.RegularExpressions;

namespace TestBed;

public static class WordNumbers2
{
    private static Dictionary<char, string> units = new()
    {
        { '0', "" },
        { '1', "one" },
        { '2', "two" },
        { '3', "three" },
        { '4', "four" },
        { '5', "five" },
        { '6', "six" },
        { '7', "seven" },
        { '8', "eight" },
        { '9', "nine" },
    };

    public static Dictionary<char, string> teens = new()
    {
        { '0', "ten" },
        { '1', "eleven" },
        { '2', "twelve" },
        { '3', "thirteen" },
        { '4', "fourteen" },
        { '5', "fifteen" },
        { '6', "sixteen" },
        { '7', "seventeen" },
        { '8', "eighteen" },
        { '9', "nineteen" },
    };

    public static Dictionary<char, string?> tens = new()
    {
        { '0', null },
        { '1', null },
        { '2', "twenty" },
        { '3', "thirty" },
        { '4', "forty" },
        { '5', "fifty" },
        { '6', "sixty" },
        { '7', "seventy" },
        { '8', "eighty" },
        { '9', "ninety" },
    };
    
    private static List<string> magnitudes = new()
    {
        "billion",
        "million",
        "thousand",
        ""
    };

    public static string NumberToWords(string value)
    {
        if (value.Length < 1)
            return "zero";

        if (value.Length > 10)
            throw new ArgumentOutOfRangeException("the given string is too long");
        
        var padding = CalculatePadding(value);

        var groups = value.PadLeft(value.Length + padding, '0').Chunk(3).ToList();

        var skip = magnitudes.Count - groups.Count;

        var words = new List<string>();

        magnitudes.Skip(skip).Zip(groups).Aggregate(words, (list, tuple) =>
        {
            ProcessHundredGroup(list, tuple.Second);
            list.Add(tuple.First);
            return list;
        });
        
        return string.Join(" ", words);
    }
    
    private static void ProcessHundredGroup(List<string> words, IReadOnlyList<char> digits)
    {
        if (digits[0] != '0')
        {
            words.Add(units[digits[0]]);
            words.Add("hundred");
        }

        words.Add(digits[1] == '1' ? teens[digits[2]] : Hypenate(tens[digits[1]], units[digits[2]]));
    }

    private static int CalculatePadding(string? value)
    {
        var rem = value!.Length % 3;
        return rem == 0 ? 0 : 3 - (value.Length % 3);
    }

    private static string Hypenate(string? first, string second) => first != null ? $"{first}-{second}" : second;

    public static void DoDigitSplit()
    {
        try
        {
            Console.WriteLine($"Enter a number between 0 and {int.MaxValue}");
            for (;;)
            {
                Console.Write(":> ");
                var value = Console.ReadLine();
                var words = NumberToWords(value??"");
                Console.WriteLine(words);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed: {0}", e.Message);
        }

        
    }
}