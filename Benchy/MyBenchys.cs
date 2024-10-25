using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using TestBed;
using TestBed.monads;
using TestBed.monads.other;

namespace Benchy;

[MemoryDiagnoser]
public class MyBenchys
{

    [Benchmark]
    public Option<IEnumerable<int>> TraverseA() => Enumerable.Range(1, 100).TraverseA(Option.Some);
    
    [Benchmark]
    public Option<IEnumerable<int>> TraverseM() => Enumerable.Range(1, 100).TraverseM(Option.Some);
    
    [Benchmark]
    public Option<IEnumerable<int>> TraverseM2() => Enumerable.Range(1, 100).Traverse2(Option.Some);
}

[MemoryDiagnoser]
public class WordsBenchy
{

    [Benchmark]
    public string Words1() => WordNumbers.NumberToWords(999999);
    
    [Benchmark]
    public string Words2() => WordNumbers2.NumberToWords("999999");
}

[MemoryDiagnoser]
public class AnaBenchy
{
    [Benchmark]
    public bool Ana1() => Anagrams.IsAnagramSorted("basiparachromatin", "marsipobranchiata");
    [Benchmark]
    public bool Ana2() => Anagrams.IsAnagramHashed("basiparachromatin", "marsipobranchiata");
}

[MemoryDiagnoser]
public class DatesBenchy
{
    [Benchmark]
    public void DatesWithOGStyle() => Anagrams.EnumerateDates(new DateOnly(1999, 1, 1), new DateOnly(1999, 3, 20)).Consume(new Consumer());
    [Benchmark]
    public void DatesWithFunkyStype() => Anagrams.EnumerateDates2(new DateOnly(1999, 1, 1), new DateOnly(1999, 3, 20)).Consume(new Consumer());
}

[MemoryDiagnoser]
public class ResultBenchy
{

    public static string ss = string.Join(",", Enumerable.Range(1, 100));
    
    private Result<int> Parse(string x)
    {
        return int.TryParse(x, out var val) ? Result.Success(val) : Result.Failure($"No: {val}");
    }
    
    private ResultX<int> ParseOld(string x)
    {
        return int.TryParse(x, out var val) ? ResultX.Success(val) : ResultX.Fail($"No: {val}");
    }
    
    [Benchmark]
    public void SubStyle() => ss.Split(',')
        .Traverse(Parse)
        .GetOrElse([]).Consume(new Consumer());
    
    [Benchmark]
    public void OldStyle() => ss.Split(',')
        .Traverse(ParseOld).Match(x => x, _ => []).Consume(new Consumer());
}

