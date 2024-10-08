namespace Benchy;

public static class Anagrams
{
    public static bool IsAnagramSorted(string s, string t)
    {
        var ssorted = s.ToArray();
        Array.Sort(ssorted);
        var tsorted = t.ToArray();
        Array.Sort(tsorted);
        return ssorted == tsorted;
    }

    public static bool IsAnagramHashed(string s, string t)
    {
        var hs = s.ToHashSet();
        var ht = t.ToHashSet();
        return hs == ht;
    }
    
    public static IEnumerable<DateOnly> EnumerateDates(DateOnly arrival, DateOnly departure)
    {
        var d = arrival;
        while (d < departure)
        {
            yield return d;
            d = d.AddDays(1);
        }
    }

    public static IEnumerable<DateOnly> EnumerateDates2(DateOnly arrival, DateOnly departure) =>
        Enumerable.Range(0, Int32.MaxValue)
            .Select(arrival.AddDays)
            .TakeWhile(d => d < departure);
}