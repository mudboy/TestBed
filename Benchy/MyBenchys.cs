using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Monads;
using TestBed;
using TestBed.monads;

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
